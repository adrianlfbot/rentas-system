using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentasApi.Data;
using RentasApi.Models;
using System.Security.Claims;

namespace RentasApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Propietario")]
public class DepartamentosController : ControllerBase
{
    private readonly RentasContext _db;
    public DepartamentosController(RentasContext db) => _db = db;

    private string GetCorreo() => User.FindFirst("correo")?.Value ?? "";

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.Departamentos
            .Include(d => d.Ubicacion)
            .Include(d => d.ContratoLuz)
            .Include(d => d.Inquilino)
            .ToListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var d = await _db.Departamentos
            .Include(d => d.Ubicacion)
            .Include(d => d.ContratoLuz)
            .Include(d => d.Inquilino)
            .FirstOrDefaultAsync(d => d.ID == id);
        if (d == null) return NotFound();
        return Ok(d);
    }

    // === NOTAS ===
    [HttpGet("{id}/notas")]
    public async Task<IActionResult> GetNotas(int id) =>
        Ok(await _db.NotasDepartamento.Where(n => n.DepartamentoId == id).OrderByDescending(n => n.Fecha).ToListAsync());

    [HttpPost("{id}/notas")]
    public async Task<IActionResult> AddNota(int id, [FromBody] NotaDepartamento nota)
    {
        nota.DepartamentoId = id;
        nota.Fecha = DateTime.UtcNow;
        nota.UsuarioCreo = GetCorreo();
        _db.NotasDepartamento.Add(nota);
        await _db.SaveChangesAsync();
        return Ok(nota);
    }

    [HttpDelete("notas/{notaId}")]
    public async Task<IActionResult> DeleteNota(int notaId)
    {
        var n = await _db.NotasDepartamento.FindAsync(notaId);
        if (n == null) return NotFound();
        _db.NotasDepartamento.Remove(n);
        await _db.SaveChangesAsync();
        return NoContent();
    }
    // =============

    [HttpGet("{id}/historial")]
    public async Task<IActionResult> GetHistorial(int id) =>
        Ok(await _db.HistorialInquilinos.Where(h => h.DepartamentoId == id).OrderByDescending(h => h.FechaInicio).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Departamento depto)
    {
        // Validar que el inquilino exista si se especificó
        if (!string.IsNullOrEmpty(depto.InquilinoCorreo))
        {
            var inquilino = await _db.Usuarios.FindAsync(depto.InquilinoCorreo);
            if (inquilino == null)
                return BadRequest(new { message = $"El inquilino '{depto.InquilinoCorreo}' no está registrado. Primero debes darlo de alta en Usuarios." });
        }

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

        // Validar que el inquilino exista si se especificó
        if (!string.IsNullOrEmpty(updated.InquilinoCorreo))
        {
            var inquilino = await _db.Usuarios.FindAsync(updated.InquilinoCorreo);
            if (inquilino == null)
                return BadRequest(new { message = $"El inquilino '{updated.InquilinoCorreo}' no está registrado. Primero debes darlo de alta en Usuarios." });
        }

        if (d.InquilinoCorreo != updated.InquilinoCorreo)
        {
            if (!string.IsNullOrEmpty(d.InquilinoCorreo))
            {
                var hist = await _db.HistorialInquilinos
                    .Where(h => h.DepartamentoId == id && h.CorreoInquilino == d.InquilinoCorreo && h.FechaFin == null)
                    .FirstOrDefaultAsync();
                if (hist != null) hist.FechaFin = DateTime.UtcNow;
            }
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
        d.ContratoLuzId = updated.ContratoLuzId;
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
