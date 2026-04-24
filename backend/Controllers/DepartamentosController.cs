using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentasApi.Data;
using RentasApi.Models;
using System.Security.Claims;

namespace RentasApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Propietario")]
public class DepartamentosController : ControllerBase
{
    private readonly RentasContext _db;
    public DepartamentosController(RentasContext db) => _db = db;

    private string GetCorreo() => User.FindFirst("correo")?.Value ?? "";

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.Departamentos
            .Include(d => d.Ubicacion)
            .Include(d => d.ContratoLuz)
            .Include(d => d.Inquilino)
            .ToListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var d = await _db.Departamentos
            .Include(d => d.Ubicacion)
            .Include(d => d.ContratoLuz)
            .Include(d => d.Inquilino)
            .FirstOrDefaultAsync(d => d.ID == id);
        if (d == null) return NotFound();
        return Ok(d);
    }

    // === NOTAS ===
    [HttpGet("{id}/notas")]
    public async Task<IActionResult> GetNotas(int id) =>
        Ok(await _db.NotasDepartamento.Where(n => n.DepartamentoId == id).OrderByDescending(n => n.Fecha).ToListAsync());

    [HttpPost("{id}/notas")]
    public async Task<IActionResult> AddNota(int id, [FromBody] NotaDepartamento nota)
    {
        nota.DepartamentoId = id;
        nota.Fecha = DateTime.UtcNow;
        nota.UsuarioCreo = GetCorreo();
        _db.NotasDepartamento.Add(nota);
        await _db.SaveChangesAsync();
        return Ok(nota);
    }

    [HttpDelete("notas/{notaId}")]
    public async Task<IActionResult> DeleteNota(int notaId)
    {
        var n = await _db.NotasDepartamento.FindAsync(notaId);
        if (n == null) return NotFound();
        _db.NotasDepartamento.Remove(n);
        await _db.SaveChangesAsync();
        return NoContent();
    }
    // =============

    [HttpGet("{id}/historial")]
    public async Task<IActionResult> GetHistorial(int id) =>
        Ok(await _db.HistorialInquilinos.Where(h => h.DepartamentoId == id).OrderByDescending(h => h.FechaInicio).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Departamento depto)
    {
        // Validar que el inquilino exista si se especificó
        if (!string.IsNullOrEmpty(depto.InquilinoCorreo))
        {
            var inquilino = await _db.Usuarios.FindAsync(depto.InquilinoCorreo);
            if (inquilino == null)
                return BadRequest(new { message = $"El inquilino '{depto.InquilinoCorreo}' no está registrado. Primero debes darlo de alta en Usuarios." });
        }

        _db.Departamentos.Add(depto);
        await _db.SaveChangesAsync();

        if (!string.IsNullOrEmpty(depto.InquilinoCorreo))
        {
            _db.HistorialInquilinos.Add(new HistorialInquilino
            {
                DepartamentoId = depto.ID,
                CorreoInquilino = depto.InquilinoCorreo,
                FechaInicio = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        return CreatedAtAction(nameof(Get), new { id = depto.ID }, depto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Departamento updated)
    {
        var d = await _db.Departamentos.FindAsync(id);
        if (d == null) return NotFound();

        // Validar que el inquilino exista si se especificó
        if (!string.IsNullOrEmpty(updated.InquilinoCorreo))
        {
            var inquilino = await _db.Usuarios.FindAsync(updated.InquilinoCorreo);
            if (inquilino == null)
                return BadRequest(new { message = $"El inquilino '{updated.InquilinoCorreo}' no está registrado. Primero debes darlo de alta en Usuarios." });
        }

        if (d.InquilinoCorreo != updated.InquilinoCorreo)
        {
            if (!string.IsNullOrEmpty(d.InquilinoCorreo))
            {
                var hist = await _db.HistorialInquilinos
                    .Where(h => h.DepartamentoId == id && h.CorreoInquilino == d.InquilinoCorreo && h.FechaFin == null)
                    .FirstOrDefaultAsync();
                if (hist != null) hist.FechaFin = DateTime.UtcNow;
            }
            if (!string.IsNullOrEmpty(updated.InquilinoCorreo))
            {
                _db.HistorialInquilinos.Add(new HistorialInquilino
                {
                    DepartamentoId = id,
                    CorreoInquilino = updated.InquilinoCorreo,
                    FechaInicio = DateTime.UtcNow
                });
            }
        }

        d.IDUbicacion = updated.IDUbicacion;
        d.Clave = updated.Clave;
        d.Descripcion = updated.Descripcion;
        d.Cuartos = updated.Cuartos;
        d.Banos = updated.Banos;
        d.Estacionamiento = updated.Estacionamiento;
        d.Extras = updated.Extras;
        d.MontoRenta = updated.MontoRenta;
        d.CuotaAgua = updated.CuotaAgua;
        d.ContratoLuzId = updated.ContratoLuzId;
        d.DiaVencimiento = updated.DiaVencimiento;
        d.DescripcionPublicacion = updated.DescripcionPublicacion;
        d.InquilinoCorreo = updated.InquilinoCorreo;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var d = await _db.Departamentos.FindAsync(id);
        if (d == null) return NotFound();
        _db.Departamentos.Remove(d);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // === EXPORTAR CSV ===
    [HttpGet("exportar")]
    public async Task<IActionResult> Exportar()
    {
        var items = await _db.Departamentos
            .Include(d => d.Ubicacion)
            .Include(d => d.ContratoLuz)
            .ToListAsync();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("ID,IDUbicacion,Ubicacion,Clave,Descripcion,Cuartos,Banos,Estacionamiento,Extras,MontoRenta,CuotaAgua,ContratoLuzId,RPUContrato,DiaVencimiento,InquilinoCorreo");
        foreach (var d in items)
        {
            var ubi = d.Ubicacion != null ? $"{d.Ubicacion.Calle} {d.Ubicacion.Numero}" : "";
            sb.AppendLine($"{d.ID},{d.IDUbicacion},{Csv(ubi)},{Csv(d.Clave)},{Csv(d.Descripcion)},{d.Cuartos},{d.Banos},{d.Estacionamiento},{Csv(d.Extras)},{d.MontoRenta},{d.CuotaAgua},{d.ContratoLuzId},{Csv(d.ContratoLuz?.RPU)},{d.DiaVencimiento},{Csv(d.InquilinoCorreo)}");
        }
        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", "departamentos.csv");
    }

    // === IMPORTAR CSV (upsert por ID) ===
    [HttpPost("importar")]
    public async Task<IActionResult> Importar(IFormFile archivo)
    {
        if (archivo == null || archivo.Length == 0) return BadRequest("Archivo vacío.");
        int insertados = 0, actualizados = 0, errores = 0;
        var detalle = new System.Collections.Generic.List<string>();
        int lineaNum = 1;
        using var reader = new System.IO.StreamReader(archivo.OpenReadStream());
        var header = await reader.ReadLineAsync(); // saltar encabezado
        while (!reader.EndOfStream)
        {
            lineaNum++;
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line);
            if (cols.Length < 15)
            {
                errores++;
                detalle.Add($"Línea {lineaNum}: solo {cols.Length} columnas (se esperan 15). Verifica el formato del CSV.");
                continue;
            }
            try
            {
                bool tieneId = int.TryParse(cols[0], out int id) && id > 0;
                if (tieneId)
                {
                    var existing = await _db.Departamentos.FindAsync(id);
                    if (existing == null) { errores++; detalle.Add($"Línea {lineaNum}: ID {id} no encontrado."); continue; }
                    existing.IDUbicacion     = int.TryParse(cols[1], out int u) ? u : existing.IDUbicacion;
                    existing.Clave           = cols[3];
                    existing.Descripcion     = cols[4];
                    existing.Cuartos         = int.TryParse(cols[5], out int c) ? c : existing.Cuartos;
                    existing.Banos           = int.TryParse(cols[6], out int b) ? b : existing.Banos;
                    existing.Estacionamiento = int.TryParse(cols[7], out int e) ? e : existing.Estacionamiento;
                    existing.Extras          = cols[8];
                    existing.MontoRenta      = double.TryParse(cols[9], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double mr) ? mr : existing.MontoRenta;
                    existing.CuotaAgua       = double.TryParse(cols[10], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double ca) ? ca : existing.CuotaAgua;
                    existing.ContratoLuzId   = int.TryParse(cols[11], out int cl) && cl > 0 ? cl : (int?)null;
                    existing.DiaVencimiento  = int.TryParse(cols[13], out int dv) ? dv : existing.DiaVencimiento;
                    existing.InquilinoCorreo = string.IsNullOrEmpty(cols[14]) ? null : cols[14];
                    actualizados++;
                }
                else
                {
                    if (!int.TryParse(cols[1], out int idUbi) || idUbi == 0)
                    { errores++; detalle.Add($"Línea {lineaNum}: IDUbicacion inválido '{cols[1]}'"); continue; }
                    _db.Departamentos.Add(new Departamento
                    {
                        IDUbicacion      = idUbi,
                        Clave            = cols[3],
                        Descripcion      = cols[4],
                        Cuartos          = int.TryParse(cols[5], out int c) ? c : 0,
                        Banos            = int.TryParse(cols[6], out int b) ? b : 0,
                        Estacionamiento  = int.TryParse(cols[7], out int es) ? es : 0,
                        Extras           = cols[8],
                        MontoRenta       = double.TryParse(cols[9], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double mr) ? mr : 0,
                        CuotaAgua        = double.TryParse(cols[10], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double ca) ? ca : 0,
                        ContratoLuzId    = int.TryParse(cols[11], out int cl) && cl > 0 ? cl : (int?)null,
                        DiaVencimiento   = int.TryParse(cols[13], out int dv) ? dv : 1,
                        InquilinoCorreo  = string.IsNullOrEmpty(cols[14]) ? null : cols[14]
                    });
                    insertados++;
                }
            }
            catch (Exception ex)
            {
                errores++;
                detalle.Add($"Línea {lineaNum}: {ex.Message}");
            }
        }
        await _db.SaveChangesAsync();
        return Ok(new { insertados, actualizados, errores, detalle });
    }

    private static string Csv(string? v) => v == null ? "" : v.Contains(',') ? $"\"{v}\"" : v;
    private static string[] ParseCsvLine(string line)
    {
        var result = new System.Collections.Generic.List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();
        foreach (var ch in line)
        {
            if (ch == '"') { inQuotes = !inQuotes; }
            else if (ch == ',' && !inQuotes) { result.Add(current.ToString().Trim()); current.Clear(); }
            else { current.Append(ch); }
        }
        result.Add(current.ToString().Trim());
        return result.ToArray();
    }
}
