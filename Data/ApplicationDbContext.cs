using Microsoft.AspNetCore.Identity;
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

            // Enforce unique constraint on IC Number
            builder.Entity<ApplicationUser>()
                .HasIndex(u => u.IcNumber)
                .IsUnique();

            // Make optional Identity columns nullable in PostgreSQL
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(u => u.PhoneNumber).IsRequired(false);
                entity.Property(u => u.Email).IsRequired(false);
                entity.Property(u => u.NormalizedEmail).IsRequired(false);
            });
        }
    }
}