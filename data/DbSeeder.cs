using Microsoft.AspNetCore.Identity;
using pfe.ecom.api.Models;

namespace pfe.ecom.api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        var roles = new[] { "Admin", "Customer", "Supplier" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        await CreateAdminIfNotExists(
            userManager,
            fullName: "islem",
            email: "islemml12@gmail.com",
            password: "islem1234"
        );

        await CreateAdminIfNotExists(
            userManager,
            fullName: "mohamed",
            email: "mohamed23@gmail.com",
            password: "mido4321"
        );
    }

    private static async Task CreateAdminIfNotExists(
        UserManager<ApplicationUser> userManager,
        string fullName,
        string email,
        string password)
    {
        var existingUser = await userManager.FindByEmailAsync(email);

        if (existingUser != null)
        {
            if (!await userManager.IsInRoleAsync(existingUser, "Admin"))
            {
                await userManager.AddToRoleAsync(existingUser, "Admin");
            }

            var shouldUpdate = false;

            if (string.IsNullOrWhiteSpace(existingUser.FullName))
            {
                existingUser.FullName = fullName;
                shouldUpdate = true;
            }

            if (string.IsNullOrWhiteSpace(existingUser.AccountType))
            {
                existingUser.AccountType = "admin";
                shouldUpdate = true;
            }

            if (shouldUpdate)
            {
                await userManager.UpdateAsync(existingUser);
            }

            return;
        }

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            AccountType = "admin",
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(admin, password);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}