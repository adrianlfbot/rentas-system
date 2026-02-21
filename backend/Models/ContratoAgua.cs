using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentasApi.Models;

[Table("ContratoAgua")]
public class ContratoAgua
{
    [Key]
    public int ID { get; set; }
    public string NumeroInmueble { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string? NumeroContrato { get; set; }
    public string? FechaVencimiento { get; set; }
    public string? PeriodoEmision { get; set; }
}
