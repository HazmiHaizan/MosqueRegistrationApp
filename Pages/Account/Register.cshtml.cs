using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MosqueRegistrationApp.Models;
using MosqueRegistrationApp.Services; // Ensure this matches your namespace for IR2StorageService

namespace MosqueRegistrationApp.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IR2StorageService _r2Storage; // 1. Add reference to R2 service

        public RegisterModel(
            UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager,
            IR2StorageService r2Storage) // 2. Inject via constructor
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _r2Storage = r2Storage;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string FullName { get; set; }
            public string IcNumber { get; set; }
            public string PhoneNumber { get; set; }
            public string CurrentAddress { get; set; }
            public string MaritalStatus { get; set; }
            public int ResidencyDurationYears { get; set; }
            public IFormFile ProofOfResidency { get; set; }
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            // Validate IC Number uniqueness
            bool icExists = await _userManager.Users.AnyAsync(u => u.IcNumber == Input.IcNumber);
            if (icExists)
            {
                ModelState.AddModelError("Input.IcNumber", "An account with this IC Number is already registered.");
                return Page();
            }

            string imagePath = null;
            if (Input.ProofOfResidency != null && Input.ProofOfResidency.Length > 0)
            {
                // 3. Upload directly to Cloudflare R2 instead of local wwwroot/uploads
                imagePath = await _r2Storage.UploadFileAsync(Input.ProofOfResidency);
            }

            var user = new ApplicationUser
            {
                UserName = Input.Username,
                FullName = Input.FullName,
                IcNumber = Input.IcNumber,
                PhoneNumber = Input.PhoneNumber,
                CurrentAddress = Input.CurrentAddress,
                MaritalStatus = Input.MaritalStatus,
                ResidencyDurationYears = Input.ResidencyDurationYears,
                ProofOfResidencyImagePath = imagePath, // Stores the secure public R2 URL string
                ApprovalStatus = "Pending"
            };

            var result = await _userManager.CreateAsync(user, Input.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, Roles.User);
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToPage("/User/Dashboard");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }
    }
}