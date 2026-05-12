using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentasApi.Data;

namespace RentasApi.Controllers;

[ApiController]
[Route("api/contratos/arrendamiento")]
[Authorize]
public class ContratosArrendamientoController : ControllerBase
{
    private readonly RentasContext _db;
    private readonly IWebHostEnvironment _env;

    public ContratosArrendamientoController(RentasContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpGet("{departamentoId}")]
    public async Task<IActionResult> GenerarContrato(
        int departamentoId,
        [FromQuery] string? fechaIni,
        [FromQuery] string? fechaFin,
        [FromQuery] string? corresponsable,
        [FromQuery] string? instalaciones,
        [FromQuery] string? ocupacion,
        [FromQuery] string? notificaciones,
        [FromQuery] int? maxOcupantes)
    {
        var depto = await _db.Departamentos
            .Include(d => d.Ubicacion)
            .Include(d => d.Inquilino)
            .FirstOrDefaultAsync(d => d.ID == departamentoId);

        if (depto == null)
            return NotFound("Departamento no encontrado");

        var templatePath = Path.Combine(_env.ContentRootPath, "Templates", "contrato_machote.rtf");
        if (!System.IO.File.Exists(templatePath))
            return StatusCode(500, "Plantilla de contrato no encontrada");

        var rtf = await System.IO.File.ReadAllTextAsync(templatePath);

        // Datos del inquilino
        var inquilino = depto.Inquilino;
        var nombreInquilino = inquilino?.Correo?.Split('@')[0] ?? "_______________";
        var correoInquilino = inquilino?.Correo ?? "_______________";
        var telefonoInquilino = inquilino?.Telefono ?? "_______________";
        var ocupacionInquilino = ocupacion ?? inquilino?.Ocupacion ?? "_______________";
        var corresponsableVal = corresponsable ?? inquilino?.Corresponsable ?? "_______________";
        var notificacionesVal = notificaciones ?? inquilino?.DomicilioNotificaciones
            ?? $"{depto.Ubicacion?.Calle} {depto.Ubicacion?.Numero}, Depto {depto.Clave}";

        // Datos del departamento
        var direccion = $"{depto.Ubicacion?.Calle} {depto.Ubicacion?.Numero}, Depto {depto.Clave}";
        var renta = depto.MontoRenta.ToString("N0");
        var maxOc = (maxOcupantes ?? depto.MaxOcupantes).ToString();
        var instalacionesVal = instalaciones ?? depto.Instalaciones ?? "_______________";
        var fechaIniVal = fechaIni ?? DateTime.Now.ToString("dd/MM/yyyy");
        var fechaFinVal = fechaFin ?? DateTime.Now.AddMonths(6).ToString("dd/MM/yyyy");

        // Reemplazar placeholders
        rtf = rtf.Replace("xxxarrendadorxxx", EscapeRtf(nombreInquilino));
        rtf = rtf.Replace("xxxdireccionxxx", EscapeRtf(direccion));
        rtf = rtf.Replace("xxxmaximoxxx", EscapeRtf(maxOc));
        rtf = rtf.Replace("xxxcorreoxxx", EscapeRtf(correoInquilino));
        rtf = rtf.Replace("xxxcelularxxx", EscapeRtf(telefonoInquilino));
        rtf = rtf.Replace("xxxnotificacionesxxx", EscapeRtf(notificacionesVal));
        rtf = rtf.Replace("xxxcorresponsablexxx", EscapeRtf(corresponsableVal));
        rtf = rtf.Replace("xxxrentaxxx", EscapeRtf(renta));
        rtf = rtf.Replace("xxxinstalacionesxxx", EscapeRtf(instalacionesVal));
        rtf = rtf.Replace("xxxfechainixxx", EscapeRtf(fechaIniVal));
        rtf = rtf.Replace("xxxfechafinxxx", EscapeRtf(fechaFinVal));
        rtf = rtf.Replace("xxxocupacionxxx", EscapeRtf(ocupacionInquilino));

        var fileName = $"Contrato_{depto.Clave}_{DateTime.Now:yyyyMMdd}.rtf";
        var bytes = System.Text.Encoding.Latin1.GetBytes(rtf);
        return File(bytes, "application/rtf", fileName);
    }

    private static string EscapeRtf(string text)
    {
        // Escapar caracteres especiales RTF y acentos básicos
        return text
            .Replace("\\", "\\\\")
            .Replace("{", "\\{")
            .Replace("}", "\\}")
            .Replace("á", "\\'e1").Replace("é", "\\'e9").Replace("í", "\\'ed")
            .Replace("ó", "\\'f3").Replace("ú", "\\'fa").Replace("ü", "\\'fc")
            .Replace("Á", "\\'c1").Replace("É", "\\'c9").Replace("Í", "\\'cd")
            .Replace("Ó", "\\'d3").Replace("Ú", "\\'da").Replace("ñ", "\\'f1")
            .Replace("Ñ", "\\'d1");
    }
}
