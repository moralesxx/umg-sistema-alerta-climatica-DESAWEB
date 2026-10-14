using AlertaClimatica.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace AlertaClimatica.Infrastructure;

public static class DbInitializer
{
    public static void Seed(ApplicationDbContext context, string? adminPassword = null)
    {
        // 0. Crear la base de datos y sus tablas automáticamente en SQL Server si no existen
        context.Database.EnsureCreated();

        // 1. Verificar si la base de datos ya contiene datos para evitar duplicados
        if (context.Roles.Any() || context.TiposSensor.Any())
        {
            // La base ya estaba poblada: solo se completan los tipos que falten
            // (instalaciones anteriores no tenían reservorio, humo ni presión).
            SembrarTiposAdicionales(context);
            return;
        }

        // Sin contraseña inicial no se crea el administrador (RNF-ADM-07: nada de credenciales en el código).
        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new InvalidOperationException(
                "La base de datos está vacía y falta la contraseña inicial del administrador. " +
                "Defina la variable de entorno Seed__AdminPassword.");
        }

        // 2. Sembrar Roles (RF-ADM-06: nombres exactos según el PDF de Fase 2)
        var roles = new[]
        {
            new Rol { NombreRol = "Administrador", Descripcion = "Acceso total al sistema y gestión de usuarios" },
            new Rol { NombreRol = "Operador", Descripcion = "Gestión y control de sensores y alertas" },
            new Rol { NombreRol = "Usuario de consulta", Descripcion = "Acceso únicamente a dashboards y reportes" }
        };
        context.Roles.AddRange(roles);
        context.SaveChanges();

        // 3. Sembrar Usuario Administrador Inicial
        var adminRole = context.Roles.First(r => r.NombreRol == "Administrador");
        var passwordHasher = new PasswordHasher<Usuario>();

        var adminUser = new Usuario
        {
            Nombre = "Administrador Del Sistema",
            Correo = "admin@alerta.com",
            RolId = adminRole.RolId,
            Estado = true,
            FechaCreacion = DateTime.Now
        };

        adminUser.ContraseniaHash =
        passwordHasher.HashPassword(adminUser, adminPassword);
        context.Usuarios.Add(adminUser);
        context.SaveChanges();

        // 4. Sembrar Tipos de Sensores
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
            new Sensor { Codigo = "SEN-01", NombreSensor = "Sensor Temp - Comunidad Norte", TipoSensorId = tempTipo.TipoSensorId, Ubicacion = "Comunidad Norte", Estado = true },
            new Sensor { Codigo = "SEN-02", NombreSensor = "Sensor Humedad - Comunidad Norte", TipoSensorId = humTipo.TipoSensorId, Ubicacion = "Comunidad Norte", Estado = true },
            new Sensor { Codigo = "SEN-03", NombreSensor = "Estación de Viento - Sector Central", TipoSensorId = vientoTipo.TipoSensorId, Ubicacion = "Sector Central", Estado = true },
            new Sensor { Codigo = "SEN-04", NombreSensor = "Pluviómetro - Cuenca Sur", TipoSensorId = lluviaTipo.TipoSensorId, Ubicacion = "Cuenca Sur", Estado = true },
            new Sensor { Codigo = "SEN-05", NombreSensor = "Medidor de Río - Puente Principal", TipoSensorId = rioTipo.TipoSensorId, Ubicacion = "Puente Principal", Estado = true }
        };
        context.Sensores.AddRange(sensores);
        context.SaveChanges();

        SembrarTiposAdicionales(context);
    }

    // RF-ADM-15: tipos de sensor que pide el enunciado además de los cinco
    // iniciales. Se agregan solo si no existen (se puede ejecutar muchas veces).
    private static void SembrarTiposAdicionales(ApplicationDbContext context)
    {
        var adicionales = new[]
        {
            new TipoSensor { NombreTipo = "Nivel de Reservorio", UnidadMedida = "m" },
            new TipoSensor { NombreTipo = "Humo / Incendio", UnidadMedida = "ppm" },
            new TipoSensor { NombreTipo = "Presión Atmosférica", UnidadMedida = "hPa" }
        };

        var existentes = context.TiposSensor.Select(t => t.NombreTipo).ToList();
        var faltantes = adicionales.Where(a => !existentes.Contains(a.NombreTipo)).ToList();

        if (faltantes.Count > 0)
        {
            context.TiposSensor.AddRange(faltantes);
            context.SaveChanges();
        }
    }
}