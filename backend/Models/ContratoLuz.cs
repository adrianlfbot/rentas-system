namespace RentasApi.Models;

public class ContratoLuz
{
    public int ID { get; set; }
    public string RPU { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? NumeroMedidor { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public string? PeriodoEmision { get; set; }
}
