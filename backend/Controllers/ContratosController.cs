using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentasApi.Data;
using RentasApi.Models;
using System.Xml.Linq;

namespace RentasApi.Controllers;

// === CONTRATO LUZ ===
[ApiController]
[Route("api/contratos/luz")]
[Authorize(Roles = "Propietario")]
public class ContratoLuzController : ControllerBase
{
    private readonly RentasContext _db;
    public ContratoLuzController(RentasContext db) => _db = db;

    [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _db.ContratoLuz.ToListAsync());
    [HttpGet("{id}")] public async Task<IActionResult> Get(int id) { var c = await _db.ContratoLuz.FindAsync(id); return c == null ? NotFound() : Ok(c); }

    // Contratos disponibles: sin departamento asignado, o el que ya tiene el depto indicado
    [HttpGet("disponibles")]
    public async Task<IActionResult> Disponibles([FromQuery] int? departamentoId)
    {
        // IDs de contratos ya asignados a algún departamento
        var asignados = await _db.Departamentos
            .Where(d => d.ContratoLuzId != null)
            .Select(d => d.ContratoLuzId!.Value)
            .ToListAsync();

        // Si estamos editando un depto, excluimos su propio contrato de la lista de "asignados"
        if (departamentoId.HasValue)
        {
            var propioContrato = await _db.Departamentos
                .Where(d => d.ID == departamentoId.Value)
                .Select(d => d.ContratoLuzId)
                .FirstOrDefaultAsync();
            if (propioContrato.HasValue)
                asignados.Remove(propioContrato.Value);
        }

        var disponibles = await _db.ContratoLuz
            .Where(c => !asignados.Contains(c.ID))
            .ToListAsync();

        return Ok(disponibles);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ContratoLuz c) { _db.ContratoLuz.Add(c); await _db.SaveChangesAsync(); return CreatedAtAction(nameof(Get), new { id = c.ID }, c); }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ContratoLuz updated)
    {
        var c = await _db.ContratoLuz.FindAsync(id); if (c == null) return NotFound();
        c.RPU = updated.RPU; c.Nombre = updated.Nombre; c.Email = updated.Email; c.NumeroMedidor = updated.NumeroMedidor;
        c.FechaVencimiento = updated.FechaVencimiento; c.PeriodoEmision = updated.PeriodoEmision;
        await _db.SaveChangesAsync(); return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.ContratoLuz.FindAsync(id);
        if (c == null) return NotFound();
        // Verificar si hay ubicaciones o departamentos usando este contrato
        var ubicacion = await _db.Ubicaciones.FirstOrDefaultAsync(u => u.ContratoLuzId == id);
        if (ubicacion != null)
            return Conflict($"No se puede eliminar: está asignado a la ubicación '{ubicacion.Calle} {ubicacion.Numero}'. Primero desasígnalo desde Ubicaciones.");
        var depto = await _db.Departamentos.FirstOrDefaultAsync(d => d.ContratoLuzId == id);
        if (depto != null)
            return Conflict($"No se puede eliminar: está asignado al departamento '{depto.Clave}'. Primero desasígnalo desde Departamentos.");
        try { _db.ContratoLuz.Remove(c); await _db.SaveChangesAsync(); return NoContent(); }
        catch (Exception ex) { return StatusCode(500, "Error al eliminar: " + ex.Message); }
    }

    // === EXPORTAR CSV ===
    [HttpGet("exportar")]
    public async Task<IActionResult> Exportar()
    {
        var items = await _db.ContratoLuz.ToListAsync();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("ID,RPU,Nombre,Email,NumeroMedidor,FechaVencimiento,PeriodoEmision");
        foreach (var c in items)
            sb.AppendLine($"{c.ID},{Csv(c.RPU)},{Csv(c.Nombre)},{Csv(c.Email)},{Csv(c.NumeroMedidor)},{c.FechaVencimiento?.ToString("yyyy-MM-dd")},{Csv(c.PeriodoEmision)}");
        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", "contratos_luz.csv");
    }

    // === IMPORTAR CSV (upsert) ===
    [HttpPost("importar")]
    public async Task<IActionResult> Importar(IFormFile archivo)
    {
        if (archivo == null || archivo.Length == 0) return BadRequest("Archivo vacío.");
        int insertados = 0, actualizados = 0, errores = 0;
        using var reader = new System.IO.StreamReader(archivo.OpenReadStream());
        var header = await reader.ReadLineAsync(); // saltar encabezado
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line);
            if (cols.Length < 7) { errores++; continue; }
            try
            {
                bool tieneId = int.TryParse(cols[0], out int id) && id > 0;
                if (tieneId)
                {
                    var existing = await _db.ContratoLuz.FindAsync(id);
                    if (existing != null)
                    {
                        existing.RPU = cols[1]; existing.Nombre = cols[2]; existing.Email = cols[3];
                        existing.NumeroMedidor = cols[4];
                        existing.FechaVencimiento = string.IsNullOrEmpty(cols[5]) ? null : DateTime.Parse(cols[5]);
                        existing.PeriodoEmision = cols[6];
                        actualizados++;
                    }
                    else { errores++; }
                }
                else
                {
                    var nuevo = new ContratoLuz
                    {
                        RPU = cols[1], Nombre = cols[2], Email = cols[3], NumeroMedidor = cols[4],
                        FechaVencimiento = string.IsNullOrEmpty(cols[5]) ? null : DateTime.Parse(cols[5]),
                        PeriodoEmision = cols[6]
                    };
                    _db.ContratoLuz.Add(nuevo);
                    insertados++;
                }
            }
            catch { errores++; }
        }
        await _db.SaveChangesAsync();
        return Ok(new { insertados, actualizados, errores });
    }

    // === IMPORTAR CONTRATOS DESDE XML (CFDi CFE) ===
    // Crea un nuevo ContratoLuz por cada XML. Si ya existe un contrato con el mismo RPU, lo actualiza.
    [HttpPost("importar-xml")]
    public async Task<IActionResult> ImportarXml(List<IFormFile> archivos)
    {
        if (archivos == null || archivos.Count == 0) return BadRequest("Sin archivos.");

        XNamespace cfdi = "http://www.sat.gob.mx/cfd/4";
        XNamespace cfe  = "http://www.itcomplements.com/cfd/cfe/v1";

        int insertados = 0, actualizados = 0, errores = 0;
        var detalle = new List<string>();

        foreach (var archivo in archivos)
        {
            try
            {
                XDocument doc;
                using (var stream = archivo.OpenReadStream())
                    doc = XDocument.Load(stream);

                var comprobante = doc.Root!;
                var regArch = comprobante.Descendants("clsRegArchFact").FirstOrDefault();

                // === RPU ===
                string? rpu = regArch?.Element("RPU")?.Value;
                if (string.IsNullOrEmpty(rpu))
                    rpu = comprobante.Descendants(cfe + "ComisionFederalElectricidad").FirstOrDefault()?.Attribute("RPU")?.Value;
                if (string.IsNullOrEmpty(rpu)) { errores++; detalle.Add($"{archivo.FileName}: RPU no encontrado"); continue; }

                // === NOMBRE (titular del contrato) ===
                string nombre = regArch?.Element("NOMBRE")?.Value?.Trim() ?? "";
                if (string.IsNullOrEmpty(nombre))
                    nombre = comprobante.Descendants(cfdi + "Receptor").FirstOrDefault()?.Attribute("Nombre")?.Value ?? rpu;

                // === NUMERO DE MEDIDOR ===
                string? numMedidor = regArch?.Element("NUMMED1")?.Value?.Trim();
                if (numMedidor == "NUMMED1" || string.IsNullOrWhiteSpace(numMedidor)) numMedidor = null;

                // === FECHA VENCIMIENTO (fecha límite de pago del recibo) ===
                DateTime? fechaVencimiento = null;
                var fecLimite = regArch?.Element("FECLIMITE")?.Value; // ej: "27 MAR 26"
                if (!string.IsNullOrEmpty(fecLimite))
                {
                    var meses = new[] { "ENE","FEB","MAR","ABR","MAY","JUN","JUL","AGO","SEP","OCT","NOV","DIC" };
                    var partes = fecLimite.Split(' ');
                    if (partes.Length == 3)
                    {
                        int dia = int.Parse(partes[0]);
                        int mes = Array.IndexOf(meses, partes[1]) + 1;
                        int anio = 2000 + int.Parse(partes[2]);
                        if (mes > 0) fechaVencimiento = new DateTime(anio, mes, dia);
                    }
                }

                // === PERIODO DE EMISION (CFE es siempre Bimestral) ===
                string periodoEmision = "Bimestral";

                // Upsert: si ya existe contrato con este RPU se actualiza, si no se crea
                var contrato = await _db.ContratoLuz.FirstOrDefaultAsync(c => c.RPU == rpu);
                if (contrato != null)
                {
                    contrato.Nombre = nombre;
                    if (numMedidor != null) contrato.NumeroMedidor = numMedidor;
                    if (fechaVencimiento.HasValue) contrato.FechaVencimiento = fechaVencimiento;
                    contrato.PeriodoEmision = periodoEmision;
                    actualizados++;
                    detalle.Add($"{archivo.FileName}: actualizado — RPU {rpu}, {nombre}");
                }
                else
                {
                    _db.ContratoLuz.Add(new ContratoLuz
                    {
                        RPU            = rpu,
                        Nombre         = nombre,
                        NumeroMedidor  = numMedidor,
                        FechaVencimiento = fechaVencimiento,
                        PeriodoEmision = periodoEmision
                    });
                    insertados++;
                    detalle.Add($"{archivo.FileName}: creado — RPU {rpu}, {nombre}");
                }
            }
            catch (Exception ex)
            {
                errores++;
                detalle.Add($"{archivo.FileName}: Error — {ex.Message}");
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { insertados, actualizados, errores, detalle });
    }

    private static string Csv(string? v) => v == null ? "" : v.Contains(',') ? $"\"{v}\"" : v;
    private static string[] ParseCsvLine(string line)
    {
        var result = new System.Collections.Generic.List<string>();
        bool inQuotes = false; var current = new System.Text.StringBuilder();
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

// === CONTRATO AGUA ===
[ApiController]
[Route("api/contratos/agua")]
[Authorize(Roles = "Propietario")]
public class ContratoAguaController : ControllerBase
{
    private readonly RentasContext _db;
    public ContratoAguaController(RentasContext db) => _db = db;

    [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _db.ContratoAgua.ToListAsync());
    [HttpGet("{id}")] public async Task<IActionResult> Get(int id) { var c = await _db.ContratoAgua.FindAsync(id); return c == null ? NotFound() : Ok(c); }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ContratoAgua c) { _db.ContratoAgua.Add(c); await _db.SaveChangesAsync(); return CreatedAtAction(nameof(Get), new { id = c.ID }, c); }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ContratoAgua updated)
    {
        var c = await _db.ContratoAgua.FindAsync(id); if (c == null) return NotFound();
        c.NumeroInmueble = updated.NumeroInmueble; c.Nombre = updated.Nombre; c.NumeroContrato = updated.NumeroContrato;
        c.FechaVencimiento = updated.FechaVencimiento; c.PeriodoEmision = updated.PeriodoEmision;
        await _db.SaveChangesAsync(); return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.ContratoAgua.FindAsync(id);
        if (c == null) return NotFound();
        try { _db.ContratoAgua.Remove(c); await _db.SaveChangesAsync(); return NoContent(); }
        catch (Exception ex) when (ex.InnerException?.Message.Contains("FOREIGN KEY") == true || ex.Message.Contains("FOREIGN KEY"))
        { return Conflict("No se puede eliminar el contrato porque está asignado a una ubicación. Primero desasígnalo."); }
        catch (Exception ex) { return StatusCode(500, "Error al eliminar: " + ex.Message); }
    }
}

