using System.Security.Claims;
using Example.IdentityService.Data;
using Example.IdentityService.Endpoints;
using Example.IdentityService.Endpoints.OAuth2;
using Example.IdentityService.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();
builder.Services.AddAuthorization();
builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseInMemoryDatabase("identity");
    options.UseOpenIddict();
});

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddOpenIddict()
    .AddCore(options => options.UseEntityFrameworkCore().UseDbContext<ApplicationDbContext>())
    .AddServer(options =>
    {
        options.SetTokenEndpointUris("/connect/token");
        options.AllowClientCredentialsFlow();
        options.AllowPasswordFlow();
        options.AllowRefreshTokenFlow();

        options.RegisterScopes("api");
        options.AddDevelopmentEncryptionCertificate();
        options.AddDevelopmentSigningCertificate();

        options.UseAspNetCore()
            .EnableTokenEndpointPassthrough();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
});

builder.Services.AddScoped<IdentityDataSeeder>();
builder.Services.RegisterEndpointsFromAssemblyContaining<ConnectTokenEndpoint>();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapEndpoints();

app.MapGet("/api/me", (ClaimsPrincipal user) => Results.Ok(new
{
    subject = user.FindFirstValue(Claims.Subject),
    name = user.FindFirstValue(Claims.Name),
    claims = user.Claims.Select(claim => new { claim.Type, claim.Value })
})).RequireAuthorization();

app.MapGet("/ping", () => Results.Ok(new { message = "Pong from Identity Service" }));

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<IdentityDataSeeder>().SeedAsync();
}

app.Run();
