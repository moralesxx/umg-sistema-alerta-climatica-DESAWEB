using AlertaClimatica.Domain;

namespace AlertaClimatica.Infrastructure.Services;

// RF-ADM-32 / RF-ADM-33: compara una lectura contra las reglas activas y,
// si incumple alguna, genera la alerta (y su evento) automáticamente.
public interface IEvaluadorAlertas
{
    // Devuelve la alerta creada, o null si no hubo regla activada o ya existía
    // una alerta activa para ese sensor y esa regla.
    Task<Alerta?> EvaluarAsync(LecturaClimatica lectura, CancellationToken ct = default);
}
