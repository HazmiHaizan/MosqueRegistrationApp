using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MosqueRegistrationApp.Data;
using MosqueRegistrationApp.Models;

namespace MosqueRegistrationApp.Pages.Admin
{
    [Authorize(Roles = Roles.Admin)]
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DashboardModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<ApplicationUser> RegisteredUsers { get; set; }

        public async Task OnGetAsync()
        {
            RegisteredUsers = await _context.Users.ToListAsync();
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(string userId, string status)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.ApprovalStatus = status;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
}