using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MosqueRegistrationApp.Data;
using MosqueRegistrationApp.Models;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

namespace MosqueRegistrationApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 4;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
            });

            builder.Services.AddRazorPages();

            // 1. Tell ASP.NET Core where your .resx files are located
            builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

            // 2. Add localization support to Razor Pages and DataAnnotations
            builder.Services.AddRazorPages()
                .AddViewLocalization()
                .AddDataAnnotationsLocalization();

            // 3. Define supported cultures
            var supportedCultures = new[]
            {
                new CultureInfo("en"),
                new CultureInfo("ms")
            };

            builder.Services.Configure<RequestLocalizationOptions>(options =>
            {
                options.DefaultRequestCulture = new RequestCulture("en");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;

                // Ensure Cookie provider takes precedence
                options.RequestCultureProviders = new List<IRequestCultureProvider>
                {
                    new QueryStringRequestCultureProvider(),
                    new CookieRequestCultureProvider(),
                    new AcceptLanguageHeaderRequestCultureProvider()
                };
            });

            builder.Services.AddControllers();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            // 4. CRITICAL: Add RequestLocalization AFTER UseRouting and BEFORE UseAuthorization
            var localizationOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
            app.UseRequestLocalization(localizationOptions);

            app.MapControllers();

            app.Use(async (context, next) =>
                {
                    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                    context.Response.Headers["Pragma"] = "no-cache";
                    context.Response.Headers["Expires"] = "0";
                    await next();
                });

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapRazorPages();

            // Seed Roles and Admin Account
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<ApplicationDbContext>();
                var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

                context.Database.Migrate();

                if (!await roleManager.RoleExistsAsync(Roles.Admin))
                    await roleManager.CreateAsync(new IdentityRole(Roles.Admin));

                if (!await roleManager.RoleExistsAsync(Roles.User))
                    await roleManager.CreateAsync(new IdentityRole(Roles.User));

                string adminUser = "admin";
                if (await userManager.FindByNameAsync(adminUser) == null)
                {
                    var admin = new ApplicationUser
                    {
                        UserName = adminUser,
                        FullName = "Mosque Admin",
                        IcNumber = "000000000000",
                        CurrentAddress = "Mosque HQ",
                        MaritalStatus = "N/A",
                        ResidencyDurationYears = 0,
                        ProofOfResidencyImagePath = ""
                    };
                    var createResult = await userManager.CreateAsync(admin, "Admin123!");
                    if (createResult.Succeeded)
                    {
                        await userManager.AddToRoleAsync(admin, Roles.Admin);
                    }
                }
            }

            app.Run();
        }
    }
}