// === CONTRATO INTERNET ===
[ApiController]
[Route("api/contratos/internet")]
[Authorize(Roles = "Propietario")]
public class ContratoInternetController : ControllerBase
{
    private readonly RentasContext _db;
    public ContratoInternetController(RentasContext db) => _db = db;

    [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _db.ContratoInternet.ToListAsync());
    [HttpGet("{id}")] public async Task<IActionResult> Get(int id) { var c = await _db.ContratoInternet.FindAsync(id); return c == null ? NotFound() : Ok(c); }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ContratoInternet c) { _db.ContratoInternet.Add(c); await _db.SaveChangesAsync(); return CreatedAtAction(nameof(Get), new { id = c.ID }, c); }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ContratoInternet updated)
    {
        var c = await _db.ContratoInternet.FindAsync(id); if (c == null) return NotFound();
        c.NumeroContrato = updated.NumeroContrato; c.Nombre = updated.Nombre; c.NumeroPagoOXXO = updated.NumeroPagoOXXO;
        c.FechaVencimiento = updated.FechaVencimiento; c.PeriodoEmision = updated.PeriodoEmision;
        await _db.SaveChangesAsync(); return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.ContratoInternet.FindAsync(id);
        if (c == null) return NotFound();
        try { _db.ContratoInternet.Remove(c); await _db.SaveChangesAsync(); return NoContent(); }
        catch (Exception ex) when (ex.InnerException?.Message.Contains("FOREIGN KEY") == true || ex.Message.Contains("FOREIGN KEY"))
        { return Conflict("No se puede eliminar el contrato porque está asignado a una ubicación. Primero desasígnalo."); }
        catch (Exception ex) { return StatusCode(500, "Error al eliminar: " + ex.Message); }
    }
}
