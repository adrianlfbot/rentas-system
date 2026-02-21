namespace RentasApi.Models;

public class HistorialInquilino
{
    public int ID { get; set; }
    public int DepartamentoId { get; set; }
    public string CorreoInquilino { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }

    // Navigation
    public Departamento? Departamento { get; set; }
    public Usuario? Inquilino { get; set; }
}
