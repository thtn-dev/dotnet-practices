using Microsoft.IdentityModel.Tokens;
using MyProject.WebApi.Modules.Identity.Abstractions;
using MyProject.WebApi.Modules.Identity.Models;

namespace MyProject.WebApi.Modules.Identity.Services;

public class KeyManagementService : IKeyManagementService
{
    public Task<RsaKeyInfo> RotateKey(int keySize = 2048)
    {
        throw new NotImplementedException();
    }

    public RsaSecurityKey GetCurrentPrivateKey()
    {
        throw new NotImplementedException();
    }

    public RsaSecurityKey GetCurrentPublicKey()
    {
        throw new NotImplementedException();
    }

    public List<RsaSecurityKey> GetAllPublicKeys()
    {
        throw new NotImplementedException();
    }

    public KeyStore GetKeyStore()
    {
        throw new NotImplementedException();
    }

    public string GetCurrentKeyId()
    {
        throw new NotImplementedException();
    }
}