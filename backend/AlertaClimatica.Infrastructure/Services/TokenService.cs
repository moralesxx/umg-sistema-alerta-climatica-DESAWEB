using AlertaClimatica.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AlertaClimatica.Infrastructure.Services;

// RF-ADM-05 y objetivo #19 de la Fase 2 ("La autenticación deberá estar
// desacoplada de la lógica principal de negocio"): todo lo relacionado
// a generar el JWT vive aislado aquí. Antes era un método privado
// dentro de UsuariosController, mezclado con el resto de la lógica.
public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerarToken(Usuario usuario, string nombreRol)
    {
        var jwtSettings = _configuration.GetSection("Jwt");

        var key = jwtSettings["Key"];
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];

        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("La configuración Jwt:Key no está definida.");

        if (string.IsNullOrWhiteSpace(issuer))
            throw new InvalidOperationException("La configuración Jwt:Issuer no está definida.");

        if (string.IsNullOrWhiteSpace(audience))
            throw new InvalidOperationException("La configuración Jwt:Audience no está definida.");

        var expirationMinutes = 60;
        if (int.TryParse(jwtSettings["ExpirationMinutes"], out var configuredExpiration))
        {
            expirationMinutes = configuredExpiration;
        }

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.UsuarioId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Correo),
            new Claim(ClaimTypes.Name, usuario.Nombre),
            new Claim(ClaimTypes.Role, nombreRol),
            new Claim("rolId", usuario.RolId.ToString())
        };

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}