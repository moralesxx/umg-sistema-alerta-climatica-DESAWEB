namespace AlertaClimatica.Application.DTOs;

// Cuerpo de PATCH api/sensores/{id}/valor-simulado. Valor null = modo automático.
public class ValorSimuladoDto
{
    public decimal? Valor { get; set; }
}
