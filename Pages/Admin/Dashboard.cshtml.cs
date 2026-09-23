using System.Collections.Generic;
using System.Linq;
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

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; }

        public async Task OnGetAsync()
        {
            IQueryable<ApplicationUser> query = _context.Users;

            // Apply Keyword Search (Full Name or IC Number)
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(u => 
                    (u.FullName != null && u.FullName.Contains(SearchTerm)) || 
                    (u.IcNumber != null && u.IcNumber.Contains(SearchTerm)));
            }

            // Apply Approval Status Filter
            if (!string.IsNullOrWhiteSpace(StatusFilter) && StatusFilter != "All")
            {
                query = query.Where(u => u.ApprovalStatus == StatusFilter);
            }

            RegisteredUsers = await query.ToListAsync();
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(string userId, string status)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.ApprovalStatus = status;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage(new { SearchTerm, StatusFilter });
        }
    }
}