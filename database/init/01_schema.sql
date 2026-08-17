-- Active: 1786989173917@@127.0.0.1@1433
-- database/init/01_schema.sql
-- Creación del esquema para el Sistema de Alertas Climáticas (SQL Server)

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'AlertaClimaticaDB')
BEGIN
    CREATE DATABASE AlertaClimaticaDB;
END
GO

USE AlertaClimaticaDB;
GO

-- 1. TABLA: Roles
IF OBJECT_ID('dbo.Roles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles (
        IdRol INT IDENTITY(1,1) PRIMARY KEY,
        Nombre VARCHAR(50) NOT NULL UNIQUE,
        Descripcion VARCHAR(200) NULL,
        FechaCreacion DATETIME2 DEFAULT GETDATE()
    );
END;

-- 2. TABLA: Usuarios
IF OBJECT_ID('dbo.Usuarios', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Usuarios (
        IdUsuario INT IDENTITY(1,1) PRIMARY KEY,
        IdRol INT NOT NULL,
        Nombre VARCHAR(100) NOT NULL,
        Apellido VARCHAR(100) NOT NULL,
        Email VARCHAR(150) NOT NULL UNIQUE,
        PasswordHash VARCHAR(255) NOT NULL,
        Estado BIT DEFAULT 1, -- 1: Activo, 0: Inactivo
        FechaCreacion DATETIME2 DEFAULT GETDATE(),
        CONSTRAINT FK_Usuarios_Roles FOREIGN KEY (IdRol) REFERENCES dbo.Roles(IdRol)
    );
END;

-- 3. TABLA: TiposSensor
IF OBJECT_ID('dbo.TiposSensor', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TiposSensor (
        IdTipoSensor INT IDENTITY(1,1) PRIMARY KEY,
        Nombre VARCHAR(50) NOT NULL, -- Ej: Temperatura, Humedad, Pluviómetro
        UnidadMedida VARCHAR(20) NOT NULL, -- Ej: °C, %, mm, hPa
        Descripcion VARCHAR(200) NULL
    );
END;

-- 4. TABLA: Sensores
IF OBJECT_ID('dbo.Sensores', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sensores (
        IdSensor INT IDENTITY(1,1) PRIMARY KEY,
        Codigo VARCHAR(50) NOT NULL UNIQUE,
        Nombre VARCHAR(100) NOT NULL,
        IdTipoSensor INT NOT NULL,
        Ubicacion VARCHAR(200) NOT NULL,
        Latitud DECIMAL(9, 6) NULL,
        Longitud DECIMAL(9, 6) NULL,
        Estado BIT DEFAULT 1, -- 1: Operativo, 0: Mantenimiento/Inactivo
        FechaInstalacion DATETIME2 DEFAULT GETDATE(),
        CONSTRAINT FK_Sensores_TiposSensor FOREIGN KEY (IdTipoSensor) REFERENCES dbo.TiposSensor(IdTipoSensor)
    );
END;

-- 5. TABLA: LecturasClimaticas
IF OBJECT_ID('dbo.LecturasClimaticas', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LecturasClimaticas (
        IdLectura BIGINT IDENTITY(1,1) PRIMARY KEY,
        IdSensor INT NOT NULL,
        Valor DECIMAL(10, 2) NOT NULL,
        FechaHora DATETIME2 DEFAULT GETDATE(),
        CONSTRAINT FK_Lecturas_Sensores FOREIGN KEY (IdSensor) REFERENCES dbo.Sensores(IdSensor)
    );
    
    -- Índice para acelerar consultas por rango de fecha y sensor
    CREATE INDEX IX_Lecturas_IdSensor_FechaHora ON dbo.LecturasClimaticas(IdSensor, FechaHora DESC);
END;

-- 6. TABLA: Eventos
IF OBJECT_ID('dbo.Eventos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Eventos (
        IdEvento INT IDENTITY(1,1) PRIMARY KEY,
        Nombre VARCHAR(100) NOT NULL,
        Descripcion VARCHAR(500) NULL,
        NivelRiesgo VARCHAR(20) NOT NULL CHECK (NivelRiesgo IN ('Bajo', 'Moderado', 'Alto', 'Critico')),
        FechaInicio DATETIME2 NOT NULL,
        FechaFin DATETIME2 NULL,
        Estado VARCHAR(20) DEFAULT 'Activo' CHECK (Estado IN ('Activo', 'Finalizado', 'Cancelado'))
    );
END;

-- 7. TABLA: Alertas
IF OBJECT_ID('dbo.Alertas', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Alertas (
        IdAlerta INT IDENTITY(1,1) PRIMARY KEY,
        IdSensor INT NOT NULL,
        IdEvento INT NULL, -- Opcional: una alerta puede o no asociarse a un evento general
        TipoAlerta VARCHAR(50) NOT NULL, -- Ej: Umbral Superado, Mantenimiento
        Mensaje VARCHAR(500) NOT NULL,
        Nivel VARCHAR(20) NOT NULL CHECK (Nivel IN ('Informativa', 'Preventiva', 'Critica')),
        Atendida BIT DEFAULT 0,
        FechaEmision DATETIME2 DEFAULT GETDATE(),
        CONSTRAINT FK_Alertas_Sensores FOREIGN KEY (IdSensor) REFERENCES dbo.Sensores(IdSensor),
        CONSTRAINT FK_Alertas_Eventos FOREIGN KEY (IdEvento) REFERENCES dbo.Eventos(IdEvento)
    );
END;

-- 8. TABLA: Bitacora (Auditoría)
IF OBJECT_ID('dbo.Bitacora', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Bitacora (
        IdBitacora BIGINT IDENTITY(1,1) PRIMARY KEY,
        IdUsuario INT NULL, -- NULL si fue una acción del sistema/worker automático
        Accion VARCHAR(100) NOT NULL, -- Ej: LOGIN, CREATE_USER, DELETE_SENSOR
        TablaAfectada VARCHAR(50) NULL,
        Detalle NVARCHAR(MAX) NULL,
        IpOrigen VARCHAR(45) NULL,
        FechaHora DATETIME2 DEFAULT GETDATE(),
        CONSTRAINT FK_Bitacora_Usuarios FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuarios(IdUsuario) ON DELETE SET NULL
    );
END;
GO