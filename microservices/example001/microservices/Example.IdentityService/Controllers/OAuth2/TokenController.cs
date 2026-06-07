using System.Collections.Immutable;
using System.Security.Claims;
using Example.IdentityService.Helpers.OAuth;
using Example.IdentityService.Models;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace Example.IdentityService.Controllers.OAuth2;

[ApiExplorerSettings(IgnoreApi = true)]
public sealed partial class TokenController(
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictScopeManager scopeManager,
    OpenIddictClaimsPrincipalManager openIddictClaimsPrincipalManager,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager)
    : Controller
{
    private IOpenIddictApplicationManager ApplicationManager { get; } = applicationManager;
    private IOpenIddictScopeManager ScopeManager { get; } = scopeManager;
    private OpenIddictClaimsPrincipalManager OpenIddictClaimsPrincipalManager { get; } =
        openIddictClaimsPrincipalManager;

    private UserManager<ApplicationUser> UserManager { get; } = userManager;
    private SignInManager<ApplicationUser> SignInManager { get; } = signInManager;

    private static Task<OpenIddictRequest> GetOAuthServerRequestAsync(HttpContext context)
    {
        var request = context.GetOpenIddictServerRequest();
        ArgumentNullException.ThrowIfNull(request);
        return Task.FromResult(request);
    }

    private async Task<IEnumerable<string>> GetResourcesAsync(ImmutableArray<string> scopes)
    {
        var resources = new List<string>();

        await foreach (var resource in ScopeManager.ListResourcesAsync(scopes)) resources.Add(resource);

        return resources;
    }

    [HttpPost]
    [HttpGet]
    [IgnoreAntiforgeryToken]
    [Produces("application/json")]
    [Route("~/connect/token")]
    public async Task<IActionResult> Exchange()
    {
        var request = await GetOAuthServerRequestAsync(HttpContext);
        var cancellationToken = HttpContext.RequestAborted;

        if (request.IsClientCredentialsGrantType())
            return await HandleClientCredentialsAsync(request, cancellationToken);

        if (request.IsAuthorizationCodeGrantType())
            return await HandleAuthorizationCodeAsync(request, cancellationToken);
        
        if (request.IsRefreshTokenGrantType())
            return await HandleRefreshTokenAsync(request);
        
        if (request.IsPasswordGrantType())
            return await HandlePasswordAsync(request);
        
        if (request.IsDeviceCodeGrantType())
            return await HandleDeviceCodeAsync(request);

        return BadRequest(new OpenIddictResponse
        {
            Error = OpenIddictConstants.Errors.UnsupportedGrantType,
            ErrorDescription = "The specified grant type is not supported."
        });
    }
}

public partial class TokenController
{
    private async Task<IActionResult> HandleClientCredentialsAsync(OpenIddictRequest request,
        CancellationToken cancellationToken = default)
    {
        var application = await ApplicationManager.FindByClientIdAsync(request.ClientId!, cancellationToken);
        ArgumentNullException.ThrowIfNull(application);

        var identity = new ClaimsIdentity(
            TokenValidationParameters.DefaultAuthenticationType,
            OpenIddictConstants.Claims.Name,
            OpenIddictConstants.Claims.Role);

        // Add the claims that will be persisted in the tokens (use the client_id as the subject identifier).
        var sub = await ApplicationManager.GetClientIdAsync(application, cancellationToken);
        var name = await ApplicationManager.GetDisplayNameAsync(application, cancellationToken);
        if (string.IsNullOrEmpty(sub) || string.IsNullOrEmpty(name))
        {
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidClient,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The client application is invalid."
                }));
        }
        
        identity.AddClaim(OpenIddictConstants.Claims.Subject, sub);
        identity.AddClaim(OpenIddictConstants.Claims.PreferredUsername, name);
        // Note: In the original OAuth 2.0 specification, the client credentials grant
        // doesn't return an identity token, which is an OpenID Connect concept.
        //
        // As a non-standardized extension, OpenIddict allows returning an id_token
        // to convey information about the client application when the "openid" scope
        // is granted (i.e. specified when calling principal.SetScopes()). When the "openid"
        // scope is not explicitly set, no identity token is returned to the client application.

        // Set the list of scopes granted to the client application in access_token.
        identity.SetScopes(request.GetScopes());
        identity.SetResources(await GetResourcesAsync(request.GetScopes()));
        var principal = new ClaimsPrincipal(identity);

        // handle the token request
        await OpenIddictClaimsPrincipalManager.HandleAsync(request, principal);
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}

