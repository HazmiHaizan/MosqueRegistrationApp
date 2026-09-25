using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            RegisteredUsers = await GetFilteredUsersQuery().ToListAsync();
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

        public async Task<IActionResult> OnPostExportCsvAsync()
        {
            var users = await GetFilteredUsersQuery().ToListAsync();

            var builder = new StringBuilder();
            // CSV Header
            builder.AppendLine("Full Name,IC Number,Address,Marital Status,Residency Duration (Years),Status,Username");

            foreach (var user in users)
            {
                // Escape commas inside user input text fields
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