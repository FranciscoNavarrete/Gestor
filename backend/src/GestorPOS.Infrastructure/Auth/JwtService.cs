using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GestorPOS.Application.Common.Interfaces;
using GestorPOS.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GestorPOS.Infrastructure.Auth;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime ExpiraUtc) GenerarToken(Usuario usuario, IEnumerable<string> featuresHabilitadas)
    {
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Falta configurar Jwt:Key.");
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];
        var expiraMinutos = int.TryParse(_configuration["Jwt:ExpiraMinutos"], out var m) ? m : 60 * 8;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Email),
            new("tenant_id", usuario.TenantId.ToString()),
            new("nombre", usuario.Nombre),
            new(ClaimTypes.Role, usuario.Rol.ToString())
        };
        claims.AddRange(featuresHabilitadas.Select(f => new Claim("feature", f)));

        var expiraUtc = DateTime.UtcNow.AddMinutes(expiraMinutos);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiraUtc,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiraUtc);
    }
}
