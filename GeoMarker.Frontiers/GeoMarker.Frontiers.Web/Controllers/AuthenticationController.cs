using GeoMarker.Frontiers.Web.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenIddict.Client.AspNetCore;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace GeoMarker.Frontiers.Web.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class AuthenticationController : Controller
    {
        private readonly ApplicationRegistration _appRegistration;
        public AuthenticationController(IOptions<ApplicationRegistration> applicationRegistration)
        {
            _appRegistration = applicationRegistration.Value;
        }

        [HttpGet("~/login")]
        public ActionResult LogIn(string returnUrl)
        {
            var items = new Dictionary<string, string?>
            {
                [OpenIddictClientAspNetCoreConstants.Properties.Issuer] = _appRegistration.Issuer
            };
            var properties = new AuthenticationProperties(items)
            {
                RedirectUri = Url.IsLocalUrl(returnUrl) ? returnUrl : "/"
            };

            return Challenge(properties, OpenIddictClientAspNetCoreDefaults.AuthenticationScheme);
        }

        [HttpPost("~/logout"), ValidateAntiForgeryToken]
        public async Task<ActionResult> LogOut(string returnUrl)
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (result is not { Succeeded: true })
            {
                return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            var properties = new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictClientAspNetCoreConstants.Properties.Issuer] = _appRegistration.Issuer,
                [OpenIddictClientAspNetCoreConstants.Properties.IdentityTokenHint] =
                    result.Properties.GetTokenValue(OpenIddictClientAspNetCoreConstants.Tokens.BackchannelIdentityToken)
            })
            {
                RedirectUri = Url.IsLocalUrl(returnUrl) ? returnUrl : "/"
            };
            return SignOut(properties, OpenIddictClientAspNetCoreDefaults.AuthenticationScheme);
        }

        [HttpGet("~/callback/login/{provider}"), HttpPost("~/callback/login/{provider}"), IgnoreAntiforgeryToken]
        public async Task<ActionResult> LogInCallback()
        {
            var result = await HttpContext.AuthenticateAsync(OpenIddictClientAspNetCoreDefaults.AuthenticationScheme);

            if (result.Principal.Identity is not ClaimsIdentity { IsAuthenticated: true })
            {
                throw new InvalidOperationException("The external authorization data cannot be used for authentication.");
            }

            var claims = new List<Claim>(result.Principal.Claims
                .Select(claim => claim switch
                {
                    { Type: Claims.Subject } => new Claim(ClaimTypes.NameIdentifier, claim.Value, claim.ValueType, claim.Issuer),
                    { Type: Claims.Name } => new Claim(ClaimTypes.Name, claim.Value, claim.ValueType, claim.Issuer),
                    { Type: Claims.Role } => new Claim(ClaimTypes.Role, claim.Value, claim.ValueType, claim.Issuer),
                    _ => claim
                })
                .Where(claim => claim switch
                {
                    { Type: ClaimTypes.NameIdentifier or ClaimTypes.Name or ClaimTypes.Role } => true,
                    _ => false
                }));

            var identity = new ClaimsIdentity(claims,
                authenticationType: CookieAuthenticationDefaults.AuthenticationScheme,
                nameType: ClaimTypes.Name,
                roleType: ClaimTypes.Role);

            var properties = new AuthenticationProperties(result.Properties.Items);

            properties.StoreTokens(result.Properties.GetTokens().Where(token => token switch
            {
                {
                    Name: OpenIddictClientAspNetCoreConstants.Tokens.BackchannelAccessToken or
                          OpenIddictClientAspNetCoreConstants.Tokens.BackchannelIdentityToken or
                          OpenIddictClientAspNetCoreConstants.Tokens.RefreshToken
                } => true,
                _ => false
            }));

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), properties);

            return Redirect(properties.RedirectUri);
        }

        [HttpGet("~/callback/logout/{provider}"), HttpPost("~/callback/logout/{provider}"), IgnoreAntiforgeryToken]
        public async Task<ActionResult> LogOutCallback()
        {
            var result = await HttpContext.AuthenticateAsync(OpenIddictClientAspNetCoreDefaults.AuthenticationScheme);
            return Redirect(result!.Properties!.RedirectUri);
        }
    }
}
