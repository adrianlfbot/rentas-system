using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentasApi.Data;
using RentasApi.Models;

namespace RentasApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Propietario")]
public class GastosController : ControllerBase
{
    private readonly RentasContext _db;
    public GastosController(RentasContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.Gastos.Include(g => g.Departamento).ThenInclude(d => d!.Ubicacion).ToListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var g = await _db.Gastos.Include(g => g.Departamento).ThenInclude(d => d!.Ubicacion).FirstOrDefaultAsync(g => g.ID == id);
        return g == null ? NotFound() : Ok(g);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Gasto g)
    {
        try
        {
            _db.Gastos.Add(g);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = g.ID }, g);
        }
        catch (Exception ex)
        {
            var inner = ex.InnerException?.Message ?? ex.Message;
            if (inner.Contains("FOREIGN KEY"))
                return BadRequest("El departamento especificado no existe.");
            return StatusCode(500, $"Error al guardar: {inner}");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Gasto updated)
    {
        var g = await _db.Gastos.FindAsync(id);
        if (g == null) return NotFound();
        g.Fecha         = updated.Fecha;
        g.DepartamentoId = updated.DepartamentoId;
        g.Descripcion   = updated.Descripcion;
        g.ManoDeObra    = updated.ManoDeObra;
        g.Material      = updated.Material;
        try { await _db.SaveChangesAsync(); return NoContent(); }
        catch (Exception ex)
        {
            var inner = ex.InnerException?.Message ?? ex.Message;
            return StatusCode(500, $"Error al actualizar: {inner}");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var g = await _db.Gastos.FindAsync(id);
        if (g == null) return NotFound();
        _db.Gastos.Remove(g);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // === EXPORTAR CSV ===
    [HttpGet("exportar")]
    public async Task<IActionResult> Exportar()
    {
        var items = await _db.Gastos
            .Include(g => g.Departamento).ThenInclude(d => d!.Ubicacion)
            .OrderBy(g => g.Fecha)
            .ToListAsync();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("ID,Fecha,DepartamentoId,Ubicacion,Clave,Descripcion,ManoDeObra,Material,Total");
        foreach (var g in items)
        {
            var ubi = g.Departamento?.Ubicacion != null
                ? $"{g.Departamento.Ubicacion.Calle} {g.Departamento.Ubicacion.Numero}"
                : "";
            sb.AppendLine($"{g.ID},{g.Fecha:yyyy-MM-dd},{g.DepartamentoId},{Csv(ubi)},{Csv(g.Departamento?.Clave)},{Csv(g.Descripcion)},{g.ManoDeObra},{g.Material},{g.ManoDeObra + g.Material}");
        }
        var bytes = System.Text.Encoding.UTF8.GetPreamble()
            .Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", "gastos.csv");
    }

    // === IMPORTAR CSV (upsert por ID) ===
    [HttpPost("importar")]
    public async Task<IActionResult> Importar(IFormFile archivo)
    {
        if (archivo == null || archivo.Length == 0) return BadRequest("Archivo vacío.");
        int insertados = 0, actualizados = 0, errores = 0;
        var detalle = new List<string>();
        int lineaNum = 1;
        using var reader = new System.IO.StreamReader(archivo.OpenReadStream());
        await reader.ReadLineAsync(); // saltar encabezado
        while (!reader.EndOfStream)
        {
            lineaNum++;
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line);
            if (cols.Length < 8) { errores++; detalle.Add($"Línea {lineaNum}: columnas insuficientes ({cols.Length}/8)"); continue; }
            try
            {
                bool tieneId = int.TryParse(cols[0], out int id) && id > 0;
                DateTime fecha = DateTime.TryParse(cols[1], out var fd) ? fd : DateTime.UtcNow;
                int deptId = int.TryParse(cols[2], out int di) ? di : 0;
                string? desc = string.IsNullOrEmpty(cols[5]) ? null : cols[5];
                decimal mdo = decimal.TryParse(cols[6], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal m1) ? m1 : 0;
                decimal mat = decimal.TryParse(cols[7], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal m2) ? m2 : 0;

                if (deptId == 0) { errores++; detalle.Add($"Línea {lineaNum}: DepartamentoId inválido"); continue; }

                if (tieneId)
                {
                    var existing = await _db.Gastos.FindAsync(id);
                    if (existing == null) { errores++; detalle.Add($"Línea {lineaNum}: ID {id} no encontrado"); continue; }
                    existing.Fecha = fecha; existing.DepartamentoId = deptId;
                    existing.Descripcion = desc; existing.ManoDeObra = mdo; existing.Material = mat;
                    actualizados++;
                }
                else
                {
                    _db.Gastos.Add(new Gasto { Fecha = fecha, DepartamentoId = deptId, Descripcion = desc, ManoDeObra = mdo, Material = mat });
                    insertados++;
                }
            }
            catch (Exception ex) { errores++; detalle.Add($"Línea {lineaNum}: {ex.Message}"); }
        }
        await _db.SaveChangesAsync();
        return Ok(new { insertados, actualizados, errores, detalle });
    }

    private static string Csv(string? v) => v == null ? "" : v.Contains(',') ? $"\"{v}\"" : v;
    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
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
