using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentasApi.Data;
using RentasApi.Models;

namespace RentasApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepartamentoController : ControllerBase
{
    private readonly RentasContext _db;
    public DepartamentoController(RentasContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.Departamentos.Include(d => d.Ubicacion).Include(d => d.Inquilino).ToListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var d = await _db.Departamentos.Include(x => x.Ubicacion).Include(x => x.Inquilino)
            .Include(x => x.HistorialInquilinos).FirstOrDefaultAsync(x => x.ID == id);
        return d == null ? NotFound() : Ok(d);
    }

    [HttpPost]
    [Authorize(Roles = "Propietario")]
    public async Task<IActionResult> Create([FromBody] Departamento d)
    {
        _db.Departamentos.Add(d);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = d.ID }, d);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Propietario")]
    public async Task<IActionResult> Update(int id, [FromBody] Departamento dto)
    {
        var d = await _db.Departamentos.FindAsync(id);
        if (d == null) return NotFound();

        // If tenant is changing, record history
        if (d.InquilinoCorreo != dto.InquilinoCorreo)
        {
            if (!string.IsNullOrEmpty(d.InquilinoCorreo))
            {
                var hist = await _db.HistorialInquilinos
                    .Where(h => h.DepartamentoId == id && h.CorreoInquilino == d.InquilinoCorreo && h.FechaFin == null)
                    .FirstOrDefaultAsync();
                if (hist != null)
                    hist.FechaFin = DateTime.UtcNow;
            }
            if (!string.IsNullOrEmpty(dto.InquilinoCorreo))
            {
                _db.HistorialInquilinos.Add(new HistorialInquilino
                {
                    DepartamentoId = id,
                    CorreoInquilino = dto.InquilinoCorreo,
                    FechaInicio = DateTime.UtcNow
                });
            }
            d.InquilinoCorreo = dto.InquilinoCorreo;
        }

        d.Clave = dto.Clave; d.Descripcion = dto.Descripcion; d.Cuartos = dto.Cuartos;
        d.Banos = dto.Banos; d.Estacionamiento = dto.Estacionamiento; d.Extras = dto.Extras;
        d.MontoRenta = dto.MontoRenta; d.CuotaAgua = dto.CuotaAgua;
        d.DiaVencimiento = dto.DiaVencimiento; d.DescripcionPublicacion = dto.DescripcionPublicacion;
        d.IDUbicacion = dto.IDUbicacion;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Propietario")]
    public async Task<IActionResult> Delete(int id)
    {
        var d = await _db.Departamentos.FindAsync(id);
        if (d == null) return NotFound();
        _db.Departamentos.Remove(d);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
