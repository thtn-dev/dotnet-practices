using System.Net.Http.Headers;
using System.Text.Json;

namespace MyProject.WebApi.Services.SecretsManager;

public static class DopplerClient
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("https://api.doppler.com/")
    };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<DopplerSecretsListResponse> ListSecretsAsync(
        string dopplerToken,
        string projectName,
        string configName,
        bool includeDynamicSecrets = false,
        int? dynamicSecretsTtlSec = null,
        IEnumerable<string>? secrets = null,
        bool includeManagedSecrets = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dopplerToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectName);
        ArgumentException.ThrowIfNullOrWhiteSpace(configName);

        if (!includeDynamicSecrets && dynamicSecretsTtlSec is not null)
        {
            throw new ArgumentException("dynamicSecretsTtlSec can only be used when includeDynamicSecrets is true.", nameof(dynamicSecretsTtlSec));
        }

        var query = new Dictionary<string, string?>
        {
            ["project"] = projectName,
            ["config"] = configName,
            ["include_dynamic_secrets"] = includeDynamicSecrets ? "true" : null,
            ["dynamic_secrets_ttl_sec"] = dynamicSecretsTtlSec?.ToString(),
            ["secrets"] = secrets is null ? null : string.Join(",", secrets.Where(secret => !string.IsNullOrWhiteSpace(secret))),
            ["include_managed_secrets"] = includeManagedSecrets ? "true" : "false"
        };

        var requestUri = BuildUri("v3/configs/config/secrets", query);
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", dopplerToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await Http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException(
                $"Failed to list Doppler secrets. StatusCode={(int)response.StatusCode} ({response.ReasonPhrase}). {errorContent}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var payload = await JsonSerializer.DeserializeAsync<DopplerSecretsListResponse>(stream, JsonOptions, cancellationToken)
            .ConfigureAwait(false);

        return payload ?? throw new InvalidOperationException("Failed to deserialize Doppler secrets response.");
    }

    public static async Task<DopplerSecrets> GetSecretsAsync(
        string dopplerToken,
        string projectName,
        string configName,
        CancellationToken cancellationToken = default)
    {
        var response = await ListSecretsAsync(
            dopplerToken,
            projectName,
            configName,
            secrets: ["DB_URL", "PRIVATE_KEY", "JWT_PRIVATE_KEY", "JWT_PUBLIC_KEY"],
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return DopplerSecrets.FromResponse(response);
    }

    private static Uri BuildUri(string path, IReadOnlyDictionary<string, string?> queryParameters)
    {
        var query = string.Join(
            "&",
            queryParameters
                .Where(parameter => !string.IsNullOrWhiteSpace(parameter.Value))
                .Select(parameter => $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value!)}"));

        return new Uri(Http.BaseAddress!, string.IsNullOrEmpty(query) ? path : $"{path}?{query}");
    }
}

public sealed class DopplerSecretsListResponse
{
    public Dictionary<string, DopplerSecretValue> Secrets { get; set; } = [];

    public bool TryGetSecret(string secretName, out DopplerSecretValue? secret)
    {
        return Secrets.TryGetValue(secretName, out secret);
    }
}

public sealed record DopplerSecretValue(
    string? Raw,
    string? Computed);
