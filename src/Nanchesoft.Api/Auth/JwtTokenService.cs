using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Nanchesoft.Api.Auth;

public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, string username, string email, Guid tenantId, bool isPlatformOwner);
    string GenerateRefreshToken();
    int ExpiryMinutes { get; }
}

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiryMinutes;

    public JwtTokenService(IConfiguration configuration)
    {
        _key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Falta la configuración Jwt:Key. Configúrala como variable de entorno Jwt__Key.");
        _issuer = configuration["Jwt:Issuer"] ?? "Nanchesoft";
        _audience = configuration["Jwt:Audience"] ?? "NanchesoftApp";
        _expiryMinutes = int.TryParse(configuration["Jwt:ExpiryMinutes"], out var m) ? m : 720;
    }

    public int ExpiryMinutes => _expiryMinutes;

    public string GenerateAccessToken(Guid userId, string username, string email, Guid tenantId, bool isPlatformOwner)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("username", username),
            new Claim("email", email ?? string.Empty),
            new Claim("tenantId", tenantId.ToString()),
            new Claim("isPlatformOwner", isPlatformOwner ? "true" : "false")
        };

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
