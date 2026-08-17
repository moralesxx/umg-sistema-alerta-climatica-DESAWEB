-- database/seed/seed.sql
-- Carga de datos de prueba (Seed Data)

USE AlertaClimaticaDB;
GO

-- 1. Insertar Roles
INSERT INTO dbo.Roles (Nombre, Descripcion) VALUES
('Administrador', 'Acceso total a la configuración, sensores y usuarios'),
('Operador', 'Monitoreo de alertas y gestión de eventos climáticos'),
('Consultor', 'Solo lectura de reportes y dashboard');

-- 2. Insertar Usuarios de Prueba (Passwords en hash genérico para desarrollo)
INSERT INTO dbo.Usuarios (IdRol, Nombre, Apellido, Email, PasswordHash, Estado) VALUES
(1, 'Admin', 'Sistema', 'admin@clima.com', '$2a$11$e/y8g.3m9...hash_ejemplo_admin', 1),
(2, 'Carlos', 'Gómez', 'carlos.operador@clima.com', '$2a$11$e/y8g.3m9...hash_ejemplo_operador', 1),
(3, 'María', 'López', 'maria.consultor@clima.com', '$2a$11$e/y8g.3m9...hash_ejemplo_consultor', 1);

-- 3. Insertar Tipos de Sensores
INSERT INTO dbo.TiposSensor (Nombre, UnidadMedida, Descripcion) VALUES
('Termómetro', '°C', 'Sensor de temperatura ambiental'),
('Higrómetro', '%', 'Sensor de humedad relativa'),
('Pluviómetro', 'mm', 'Sensor de medición de precipitación pluvial'),
('Anemómetro', 'km/h', 'Sensor de velocidad del viento');

-- 4. Insertar Sensores de Prueba (Ubicaciones en Guatemala)
INSERT INTO dbo.Sensores (Codigo, Nombre, IdTipoSensor, Ubicacion, Latitud, Longitud, Estado) VALUES
('SEN-TEMP-001', 'Sensor Temp - Zona Central', 1, 'Ciudad de Guatemala - Zona 10', 14.598200, -90.509000, 1),
('SEN-PLUV-001', 'Pluviómetro - Costa Sur', 3, 'Escuintla - Finca El Rosario', 14.300900, -90.785800, 1),
('SEN-ANEM-001', 'Anemómetro - Altiplano', 4, 'Quetzaltenango - Valle de Palajunoj', 14.834700, -91.518100, 1),
('SEN-HUM-001', 'Higrómetro - Norte', 2, 'Petén - Flores', 16.923800, -89.893100, 1);

-- 5. Insertar Lecturas Climáticas Iniciales
INSERT INTO dbo.LecturasClimaticas (IdSensor, Valor, FechaHora) VALUES
(1, 24.5, DATEADD(MINUTE, -60, GETDATE())),
(1, 26.2, DATEADD(MINUTE, -30, GETDATE())),
(1, 28.0, GETDATE()),
(2, 45.2, DATEADD(MINUTE, -45, GETDATE())),
(2, 82.0, GETDATE()), -- Precipitación alta
(3, 15.4, GETDATE());

-- 6. Insertar Evento de Prueba
INSERT INTO dbo.Eventos (Nombre, Descripcion, NivelRiesgo, FechaInicio, Estado) VALUES
('Tormenta Tropical Alfa', 'Sistema de baja presión afectando el pacífico y costa sur.', 'Alto', GETDATE(), 'Activo');

-- 7. Insertar Alerta de Prueba
INSERT INTO dbo.Alertas (IdSensor, IdEvento, TipoAlerta, Mensaje, Nivel, Atendida) VALUES
(2, 1, 'Lluvia Intensa', 'Superado el umbral crítico de precipitación pluvial (80mm). Riesgo de inundación.', 'Critica', 0);

-- 8. Registrar Bitácora Inicial
INSERT INTO dbo.Bitacora (IdUsuario, Accion, TablaAfectada, Detalle, IpOrigen) VALUES
(1, 'SYSTEM_INIT', 'DATABASE', 'Carga inicial de scripts DDL y Data Seed ejecutada correctamente.', '127.0.0.1');

PRINT '--> Carga inicial de datos (Seed) completada con éxito.';
GO