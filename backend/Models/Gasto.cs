namespace RentasApi.Models;

public class Gasto
{
    public int ID { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public int DepartamentoId { get; set; }
    public string? Descripcion { get; set; }
    public decimal ManoDeObra { get; set; } = 0;
    public decimal Material { get; set; } = 0;
    // Total = ManoDeObra + Material (calculado)

    // Navigation
    public Departamento? Departamento { get; set; }
}
