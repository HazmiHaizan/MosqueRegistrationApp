using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MosqueRegistrationApp.Models;
using MosqueRegistrationApp.Services; // Namespace for IR2StorageService

namespace MosqueRegistrationApp.Pages.Admin
{
    [Authorize(Roles = Roles.Admin)]
    public class EditUserModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IR2StorageService _r2Storage; // 1. Add reference to R2 service

        public EditUserModel(
            UserManager<ApplicationUser> userManager,
            IR2StorageService r2Storage) // 2. Inject via constructor
        {
            _userManager = userManager;
            _r2Storage = r2Storage;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            public string Id { get; set; }
            public string FullName { get; set; }
            public string IcNumber { get; set; }
            public string PhoneNumber { get; set; }
            public string CurrentAddress { get; set; }
            public string MaritalStatus { get; set; }
            public int ResidencyDurationYears { get; set; }
            public string ExistingImagePath { get; set; }
            public IFormFile NewProofOfResidency { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            Input = new InputModel
            {
                Id = user.Id,
                FullName = user.FullName,
                IcNumber = user.IcNumber,
                PhoneNumber = user.PhoneNumber,
                CurrentAddress = user.CurrentAddress,
                MaritalStatus = user.MaritalStatus,
                ResidencyDurationYears = user.ResidencyDurationYears,
                ExistingImagePath = user.ProofOfResidencyImagePath
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            // Validate that no other user has this IC Number
            bool icExists = await _userManager.Users.AnyAsync(u => u.IcNumber == Input.IcNumber && u.Id != Input.Id);
            if (icExists)
            {
                ModelState.AddModelError("Input.IcNumber", "Another account with this IC Number already exists.");
                return Page();
            }

            var user = await _userManager.FindByIdAsync(Input.Id);
            if (user == null) return NotFound();

            user.FullName = Input.FullName;
            user.IcNumber = Input.IcNumber;
            user.PhoneNumber = Input.PhoneNumber;
            user.CurrentAddress = Input.CurrentAddress;
            user.MaritalStatus = Input.MaritalStatus;
            user.ResidencyDurationYears = Input.ResidencyDurationYears;

            if (Input.NewProofOfResidency != null && Input.NewProofOfResidency.Length > 0)
            {
                // 3. Upload directly to Cloudflare R2 instead of local wwwroot/uploads
                user.ProofOfResidencyImagePath = await _r2Storage.UploadFileAsync(Input.NewProofOfResidency);
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return RedirectToPage("/Admin/Dashboard");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }
    }
}