using FitRazor.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace FitRazor.Data
{
    public static class RoleSeed
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            // Создаём роли
            var roles = new[] { "Admin", "Trainer", "Client" };
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Создаём админа по умолчанию (если нет)
            var adminLogin = "admin";
            var adminPassword = "Admin123!";

            var admin = await userManager.FindByNameAsync(adminLogin);
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminLogin,
                    Email = "admin@fitnesscenter.ru",
                    FullName = "Администратор",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(admin, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }
        }
    }
}
