using BookingApp.Helper;
using BookingApp.Models;
using BookingApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace BookingApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            var serverVersion = ServerVersion.AutoDetect(connectionString);
            builder.Services.AddDbContext<StpetrusSeDb1Context>(options =>
            {
                options.UseMySql(connectionString, serverVersion);
            });
            builder.Services.AddDbContext<IdentityDbContext>(options =>
            {
                options.UseMySql(connectionString, serverVersion);
            });
            builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireDigit = false;
                options.Password.RequireUppercase = false;
            }).AddEntityFrameworkStores<IdentityDbContext>().AddDefaultTokenProviders();

            builder.Services.AddScoped<ISqlRepository, SqlRepository>();
            builder.Services.AddScoped<IDataService, DataService>();

            builder.Services.Configure<MailSettings>(builder.Configuration.GetSection(Constants.AppSettings.MailSettings));

            builder.Services.AddLogging(builder =>
            {
                builder.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
            });

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAnyOrigin",
                    builder => builder.AllowAnyOrigin()
                                      .AllowAnyMethod()
                                      .AllowAnyHeader());
            });

            var culture = new CultureInfo("sv-SE");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            await Task.Run(async () =>
            {
                using (var scope = app.Services.CreateScope())
                {
                    var identityDbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                    identityDbContext.Database.EnsureCreated();
                    var bokningDbContext = scope.ServiceProvider.GetRequiredService<StpetrusSeDb1Context>();
                    bokningDbContext.Database.EnsureCreated();

                    // Add the code for creating roles and an admin user here
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                    // Create roles if they don't exist
                    string[] roleNames = { Constants.Role.Admin, Constants.Role.User };
                    IdentityResult roleResult;

                    foreach (var roleName in roleNames)
                    {
                        var roleExist = await roleManager.RoleExistsAsync(roleName);
                        if (!roleExist)
                        {
                            // Create the roles and seed them to the database
                            roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                        }
                    }

                    // Create an admin user if it doesn't exist
                    IdentityUser adminUser = await userManager.FindByEmailAsync(Constants.Email.DevMail);

                    if (adminUser == null)
                    {
                        adminUser = new IdentityUser()
                        {
                            UserName = Constants.Role.Admin,
                            Email = Constants.Email.DevMail,
                        };
                        await userManager.CreateAsync(adminUser, "Admin@123"); // Set a strong password here

                        // Assign the admin user to the "Admin" role
                        await userManager.AddToRoleAsync(adminUser, Constants.Role.Admin);
                    }
                }
            });

            app.UseCors("AllowAnyOrigin");

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Login}/{id?}");

            app.Run();
        }
    }
}
