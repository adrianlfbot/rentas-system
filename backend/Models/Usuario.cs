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
    public string? INE { get; set; }
    public string? Telefono { get; set; }
}
