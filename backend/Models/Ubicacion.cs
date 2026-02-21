using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentasApi.Models;

[Table("Ubicaciones")]
public class Ubicacion
{
    [Key]
    public int IDUbicacion { get; set; }
    public string Calle { get; set; } = null!;
    public string Numero { get; set; } = null!;
    public string? Propietario { get; set; }
    public string? NumeroPredial { get; set; }
    public int? ContratoLuzId { get; set; }
    public int? ContratoAguaId { get; set; }
    public int? ContratoInternetId { get; set; }

    [ForeignKey("ContratoLuzId")]
    public ContratoLuz? ContratoLuz { get; set; }
    [ForeignKey("ContratoAguaId")]
    public ContratoAgua? ContratoAgua { get; set; }
    [ForeignKey("ContratoInternetId")]
    public ContratoInternet? ContratoInternet { get; set; }

    public ICollection<Departamento> Departamentos { get; set; } = new List<Departamento>();
}
