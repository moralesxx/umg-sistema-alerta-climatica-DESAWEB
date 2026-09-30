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
    private readonly ITipoSensorRepository _tipoSensorRepository;

    public TiposSensorController(ITipoSensorRepository tipoSensorRepository)
    {
        _tipoSensorRepository = tipoSensorRepository;
    }

    // GET: api/tipossensor
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TipoSensor>>> GetTiposSensor()
    {
        var tipos = await _tipoSensorRepository.ObtenerTodosAsync();
        return Ok(tipos);
    }
}