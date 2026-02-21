using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentasApi.Data;
using RentasApi.Models;

namespace RentasApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Propietario")]
public class ContratoInternetController : ControllerBase
{
    private readonly RentasContext _db;
    public ContratoInternetController(RentasContext db) => _db = db;

    [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _db.ContratoInternet.ToListAsync());
    [HttpGet("{id}")] public async Task<IActionResult> Get(int id) { var e = await _db.ContratoInternet.FindAsync(id); return e == null ? NotFound() : Ok(e); }
    [HttpPost] public async Task<IActionResult> Create([FromBody] ContratoInternet e) { _db.ContratoInternet.Add(e); await _db.SaveChangesAsync(); return CreatedAtAction(nameof(Get), new { id = e.ID }, e); }
    [HttpPut("{id}")] public async Task<IActionResult> Update(int id, [FromBody] ContratoInternet dto) { var e = await _db.ContratoInternet.FindAsync(id); if (e == null) return NotFound(); e.NumeroContrato = dto.NumeroContrato; e.Nombre = dto.Nombre; e.NumeroPagoOXXO = dto.NumeroPagoOXXO; e.FechaVencimiento = dto.FechaVencimiento; e.PeriodoEmision = dto.PeriodoEmision; await _db.SaveChangesAsync(); return NoContent(); }
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id) { var e = await _db.ContratoInternet.FindAsync(id); if (e == null) return NotFound(); _db.ContratoInternet.Remove(e); await _db.SaveChangesAsync(); return NoContent(); }
}
