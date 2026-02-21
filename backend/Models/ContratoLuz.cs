using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentasApi.Models;

[Table("ContratoLuz")]
public class ContratoLuz
{
    [Key]
    public int ID { get; set; }
    public string RPU { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string? NumeroMedidor { get; set; }
    public string? FechaVencimiento { get; set; }
    public string? PeriodoEmision { get; set; }
}
