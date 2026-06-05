using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<DaprInvoker>();

var app = builder.Build();

if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.MapDefaultEndpoints();

app.MapGet("/test", async (DaprInvoker dapr, CancellationToken cancellationToken) =>
{
    var result = await dapr.GetAsync("worker-service", "callback", cancellationToken);

    return Results.Ok(result);
});

app.MapGet("/ping", () =>
    Results.Ok(new { message = "Pong from Service A" }));

app.Run();

sealed class DaprInvoker
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(10) };

    public async Task<JsonElement> GetAsync(string appId, string method, CancellationToken cancellationToken)
    {
        var daprEndpoint = ResolveDaprEndpoint();
        var requestUri = $"{daprEndpoint}/v1.0/invoke/{Uri.EscapeDataString(appId)}/method/{method.TrimStart('/')}";

        using var response = await Client.GetAsync(requestUri, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(content);
    }

    private static string ResolveDaprEndpoint()
    {
        var endpoint = Environment.GetEnvironmentVariable("DAPR_HTTP_ENDPOINT");
        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            return endpoint.TrimEnd('/');
        }

        var port = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT");
        return string.IsNullOrWhiteSpace(port)
            ? "http://127.0.0.1:3500"
            : $"http://127.0.0.1:{port}";
    }
}
