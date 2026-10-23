using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlertaClimatica.Domain;
using AlertaClimatica.Infrastructure.Repositories.Interfaces;

namespace AlertaClimatica.Api.Controllers;

// Solo lectura de catálogo. Habla directo con el repositorio (sin Service),
// porque no hay ninguna regla de negocio de por medio, solo una consulta.
// Requiere estar autenticado (cualquier rol), sin restricción adicional.
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TiposSensorController : ControllerBase
{
    // Repositorio privado para realizar consultas directas sobre los tipos de sensor
    private readonly ITipoSensorRepository _tipoSensorRepository;

    // Constructor que inyecta la dependencia del repositorio de tipos de sensor
    public TiposSensorController(ITipoSensorRepository tipoSensorRepository)
    {
        _tipoSensorRepository = tipoSensorRepository;
    }

    // GET: api/tipossensor
    // Endpoint GET que permite consultar y obtener el catálogo completo de todos los tipos de sensor disponibles
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TipoSensor>>> GetTiposSensor()
    {
        // Llama al repositorio para recuperar todos los registros de tipos de sensor de forma asíncrona
        var tipos = await _tipoSensorRepository.ObtenerTodosAsync();
        // Retorna la colección obtenida con un código de estado HTTP 200 OK
        return Ok(tipos);
    }
}