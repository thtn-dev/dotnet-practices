using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MyProject.WebApi.Common;
using MyProject.WebApi.Extensions;
using MyProject.WebApi.Modules.Identity;
using MyProject.WebApi.Modules.Identity.Abstractions;
using MyProject.WebApi.Modules.Identity.Services;
using MyProject.WebApi.Services.SecretsManager;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

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

        var token = jwtService.GenerateToken(
            userId: "16112001",
            username: "nathan",
            roles: new List<string> { "User" }
        );

        return Results.Ok(ApiResponse<string>.Ok(token));
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

