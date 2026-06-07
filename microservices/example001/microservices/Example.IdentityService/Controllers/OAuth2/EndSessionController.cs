using Example.IdentityService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Server.AspNetCore;

namespace Example.IdentityService.Controllers.OAuth2;

[Route("connect/endsession")]
[ApiExplorerSettings(IgnoreApi = true)]
public class EndSessionController(SignInManager<ApplicationUser> signInManager) : Controller
{
    [HttpGet]
    public virtual async Task<IActionResult> GetAsync()
    {
        // Ask ASP.NET Core Identity to delete the local and external cookies created
        // when the user agent is redirected from the external identity provider
        // after a successful authentication flow (.e.g Google or Facebook).
        await signInManager.SignOutAsync();

        // Returning a SignOutResult will ask OpenIddict to redirect the user agent
        // to the post_logout_redirect_uri specified by the client application or to
        // the RedirectUri specified in the authentication properties if none was set.
        return SignOut(authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}