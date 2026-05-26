using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MyProject.WebApi.Modules.Identity.Abstractions;

namespace MyProject.WebApi.Modules.Identity.Services;

public class RsaKeyProvider : IRsaKeyProvider, IAsyncDisposable
{
    private readonly RSA _privateRsa;
    private readonly RSA _publicRsa;

    public RsaKeyProvider(IOptions<JwtSettings> options)
    {
        var settings = options.Value;

        _privateRsa = RSA.Create();
        _privateRsa.ImportFromPem(settings.PrivateKey);

        _publicRsa = RSA.Create();
        _publicRsa.ImportFromPem(settings.PublicKey);

        var keyId = ComputeKeyId(_publicRsa);

        PrivateKey = new RsaSecurityKey(_privateRsa) { KeyId = keyId };
        PublicKey  = new RsaSecurityKey(_publicRsa)  { KeyId = keyId };
    }

    public RsaSecurityKey PrivateKey { get; }
    public RsaSecurityKey PublicKey  { get; }

    private static string ComputeKeyId(RSA rsa)
    {
        var pubBytes = rsa.ExportRSAPublicKey();
        var hash = SHA256.HashData(pubBytes);
        return Convert.ToBase64String(hash)[..16];
    }


    public async ValueTask DisposeAsync()
    {
        await CastAndDispose(_privateRsa);
        await CastAndDispose(_publicRsa);

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
                await resourceAsyncDisposable.DisposeAsync();
            else
                resource.Dispose();
        }
    }
}