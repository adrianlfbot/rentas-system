using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentasApi.Models;

[Table("Departamento")]
public class Departamento
{
    [Key]
    public int ID { get; set; }
    public int IDUbicacion { get; set; }
    public string Clave { get; set; } = null!;
    public string? Descripcion { get; set; }
    public int Cuartos { get; set; }
    public int Banos { get; set; }
    public int Estacionamiento { get; set; }
    public string? Extras { get; set; }
    public double MontoRenta { get; set; }
    public double CuotaAgua { get; set; }
    public int DiaVencimiento { get; set; } = 1;
    public string? DescripcionPublicacion { get; set; }
    public string? InquilinoCorreo { get; set; }

    [ForeignKey("IDUbicacion")]
    public Ubicacion? Ubicacion { get; set; }
    [ForeignKey("InquilinoCorreo")]
    public Usuario? Inquilino { get; set; }

    public ICollection<HistorialInquilino> HistorialInquilinos { get; set; } = new List<HistorialInquilino>();
}
