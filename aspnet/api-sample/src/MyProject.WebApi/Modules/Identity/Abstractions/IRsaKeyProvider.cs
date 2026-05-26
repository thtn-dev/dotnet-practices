using Microsoft.IdentityModel.Tokens;

namespace MyProject.WebApi.Modules.Identity.Abstractions;

public interface IRsaKeyProvider
{
    RsaSecurityKey PrivateKey { get; }
    RsaSecurityKey PublicKey { get; }
}