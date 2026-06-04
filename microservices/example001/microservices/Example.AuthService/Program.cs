using Microsoft.AspNetCore.Authentication.Certificate;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;

var clientCertBytes = File.ReadAllBytes("../certs/service-a.pfx");
var caCertBytes = File.ReadAllBytes("../certs/ca.crt");

var clientCert = X509CertificateLoader.LoadPkcs12(
    clientCertBytes,
    "yourpassword"
);
var caCert = X509CertificateLoader.LoadCertificate(caCertBytes);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddCertificateForwarding(options => { });
builder.Services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
    .AddCertificate(options =>
    {
        options.AllowedCertificateTypes = CertificateTypes.All;
        options.RevocationMode = X509RevocationMode.NoCheck;

        options.ChainTrustValidationMode = X509ChainTrustMode.CustomRootTrust;
        options.CustomTrustStore.Add(caCert);
        options.Events = new CertificateAuthenticationEvents
        {
            OnCertificateValidated = context =>
            {
                var cn = context.ClientCertificate.GetNameInfo(X509NameType.SimpleName, false);

                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, cn),
                    new Claim(ClaimTypes.NameIdentifier, cn)
                };
                context.Principal = new ClaimsPrincipal(
                    new ClaimsIdentity(claims, context.Scheme.Name));
                context.Success();
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddHttpClient("ServiceB", client =>
{
    client.BaseAddress = new Uri("https://localhost:7217");
}).ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();

    handler.ClientCertificates.Add(clientCert);

    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
    {
        if (cert == null) return false;

        var customChain = new X509Chain();
        customChain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        customChain.ChainPolicy.CustomTrustStore.Add(caCert);
        customChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

        return customChain.Build(new X509Certificate2(cert));
    };

    return handler;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.MapGet("/test", async (IHttpClientFactory factory) =>
    {
        var client = factory.CreateClient("ServiceB");
        var response = await client.GetAsync("/weatherforecast");
        if (!response.IsSuccessStatusCode)
            return Results.BadRequest("Failed to call Service B");

        var content = await response.Content.ReadAsByteArrayAsync();
        var json = System.Text.Json.JsonSerializer.Deserialize<object>(content);
        return Results.Ok(json);
    });

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}