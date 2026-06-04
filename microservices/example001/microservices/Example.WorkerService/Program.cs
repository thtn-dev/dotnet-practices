
using System.Net.Security;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Server.Kestrel.Https;

var serverCertBytes = File.ReadAllBytes("../certs/service-b.pfx");
var caCertBytes = File.ReadAllBytes("../certs/ca.crt");

var serverCert = X509CertificateLoader.LoadPkcs12(
    serverCertBytes,
    "yourpassword"
);
var caCert = X509CertificateLoader.LoadCertificate(caCertBytes);

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5001, listenOptions =>
    {
        listenOptions.UseHttps(httpsOptions =>
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
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddCertificateForwarding(options => { });
builder.Services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
    .AddCertificate(options =>
    {
        options.AllowedCertificateTypes = CertificateTypes.All;
        options.RevocationMode = X509RevocationMode.NoCheck;
        options.Events = new CertificateAuthenticationEvents
        {
            OnCertificateValidated = context =>
            {
                // CN của cert chính là identity của service
                var cn = context.ClientCertificate.GetNameInfo(X509NameType.SimpleName, false);
                Console.WriteLine($"Request from: {cn}"); // "service-a"

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}