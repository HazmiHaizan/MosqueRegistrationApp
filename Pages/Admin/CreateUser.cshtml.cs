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
    public class CreateUserModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public CreateUserModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string FullName { get; set; }
            public string IcNumber { get; set; }
            public string CurrentAddress { get; set; }
            public string MaritalStatus { get; set; }
            public int ResidencyDurationYears { get; set; }
            public IFormFile ProofOfResidency { get; set; }
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            string imagePath = null;
            if (Input.ProofOfResidency != null && Input.ProofOfResidency.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(Input.ProofOfResidency.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await Input.ProofOfResidency.CopyToAsync(fileStream);
                }
                imagePath = "/uploads/" + uniqueFileName;
            }

            var user = new ApplicationUser
            {
                UserName = Input.Username,
                FullName = Input.FullName,
                IcNumber = Input.IcNumber,
                CurrentAddress = Input.CurrentAddress,
                MaritalStatus = Input.MaritalStatus,
                ResidencyDurationYears = Input.ResidencyDurationYears,
                ProofOfResidencyImagePath = imagePath
            };

            var result = await _userManager.CreateAsync(user, Input.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, Roles.User);
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