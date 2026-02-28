namespace RentasApi.Models;

public class NotaDepartamento
{
    public int ID { get; set; }
    public int DepartamentoId { get; set; }
    public string Texto { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string? UsuarioCreo { get; set; }

    // Navigation
    public Departamento? Departamento { get; set; }
    public Usuario? Usuario { get; set; }
}
