using AlertaClimatica.Domain;
using Microsoft.EntityFrameworkCore;

namespace AlertaClimatica.Infrastructure;

public static class DbInitializer
{
    // Método principal que se encarga de poblar la base de datos con datos iniciales si está vacía
    public static void Seed(ApplicationDbContext context)
    {
        // 0. Crear la base de datos y sus tablas automáticamente en SQL Server si no existen
        context.Database.EnsureCreated();

        // 1. Verificar si la base de datos ya contiene datos para evitar duplicados
        if (context.Roles.Any() || context.TiposSensor.Any())
        {
            return; // La base de datos ya fue poblada anteriormente
        }

        // 2. Sembrar Roles utilizando las propiedades actualizadas (Nombre en lugar de NombreRol)
        var roles = new[]
        {
            new Rol { Nombre = "Administrador", Descripcion = "Acceso total al sistema y gestión de usuarios" },
            new Rol { Nombre = "Operador", Descripcion = "Gestión y control de sensores y alertas" },
            new Rol { Nombre = "Visualizador", Descripcion = "Acceso únicamente a dashboards y reportes" }
        };
        context.Roles.AddRange(roles);
        context.SaveChanges();

        // 3. Sembrar Usuario Administrador Inicial con Estado booleano (true)
        var adminRole = context.Roles.First(r => r.Nombre == "Administrador");
        var adminUser = new Usuario
        {
            Nombre = "Administrador",
            Apellido = "Del Sistema",
            Email = "admin@alerta.com",
            PasswordHash = "Admin123!",
            IdRol = adminRole.IdRol,
            Estado = true, // Corregido a booleano porque Usuario.Estado es bool
            FechaCreacion = DateTime.Now
        };
        context.Usuarios.Add(adminUser);
        context.SaveChanges();

        // 4. Sembrar Tipos de Sensores Requeridos utilizando 'Nombre' en lugar de 'NombreTipo'
        var tiposSensor = new[]
        {
            new TipoSensor { Nombre = "Temperatura Ambiente", UnidadMedida = "°C", Descripcion = "Medición de temperatura" },
            new TipoSensor { Nombre = "Humedad Relativa", UnidadMedida = "%", Descripcion = "Medición de humedad" },
            new TipoSensor { Nombre = "Velocidad del Viento", UnidadMedida = "km/h", Descripcion = "Medición de viento" },
            new TipoSensor { Nombre = "Nivel de Lluvia", UnidadMedida = "mm", Descripcion = "Medición de precipitaciones" },
            new TipoSensor { Nombre = "Nivel de Río", UnidadMedida = "m", Descripcion = "Medición de caudal de agua" }
        };
        context.TiposSensor.AddRange(tiposSensor);
        context.SaveChanges();

        // 5. Sembrar Sensores Iniciales de Prueba usando Estado de tipo string ("Activo")
        var tempTipo = context.TiposSensor.First(t => t.Nombre.Contains("Temperatura"));
        var humTipo = context.TiposSensor.First(t => t.Nombre.Contains("Humedad"));
        var vientoTipo = context.TiposSensor.First(t => t.Nombre.Contains("Viento"));
        var lluviaTipo = context.TiposSensor.First(t => t.Nombre.Contains("Lluvia"));
        var rioTipo = context.TiposSensor.First(t => t.Nombre.Contains("Río"));

        var sensores = new[]
        {
            new Sensor { Codigo = "SEN-01", Nombre = "Sensor Temp - Comunidad Norte", IdTipoSensor = tempTipo.IdTipoSensor, Ubicacion = "Comunidad Norte", Estado = true },
            new Sensor { Codigo = "SEN-02", Nombre = "Sensor Humedad - Comunidad Norte", IdTipoSensor = humTipo.IdTipoSensor, Ubicacion = "Comunidad Norte", Estado = true },
            new Sensor { Codigo = "SEN-03", Nombre = "Estación de Viento - Sector Central", IdTipoSensor = vientoTipo.IdTipoSensor, Ubicacion = "Sector Central", Estado = true },
            new Sensor { Codigo = "SEN-04", Nombre = "Pluviómetro - Cuenca Sur", IdTipoSensor = lluviaTipo.IdTipoSensor, Ubicacion = "Cuenca Sur", Estado = true },
            new Sensor { Codigo = "SEN-05", Nombre = "Medidor de Río - Puente Principal", IdTipoSensor = rioTipo.IdTipoSensor, Ubicacion = "Puente Principal", Estado = true }
        };
        context.Sensores.AddRange(sensores);
        context.SaveChanges();
    }
}