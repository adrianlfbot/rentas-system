namespace RentasApi.Models;

public class Ticket
{
    public int ID { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public string UsuarioCreo { get; set; } = string.Empty;
    public string Prioridad { get; set; } = "Media"; // Alta | Media | Baja
    public string Descripcion { get; set; } = string.Empty;
    public string Estado { get; set; } = "Abierto"; // Abierto | EnProgreso | Cerrado
    public DateTime? UltimoRecordatorio { get; set; }

    // Navigation
    public Usuario? Usuario { get; set; }
}
