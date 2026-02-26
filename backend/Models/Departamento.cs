namespace RentasApi.Models;

public class Departamento
{
    public int ID { get; set; }
    public int IDUbicacion { get; set; }
    public string Clave { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Cuartos { get; set; }
    public int Banos { get; set; }
    public int Estacionamiento { get; set; }
    public string? Extras { get; set; }
    public double MontoRenta { get; set; }
    public double CuotaAgua { get; set; }
    public int? ContratoLuzId { get; set; } // Nuevo
    public int DiaVencimiento { get; set; } = 1;
    public string? DescripcionPublicacion { get; set; }
    public string? InquilinoCorreo { get; set; }

    // Navigation
    public Ubicacion? Ubicacion { get; set; }
    public ContratoLuz? ContratoLuz { get; set; } // Nuevo
    public Usuario? Inquilino { get; set; }
}
