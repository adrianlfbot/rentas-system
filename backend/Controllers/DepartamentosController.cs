using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentasApi.Data;
using RentasApi.Models;

namespace RentasApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Propietario")]
public class DepartamentosController : ControllerBase
{
    private readonly RentasContext _db;
    public DepartamentosController(RentasContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.Departamentos.Include(d => d.Ubicacion).Include(d => d.Inquilino).ToListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var d = await _db.Departamentos.Include(d => d.Ubicacion).Include(d => d.Inquilino).FirstOrDefaultAsync(d => d.ID == id);
        if (d == null) return NotFound();
        return Ok(d);
    }

    [HttpGet("{id}/historial")]
    public async Task<IActionResult> GetHistorial(int id) =>
        Ok(await _db.HistorialInquilinos.Where(h => h.DepartamentoId == id).OrderByDescending(h => h.FechaInicio).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Departamento depto)
    {
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

        // Si cambia el inquilino, registrar en historial
        if (d.InquilinoCorreo != updated.InquilinoCorreo)
        {
            // Cerrar historial anterior
            if (!string.IsNullOrEmpty(d.InquilinoCorreo))
            {
                var hist = await _db.HistorialInquilinos
                    .Where(h => h.DepartamentoId == id && h.CorreoInquilino == d.InquilinoCorreo && h.FechaFin == null)
                    .FirstOrDefaultAsync();
                if (hist != null) hist.FechaFin = DateTime.UtcNow;
            }
            // Abrir nuevo historial
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
}
