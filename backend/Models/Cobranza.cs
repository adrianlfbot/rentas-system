namespace RentasApi.Models;

public class Cobranza
{
    public int ID { get; set; }
    public int IDUbicacion { get; set; }
    public string ClaveDepartamento { get; set; } = string.Empty;
    public string Periodo { get; set; } = string.Empty; // "2026-02"
    public DateTime? FechaCobro { get; set; }
    public string? Medio { get; set; }
    public double Monto { get; set; }

    // Navigation
    public Ubicacion? Ubicacion { get; set; }
}
