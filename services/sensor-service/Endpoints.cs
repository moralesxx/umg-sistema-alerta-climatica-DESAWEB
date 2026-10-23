using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Clima.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SensorSvc;

public class SensorInput
{
    public int SensorId { get; set; }
    public string NombreSensor { get; set; } = "";
    public int TipoSensorId { get; set; }
    public string Ubicacion { get; set; } = "";
    public bool Estado { get; set; }
    public string? Codigo { get; set; }
    public int? ComunidadId { get; set; }
    public DateTime? FechaInstalacion { get; set; }
    public string? Descripcion { get; set; }
}
public record SensorCount(int ComunidadId, int Total);
public class IngestItem { public string? Codigo { get; set; } public decimal? Valor { get; set; } }
public class IngestInput { public string? Codigo { get; set; } public decimal? Valor { get; set; } public List<IngestItem>? Lecturas { get; set; } }

/// <summary>Sensores alimentados por hardware real: el simulador los omite mientras lleguen lecturas.</summary>
public static class Dispositivos
{
    private static readonly ConcurrentDictionary<int, DateTime> Ultima = new();
    public static void Marcar(int sensorId) => Ultima[sensorId] = DateTime.UtcNow;
    public static bool TieneHardware(int sensorId) => Ultima.TryGetValue(sensorId, out var t) && DateTime.UtcNow - t < TimeSpan.FromMinutes(2);
}
public record ValorSimuladoInput(decimal? Valor);
public class LecturaInput { public int SensorId { get; set; } public decimal Valor { get; set; } public DateTime? FechaHora { get; set; } }

public static class Endpoints
{
    private static readonly string[] AdminOper = ["Administrador", "Operador"];
    private static readonly string[] Admin = ["Administrador"];
    private static int MinutosSinComunicacion(IConfiguration c) => Math.Max(1, c.GetValue("Status:SinComunicacionMinutos", 5));

