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

        // Mapeo directo de nombres de tablas y columnas exactas
        modelBuilder.Entity<Sensor>().ToTable("Sensores").HasKey(s => s.IdSensor);
        modelBuilder.Entity<Alerta>().ToTable("Alertas").HasKey(a => a.IdAlerta);
        
        modelBuilder.Entity<Rol>().ToTable("Roles");
        modelBuilder.Entity<Usuario>().ToTable("Usuarios");
        modelBuilder.Entity<TipoSensor>().ToTable("TiposSensor");
        modelBuilder.Entity<LecturaClimatica>().ToTable("LecturasClimaticas");
        modelBuilder.Entity<Evento>().ToTable("Eventos");
        modelBuilder.Entity<Bitacora>().ToTable("Bitacora");
    }
}