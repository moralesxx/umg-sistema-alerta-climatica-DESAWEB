using Microsoft.EntityFrameworkCore;

namespace AlertaClimatica.Infrastructure;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
        : base(options)
    {
    }

    // Aquí mapearemos los DbSets (tablas) más adelante
}