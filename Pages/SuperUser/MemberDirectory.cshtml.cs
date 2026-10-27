using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MosqueRegistrationApp.Data;
using MosqueRegistrationApp.Models;

namespace MosqueRegistrationApp.Pages.SuperUser
{
    [Authorize(Roles = "Admin,SuperUser")]
    public class MemberDirectoryModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MemberDirectoryModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<ApplicationUser> RegisteredUsers { get; set; }
        public ApplicationUser CurrentUserProfile { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; }

        public async Task OnGetAsync()
        {
            // Get the currently logged-in Super User's personal record
            CurrentUserProfile = await _userManager.GetUserAsync(User);

            // Fetch the filtered member directory list
            RegisteredUsers = await GetFilteredUsersQuery().ToListAsync();
        }

        private IQueryable<ApplicationUser> GetFilteredUsersQuery()
        {
            IQueryable<ApplicationUser> query = _context.Users;

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(u => 
                    (u.FullName != null && u.FullName.Contains(SearchTerm)) || 
                    (u.IcNumber != null && u.IcNumber.Contains(SearchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(StatusFilter) && StatusFilter != "All")
            {
                query = query.Where(u => u.ApprovalStatus == StatusFilter);
            }

            return query;
        }
    }
}