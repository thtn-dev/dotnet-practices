using System.ComponentModel.DataAnnotations;
using MyProject.WebApi.Common;

namespace MyProject.WebApi.Settings;

public class JwtSettings : IBaseSettings
{
    public const string SectionName = "Jwt";

    [Required] public string Issuer { get; set; } = string.Empty;

    [Required] public string Audience { get; set; } = string.Empty;

    public string KeyStorePath { get; set; } = "Keys/keystore.json";
    public int ExpirationMinutes { get; set; } = 60;
}