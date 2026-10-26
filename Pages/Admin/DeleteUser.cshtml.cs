using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using MosqueRegistrationApp.Models;

namespace MosqueRegistrationApp.Pages.Admin
{
    [Authorize(Roles = Roles.Admin)]
    public class DeleteUserModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<DeleteUserModel> _localizer;

        public DeleteUserModel(
            UserManager<ApplicationUser> userManager,
            IStringLocalizer<DeleteUserModel> localizer)
        {
            _userManager = userManager;
            _localizer = localizer;
        }

        public ApplicationUser UserToDelete { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            // Prevent admin from navigating to delete their own account
            string currentUserId = _userManager.GetUserId(User);
            if (currentUserId == id)
            {
                TempData["ErrorMessage"] = _localizer["ErrorCannotDeleteSelf"];
                return RedirectToPage("/Admin/Dashboard");
            }

            UserToDelete = await _userManager.FindByIdAsync(id);
            if (UserToDelete == null) return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            // Prevent admin from posting a deletion request for their own account
            string currentUserId = _userManager.GetUserId(User);
            if (currentUserId == id)
            {
                ModelState.AddModelError(string.Empty, _localizer["ErrorCannotDeleteSelf"]);
                UserToDelete = await _userManager.FindByIdAsync(id);
                return Page();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // 1. Delete associated proof of residency file from wwwroot if it exists
            if (!string.IsNullOrEmpty(user.ProofOfResidencyImagePath))
            {
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProofOfResidencyImagePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            // 2. Delete user account from ASP.NET Core Identity
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return RedirectToPage("/Admin/Dashboard");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            UserToDelete = user;
            return Page();
        }
    }
}