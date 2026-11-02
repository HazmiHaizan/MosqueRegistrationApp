using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MosqueRegistrationApp.Models;

namespace MosqueRegistrationApp.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "UsernameRequired")]
            public string Username { get; set; }

            [Required(ErrorMessage = "IcNumberRequired")]
            public string IcNumber { get; set; }

            [Required(ErrorMessage = "NewPasswordRequired")]
            [DataType(DataType.Password)]
            public string NewPassword { get; set; }

            [Required(ErrorMessage = "ConfirmPasswordRequired")]
            [DataType(DataType.Password)]
            [Compare("NewPassword", ErrorMessage = "PasswordsDoNotMatch")]
            public string ConfirmPassword { get; set; }
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Find user by username
            var user = await _userManager.FindByNameAsync(Input.Username);
            if (user == null || user.IcNumber != Input.IcNumber)
            {
                // Generic error key matching your resource files
                ModelState.AddModelError(string.Empty, "InvalidAccountDetails");
                return Page();
            }

            // Prevent self-service password reset for Admin accounts
            if (await _userManager.IsInRoleAsync(user, Roles.Admin))
            {
                ModelState.AddModelError(string.Empty, "AdminResetNotAllowed");
                return Page();
            }

            // Directly remove old password and set the new one safely without token/email constraints
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, Input.NewPassword);

            if (result.Succeeded)
            {
                TempData["StatusMessage"] = "PasswordResetSuccess";
                return RedirectToPage("/Account/Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }
    }
}