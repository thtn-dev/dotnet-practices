using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<DaprInvoker>();

var app = builder.Build();

if (app.Environment.IsDevelopment()) app.MapOpenApi();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapDefaultEndpoints();

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast(
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        )).ToArray();
    return forecast;
}).WithName("GetWeatherForecast");

app.MapGet("/callback", async (DaprInvoker dapr, CancellationToken cancellationToken) =>
{
    var pingResult = await dapr.GetAsync("auth-service", "ping", cancellationToken);

    return Results.Ok(new { from = "worker-service", pingResult });
});

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

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
