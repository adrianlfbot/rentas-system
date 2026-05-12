using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentasApi.Models;

[Table("Usuarios")]
public class Usuario
{
    [Key]
    public string Correo { get; set; } = null!;
    public string Password { get; set; } = null!;
    public DateTime? FechaUltimoAcceso { get; set; }
    public string Tipo { get; set; } = null!; // Propietario | Inquilino
    public int? INE { get; set; }  // FK to Adjuntos.ID
    
    [ForeignKey("INE")]
    public Adjunto? INEAdjunto { get; set; }
    public string? Telefono { get; set; }
    public string? Ocupacion { get; set; }
    public string? Corresponsable { get; set; }
    public string? DomicilioNotificaciones { get; set; }
}
