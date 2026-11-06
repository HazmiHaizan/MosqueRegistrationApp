using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MosqueRegistrationApp.Data;
using MosqueRegistrationApp.Models;
using MosqueRegistrationApp.Services;
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

            // 1. Fetch connection string and strip any accidental quotes or whitespace
            var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                                   ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") 
                                   ?? Environment.GetEnvironmentVariable("DATABASE_URL") 
                                   ?? string.Empty;

            var connectionString = rawConnectionString.Trim().Trim('"').Trim('\'');

            // 2. Convert Render's postgresql:// URL format into Npgsql keyword format
            if (connectionString.StartsWith("postgres://") || connectionString.StartsWith("postgresql://"))
            {
                var uri = new Uri(connectionString);
                var userInfo = uri.UserInfo.Split(':');
                connectionString = $"Host={uri.Host};Port={(uri.Port > 0 ? uri.Port : 5432)};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true;";
            }

            Console.WriteLine($"[RENDER DEBUG] Converted Connection String Length: {connectionString.Length}");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Critical Error: DefaultConnection string is missing or empty.");
            }

            // 3. Register database context cleanly
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));

            builder.Services.AddScoped<IR2StorageService, R2StorageService>();

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

            builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

            builder.Services.AddRazorPages()
                .AddViewLocalization()
                .AddDataAnnotationsLocalization();

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

            // Seed Roles and Admin Account with error trapping
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<ApplicationDbContext>();
                    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

                    context.Database.Migrate();

                    if (!await roleManager.RoleExistsAsync(Roles.Admin))
                        await roleManager.CreateAsync(new IdentityRole(Roles.Admin));

                    if (!await roleManager.RoleExistsAsync(Roles.User))
                        await roleManager.CreateAsync(new IdentityRole(Roles.User));

                    if (!await roleManager.RoleExistsAsync(Roles.SuperUser))
                        await roleManager.CreateAsync(new IdentityRole(Roles.SuperUser));

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
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred during database migration or seeding.");
                    throw;
                }
            }

            app.Run();
        }
    }
}