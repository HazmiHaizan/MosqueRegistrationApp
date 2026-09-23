using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MosqueRegistrationApp.Models;

namespace MosqueRegistrationApp.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;

        public LogoutModel(SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await _signInManager.SignOutAsync();
            return RedirectToPage("/Account/Login");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Sign out from ASP.NET Core Identity
            await _signInManager.SignOutAsync();
            
            // Explicitly sign out of default authentication scheme to ensure cookie disposal
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

            return RedirectToPage("/Account/Login");
        }
    }
}