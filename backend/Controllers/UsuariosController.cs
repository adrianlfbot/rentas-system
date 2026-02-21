using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentasApi.Data;
using RentasApi.Models;

namespace RentasApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Propietario")]
public class UsuariosController : ControllerBase
{
    private readonly RentasContext _db;
    public UsuariosController(RentasContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.Usuarios.Select(u => new { u.Correo, u.Tipo, u.Telefono, u.INE, u.FechaUltimoAcceso }).ToListAsync());

    [HttpGet("{correo}")]
    public async Task<IActionResult> Get(string correo)
    {
        var u = await _db.Usuarios.FindAsync(correo);
        return u == null ? NotFound() : Ok(new { u.Correo, u.Tipo, u.Telefono, u.INE, u.FechaUltimoAcceso });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Usuario u)
    {
        u.Password = BCrypt.Net.BCrypt.HashPassword(u.Password);
        _db.Usuarios.Add(u);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { correo = u.Correo }, new { u.Correo, u.Tipo });
    }

    [HttpPut("{correo}")]
    public async Task<IActionResult> Update(string correo, [FromBody] Usuario dto)
    {
        var u = await _db.Usuarios.FindAsync(correo);
        if (u == null) return NotFound();
        u.Tipo = dto.Tipo;
        u.Telefono = dto.Telefono;
        u.INE = dto.INE;
        if (!string.IsNullOrEmpty(dto.Password))
            u.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{correo}")]
    public async Task<IActionResult> Delete(string correo)
    {
        var u = await _db.Usuarios.FindAsync(correo);
        if (u == null) return NotFound();
        _db.Usuarios.Remove(u);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
