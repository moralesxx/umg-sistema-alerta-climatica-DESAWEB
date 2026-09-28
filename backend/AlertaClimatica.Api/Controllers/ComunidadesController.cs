using Microsoft.AspNetCore.Mvc;
using AlertaClimatica.Domain;
using AlertaClimatica.Infrastructure.Repositories.Interfaces;

namespace AlertaClimatica.Api.Controllers;

// Solo lectura por ahora — soporte al <select> de Comunidad en el formulario
// de Sensores. El CRUD completo de Comunidades (RF-ADM-08 a RF-ADM-14) es
// el objetivo #5 de la fase, todavía no implementado.
[ApiController]
[Route("api/[controller]")]
public class ComunidadesController : ControllerBase
{
    private readonly IComunidadRepository _comunidadRepository;

    public ComunidadesController(IComunidadRepository comunidadRepository)
    {
        _comunidadRepository = comunidadRepository;
    }

    // GET: api/comunidades
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Comunidad>>> GetComunidades()
    {
        var comunidades = await _comunidadRepository.ObtenerTodosAsync();
        return Ok(comunidades);
    }
}