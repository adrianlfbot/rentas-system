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

    [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id) { var c = await _db.ContratoLuz.FindAsync(id); if (c == null) return NotFound(); _db.ContratoLuz.Remove(c); await _db.SaveChangesAsync(); return NoContent(); }

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

    // === IMPORTAR RECIBOS XML (CFDi CFE) ===
    [HttpPost("importar-xml")]
    public async Task<IActionResult> ImportarXml(List<IFormFile> archivos)
    {
        if (archivos == null || archivos.Count == 0) return BadRequest("Sin archivos.");

        XNamespace cfdi = "http://www.sat.gob.mx/cfd/4";
        XNamespace cfe  = "http://www.itcomplements.com/cfd/cfe/v1";

        int insertados = 0, omitidos = 0, errores = 0;
        var detalle = new List<string>();

        foreach (var archivo in archivos)
        {
            try
            {
                XDocument doc;
                using (var stream = archivo.OpenReadStream())
                    doc = XDocument.Load(stream);

                var comprobante = doc.Root!;

                // Extraer Total y Fecha del comprobante
                decimal monto = decimal.Parse(comprobante.Attribute("Total")?.Value ?? "0",
                    System.Globalization.CultureInfo.InvariantCulture);
                DateTime fechaRegistro = DateTime.Parse(comprobante.Attribute("Fecha")?.Value ?? DateTime.UtcNow.ToString());

                // Buscar RPU en la Addenda CFE
                var cfeCFE = comprobante.Descendants(cfe + "ComisionFederalElectricidad").FirstOrDefault();
                string? rpu = cfeCFE?.Attribute("RPU")?.Value;
                if (string.IsNullOrEmpty(rpu)) { errores++; detalle.Add($"{archivo.FileName}: RPU no encontrado"); continue; }

                // Extraer periodo (año y mes)
                var cfeCFEParent = comprobante.Descendants(cfe + "CFE").FirstOrDefault();
                string? ano = cfeCFEParent?.Attribute("ano")?.Value;
                string? mes = cfeCFEParent?.Attribute("mes")?.Value;
                string periodo = (ano != null && mes != null) ? $"{ano}-{mes.PadLeft(2,'0')}" : fechaRegistro.ToString("yyyy-MM");

                // Extraer KWh del concepto de Energía
                decimal? kwh = null;
                var concepto = comprobante.Descendants(cfdi + "Concepto")
                    .FirstOrDefault(c => c.Attribute("Descripcion")?.Value?.Contains("Energ") == true);
                if (concepto != null && decimal.TryParse(concepto.Attribute("ValorUnitario")?.Value,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal kwhVal))
                {
                    kwh = kwhVal > 0 ? kwhVal : (decimal?)null;
                }

                // Buscar contrato por RPU
                var contrato = await _db.ContratoLuz.FirstOrDefaultAsync(c => c.RPU == rpu);
                if (contrato == null) { omitidos++; detalle.Add($"{archivo.FileName}: RPU {rpu} no tiene contrato registrado"); continue; }

                // Verificar duplicado por contrato + periodo
                bool existe = await _db.ConsumoLuz.AnyAsync(c => c.ContratoLuzId == contrato.ID && c.Periodo == periodo);
                if (existe) { omitidos++; detalle.Add($"{archivo.FileName}: ya existe registro para RPU {rpu} periodo {periodo}"); continue; }

                _db.ConsumoLuz.Add(new ConsumoLuz
                {
                    ContratoLuzId = contrato.ID,
                    Periodo       = periodo,
                    KWh           = kwh,
                    Monto         = monto,
                    FechaRegistro = fechaRegistro
                });
                insertados++;
                detalle.Add($"{archivo.FileName}: OK — RPU {rpu}, periodo {periodo}, ${monto}");
            }
            catch (Exception ex)
            {
                errores++;
                detalle.Add($"{archivo.FileName}: Error — {ex.Message}");
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { insertados, omitidos, errores, detalle });
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

    [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id) { var c = await _db.ContratoAgua.FindAsync(id); if (c == null) return NotFound(); _db.ContratoAgua.Remove(c); await _db.SaveChangesAsync(); return NoContent(); }
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

    [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id) { var c = await _db.ContratoInternet.FindAsync(id); if (c == null) return NotFound(); _db.ContratoInternet.Remove(c); await _db.SaveChangesAsync(); return NoContent(); }
}
