using System.Security.Claims;

namespace MyProject.WebApi.Modules.Identity.Abstractions;

public interface IJwtService
{
    string GenerateToken(string userId, string username, List<string> roles, List<Claim>? customClaims = null);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}