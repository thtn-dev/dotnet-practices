using System.Net.Security;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Server.Kestrel.Https;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();
builder.Services.AddCertificateForwarding(options => { });
builder.Services.AddAuthentication();

builder.Services.AddAuthorization();

builder.Services.AddHttpClient("ServiceA", client =>
{
    client.BaseAddress = new Uri("http://localhost:5100"); // port Service A
});

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

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


app.MapGet("/callback", async (IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("ServiceA");
    var response = await client.GetAsync("/ping");
    if (!response.IsSuccessStatusCode)
        return Results.BadRequest("Failed to call Service A /ping");

    var content = await response.Content.ReadAsStringAsync();
    return Results.Ok(new { from = "service-b", pingResult = content });
});

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
