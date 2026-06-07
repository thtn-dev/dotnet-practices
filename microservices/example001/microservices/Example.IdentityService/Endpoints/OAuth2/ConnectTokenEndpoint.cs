using System.Collections.Immutable;
using System.Security.Claims;
using Example.IdentityService.Models;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Example.IdentityService.Endpoints.OAuth2;

public sealed class ConnectTokenEndpoint : IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapPost("/connect/token", ExchangeAsync)
            .DisableAntiforgery()
            .ExcludeFromDescription();
    }

    private static async Task<IResult> ExchangeAsync(
        HttpContext context,
        UserManager<ApplicationUser> userManager)
    {
        var request = context.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request is unavailable.");

        if (request.IsPasswordGrantType())
        {
            return await HandlePasswordAsync(request, userManager);
        }

        if (request.IsClientCredentialsGrantType())
        {
            return HandleClientCredentials(request);
        }

        if (request.IsRefreshTokenGrantType())
        {
            return await HandleRefreshTokenAsync(context, request, userManager);
        }

        return Results.BadRequest(new OpenIddictResponse
        {
            Error = Errors.UnsupportedGrantType,
            ErrorDescription = "The specified grant type is not supported."
        });
    }

    private static async Task<IResult> HandlePasswordAsync(
        OpenIddictRequest request,
        UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.FindByNameAsync(request.Username!);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password!))
        {
            return InvalidGrant("Invalid username or password.");
        }

        var identity = CreateIdentity();
        await SetUserClaimsAsync(identity, user, userManager);

        var principal = CreatePrincipal(identity, request.GetScopes());
        SetClaimDestinations(identity);

        return SignIn(principal);
    }

    private static IResult HandleClientCredentials(OpenIddictRequest request)
    {
        var identity = CreateIdentity();
        identity.SetClaim(Claims.Subject, request.ClientId);
        identity.SetClaim(Claims.Name, request.ClientId);

        var principal = CreatePrincipal(identity, request.GetScopes());
        SetClaimDestinations(identity);

        return SignIn(principal);
    }

    private static async Task<IResult> HandleRefreshTokenAsync(
        HttpContext context,
        OpenIddictRequest request,
        UserManager<ApplicationUser> userManager)
    {
        var result = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var userId = result.Principal?.GetClaim(Claims.Subject);

        if (!result.Succeeded || string.IsNullOrEmpty(userId))
        {
            return InvalidGrant("The refresh token is invalid.");
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return InvalidGrant("The refresh token is no longer valid.");
        }

        var identity = CreateIdentity(result.Principal!.Claims);
        await SetUserClaimsAsync(identity, user, userManager);

        var scopes = request.GetScopes();
        var principal = CreatePrincipal(
            identity,
            scopes.IsDefaultOrEmpty ? result.Principal.GetScopes() : scopes);

        SetClaimDestinations(identity);

        return SignIn(principal);
    }

    private static ClaimsIdentity CreateIdentity(IEnumerable<Claim>? claims = null)
    {
        return claims is null
            ? new ClaimsIdentity(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                Claims.Name,
                Claims.Role)
            : new ClaimsIdentity(
                claims,
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                Claims.Name,
                Claims.Role);
    }

    private static async Task SetUserClaimsAsync(
        ClaimsIdentity identity,
        ApplicationUser user,
        UserManager<ApplicationUser> userManager)
    {
        identity.SetClaim(Claims.Subject, await userManager.GetUserIdAsync(user));
        identity.SetClaim(Claims.Name, await userManager.GetUserNameAsync(user));
        identity.SetClaim(Claims.Email, await userManager.GetEmailAsync(user));
        identity.SetClaims(Claims.Role, (await userManager.GetRolesAsync(user)).ToImmutableArray());
    }

    private static ClaimsPrincipal CreatePrincipal(ClaimsIdentity identity, IEnumerable<string> scopes)
    {
        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(scopes);
        return principal;
    }

    private static void SetClaimDestinations(ClaimsIdentity identity)
    {
        identity.SetDestinations(static claim => claim.Type switch
        {
            Claims.Name when claim.Subject!.HasScope(Scopes.Profile) =>
                [Destinations.AccessToken, Destinations.IdentityToken],
            Claims.Email when claim.Subject!.HasScope(Scopes.Email) =>
                [Destinations.AccessToken, Destinations.IdentityToken],
            _ => [Destinations.AccessToken]
        });
    }

    private static IResult SignIn(ClaimsPrincipal principal)
    {
        return Results.SignIn(
            principal,
            authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static IResult InvalidGrant(string description)
    {
        return Results.Forbid(
            authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme],
            properties: new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
            }));
    }
}
