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
var jwtSettings = builder.Configuration.GetSection(nameof(JwtSettings)).Get<JwtSettings>()!;
jwtSettings.PrivateKey = dopplerSecrets.JwtPrivateKey ?? "";
jwtSettings.PublicKey = dopplerSecrets.JwtPublicKey ?? "";

// Add services to the container.
builder.Services.AddSingleton<IRsaKeyProvider, RsaKeyProvider>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddSingleton(Options.Create(jwtSettings));
builder.Services.AddSingleton(Options.Create(dopplerSecrets));
builder.Services.ConfigureOpenApi();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer();

builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();
builder.Services.AddScoped<IJwtService, JwtService>();
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
app.Run();

