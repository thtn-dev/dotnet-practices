using System.ComponentModel.DataAnnotations;

namespace MyProject.WebApi.Services.SecretsManager;

public class DopplerOptions
{
    /// <summary>
    /// Doppler token with read access to the project/config containing the secrets
    /// Do not save this token in source code or commit it to version control. Use secure configuration management (e.g., environment variables, secret managers) to provide this token to the application at runtime.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string DopplerToken { get; set; } = string.Empty;
    [Required(AllowEmptyStrings = false)]
    public string ProjectName { get; set; } = string.Empty;
    [Required(AllowEmptyStrings = false)]
    public string ConfigName { get; set; } = string.Empty;
}