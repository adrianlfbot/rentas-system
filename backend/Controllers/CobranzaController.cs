using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentasApi.Data;
using RentasApi.DTOs;
using RentasApi.Models;

namespace RentasApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Propietario")]
public class CobranzaController : ControllerBase
{
    private readonly RentasContext _db;
    public CobranzaController(RentasContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? periodo) =>
        Ok(await _db.Cobranza
            .Where(c => periodo == null || c.Periodo == periodo)
            .Include(c => c.Ubicacion)
            .OrderBy(c => c.IDUbicacion).ThenBy(c => c.ClaveDepartamento)
            .ToListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var c = await _db.Cobranza.Include(c => c.Ubicacion).FirstOrDefaultAsync(c => c.ID == id);
        return c == null ? NotFound() : Ok(c);
    }

    [HttpGet("tablero")]
    public async Task<IActionResult> Tablero([FromQuery] string periodo)
    {
        if (string.IsNullOrEmpty(periodo)) return BadRequest("Periodo requerido (ej: 2026-02)");

        var deptos = await _db.Departamentos
            .Include(d => d.Ubicacion)
            .OrderBy(d => d.IDUbicacion).ThenBy(d => d.Clave)
            .ToListAsync();

        var pagos = await _db.Cobranza
            .Where(c => c.Periodo == periodo)
            .ToListAsync();

        var tablero = deptos.Select(d =>
        {
            var pago = pagos.FirstOrDefault(p => p.IDUbicacion == d.IDUbicacion && p.ClaveDepartamento == d.Clave);
            return new TableroItemDto
            {
                Ubicacion = $"{d.Ubicacion?.Calle} {d.Ubicacion?.Numero}",
                IDUbicacion = d.IDUbicacion,
                Clave = d.Clave,
                Inquilino = d.InquilinoCorreo,
                MontoRenta = d.MontoRenta,
                Pagado = pago != null,
                FechaPago = pago?.FechaCobro
            };
        }).ToList();

        return Ok(tablero);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Cobranza cobranza)
    {
        _db.Cobranza.Add(cobranza);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = cobranza.ID }, cobranza);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Cobranza updated)
    {
        var c = await _db.Cobranza.FindAsync(id);
        if (c == null) return NotFound();
        c.IDUbicacion = updated.IDUbicacion;
        c.ClaveDepartamento = updated.ClaveDepartamento;
        c.Periodo = updated.Periodo;
        c.FechaCobro = updated.FechaCobro;
        c.Medio = updated.Medio;
        c.Monto = updated.Monto;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.Cobranza.FindAsync(id);
        if (c == null) return NotFound();
        _db.Cobranza.Remove(c);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
