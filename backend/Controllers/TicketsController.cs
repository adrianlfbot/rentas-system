using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using RentasApi.Data;
using RentasApi.Models;

namespace RentasApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly RentasContext _db;
    public TicketsController(RentasContext db) => _db = db;

    private string GetCorreo() => User.FindFirst("correo")?.Value ?? "";
    private bool IsPropietario() => User.IsInRole("Propietario");

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var query = _db.Tickets.Include(t => t.Usuario).AsQueryable();
        if (!IsPropietario())
            query = query.Where(t => t.UsuarioCreo == GetCorreo());
        return Ok(await query.OrderByDescending(t => t.FechaCreacion).ToListAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var t = await _db.Tickets.Include(t => t.Usuario).FirstOrDefaultAsync(t => t.ID == id);
        if (t == null) return NotFound();
        if (!IsPropietario() && t.UsuarioCreo != GetCorreo()) return Forbid();
        return Ok(t);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Ticket ticket)
    {
        ticket.UsuarioCreo = GetCorreo();
        ticket.FechaCreacion = DateTime.UtcNow;
        ticket.Estado = "Abierto";
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = ticket.ID }, ticket);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Propietario")]
    public async Task<IActionResult> Update(int id, [FromBody] Ticket updated)
    {
        var t = await _db.Tickets.FindAsync(id);
        if (t == null) return NotFound();
        t.Prioridad = updated.Prioridad;
        t.Descripcion = updated.Descripcion;
        t.Estado = updated.Estado;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Propietario")]
    public async Task<IActionResult> Delete(int id)
    {
        var t = await _db.Tickets.FindAsync(id);
        if (t == null) return NotFound();
        _db.Tickets.Remove(t);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
