using Microsoft.Extensions.Options;
using MyProject.WebApi.Services.SecretsManager;

namespace MyProject.WebApi.Modules.Identity;

public class JwtSettings
{
    public string PrivateKey { get; set; } = string.Empty; // PEM private key
    public string PublicKey { get; set; } = string.Empty;  // PEM public key
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpiryMinutes { get; set; } = 60;
}

public class ConfigureJwtSettings(IOptions<DopplerSecrets> secrets) : IConfigureOptions<JwtSettings>
{
    private readonly DopplerSecrets _secrets = secrets.Value;

    public void Configure(JwtSettings options)
    {
        options.PrivateKey = _secrets.JwtPrivateKey ?? "";
        options.PublicKey  = _secrets.JwtPublicKey  ?? "";
    }
}