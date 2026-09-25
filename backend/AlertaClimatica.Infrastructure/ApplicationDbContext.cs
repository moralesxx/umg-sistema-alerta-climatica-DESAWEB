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

        // Mapeo y configuración estricta para IDENTITY (autoincremental de 1 en 1)
        modelBuilder.Entity<Rol>()
            .ToTable("Roles")
            .HasKey(r => r.RolId);
        modelBuilder.Entity<Rol>().Property(r => r.RolId).ValueGeneratedOnAdd();

        modelBuilder.Entity<Usuario>()
            .ToTable("Usuarios")
            .HasKey(u => u.UsuarioId);
        modelBuilder.Entity<Usuario>().Property(u => u.UsuarioId).ValueGeneratedOnAdd();

        modelBuilder.Entity<TipoSensor>()
            .ToTable("TiposSensor")
            .HasKey(t => t.TipoSensorId);
        modelBuilder.Entity<TipoSensor>().Property(t => t.TipoSensorId).ValueGeneratedOnAdd();

        modelBuilder.Entity<Sensor>()
            .ToTable("Sensores")
            .HasKey(s => s.SensorId);
        modelBuilder.Entity<Sensor>().Property(s => s.SensorId).ValueGeneratedOnAdd();

        modelBuilder.Entity<LecturaClimatica>()
            .ToTable("LecturasClimaticas")
            .HasKey(l => l.LecturaId);
        modelBuilder.Entity<LecturaClimatica>().Property(l => l.LecturaId).ValueGeneratedOnAdd();
        modelBuilder.Entity<LecturaClimatica>()
            .Property(l => l.Valor)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Evento>()
            .ToTable("Eventos")
            .HasKey(e => e.EventoId);
        modelBuilder.Entity<Evento>().Property(e => e.EventoId).ValueGeneratedOnAdd();

        modelBuilder.Entity<Alerta>()
            .ToTable("Alertas")
            .HasKey(a => a.AlertaId);
        modelBuilder.Entity<Alerta>().Property(a => a.AlertaId).ValueGeneratedOnAdd();

        modelBuilder.Entity<Bitacora>()
            .ToTable("Bitacora")
            .HasKey(b => b.BitacoraId);
        modelBuilder.Entity<Bitacora>().Property(b => b.BitacoraId).ValueGeneratedOnAdd();
    }
}