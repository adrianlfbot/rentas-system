using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentasApi.Data;
using RentasApi.Models;

namespace RentasApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UbicacionesController : ControllerBase
{
    private readonly RentasContext _db;
    public UbicacionesController(RentasContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.Ubicaciones.Include(u => u.ContratoLuz).Include(u => u.ContratoAgua).Include(u => u.ContratoInternet).ToListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var u = await _db.Ubicaciones.Include(x => x.ContratoLuz).Include(x => x.ContratoAgua).Include(x => x.ContratoInternet).Include(x => x.Departamentos).FirstOrDefaultAsync(x => x.IDUbicacion == id);
        return u == null ? NotFound() : Ok(u);
    }

    [HttpPost]
    [Authorize(Roles = "Propietario")]
    public async Task<IActionResult> Create([FromBody] Ubicacion u)
    {
        _db.Ubicaciones.Add(u);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = u.IDUbicacion }, u);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Propietario")]
    public async Task<IActionResult> Update(int id, [FromBody] Ubicacion dto)
    {
        var u = await _db.Ubicaciones.FindAsync(id);
        if (u == null) return NotFound();
        u.Calle = dto.Calle; u.Numero = dto.Numero; u.Propietario = dto.Propietario;
        u.NumeroPredial = dto.NumeroPredial; u.ContratoLuzId = dto.ContratoLuzId;
        u.ContratoAguaId = dto.ContratoAguaId; u.ContratoInternetId = dto.ContratoInternetId;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Propietario")]
    public async Task<IActionResult> Delete(int id)
    {
        var u = await _db.Ubicaciones.FindAsync(id);
        if (u == null) return NotFound();
        _db.Ubicaciones.Remove(u);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
