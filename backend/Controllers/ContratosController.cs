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
