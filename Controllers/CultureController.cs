using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

[Route("[controller]/[action]")]
public class CultureController : Controller
{
    [HttpPost]
    public IActionResult SetLanguage(string culture, string returnUrl)
    {
        // Creates the required formatted string: c=ms|uic=ms
        string cookieValue = CookieRequestCultureProvider.MakeCookieValue(
            new RequestCulture(culture, culture)
        );

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            cookieValue,
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true, // Forces cookie creation even if GDPR/consent rules exist
                SameSite = SameSiteMode.Lax,
                Path = "/"
            }
        );

        return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "~/" : returnUrl);
    }
}