using AlertaClimatica.Domain;

namespace AlertaClimatica.Infrastructure;

public static class DbInitializer
{
    public static void Seed(ApplicationDbContext context)
    {
        // 0. Crear la base de datos y sus tablas automáticamente en SQL Server si no existen
        context.Database.EnsureCreated();

        // 1. Verificar si la base de datos ya contiene datos
        if (context.Roles.Any() || context.TiposSensor.Any())
        {
            return; // La base de datos ya fue poblada anteriormente
        }

        // 2. Sembrar Roles
        var roles = new[]
        {
            new Rol { NombreRol = "Administrador", Descripcion = "Acceso total al sistema y gestión de usuarios" },
            new Rol { NombreRol = "Operador", Descripcion = "Gestión y control de sensores y alertas" },
            new Rol { NombreRol = "Visualizador", Descripcion = "Acceso únicamente a dashboards y reportes" }
        };
        context.Roles.AddRange(roles);
        context.SaveChanges();

        // 3. Sembrar Usuario Administrador Inicial
        var adminRole = context.Roles.First(r => r.NombreRol == "Administrador");
        var adminUser = new Usuario
        {
            Nombre = "Administrador del Sistema",
            Correo = "admin@alerta.com",
            ContraseniaHash = "Admin123!", // En la Fase 2 integraremos BCrypt para encriptación
            RolId = adminRole.RolId,
            Estado = true,
            FechaCreacion = DateTime.Now
        };
        context.Usuarios.Add(adminUser);

        // 4. Sembrar Tipos de Sensores Requeridos
        var tiposSensor = new[]
        {
            new TipoSensor { NombreTipo = "Temperatura Ambiente", UnidadMedida = "°C" },
            new TipoSensor { NombreTipo = "Humedad Relativa", UnidadMedida = "%" },
            new TipoSensor { NombreTipo = "Velocidad del Viento", UnidadMedida = "km/h" },
            new TipoSensor { NombreTipo = "Nivel de Lluvia", UnidadMedida = "mm" },
            new TipoSensor { NombreTipo = "Nivel de Río", UnidadMedida = "m" }
        };
        context.TiposSensor.AddRange(tiposSensor);
        context.SaveChanges();

        // 5. Sembrar Sensores Iniciales de Prueba
        var tempTipo = context.TiposSensor.First(t => t.NombreTipo.Contains("Temperatura"));
        var humTipo = context.TiposSensor.First(t => t.NombreTipo.Contains("Humedad"));
        var vientoTipo = context.TiposSensor.First(t => t.NombreTipo.Contains("Viento"));
        var lluviaTipo = context.TiposSensor.First(t => t.NombreTipo.Contains("Lluvia"));
        var rioTipo = context.TiposSensor.First(t => t.NombreTipo.Contains("Río"));

        var sensores = new[]
        {
            new Sensor { NombreSensor = "Sensor Temp - Comunidad Norte", TipoSensorId = tempTipo.TipoSensorId, Ubicacion = "Comunidad Norte", Estado = true },
            new Sensor { NombreSensor = "Sensor Humedad - Comunidad Norte", TipoSensorId = humTipo.TipoSensorId, Ubicacion = "Comunidad Norte", Estado = true },
            new Sensor { NombreSensor = "Estación de Viento - Sector Central", TipoSensorId = vientoTipo.TipoSensorId, Ubicacion = "Sector Central", Estado = true },
            new Sensor { NombreSensor = "Pluviómetro - Cuenca Sur", TipoSensorId = lluviaTipo.TipoSensorId, Ubicacion = "Cuenca Sur", Estado = true },
            new Sensor { NombreSensor = "Medidor de Río - Puente Principal", TipoSensorId = rioTipo.TipoSensorId, Ubicacion = "Puente Principal", Estado = true }
        };
        context.Sensores.AddRange(sensores);
        context.SaveChanges();
    }
}