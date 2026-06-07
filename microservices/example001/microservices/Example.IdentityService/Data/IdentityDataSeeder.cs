using Example.IdentityService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Example.IdentityService.Data;

public sealed class IdentityDataSeeder(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IOpenIddictApplicationManager applicationManager)
{
    public async Task SeedAsync()
    {
        await dbContext.Database.EnsureCreatedAsync();

        if (await userManager.FindByNameAsync("admin") is null)
        {
            var result = await userManager.CreateAsync(new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@example.local",
                EmailConfirmed = true
            }, "Pass123$");

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not seed admin user: {string.Join(", ", result.Errors.Select(error => error.Description))}");
            }
        }

        if (await applicationManager.FindByClientIdAsync("example-client") is null)
        {
            await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "example-client",
                ClientSecret = "example-secret",
                DisplayName = "Example client",
                Permissions =
                {
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.ClientCredentials,
                    Permissions.GrantTypes.Password,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.Prefixes.Scope + "api"
                }
            });
        }
    }
}
