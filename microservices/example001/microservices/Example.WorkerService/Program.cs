using System.Net.Security;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Server.Kestrel.Https;

var serverCertBytes = File.ReadAllBytes("../certs/service-b.pfx");
var caCertBytes = File.ReadAllBytes("../certs/ca.crt");

var serverCert = X509CertificateLoader.LoadPkcs12(serverCertBytes, "yourpassword");
var caCert = X509CertificateLoader.LoadCertificate(caCertBytes);

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.ServerCertificate = serverCert;
        httpsOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
        httpsOptions.ClientCertificateValidation = (cert, chain, errors) =>
        {
            if (errors != SslPolicyErrors.None &&
                errors != SslPolicyErrors.RemoteCertificateChainErrors)
                return false;

            var chain2 = new X509Chain();
            chain2.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain2.ChainPolicy.CustomTrustStore.Add(caCert);
            chain2.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            return chain2.Build(new X509Certificate2(cert));
        };
    });
});

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

builder.Services.AddAuthorization();

// ✅ HttpClient gọi ngược lại Service A — kèm cert service-b
builder.Services.AddHttpClient("ServiceA", client =>
{
    client.BaseAddress = new Uri("https://localhost:7165"); // port Service A
}).ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    handler.ClientCertificates.Add(serverCert); // gửi cert service-b
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

// ✅ Endpoint nhận request từ Service A, gọi ngược /ping rồi trả về
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

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
