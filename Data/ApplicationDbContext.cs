using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MosqueRegistrationApp.Models;

namespace MosqueRegistrationApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Enforce unique index constraint on IC Number at the database level
            builder.Entity<ApplicationUser>()
                .HasIndex(u => u.IcNumber)
                .IsUnique();
        }
    }
}