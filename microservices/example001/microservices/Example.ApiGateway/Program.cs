using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

var gatewayCertBytes = File.ReadAllBytes("../certs/service-a.pfx");
var caCertBytes = File.ReadAllBytes("../certs/ca.crt");

var gatewayCert = X509CertificateLoader.LoadPkcs12(gatewayCertBytes, "yourpassword");
var caCert = X509CertificateLoader.LoadCertificate(caCertBytes);

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .ConfigureHttpClient((context, handler) =>
    {
        handler.SslOptions.RemoteCertificateValidationCallback = (_, cert, _, errors) =>
        {
            if (cert is null) return false;
            if (errors == SslPolicyErrors.None) return true;
            if (errors != SslPolicyErrors.RemoteCertificateChainErrors) return false;

            using var customChain = new X509Chain();
            customChain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            customChain.ChainPolicy.CustomTrustStore.Add(caCert);
            customChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            return customChain.Build(new X509Certificate2(cert));
        };

        if (context.ClusterId is "auth-cluster" or "worker-cluster")
        {
            handler.SslOptions.ClientCertificates = new X509CertificateCollection
            {
                gatewayCert
            };
        }
    });

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    service = "Example.ApiGateway",
    routes = new[]
    {
        "/auth/{**catch-all}",
        "/worker/{**catch-all}"
    }
}));

app.MapReverseProxy();

app.Run();
