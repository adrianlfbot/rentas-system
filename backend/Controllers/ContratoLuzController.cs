using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentasApi.Data;
using RentasApi.Models;

namespace RentasApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Propietario")]
public class ContratoLuzController : ControllerBase
{
    private readonly RentasContext _db;
    public ContratoLuzController(RentasContext db) => _db = db;

    [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _db.ContratoLuz.ToListAsync());
    [HttpGet("{id}")] public async Task<IActionResult> Get(int id) { var e = await _db.ContratoLuz.FindAsync(id); return e == null ? NotFound() : Ok(e); }
    [HttpPost] public async Task<IActionResult> Create([FromBody] ContratoLuz e) { _db.ContratoLuz.Add(e); await _db.SaveChangesAsync(); return CreatedAtAction(nameof(Get), new { id = e.ID }, e); }
    [HttpPut("{id}")] public async Task<IActionResult> Update(int id, [FromBody] ContratoLuz dto) { var e = await _db.ContratoLuz.FindAsync(id); if (e == null) return NotFound(); e.RPU = dto.RPU; e.Nombre = dto.Nombre; e.NumeroMedidor = dto.NumeroMedidor; e.FechaVencimiento = dto.FechaVencimiento; e.PeriodoEmision = dto.PeriodoEmision; await _db.SaveChangesAsync(); return NoContent(); }
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id) { var e = await _db.ContratoLuz.FindAsync(id); if (e == null) return NotFound(); _db.ContratoLuz.Remove(e); await _db.SaveChangesAsync(); return NoContent(); }
}
