using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentasApi.Models;

[Table("ContratoInternet")]
public class ContratoInternet
{
    [Key]
    public int ID { get; set; }
    public string NumeroContrato { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string? NumeroPagoOXXO { get; set; }
    public string? FechaVencimiento { get; set; }
    public string? PeriodoEmision { get; set; }
}
