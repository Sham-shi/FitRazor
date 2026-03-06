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
            var adminPassword = "Admin123!"; // 🔐 Смените в продакшене!

            var admin = await userManager.FindByNameAsync(adminLogin);
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminLogin,
                    FullName = "Администратор"
                };

                var result = await userManager.CreateAsync(admin, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }

            var trainerLogin = "trainer";
            var trainerPassword = "Trainer2!";

            var trainer = await userManager.FindByNameAsync(trainerLogin);
            if (trainer == null)
            {
                trainer = new ApplicationUser
                {
                    UserName = trainerLogin,
                    FullName = "Тренер"
                };

                var result = await userManager.CreateAsync(trainer, trainerPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(trainer, "Trainer");
                }
            }

            var clientLogin = "client";
            var clientPassword = "Client3!";

            var client = await userManager.FindByNameAsync(clientLogin);
            if (client == null)
            {
                client = new ApplicationUser
                {
                    UserName = clientLogin,
                    FullName = "Клиент"
                };

                var result = await userManager.CreateAsync(client, clientPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(client, "Client");
                }
            }
        }
    }
}
