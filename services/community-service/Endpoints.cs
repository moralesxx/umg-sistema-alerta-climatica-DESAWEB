using Clima.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunitySvc;

public static class Endpoints
{
    private static readonly string[] Admin = ["Administrador"];

    public static void MapCommunityEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/communities").RequireAuthorization();

        g.MapGet("", async (CommunityDb db, UpstreamClient up, string? busqueda, bool? estado, string? municipio, string? departamento) =>
        {
            var q = db.Comunidades.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var t = busqueda.Trim().ToLower();
                q = q.Where(c => c.Nombre.ToLower().Contains(t) || c.Municipio.ToLower().Contains(t) || c.Departamento.ToLower().Contains(t));
            }
            if (estado.HasValue) q = q.Where(c => c.Estado == estado.Value);
            if (!string.IsNullOrWhiteSpace(municipio)) { var m = municipio.Trim().ToLower(); q = q.Where(c => c.Municipio.ToLower() == m); }
            if (!string.IsNullOrWhiteSpace(departamento)) { var d = departamento.Trim().ToLower(); q = q.Where(c => c.Departamento.ToLower() == d); }
            var lista = await q.OrderBy(c => c.Nombre).ToListAsync();
            var counts = await Counts(up);
            return Results.Ok(lista.Select(c => ToDto(c, counts.GetValueOrDefault(c.ComunidadId))));
        });

        g.MapGet("/{id:int}", async (int id, CommunityDb db, UpstreamClient up) =>
        {
            var c = await db.Comunidades.AsNoTracking().FirstOrDefaultAsync(x => x.ComunidadId == id);
            if (c == null) return Results.NotFound(new { mensaje = "Comunidad no encontrada." });
            return Results.Ok(ToDto(c, (await Counts(up)).GetValueOrDefault(id)));
        });

        g.MapPost("", async (ComunidadInput dto, CommunityDb db, AuditClient audit) =>
        {
            Validar(dto);
            var (n, m, d) = (dto.Nombre.Trim(), dto.Municipio.Trim(), dto.Departamento.Trim());
            if (await Duplicada(db, n, m, d, null))
                throw new ConflictException("Ya existe una comunidad con ese nombre en el mismo municipio y departamento.");
            var c = new Comunidad { Nombre = n, Municipio = m, Departamento = d, Pais = dto.Pais.Trim(), Latitud = dto.Latitud, Longitud = dto.Longitud, Descripcion = dto.Descripcion!.Trim(), Estado = dto.Estado };
            db.Comunidades.Add(c);
            await db.SaveChangesAsync();
            audit.Registrar("CREAR_COMUNIDAD", $"El usuario creó la comunidad #{c.ComunidadId} \"{c.Nombre}\" ({c.Municipio}, {c.Departamento}).", "Comunidad", c.ComunidadId);
            return Results.Created($"/api/communities/{c.ComunidadId}", ToDto(c, 0));
        }).RequireRoles(Admin);

        g.MapPut("/{id:int}", async (int id, ComunidadInput dto, CommunityDb db, AuditClient audit) =>
        {
            var c = await db.Comunidades.FirstOrDefaultAsync(x => x.ComunidadId == id);
            if (c == null) return Results.NotFound(new { mensaje = "Comunidad no encontrada." });
            Validar(dto);
            var (n, m, d) = (dto.Nombre.Trim(), dto.Municipio.Trim(), dto.Departamento.Trim());
            if (await Duplicada(db, n, m, d, id))
                throw new ConflictException("Ya existe otra comunidad con ese nombre en el mismo municipio y departamento.");
            (c.Nombre, c.Municipio, c.Departamento, c.Pais) = (n, m, d, dto.Pais.Trim());
            (c.Latitud, c.Longitud, c.Descripcion) = (dto.Latitud, dto.Longitud, dto.Descripcion!.Trim());
            await db.SaveChangesAsync();
            audit.Registrar("EDITAR_COMUNIDAD", $"El usuario modificó la comunidad #{id} \"{n}\" ({m}, {d}).", "Comunidad", id);
            return Results.NoContent();
        }).RequireRoles(Admin);

        g.MapPatch("/{id:int}/estado", async (int id, [FromBody] bool activa, CommunityDb db, AuditClient audit) =>
        {
            var c = await db.Comunidades.FirstOrDefaultAsync(x => x.ComunidadId == id);
            if (c == null) return Results.NotFound(new { mensaje = "Comunidad no encontrada." });
            if (c.Estado != activa)
            {
                c.Estado = activa;
                await db.SaveChangesAsync();
                audit.Registrar(activa ? "ACTIVAR_COMUNIDAD" : "DESACTIVAR_COMUNIDAD",
                    $"El usuario {(activa ? "activó" : "desactivó")} la comunidad #{id} \"{c.Nombre}\".", "Comunidad", id);
            }
            return Results.Ok(new { mensaje = "Estado de la comunidad actualizado correctamente.", comunidadId = id, nuevoEstado = activa });
        }).RequireRoles(Admin);
    }

    // Cantidad de sensores: se pregunta a la API del Sensor Service (no a su base de datos).
    private static async Task<Dictionary<int, int>> Counts(UpstreamClient up) =>
        (await up.TryGetAsync<List<SensorCount>>("Sensor", "/api/sensors/stats/by-community"))?.ToDictionary(x => x.ComunidadId, x => x.Total)
        ?? new Dictionary<int, int>();

    private static Task<bool> Duplicada(CommunityDb db, string n, string m, string d, int? exceptoId) =>
        db.Comunidades.AnyAsync(c => c.Nombre.ToLower() == n.ToLower() && c.Municipio.ToLower() == m.ToLower()
            && c.Departamento.ToLower() == d.ToLower() && (exceptoId == null || c.ComunidadId != exceptoId));

    private static ComunidadDto ToDto(Comunidad c, int sensores) => new()
    {
        ComunidadId = c.ComunidadId, Nombre = c.Nombre, Municipio = c.Municipio, Departamento = c.Departamento, Pais = c.Pais,
        Latitud = c.Latitud, Longitud = c.Longitud, Descripcion = c.Descripcion, Estado = c.Estado, CantidadSensores = sensores
    };

    private static void Validar(ComunidadInput d)
    {
        if (string.IsNullOrWhiteSpace(d.Nombre)) throw new ArgumentException("El nombre de la comunidad es obligatorio.");
        if (string.IsNullOrWhiteSpace(d.Municipio)) throw new ArgumentException("El municipio es obligatorio.");
        if (string.IsNullOrWhiteSpace(d.Departamento)) throw new ArgumentException("El departamento es obligatorio.");
        if (string.IsNullOrWhiteSpace(d.Pais)) throw new ArgumentException("El país es obligatorio.");
        if (d.Latitud == null) throw new ArgumentException("La latitud es obligatoria.");
        if (d.Longitud == null) throw new ArgumentException("La longitud es obligatoria.");
        if (d.Latitud < -90 || d.Latitud > 90) throw new ArgumentException("La latitud debe estar entre -90 y 90.");
        if (d.Longitud < -180 || d.Longitud > 180) throw new ArgumentException("La longitud debe estar entre -180 y 180.");
        if (string.IsNullOrWhiteSpace(d.Descripcion)) throw new ArgumentException("La descripción es obligatoria.");
    }
}
