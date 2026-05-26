using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MyProject.WebApi.Modules.Identity.Abstractions;

namespace MyProject.WebApi.Modules.Identity.Services;

public sealed class ConfigureJwtBearerOptions(
    IConfiguration configuration,
    IRsaKeyProvider keyProvider)
    : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(JwtBearerOptions options)
    {
        Configure(JwtBearerDefaults.AuthenticationScheme, options);
    }

    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name != JwtBearerDefaults.AuthenticationScheme)
        {
            return;
        }

        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = configuration["JwtSettings:Issuer"],
            ValidAudience = configuration["JwtSettings:Audience"],

            IssuerSigningKey = keyProvider.PublicKey,

            ValidAlgorithms =
            [
                SecurityAlgorithms.RsaSsaPssSha256
            ],

            ClockSkew = TimeSpan.FromMinutes(1)
        };
    }
}