public partial class TokenController
{
    private async Task<IActionResult> HandleAuthorizationCodeAsync(OpenIddictRequest request,
        CancellationToken _ = default)
    {
        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var principal = result.Principal;
        if (!result.Succeeded || principal is null)
            return BadRequest(new OpenIddictResponse
            {
                Error = OpenIddictConstants.Errors.InvalidGrant,
                ErrorDescription = "The token is invalid."
            });
        
        var user = await UserManager.GetUserAsync(principal);
        if (user is null)
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The token is no longer valid."
                }));
        
        if (!await PreSignInCheckAsync(user))
        {
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is no longer allowed to sign in."
                }));
        }

        var identity = new ClaimsIdentity(result.Principal.Claims,
            TokenValidationParameters.DefaultAuthenticationType,
            OpenIddictConstants.Claims.Name,
            OpenIddictConstants.Claims.Role);

        // Get roles and convert to ImmutableArray
        var roles = await UserManager.GetRolesAsync(user);

        // changed since the authorization code/refresh token was issued.
        identity.SetClaim(OpenIddictConstants.Claims.Subject, user.Id)
            .SetClaim(OpenIddictConstants.Claims.Email, user.Email)
            .SetClaim(OpenIddictConstants.Claims.Name, user.UserName)
            .SetClaim(OpenIddictConstants.Claims.PreferredUsername, user.UserName)
            .SetClaims(OpenIddictConstants.Claims.Role, [..roles]);
        
        var finalPrincipal = new ClaimsPrincipal(identity);
        await OpenIddictClaimsPrincipalManager.HandleAsync(request, finalPrincipal);
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}


public partial class TokenController
{
    private async Task<IActionResult> HandleRefreshTokenAsync(OpenIddictRequest request)
    {
        // Retrieve the claims principal stored in the authorization code/device code/refresh token.
        var principal = (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;

        if (principal is null)
        {
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The token is invalid."
                }!));
        }
        
        var user = await UserManager.GetUserAsync(principal);
        if (user == null)
        {
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The token is no longer valid."
                }!));
        }
        if (!await PreSignInCheckAsync(user))
        {
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is no longer allowed to sign in."
                }!));
        }
        
        await OpenIddictClaimsPrincipalManager.HandleAsync(request, principal);

        // Returning a SignInResult will ask OpenIddict to issue the appropriate access/identity tokens.
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
    
    private async Task<bool> PreSignInCheckAsync(ApplicationUser user)
    {
        if (!user.EmailConfirmed)
        {
            return false;
        }

        if (!await SignInManager.CanSignInAsync(user))
        {
            return false;
        }

        if (await UserManager.IsLockedOutAsync(user))
        {
            return false;
        }

        return true;
    }
}

public partial class TokenController
{
    private async Task<IActionResult> HandlePasswordAsync(OpenIddictRequest request)
    {
        var user = await UserManager.FindByNameAsync(request.Username!);
        if (user is null || !await UserManager.CheckPasswordAsync(user, request.Password!))
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The username/password couple is invalid."
                }));
        
        if(!await PreSignInCheckAsync(user))
        {
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is no longer allowed to sign in."
                }));
        }

        var identity = new ClaimsIdentity(
            TokenValidationParameters.DefaultAuthenticationType,
            OpenIddictConstants.Claims.Name,
            OpenIddictConstants.Claims.Role);

        var roles = await UserManager.GetRolesAsync(user);
        identity.SetClaim(OpenIddictConstants.Claims.Subject, user.Id)
            .SetClaim(OpenIddictConstants.Claims.Email, user.Email)
            .SetClaim(OpenIddictConstants.Claims.Name, user.UserName)
            .SetClaim(OpenIddictConstants.Claims.PreferredUsername, user.UserName)
            .SetClaims(OpenIddictConstants.Claims.Role, [..roles]);

        var principal = new ClaimsPrincipal(identity);
        await OpenIddictClaimsPrincipalManager.HandleAsync(request, principal);
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
    
    private async Task<IActionResult> HandleDeviceCodeAsync(OpenIddictRequest request)
    {
        // Retrieve the claims principal stored in the authorization code/device code/refresh token.
        var principal = (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;
        if (principal is null)
        {
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The token is invalid."
                }));
        }
        
        // Retrieve the user profile corresponding to the authorization code/refresh token.
        // Note: if you want to automatically invalidate the authorization code/refresh token
        // when the user password/roles change, use the following line instead:
        // var user = _signInManager.ValidateSecurityStampAsync(info.Principal);
        var user = await UserManager.GetUserAsync(principal);
        if (user == null)
        {
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The token is no longer valid."
                }));
        }
        
        // Ensure the user is still allowed to sign in.
        if (!await PreSignInCheckAsync(user))
        {
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is no longer allowed to sign in."
                }));
        }
        
        await OpenIddictClaimsPrincipalManager.HandleAsync(request, principal);

        // Returning a SignInResult will ask OpenIddict to issue the appropriate access/identity tokens.
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}