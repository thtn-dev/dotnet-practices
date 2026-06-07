using System.Security.Claims;
using Example.IdentityService.Data;
using Example.IdentityService.Endpoints;
using Example.IdentityService.Extensions;
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
builder.Services.ConfigureIdentity();
builder.Services.RegisterNpgSqlDbContexts<ApplicationDbContext>(
    builder.Configuration.GetConnectionString("DefaultConnection") ??
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found."));
builder.Services.ConfigureOpenIddict(builder.Configuration);
builder.Services.ConfigureQuartz();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
});
builder.Services.AddAuthorization();
var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapEndpoints();
app.Run();