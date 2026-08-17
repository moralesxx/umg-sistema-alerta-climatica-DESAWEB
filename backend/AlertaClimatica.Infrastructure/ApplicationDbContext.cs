using Microsoft.EntityFrameworkCore;
using AlertaClimatica.Domain;

namespace AlertaClimatica.Infrastructure;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
        : base(options)
    {
    }

    // Mapeo exacto con los nombres de tablas en SQL Server (SSMS)
    public DbSet<Rol> Roles { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<TipoSensor> TiposSensor { get; set; } // Nombre exacto en DB: TiposSensor
    public DbSet<Sensor> Sensores { get; set; }
    public DbSet<LecturaClimatica> LecturasClimaticas { get; set; }
    public DbSet<Evento> Eventos { get; set; }
    public DbSet<Alerta> Alertas { get; set; }
    public DbSet<Bitacora> Bitacora { get; set; } // Nombre exacto en DB: Bitacora
}