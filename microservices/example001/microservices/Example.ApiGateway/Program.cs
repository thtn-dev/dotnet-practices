var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
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
