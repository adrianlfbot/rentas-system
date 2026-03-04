using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using RentasApi.Data;
using RentasApi.DTOs;

namespace RentasApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly RentasContext _db;
    private readonly IConfiguration _config;

    public AuthController(RentasContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _db.Usuarios.FindAsync(dto.Correo);
        if (user == null) return Unauthorized("Credenciales inválidas");

        // Plain text comparison (NOT RECOMMENDED - only for testing)
        if (dto.Password != user.Password)
            return Unauthorized("Credenciales inválidas");

        user.FechaUltimoAcceso = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var token = GenerateToken(user.Correo, user.Tipo);
        return Ok(new LoginResponseDto
        {
            Token = token,
            Correo = user.Correo,
            Tipo = user.Tipo
        });
    }

    private string GenerateToken(string correo, string tipo)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _config["Jwt:Key"] ?? "RentasSystem2026SuperSecretKeyMinimo32Chars!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Email, correo),
            new Claim(ClaimTypes.Role, tipo),
            new Claim("correo", correo)
        };

        var token = new JwtSecurityToken(
            issuer: "RentasApi",
            audience: "RentasApp",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(30),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
