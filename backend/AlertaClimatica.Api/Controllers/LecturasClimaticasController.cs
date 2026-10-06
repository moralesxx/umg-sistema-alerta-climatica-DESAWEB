using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlertaClimatica.Domain;
using AlertaClimatica.Infrastructure.Services.Interfaces;

namespace AlertaClimatica.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LecturasClimaticasController : ControllerBase
{
    private readonly ILecturaService _service;

    public LecturasClimaticasController(ILecturaService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LecturaClimatica>>> GetLecturas()
    {
        var lecturas = await _service.GetLecturasAsync();
        return Ok(lecturas);
    }

    [HttpGet("sensor/{sensorId}")]
    public async Task<ActionResult<IEnumerable<LecturaClimatica>>> GetLecturasPorSensor(int sensorId)
    {
        var lecturas = await _service.GetLecturasPorSensorAsync(sensorId);
        return Ok(lecturas);
    }

    [HttpGet("comunidad/{comunidadId}")]
    public async Task<ActionResult<IEnumerable<LecturaClimatica>>> GetLecturasPorComunidad(int comunidadId)
    {
        var lecturas = await _service.GetLecturasPorComunidadAsync(comunidadId);
        return Ok(lecturas);
    }

    [HttpGet("filtro")]
    public async Task<ActionResult<IEnumerable<LecturaClimatica>>> GetLecturasPorFiltro(
        [FromQuery] int? sensorId, 
        [FromQuery] DateTime? fechaInicio, 
        [FromQuery] DateTime? fechaFin)
    {
        var lecturas = await _service.GetLecturasPorFiltroAsync(sensorId, fechaInicio, fechaFin);
        return Ok(lecturas);
    }

    [HttpPost]
    [Authorize(Roles = "Administrador,Operador")]
    public async Task<ActionResult<LecturaClimatica>> CreateLectura(LecturaClimatica lectura)
    {
        await _service.CrearLecturaAsync(lectura);
        return CreatedAtAction(nameof(GetLecturas), new { id = lectura.LecturaId }, lectura);
    }
}