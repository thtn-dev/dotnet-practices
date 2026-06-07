using System.Security.Claims;
using OpenIddict.Abstractions;

namespace Example.IdentityService.Helpers.OAuth;

public interface IOpenIddictClaimsPrincipalHandler
{
    Task HandleAsync(OpenIddictClaimsPrincipalHandlerContext context);
}

public sealed class OpenIddictClaimsPrincipalHandlerContext(
    IServiceProvider scopeServiceProvider,
    OpenIddictRequest openIddictRequest,
    ClaimsPrincipal principal)
{
    public IServiceProvider ScopeServiceProvider { get; } = scopeServiceProvider;

    public OpenIddictRequest OpenIddictRequest { get; } = openIddictRequest;

    public ClaimsPrincipal Principal { get; } = principal;
}

public sealed class DefaultOpenIddictClaimsPrincipalHandler : IOpenIddictClaimsPrincipalHandler
{
    public Task HandleAsync(OpenIddictClaimsPrincipalHandlerContext context)
    {
        context.Principal.SetDestinations(GetDestinations);
        return Task.CompletedTask;
    }

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        // Note: by default, claims are NOT automatically included in the access and identity tokens.
        // To allow OpenIddict to serialize them, you must attach them a destination, that specifies
        // whether they should be included in access tokens, in identity tokens or in both.

        switch (claim.Type)
        {
            case OpenIddictConstants.Claims.Name or OpenIddictConstants.Claims.PreferredUsername:
                yield return OpenIddictConstants.Destinations.AccessToken;

                if (claim.Subject != null && claim.Subject.HasScope(OpenIddictConstants.Permissions.Scopes.Profile))
                    yield return OpenIddictConstants.Destinations.IdentityToken;

                yield break;

            case OpenIddictConstants.Claims.Email:
                yield return OpenIddictConstants.Destinations.AccessToken;

                if (claim.Subject != null && claim.Subject.HasScope(OpenIddictConstants.Permissions.Scopes.Email))
                    yield return OpenIddictConstants.Destinations.IdentityToken;

                yield break;

            case OpenIddictConstants.Claims.Role:
                yield return OpenIddictConstants.Destinations.AccessToken;

                if (claim.Subject != null && claim.Subject.HasScope(OpenIddictConstants.Permissions.Scopes.Roles))
                    yield return OpenIddictConstants.Destinations.IdentityToken;

                yield break;

            // Never include the security stamp in the access and identity tokens, as it's a secret value.
            case "AspNet.Identity.SecurityStamp": yield break;

            default:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield break;
        }
    }
}