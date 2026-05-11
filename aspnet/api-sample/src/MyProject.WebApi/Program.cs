using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MyProject.WebApi.Common;
using MyProject.WebApi.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.ConfigureOpenApi();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "webapi",
            ValidAudience = "webapi",
            IssuerSigningKey = new SymmetricSecurityKey("example-api-secret-key"u8.ToArray())
        };
    });
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