    public static void MapSensorEndpoints(this WebApplication app)
    {
        // ------------------------------------------------------------------ ingesta desde dispositivos (Arduino/ESP32)
        // Sin JWT de usuario: el dispositivo se identifica con la clave del header X-Device-Key (variable Devices__ApiKey).
        app.MapPost("/api/sensors/ingest", async (IngestInput input, HttpRequest req, SensorDb db, UpstreamClient up, IConfiguration cfg) =>
        {
            var expected = cfg["Devices:ApiKey"];
            if (string.IsNullOrWhiteSpace(expected) || expected.Length < 16)
                return Results.Json(new { mensaje = "La ingesta de dispositivos no está habilitada." }, statusCode: 503);
            var given = req.Headers["X-Device-Key"].ToString();
            if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(given), Encoding.UTF8.GetBytes(expected)))
                return Results.Json(new { mensaje = "Clave de dispositivo no válida." }, statusCode: 401);

            var items = input.Lecturas ?? new List<IngestItem>();
            if (input.Codigo != null) items.Add(new IngestItem { Codigo = input.Codigo, Valor = input.Valor });
            if (items.Count == 0) throw new ArgumentException("No se enviaron lecturas.");
            if (items.Count > 50) throw new ArgumentException("Máximo 50 lecturas por solicitud.");

            var ahora = DateTime.UtcNow;
            var aceptadas = new List<(LecturaClimatica L, Sensor S)>();
            var rechazadas = new List<object>();
            foreach (var it in items)
            {
                var codigo = it.Codigo?.Trim();
                if (string.IsNullOrEmpty(codigo) || it.Valor is null) { rechazadas.Add(new { codigo, motivo = "Falta codigo o valor." }); continue; }
                if (Math.Abs(it.Valor.Value) > 1_000_000m) { rechazadas.Add(new { codigo, motivo = "Valor fuera de rango." }); continue; }
                var sensor = await db.Sensores.Include(x => x.TipoSensor).FirstOrDefaultAsync(x => x.Codigo == codigo);
                if (sensor == null) { rechazadas.Add(new { codigo, motivo = "Sensor no registrado en el sistema." }); continue; }
                if (!sensor.Estado) { rechazadas.Add(new { codigo, motivo = "Sensor inactivo." }); continue; }
                // La hora la pone el servidor (el Arduino no tiene reloj confiable).
                var l = new LecturaClimatica { SensorId = sensor.SensorId, Valor = Math.Round(it.Valor.Value, 2), FechaHora = ahora };
                db.Lecturas.Add(l); aceptadas.Add((l, sensor));
            }
            if (aceptadas.Count == 0) return Results.BadRequest(new { mensaje = "Ninguna lectura fue aceptada.", rechazadas });
            await db.SaveChangesAsync();
            foreach (var (_, s) in aceptadas) Dispositivos.Marcar(s.SensorId);
            await Evaluacion.EnviarAsync(up, aceptadas.Select(a => Evaluacion.Map(a.L, a.S)));
            return Results.Ok(new { aceptadas = aceptadas.Count, rechazadas, fechaHora = ahora });
        }).AllowAnonymous();

        var g = app.MapGroup("/api/sensors").RequireAuthorization();

        // ------------------------------------------------------------------ tipos
        g.MapGet("/types", async (SensorDb db) => Results.Ok(await db.TiposSensor.AsNoTracking().OrderBy(t => t.TipoSensorId).ToListAsync()));

        // ------------------------------------------------------------------ sensores
        g.MapGet("", async (SensorDb db, CommunityDirectory dir, int? comunidadId, int? tipoSensorId, bool? estado, string? codigo) =>
        {
            var q = db.Sensores.AsNoTracking().Include(s => s.TipoSensor).AsQueryable();
            if (comunidadId.HasValue) q = q.Where(s => s.ComunidadId == comunidadId);
            if (tipoSensorId.HasValue) q = q.Where(s => s.TipoSensorId == tipoSensorId);
            if (estado.HasValue) q = q.Where(s => s.Estado == estado);
            if (!string.IsNullOrWhiteSpace(codigo)) { var c = codigo.Trim(); q = q.Where(s => s.Codigo.Contains(c)); }
            var list = await q.OrderBy(s => s.SensorId).ToListAsync();
            var names = await dir.NamesAsync();
            return Results.Ok(list.Select(s => ToDto(s, names)));
        });

        g.MapGet("/{id:int}", async (int id, SensorDb db, CommunityDirectory dir) =>
        {
            var s = await db.Sensores.AsNoTracking().Include(x => x.TipoSensor).FirstOrDefaultAsync(x => x.SensorId == id);
            return s == null ? Results.NotFound(new { mensaje = "Sensor no encontrado." }) : Results.Ok(ToDto(s, await dir.NamesAsync()));
        });

        g.MapPost("", async (SensorInput i, SensorDb db, CommunityDirectory dir, AuditClient audit) =>
        {
            if (string.IsNullOrWhiteSpace(i.NombreSensor)) throw new ArgumentException("El nombre del sensor es obligatorio.");
            if (string.IsNullOrWhiteSpace(i.Ubicacion)) throw new ArgumentException("La ubicación del sensor es obligatoria.");
            if (i.FechaInstalacion == null) throw new ArgumentException("La fecha de instalación es obligatoria.");
            if (string.IsNullOrWhiteSpace(i.Descripcion)) throw new ArgumentException("La descripción del sensor es obligatoria.");
            await ValidarRefs(db, dir, i);
            var codigo = string.IsNullOrWhiteSpace(i.Codigo) ? $"SEN-{Guid.NewGuid().ToString("N")[..8].ToUpper()}" : i.Codigo.Trim();
            if (await db.Sensores.AnyAsync(x => x.Codigo == codigo)) throw new ConflictException($"Ya existe un sensor con el código {codigo}.");
            var s = new Sensor
            {
                NombreSensor = i.NombreSensor.Trim(), Ubicacion = i.Ubicacion.Trim(), TipoSensorId = i.TipoSensorId, ComunidadId = i.ComunidadId,
                FechaInstalacion = Utc(i.FechaInstalacion), Descripcion = i.Descripcion.Trim(), Codigo = codigo, Estado = true
            };
            db.Sensores.Add(s);
            await db.SaveChangesAsync();
            audit.Registrar("AGREGAR_SENSOR", $"Se agregó el sensor \"{s.NombreSensor}\" ({s.Codigo}) en la ubicación \"{s.Ubicacion}\".", "Sensor", s.SensorId);
            s.TipoSensor = await db.TiposSensor.FindAsync(s.TipoSensorId);
            return Results.Created($"/api/sensors/{s.SensorId}", ToDto(s, await dir.NamesAsync()));
        }).RequireRoles(AdminOper);

        g.MapPut("/{id:int}", async (int id, SensorInput i, SensorDb db, CommunityDirectory dir, AuditClient audit) =>
        {
            if (id != i.SensorId) throw new ArgumentException("El ID del sensor no coincide.");
            var s = await db.Sensores.FirstOrDefaultAsync(x => x.SensorId == id);
            if (s == null) return Results.NotFound(new { mensaje = "Sensor no encontrado." });
            await ValidarRefs(db, dir, i);
            if (!string.IsNullOrWhiteSpace(i.Codigo) && i.Codigo.Trim() != s.Codigo)
            {
                if (await db.Sensores.AnyAsync(x => x.Codigo == i.Codigo.Trim() && x.SensorId != id)) throw new ConflictException($"Ya existe un sensor con el código {i.Codigo.Trim()}.");
                s.Codigo = i.Codigo.Trim();
            }
            (s.NombreSensor, s.Ubicacion, s.TipoSensorId, s.ComunidadId) = (i.NombreSensor, i.Ubicacion, i.TipoSensorId, i.ComunidadId);
            (s.Estado, s.FechaInstalacion, s.Descripcion) = (i.Estado, Utc(i.FechaInstalacion), i.Descripcion);
            await db.SaveChangesAsync();
            audit.Registrar("EDITAR_SENSOR", $"Se modificó el sensor #{id}. Nombre: \"{i.NombreSensor}\", ubicación: \"{i.Ubicacion}\".", "Sensor", id);
            return Results.NoContent();
        }).RequireRoles(AdminOper);

        g.MapPatch("/{id:int}/estado", async (int id, [FromBody] bool activo, SensorDb db, AuditClient audit) =>
        {
            var s = await db.Sensores.FirstOrDefaultAsync(x => x.SensorId == id);
            if (s == null) return Results.NotFound(new { mensaje = "Sensor no encontrado." });
            s.Estado = activo; await db.SaveChangesAsync();
            audit.Registrar("CAMBIAR_ESTADO_SENSOR", $"Se cambió el estado del sensor #{id} a {(activo ? "Activo" : "Inactivo")}.", "Sensor", id);
            return Results.Ok(new { mensaje = "Estado del sensor actualizado correctamente.", sensorId = id, nuevoEstado = activo });
        }).RequireRoles(AdminOper);

        g.MapPatch("/{id:int}/valor-simulado", async (int id, ValorSimuladoInput dto, SensorDb db, AuditClient audit) =>
        {
            var s = await db.Sensores.FirstOrDefaultAsync(x => x.SensorId == id);
            if (s == null) return Results.NotFound(new { mensaje = "Sensor no encontrado." });
            s.ValorSimulado = dto.Valor; await db.SaveChangesAsync();
            audit.Registrar("VALOR_SIMULADO_SENSOR", dto.Valor.HasValue
                ? $"Valor simulado fijado en {dto.Valor.Value:0.##} para el sensor {id}."
                : $"Valor simulado eliminado; el sensor {id} vuelve a modo automático.", "Sensor", id);
            return Results.Ok(new { mensaje = "Valor simulado actualizado.", sensorId = id, valor = dto.Valor });
        }).RequireRoles(Admin);

        g.MapPost("/reiniciar", async (SensorDb db, AuditClient audit) =>
        {
            await db.Sensores.ExecuteUpdateAsync(x => x.SetProperty(s => s.Estado, true));
            audit.Registrar("REINICIAR_SISTEMA", "Se ejecutó el reinicio general del sistema de monitoreo (todos los sensores activados).", "Sensor");
            return Results.Ok(new { mensaje = "Sistema de monitoreo reiniciado con éxito." });
        }).RequireRoles(AdminOper);

        // ------------------------------------------------------------------ agregados (consumidos por Community / Report)
        g.MapGet("/stats/by-community", async (SensorDb db) =>
            Results.Ok(await db.Sensores.AsNoTracking().Where(s => s.ComunidadId != null).GroupBy(s => s.ComunidadId!.Value)
                .Select(x => new SensorCount(x.Key, x.Count())).ToListAsync()));

        g.MapGet("/summary", async (SensorDb db, IConfiguration cfg, int? comunidadId) =>
        {
            var limite = DateTime.UtcNow.AddMinutes(-MinutosSinComunicacion(cfg));
            var q = db.Sensores.AsNoTracking().AsQueryable();
            if (comunidadId.HasValue) q = q.Where(s => s.ComunidadId == comunidadId);
            var activos = await q.CountAsync(s => s.Estado);
            var inactivos = await q.CountAsync(s => !s.Estado);
            var conLectura = await q.Where(s => s.Estado && db.Lecturas.Any(l => l.SensorId == s.SensorId && l.FechaHora >= limite)).CountAsync();
            return Results.Ok(new { sensoresActivos = activos, sensoresInactivos = inactivos, sinComunicacion = activos - conLectura, total = activos + inactivos });
        });

        g.MapGet("/status", async (SensorDb db, CommunityDirectory dir, IConfiguration cfg, int? comunidadId) =>
        {
            var limite = DateTime.UtcNow.AddMinutes(-MinutosSinComunicacion(cfg));
            var q = db.Sensores.AsNoTracking().Include(s => s.TipoSensor).AsQueryable();
            if (comunidadId.HasValue) q = q.Where(s => s.ComunidadId == comunidadId);
            var rows = await q.OrderBy(s => s.SensorId).Select(s => new
            {
                s.SensorId, s.Codigo, s.NombreSensor, s.ComunidadId, s.Estado, Unidad = s.TipoSensor!.UnidadMedida, Tipo = s.TipoSensor!.NombreTipo,
                Ultima = db.Lecturas.Where(l => l.SensorId == s.SensorId).OrderByDescending(l => l.FechaHora)
                    .Select(l => new { l.FechaHora, l.Valor }).FirstOrDefault()
            }).ToListAsync();
            var names = await dir.NamesAsync();
            return Results.Ok(rows.Select(r => new
            {
                sensorId = r.SensorId, codigo = r.Codigo, nombre = r.NombreSensor, tipo = r.Tipo, unidad = r.Unidad, comunidadId = r.ComunidadId,
                comunidadNombre = r.ComunidadId.HasValue ? names.GetValueOrDefault(r.ComunidadId.Value) : null,
                estadoOperativo = !r.Estado ? "Inactivo" : (r.Ultima == null || r.Ultima.FechaHora < limite ? "Sin comunicación" : "Activo"),
                ultimaLectura = r.Ultima?.FechaHora, ultimoValor = r.Ultima?.Valor
            }));
        });

        // ------------------------------------------------------------------ lecturas
        var rd = g.MapGroup("/readings");

        rd.MapGet("", async (SensorDb db, CommunityDirectory dir) => Results.Ok(await Leer(db, dir, db.Lecturas.AsNoTracking(), 100)));
        rd.MapGet("/sensor/{sensorId:int}", async (int sensorId, SensorDb db, CommunityDirectory dir) =>
            Results.Ok(await Leer(db, dir, db.Lecturas.AsNoTracking().Where(l => l.SensorId == sensorId), 2000)));
        rd.MapGet("/community/{comunidadId:int}", async (int comunidadId, SensorDb db, CommunityDirectory dir) =>
            Results.Ok(await Leer(db, dir, db.Lecturas.AsNoTracking().Where(l => l.Sensor!.ComunidadId == comunidadId), 2000)));
        rd.MapGet("/filter", async (SensorDb db, CommunityDirectory dir, int? sensorId, int? comunidadId, string? fechaInicio, string? fechaFin) =>
        {
            var (d, h) = (FechaUtil.Desde(fechaInicio), FechaUtil.Hasta(fechaFin));
            if (d.HasValue && h.HasValue && d > h) throw new ArgumentException("La fecha inicial no puede ser mayor que la fecha final.");
            return Results.Ok(await Leer(db, dir, Filtrar(db, sensorId, comunidadId, null, d, h), 2000));
        });

        rd.MapPost("", async (LecturaInput i, SensorDb db, UpstreamClient up) =>
        {
            var s = await db.Sensores.Include(x => x.TipoSensor).FirstOrDefaultAsync(x => x.SensorId == i.SensorId)
                ?? throw new ArgumentException($"El sensor con Id {i.SensorId} no existe.");
            if (!s.Estado) throw new ArgumentException("El sensor está inactivo; no se pueden registrar lecturas.");
            var l = new LecturaClimatica { SensorId = s.SensorId, Valor = i.Valor, FechaHora = Utc(i.FechaHora) ?? DateTime.UtcNow };
            db.Lecturas.Add(l); await db.SaveChangesAsync();
            await Evaluacion.EnviarAsync(up, [Evaluacion.Map(l, s)]);
            return Results.Created($"/api/sensors/readings/{l.LecturaId}", new { l.LecturaId, l.SensorId, l.Valor, l.FechaHora });
        }).RequireRoles(AdminOper);

        // Paginado para el Report Service (RNF-REP-05).
        rd.MapGet("/export", async (SensorDb db, CommunityDirectory dir, int? sensorId, int? comunidadId, int? tipoSensorId, string? desde, string? hasta,
            int page = 1, int size = 2000) =>
        {
            page = Math.Max(1, page); size = Math.Clamp(size, 1, 5000);
            var q = Filtrar(db, sensorId, comunidadId, tipoSensorId, FechaUtil.Desde(desde), FechaUtil.Hasta(hasta));
            var total = await q.CountAsync();
            var items = await Leer(db, dir, q, size, (page - 1) * size);
            return Results.Ok(new { items, total, page, size });
        });

        rd.MapGet("/stats", async (SensorDb db, string? desde, string? hasta, int? comunidadId) =>
        {
            var q = Filtrar(db, null, comunidadId, null, FechaUtil.Desde(desde), FechaUtil.Hasta(hasta));
            var agg = await q.GroupBy(l => new { l.Sensor!.TipoSensorId, l.Sensor.TipoSensor!.NombreTipo, l.Sensor.TipoSensor.UnidadMedida })
                .Select(x => new { x.Key.TipoSensorId, Tipo = x.Key.NombreTipo, Unidad = x.Key.UnidadMedida, Promedio = x.Average(v => v.Valor),
                    Minimo = x.Min(v => v.Valor), Maximo = x.Max(v => v.Valor), Acumulado = x.Sum(v => v.Valor), Total = x.Count() })
                .OrderBy(x => x.TipoSensorId).ToListAsync();
            // Lluvia acumulada: suma de las lecturas del periodo (el sensor reporta incrementos en mm).
            return Results.Ok(agg.Select(a => new { a.TipoSensorId, a.Tipo, a.Unidad, promedio = Math.Round(a.Promedio, 2), a.Minimo, a.Maximo,
                acumulado = Math.Round(a.Acumulado, 2), a.Total }));
        });

        rd.MapGet("/dashboard", async (SensorDb db, int? comunidadId, int horas = 1) =>
        {
            horas = Math.Clamp(horas, 1, 24);
            var desde = DateTime.UtcNow.AddHours(-horas);
            var tipos = await db.TiposSensor.AsNoTracking().ToListAsync();
            var q = db.Lecturas.AsNoTracking().Where(l => l.FechaHora >= desde && l.Sensor!.Estado);
            if (comunidadId.HasValue) q = q.Where(l => l.Sensor!.ComunidadId == comunidadId);
            var lecturas = await q.Select(l => new { l.SensorId, l.Sensor!.TipoSensorId, l.Valor, l.FechaHora }).ToListAsync();
            var ultimas = lecturas.GroupBy(l => l.SensorId).Select(x => x.OrderByDescending(v => v.FechaHora).First()).ToList();
            var valores = ultimas.GroupBy(l => l.TipoSensorId).Select(x =>
            {
                var t = tipos.FirstOrDefault(y => y.TipoSensorId == x.Key);
                return new { tipoSensorId = x.Key, tipo = t?.NombreTipo ?? "Sensor", unidad = t?.UnidadMedida ?? "", valor = Math.Round(x.Average(v => v.Valor), 2),
                    sensores = x.Count(), fecha = x.Max(v => v.FechaHora) };
            }).OrderBy(v => v.tipoSensorId).ToList();
            var ticks = TimeSpan.FromMinutes(horas <= 1 ? 1 : horas <= 6 ? 5 : 15).Ticks;
            var evolucion = lecturas.GroupBy(l => l.TipoSensorId).Select(x =>
            {
                var t = tipos.FirstOrDefault(y => y.TipoSensorId == x.Key);
                return new { tipoSensorId = x.Key, tipo = t?.NombreTipo ?? "Sensor", unidad = t?.UnidadMedida ?? "",
                    puntos = x.GroupBy(v => v.FechaHora.Ticks / ticks).OrderBy(b => b.Key)
                        .Select(b => new { fecha = DateTime.SpecifyKind(new DateTime(b.Key * ticks), DateTimeKind.Utc), valor = Math.Round(b.Average(v => v.Valor), 2) }).ToList() };
            }).OrderBy(s => s.tipoSensorId).ToList();
            return Results.Ok(new { valoresActuales = valores, evolucion });
        });
    }

    // ---------------------------------------------------------------------- helpers
    private static DateTime? Utc(DateTime? d) =>
        d == null ? null : d.Value.Kind == DateTimeKind.Local ? d.Value.ToUniversalTime() : DateTime.SpecifyKind(d.Value, DateTimeKind.Utc);

    private static IQueryable<LecturaClimatica> Filtrar(SensorDb db, int? sensorId, int? comunidadId, int? tipoSensorId, DateTime? desde, DateTime? hastaExcl)
    {
        var q = db.Lecturas.AsNoTracking().AsQueryable();
        if (sensorId.HasValue) q = q.Where(l => l.SensorId == sensorId);
        if (comunidadId.HasValue) q = q.Where(l => l.Sensor!.ComunidadId == comunidadId);
        if (tipoSensorId.HasValue) q = q.Where(l => l.Sensor!.TipoSensorId == tipoSensorId);
        if (desde.HasValue) q = q.Where(l => l.FechaHora >= desde);
        if (hastaExcl.HasValue) q = q.Where(l => l.FechaHora < hastaExcl);
        return q;
    }

    private static async Task<List<object>> Leer(SensorDb db, CommunityDirectory dir, IQueryable<LecturaClimatica> q, int take, int skip = 0)
    {
        var rows = await q.OrderByDescending(l => l.FechaHora).ThenByDescending(l => l.LecturaId).Skip(skip).Take(take)
            .Select(l => new { l.LecturaId, l.SensorId, l.Valor, l.FechaHora, Codigo = l.Sensor!.Codigo, Nombre = l.Sensor.NombreSensor, l.Sensor.ComunidadId,
                l.Sensor.TipoSensorId, Tipo = l.Sensor.TipoSensor!.NombreTipo, Unidad = l.Sensor.TipoSensor.UnidadMedida, l.Sensor.Estado }).ToListAsync();
        var names = await dir.NamesAsync();
        return rows.Select(l => (object)new
        {
            lecturaId = l.LecturaId, sensorId = l.SensorId, valor = l.Valor, fechaHora = DateTime.SpecifyKind(l.FechaHora, DateTimeKind.Utc),
            unidad = l.Unidad, estadoSensor = l.Estado ? "Activo" : "Inactivo", sensorCodigo = l.Codigo, sensorNombre = l.Nombre,
            comunidadId = l.ComunidadId, comunidadNombre = l.ComunidadId.HasValue ? names.GetValueOrDefault(l.ComunidadId.Value) : null,
            tipoSensorId = l.TipoSensorId, tipo = l.Tipo
        }).ToList();
    }

    private static async Task ValidarRefs(SensorDb db, CommunityDirectory dir, SensorInput i)
    {
        if (!await db.TiposSensor.AnyAsync(t => t.TipoSensorId == i.TipoSensorId))
            throw new ArgumentException($"El tipo de sensor con Id {i.TipoSensorId} no existe.");
        if (i.ComunidadId.HasValue)
        {
            var ok = await dir.ExistsAsync(i.ComunidadId.Value);
            if (ok == null) throw new ArgumentException("No fue posible validar la comunidad (servicio de comunidades no disponible). Intente de nuevo.");
            if (ok == false) throw new ArgumentException($"La comunidad con Id {i.ComunidadId} no existe.");
        }
    }

    private static object ToDto(Sensor s, IReadOnlyDictionary<int, string> names)
    {
        var nombre = s.ComunidadId.HasValue ? names.GetValueOrDefault(s.ComunidadId.Value) : null;
        return new
        {
            sensorId = s.SensorId, nombreSensor = s.NombreSensor, tipoSensorId = s.TipoSensorId, ubicacion = s.Ubicacion, estado = s.Estado, codigo = s.Codigo,
            comunidadId = s.ComunidadId, comunidadNombre = nombre, fechaInstalacion = s.FechaInstalacion, descripcion = s.Descripcion, valorSimulado = s.ValorSimulado,
            tipoSensor = s.TipoSensor == null ? null : new { s.TipoSensor.TipoSensorId, s.TipoSensor.NombreTipo, s.TipoSensor.UnidadMedida },
            comunidad = s.ComunidadId.HasValue ? new { comunidadId = s.ComunidadId.Value, nombre } : null
        };
    }
}
