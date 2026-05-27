using System.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MyProject.WebApi;
using MyProject.WebApi.Common;
using MyProject.WebApi.Extensions;
using MyProject.WebApi.Modules.Identity;
using MyProject.WebApi.Modules.Identity.Abstractions;
using MyProject.WebApi.Modules.Identity.Services;
using MyProject.WebApi.Services.SecretsManager;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Context;

Serilog.Debugging.SelfLog.Enable(Console.Error);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, config) =>
    config.ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
);
    
    
builder.Services
    .AddOptions<DopplerOptions>()
    .Bind(builder.Configuration.GetSection(nameof(DopplerOptions)))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var dopplerOptions = builder.Configuration.GetSection(nameof(DopplerOptions)).Get<DopplerOptions>()!;

var dopplerSecrets = await DopplerClient.GetSecretsAsync(
    dopplerOptions.DopplerToken,
    dopplerOptions.ProjectName,
    dopplerOptions.ConfigName
);

builder.Services
    .AddOptions<DopplerSecrets>()
    .Configure(dopplerSecrets.CopyTo)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection(nameof(JwtSettings)))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddSingleton<IConfigureOptions<JwtSettings>, ConfigureJwtSettings>();
// Add services to the container.
builder.Services.AddSingleton<IRsaKeyProvider, RsaKeyProvider>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.ConfigureOpenApi();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer();

builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();
builder.Services.AddAuthorization();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/docs", options =>
    {
        options.Title = "Web API";
        options.Theme = ScalarTheme.Purple;

        // Configure authentication
        options.AddPreferredSecuritySchemes("Bearer")
            .AddHttpAuthentication("Bearer", auth => { auth.Token = ""; });
        
        var addresses = app.Configuration["ASPNETCORE_URLS"]
                        ?? app.Configuration["urls"]
                        ?? "http://localhost:16111";
        var serverUrls = addresses.Split(';');
        
        foreach (var url in serverUrls) options.AddServer(new ScalarServer(url.Trim(), "Local Development"));
    });
}
app.UseExceptionHandler(o =>
{
    o.Run(async context =>
    {
        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        if (exceptionFeature is null) return;

        var handler = context.RequestServices.GetRequiredService<GlobalExceptionHandler>();
        await handler.TryHandleAsync(context, exceptionFeature.Error, CancellationToken.None);
    });
});
app.Use(async (context, next) =>
{
    var traceId = Activity.Current?.TraceId.ToString()
                  ?? context.TraceIdentifier;

    using (LogContext.PushProperty("TraceId", traceId))
    {
        await next();
    }
});

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms | TraceId: {TraceId}";

    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("TraceId",
            Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier);
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent);
    };
});
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/", () => Results.Ok(ApiResponse<string>.Ok("Hello world!")))
    .RequireAuthorization()
    .WithSummary("Root endpoint")
    .WithDescription("Returns a simple greeting message.")
    .WithTags("General")
    .Produces<ApiResponse<string>>()
    .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
    .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
    .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

app.MapPost("/auth/login", (IJwtService jwtService) =>
    {
        var rnd = new Random();
        var randomNum = rnd.Next(1000, 9999);
        if (randomNum % 2 == 0)
        {
            throw new ConflictException("Simulated login failure: even random number");
        }
        var token = jwtService.GenerateToken(
            userId: "16112001",
            username: "nathan",
            roles: ["User"]
        );

        return Results.Ok(ApiResponse.Ok(token));
    })
    .AllowAnonymous()
    .WithSummary("User login")
    .WithDescription("Authenticates a user and returns a JWT token.")
    .WithTags("Authentication")
    .Produces<ApiResponse<string>>()
    .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
    .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
    .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

app.Run();

