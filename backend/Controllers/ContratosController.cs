using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentasApi.Data;
using RentasApi.Models;

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
