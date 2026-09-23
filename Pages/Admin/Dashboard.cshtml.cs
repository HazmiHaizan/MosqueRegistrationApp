using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MosqueRegistrationApp.Data;
using MosqueRegistrationApp.Models;

namespace MosqueRegistrationApp.Pages.Admin
{
    [Authorize(Roles = Roles.Admin)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public ApplicationUser CurrentUser { get; set; }
        public IList<ApplicationUser> RegisteredUsers { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Get current logged-in Admin user details
            CurrentUser = await _userManager.GetUserAsync(User);
            if (CurrentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Get all registered users for the admin list
            RegisteredUsers = await _context.Users.ToListAsync();

            return Page();
        }
    }
}