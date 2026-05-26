namespace MyProject.WebApi.Services.SecretsManager;

public sealed class DopplerSecrets
{
    public string? DatabaseUrl { get; set; }

    public string? PrivateKey { get; set; }
    
    public string? JwtPrivateKey  { get; set; }
    public string? JwtPublicKey { get; set; }

    public static DopplerSecrets FromResponse(DopplerSecretsListResponse response)
    {
        return new DopplerSecrets
        {
            DatabaseUrl = response.Secrets.TryGetValue("DB_URL", out var dbUrl) ? dbUrl.Computed ?? dbUrl.Raw : null,
            PrivateKey = response.Secrets.TryGetValue("PRIVATE_KEY", out var privateKey) ? privateKey.Computed ?? privateKey.Raw : null,
            JwtPrivateKey = response.Secrets.TryGetValue("JWT_PRIVATE_KEY", out var jwtPrivateKey) ? jwtPrivateKey.Computed ?? jwtPrivateKey.Raw : null,
            JwtPublicKey = response.Secrets.TryGetValue("JWT_PUBLIC_KEY", out var jwtPublicKey) ? jwtPublicKey.Computed ?? jwtPublicKey.Raw : null
        };
    }
    
    public void CopyTo(DopplerSecrets target)
    {
        target.JwtPrivateKey = JwtPrivateKey;
        target.JwtPublicKey  = JwtPublicKey;
    }
}