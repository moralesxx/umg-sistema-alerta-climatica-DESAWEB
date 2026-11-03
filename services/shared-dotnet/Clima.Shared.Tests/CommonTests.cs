using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Clima.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Clima.Shared.Tests;

/// <summary>Pruebas de la lógica compartida por los servicios .NET (fechas de filtro y token de servicio).</summary>
public class FechaUtilTests
{
    // APP_TZ por defecto = America/Guatemala (UTC-6, sin horario de verano).
    [Fact]
    public void FechaSola_Desde_EsInicioDelDiaLocalEnUtc() =>
        Assert.Equal(new DateTime(2026, 10, 24, 6, 0, 0, DateTimeKind.Utc), FechaUtil.Desde("2026-10-24"));

    [Fact]
    public void FechaSola_Hasta_EsLimiteExclusivoDelDiaSiguiente() =>
        Assert.Equal(new DateTime(2026, 10, 25, 6, 0, 0, DateTimeKind.Utc), FechaUtil.Hasta("2026-10-24"));

    [Fact]
    public void FechaHoraSinOffset_SeInterpretaEnHoraLocal() =>
        Assert.Equal(new DateTime(2026, 10, 24, 16, 30, 0, DateTimeKind.Utc), FechaUtil.Desde("2026-10-24T10:30"));

    [Fact]
    public void InstanteConZ_SeRespeta()
    {
        Assert.Equal(new DateTime(2026, 10, 24, 10, 30, 0, DateTimeKind.Utc), FechaUtil.Desde("2026-10-24T10:30:00Z"));
        Assert.Equal(new DateTime(2026, 10, 24, 10, 30, 0, 1, DateTimeKind.Utc), FechaUtil.Hasta("2026-10-24T10:30:00Z"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Vacio_DevuelveNull(string? valor)
    {
        Assert.Null(FechaUtil.Desde(valor));
        Assert.Null(FechaUtil.Hasta(valor));
    }

    [Fact]
    public void FechaInvalida_LanzaArgumentException() =>
        Assert.Throws<ArgumentException>(() => FechaUtil.Desde("no-es-una-fecha"));
}

public class ServiceTokenTests
{
    [Fact]
    public void TokenDeServicio_TieneRolServicio_YEsValido()
    {
        var key = new string('k', 40);
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = key }).Build();
        var token = new ServiceTokenProvider(cfg, new ServiceInfo("prueba")).Get();

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = "AlertaClimatica.Auth",
            ValidateAudience = true, ValidAudience = "AlertaClimatica.Client",
            ValidateLifetime = true, ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        }, out _);

        Assert.Equal("Servicio", principal.FindFirst("role")?.Value);
        Assert.Equal("svc:prueba", principal.FindFirst("sub")?.Value);
    }

    [Fact]
    public void TokenDeServicio_SeReutilizaMientrasNoVence()
    {
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = new string('k', 40) }).Build();
        var p = new ServiceTokenProvider(cfg, new ServiceInfo("x"));
        Assert.Equal(p.Get(), p.Get());
    }
}
