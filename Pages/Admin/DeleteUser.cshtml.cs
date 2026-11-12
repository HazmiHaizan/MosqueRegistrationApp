using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using MosqueRegistrationApp.Models;
using MosqueRegistrationApp.Services;

namespace MosqueRegistrationApp.Pages.Admin
{
    [Authorize(Roles = Roles.Admin)]
    public class DeleteUserModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<DeleteUserModel> _localizer;
        private readonly IR2StorageService _r2Storage;

        public DeleteUserModel(
            UserManager<ApplicationUser> userManager,
            IStringLocalizer<DeleteUserModel> localizer,
            IR2StorageService r2Storage)
        {
            _userManager = userManager;
            _localizer = localizer;
            _r2Storage = r2Storage;
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

            // Delete associated proof of residency file from Cloudflare R2 bucket if it exists
            if (!string.IsNullOrEmpty(user.ProofOfResidencyImagePath))
            {
                await _r2Storage.DeleteFileAsync(user.ProofOfResidencyImagePath);
            }

            // Delete user account from ASP.NET Core Identity
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