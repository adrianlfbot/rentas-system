using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentasApi.Data;
using RentasApi.Models;

namespace RentasApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdjuntosController : ControllerBase
{
    private readonly RentasContext _db;
    private readonly string _uploadPath;

    public AdjuntosController(RentasContext db, IConfiguration config)
    {
        _db = db;
        _uploadPath = config["Uploads:Path"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        Directory.CreateDirectory(_uploadPath);
    }

    [HttpGet("{tipo}/{idPadre}")]
    public async Task<IActionResult> GetByEntity(string tipo, int idPadre) =>
        Ok(await _db.Adjuntos.Where(a => a.Tipo == tipo && a.IDPadre == idPadre).ToListAsync());

    [HttpPost("{tipo}/{idPadre}")]
    public async Task<IActionResult> Upload(string tipo, int idPadre, IFormFile file)
    {
        if (file.Length == 0) return BadRequest("Archivo vacío");

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(_uploadPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);

        var adjunto = new Adjunto
        {
            MimeType = file.ContentType,
            Tipo = tipo,
            IDPadre = idPadre,
            Filename = file.FileName,
            FilePath = filePath,
            FechaCreacion = DateTime.UtcNow
        };

        _db.Adjuntos.Add(adjunto);
        await _db.SaveChangesAsync();
        return Ok(adjunto);
    }

    [HttpGet("download/{id}")]
    public async Task<IActionResult> Download(int id)
    {
        var a = await _db.Adjuntos.FindAsync(id);
        if (a == null) return NotFound();
        if (!System.IO.File.Exists(a.FilePath)) return NotFound("Archivo no encontrado en disco");
        return PhysicalFile(a.FilePath, a.MimeType, a.Filename);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Propietario")]
    public async Task<IActionResult> Delete(int id)
    {
        var a = await _db.Adjuntos.FindAsync(id);
        if (a == null) return NotFound();
        if (System.IO.File.Exists(a.FilePath)) System.IO.File.Delete(a.FilePath);
        _db.Adjuntos.Remove(a);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
