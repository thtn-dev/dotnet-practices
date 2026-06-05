using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using System.Net.Security;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;

var clientCertBytes = File.ReadAllBytes("../certs/service-a.pfx");
var caCertBytes = File.ReadAllBytes("../certs/ca.crt");

var clientCert = X509CertificateLoader.LoadPkcs12(clientCertBytes, "yourpassword");
var caCert = X509CertificateLoader.LoadCertificate(caCertBytes);

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.ServerCertificate = clientCert; // cert service-a
        // AllowCertificate: có cert thì verify, không có thì vẫn cho qua
        httpsOptions.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
        httpsOptions.ClientCertificateValidation = (cert, chain, errors) =>
        {
            if (errors == SslPolicyErrors.None) return true;
            if (errors != SslPolicyErrors.RemoteCertificateChainErrors) return false;

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


// HttpClient gọi Service B — kèm cert
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

if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// ✅ Browser gọi — không cần cert
// Service A gọi Service B rồi trả kết quả về browser
app.MapGet("/test", async (IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("ServiceB");
    var response = await client.GetAsync("/callback");
    if (!response.IsSuccessStatusCode)
        return Results.BadRequest("Failed to call Service B");

    var content = await response.Content.ReadAsByteArrayAsync();
    var json = System.Text.Json.JsonSerializer.Deserialize<object>(content);
    return Results.Ok(json);
});

// ✅ Service B gọi vào đây — yêu cầu cert hợp lệ
app.MapGet("/ping", (HttpContext httpContext) =>
{
    // Lấy identity từ cert — biết đây là service-b
    var caller = httpContext.User.Identity?.Name ?? "unknown";
    return Results.Ok(new { message = $"Pong from Service A, called by {caller}" });
}).RequireAuthorization(); // ← chỉ cho phép nếu có cert hợp lệ

app.Run();
