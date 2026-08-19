using Microsoft.EntityFrameworkCore;
using AlertaClimatica.Domain;

namespace AlertaClimatica.Infrastructure;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
        : base(options)
    {
    }

    public DbSet<Rol> Roles { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<TipoSensor> TiposSensor { get; set; }
    public DbSet<Sensor> Sensores { get; set; }
    public DbSet<LecturaClimatica> LecturasClimaticas { get; set; }
    public DbSet<Evento> Eventos { get; set; }
    public DbSet<Alerta> Alertas { get; set; }
    public DbSet<Bitacora> Bitacora { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Mapeo directo usando las claves primarias reales de la base de datos
        modelBuilder.Entity<Sensor>().ToTable("Sensores").HasKey(s => s.SensorId);
        modelBuilder.Entity<Alerta>().ToTable("Alertas").HasKey(a => a.AlertaId);
        modelBuilder.Entity<Rol>().ToTable("Roles").HasKey(r => r.RolId);
        modelBuilder.Entity<Usuario>().ToTable("Usuarios").HasKey(u => u.UsuarioId);
        modelBuilder.Entity<TipoSensor>().ToTable("TiposSensor").HasKey(t => t.TipoSensorId);
        modelBuilder.Entity<LecturaClimatica>().ToTable("LecturasClimaticas").HasKey(l => l.LecturaId);
        modelBuilder.Entity<Evento>().ToTable("Eventos").HasKey(e => e.EventoId);
        modelBuilder.Entity<Bitacora>().ToTable("Bitacora").HasKey(b => b.BitacoraId);
    }
}