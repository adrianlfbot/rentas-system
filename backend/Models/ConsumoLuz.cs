namespace RentasApi.Models;

public class ConsumoLuz
{
    public int ID { get; set; }
    public int ContratoLuzId { get; set; }
    public string Periodo { get; set; } = string.Empty; // formato YYYY-MM
    public decimal? KWh { get; set; }
    public decimal Monto { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
}
