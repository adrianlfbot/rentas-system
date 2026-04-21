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

    // === EXPORTAR CSV ===
    [HttpGet("exportar")]
    public async Task<IActionResult> Exportar()
    {
        var items = await _db.Usuarios.ToListAsync();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Correo,Password,Tipo,Telefono");
        foreach (var u in items)
            sb.AppendLine($"{Csv(u.Correo)},,{Csv(u.Tipo)},{Csv(u.Telefono)}");
        // Password se exporta vacío por seguridad
        var bytes = System.Text.Encoding.UTF8.GetPreamble()
            .Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", "usuarios.csv");
    }

    // === IMPORTAR CSV (solo inserta inquilinos) ===
    [HttpPost("importar")]
    public async Task<IActionResult> Importar(IFormFile archivo)
    {
        if (archivo == null || archivo.Length == 0) return BadRequest("Archivo vacío.");
        int insertados = 0, omitidos = 0, errores = 0;
        using var reader = new System.IO.StreamReader(archivo.OpenReadStream());
        await reader.ReadLineAsync(); // saltar encabezado
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line);
            if (cols.Length < 2) { errores++; continue; }
            try
            {
                string correo   = cols[0].Trim();
                string password = cols[1].Trim();
                string telefono = cols.Length > 3 ? cols[3].Trim() : "";

                if (string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(password))
                    { errores++; continue; }

                // Si ya existe, omitir
                bool existe = await _db.Usuarios.AnyAsync(u => u.Correo == correo);
                if (existe) { omitidos++; continue; }

                _db.Usuarios.Add(new Usuario
                {
                    Correo   = correo,
                    Password = BCrypt.Net.BCrypt.HashPassword(password),
                    Tipo     = "Inquilino",
                    Telefono = string.IsNullOrEmpty(telefono) ? null : telefono
                });
                insertados++;
            }
            catch { errores++; }
        }
        await _db.SaveChangesAsync();
        return Ok(new { insertados, omitidos, errores });
    }

    private static string Csv(string? v) => v == null ? "" : v.Contains(',') ? $"\"{v}\"" : v;
    private static string[] ParseCsvLine(string line)
    {
        var result = new System.Collections.Generic.List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();
        foreach (var ch in line)
        {
            if (ch == '"') { inQuotes = !inQuotes; }
            else if (ch == ',' && !inQuotes) { result.Add(current.ToString().Trim()); current.Clear(); }
            else { current.Append(ch); }
        }
        result.Add(current.ToString().Trim());
        return result.ToArray();
    }
}
