namespace RentasApi.DTOs;

public class LoginDto
{
    public string Correo { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
}

public class TableroItemDto
{
    public string Ubicacion { get; set; } = string.Empty;
    public int IDUbicacion { get; set; }
    public string Clave { get; set; } = string.Empty;
    public string? Inquilino { get; set; }
    public double MontoRenta { get; set; }
    public bool Pagado { get; set; }
    public DateTime? FechaPago { get; set; }
}
