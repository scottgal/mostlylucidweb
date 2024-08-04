using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;


namespace Mostlylucid.Controllers
{
    [Route("/")]
    public class LoginController : Controller
    {
        [Route("challenge")]
        public IActionResult Login()
        {
            // Challenge the user using Google authentication
            var properties = new AuthenticationProperties { RedirectUri = "/" };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }
        
        [Route("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("/");
        }
        
        [Route("login")]
        public async Task<IActionResult> HandleGoogleCallback()
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (!authenticateResult.Succeeded)
            {
                // If authentication failed, redirect to the login page
                return Redirect("/challenge");
            }

            // If authentication was successful, sign in the user with the cookie authentication scheme
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, authenticateResult.Principal, authenticateResult.Properties);

            // Redirect to the originally requested page or the home page
            return Redirect(authenticateResult.Properties.RedirectUri ?? "/");
        }
    }
}