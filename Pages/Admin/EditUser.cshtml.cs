using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MosqueRegistrationApp.Models;

namespace MosqueRegistrationApp.Pages.Admin
{
    [Authorize(Roles = Roles.Admin)]
    public class EditUserModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public EditUserModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            public string Id { get; set; }
            public string FullName { get; set; }
            public string IcNumber { get; set; }
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

            var user = await _userManager.FindByIdAsync(Input.Id);
            if (user == null) return NotFound();

            user.FullName = Input.FullName;
            user.IcNumber = Input.IcNumber;
            user.CurrentAddress = Input.CurrentAddress;
            user.MaritalStatus = Input.MaritalStatus;
            user.ResidencyDurationYears = Input.ResidencyDurationYears;

            if (Input.NewProofOfResidency != null && Input.NewProofOfResidency.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(Input.NewProofOfResidency.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await Input.NewProofOfResidency.CopyToAsync(fileStream);
                }
                user.ProofOfResidencyImagePath = "/uploads/" + uniqueFileName;
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