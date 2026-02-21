namespace RentasApi.Models;

public class Adjunto
{
    public int ID { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty; // entity name
    public int IDPadre { get; set; }
    public string? Filename { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
