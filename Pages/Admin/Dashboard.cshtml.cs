using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<AdminUserRowViewModel> RegisteredUsers { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; }

        public class AdminUserRowViewModel
        {
            public ApplicationUser User { get; set; }
            public bool IsSuperUser { get; set; }
            public bool IsAdmin { get; set; }
            public bool IsCurrentAdmin { get; set; }
        }

        public async Task OnGetAsync()
        {
            var currentUserId = _userManager.GetUserId(User);
            var users = await GetFilteredUsersQuery().ToListAsync();
            RegisteredUsers = new List<AdminUserRowViewModel>();

            foreach (var user in users)
            {
                var isSuper = await _userManager.IsInRoleAsync(user, Roles.SuperUser);
                var isAdmin = await _userManager.IsInRoleAsync(user, Roles.Admin);

                RegisteredUsers.Add(new AdminUserRowViewModel
                {
                    User = user,
                    IsSuperUser = isSuper,
                    IsAdmin = isAdmin,
                    IsCurrentAdmin = (user.Id == currentUserId)
                });
            }
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

        // Handler to toggle Super User status safely
        public async Task<IActionResult> OnPostToggleSuperUserAsync(string userId)
        {
            var currentUserId = _userManager.GetUserId(User);
            
            // Prevent an admin from modifying their own account role
            if (userId == currentUserId)
            {
                return RedirectToPage(new { SearchTerm, StatusFilter });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // Prevent modifying other Admins through this toggle
            if (await _userManager.IsInRoleAsync(user, Roles.Admin))
            {
                return RedirectToPage(new { SearchTerm, StatusFilter });
            }

            bool isSuperUser = await _userManager.IsInRoleAsync(user, Roles.SuperUser);

            if (isSuperUser)
            {
                await _userManager.RemoveFromRoleAsync(user, Roles.SuperUser);
            }
            else
            {
                await _userManager.AddToRoleAsync(user, Roles.SuperUser);
            }

            return RedirectToPage(new { SearchTerm, StatusFilter });
        }

        public async Task<IActionResult> OnPostExportCsvAsync()
        {
            var users = await GetFilteredUsersQuery().ToListAsync();

            var builder = new StringBuilder();
            builder.AppendLine("Full Name,IC Number,Address,Marital Status,Residency Duration (Years),Status,Username");

            foreach (var user in users)
            {
                string fullName = EscapeCsvField(user.FullName);
                string icNumber = EscapeCsvField(user.IcNumber);
                string address = EscapeCsvField(user.CurrentAddress);
                string maritalStatus = EscapeCsvField(user.MaritalStatus);
                string status = EscapeCsvField(user.ApprovalStatus);
                string username = EscapeCsvField(user.UserName);

                builder.AppendLine($"{fullName},{icNumber},{address},{maritalStatus},{user.ResidencyDurationYears},{status},{username}");
            }

            byte[] buffer = Encoding.UTF8.GetBytes(builder.ToString());
            return File(buffer, "text/csv", $"Mosque_Members_Export_{System.DateTime.Now:yyyyMMdd}.csv");
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

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field)) return "\"\"";
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            return field;
        }
    }
}