namespace AlertaClimatica.Application.DTOs;

public class CrearReglaAlertaDto
{
    public string Nombre { get; set; } = string.Empty;

    public int TipoSensorId { get; set; }

    public decimal? ValorMinimo { get; set; }

    public decimal? ValorMaximo { get; set; }

    public string NivelPeligro { get; set; } = string.Empty;

    public string TipoFenomeno { get; set; } = string.Empty;

    public string Mensaje { get; set; } = string.Empty;

    public bool Estado { get; set; } = true;
}