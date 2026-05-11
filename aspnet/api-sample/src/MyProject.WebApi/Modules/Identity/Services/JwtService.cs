using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MyProject.WebApi.Modules.Identity.Abstractions;
using MyProject.WebApi.Settings;

namespace MyProject.WebApi.Modules.Identity.Services;

public class JwtService(IOptions<JwtSettings> options) : IJwtService
{
    public string GenerateToken(string userId, string username, List<string> roles, List<Claim>? customClaims = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        // Add custom claims if provided
        if (customClaims is { Count: > 0 }) claims.AddRange(customClaims);
        var rsa = RSA.Create();
        rsa.ImportFromPem("example-api-secret-key");
        var securityKey = new RsaSecurityKey(rsa);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);

        var header = new JwtHeader(credentials);
        var payload = new JwtPayload(
            "webapi",
           "webapi",
            claims,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMinutes(60)
        );
        var token = new JwtSecurityToken(header, payload);
        
        var handler = new JwtSecurityTokenHandler();
        var tokenString = handler.WriteToken(token);
        return tokenString;
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem("example-api-secret-key");
        var pub = new RsaSecurityKey(rsa) { KeyId = "key-019282834" };
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "webapi",
            ValidAudience = "webapi",
            IssuerSigningKey = pub,
            ValidateLifetime = false // Don't validate expiration for a refresh token scenario
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.RsaSha256,
                    StringComparison.InvariantCultureIgnoreCase))
                return null;

            return principal;
        }
        catch
        {
            return null;
        }
    }
}