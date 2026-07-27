using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Platform.Portal.Models;
using Platform.Shared.Constants;
using System;
using System.Threading.Tasks;

namespace Platform.Portal.Data;

/// <summary>
/// Inizializzatore del database con dati di seed
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Inizializza il database e crea utenti admin e developer di default
    /// </summary>
    public static async Task Initialize(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        // Applica le migrazioni pending e crea il database se non esiste
        await context.Database.MigrateAsync();

        // Crea i ruoli se non esistono
        if (!await roleManager.RoleExistsAsync(PlatformConstants.Roles.Admin))
        {
            await roleManager.CreateAsync(new IdentityRole(PlatformConstants.Roles.Admin));
        }

        if (!await roleManager.RoleExistsAsync(PlatformConstants.Roles.User))
        {
            await roleManager.CreateAsync(new IdentityRole(PlatformConstants.Roles.User));
        }

        if (!await roleManager.RoleExistsAsync("Developer"))
        {
            await roleManager.CreateAsync(new IdentityRole("Developer"));
        }

        // 1. Gestione utente Developer (username: developer, password: Videosystem01!)
        var devEmail = "developer@videosystem.it";
        var devUser = await userManager.FindByNameAsync("developer");

        if (devUser == null)
        {
            devUser = new ApplicationUser
            {
                UserName = "developer",
                Email = devEmail,
                EmailConfirmed = true,
                FullName = "Sviluppatore Sistema",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(devUser, "Videosystem01!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(devUser, "Developer");
            }
        }
        else
        {
            if (!await userManager.IsInRoleAsync(devUser, "Developer"))
            {
                await userManager.AddToRoleAsync(devUser, "Developer");
            }
            
            // Forza reset password a Videosystem01! per sicurezza
            var token = await userManager.GeneratePasswordResetTokenAsync(devUser);
            await userManager.ResetPasswordAsync(devUser, token, "Videosystem01!");
        }

        // 2. Gestione utente Admin (username: admin, password: Admin123!)
        var adminEmail = "admin@videosystem.it";
        var adminUser = await userManager.FindByNameAsync("admin");

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin",
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "Amministratore Sistema",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, "Admin123!");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, PlatformConstants.Roles.Admin);
                await userManager.AddToRoleAsync(adminUser, "Developer");
            }
        }
        else
        {
            if (!await userManager.IsInRoleAsync(adminUser, PlatformConstants.Roles.Admin))
            {
                await userManager.AddToRoleAsync(adminUser, PlatformConstants.Roles.Admin);
            }
            if (!await userManager.IsInRoleAsync(adminUser, "Developer"))
            {
                await userManager.AddToRoleAsync(adminUser, "Developer");
            }
            
            // Forza reset password a Admin123! per sicurezza
            var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);
            await userManager.ResetPasswordAsync(adminUser, token, "Admin123!");
        }

        // 3. Gestione utente Test (username: user, password: User123!)
        var testEmail = "user@videosystem.it";
        var testUser = await userManager.FindByNameAsync("user");

        if (testUser == null)
        {
            testUser = new ApplicationUser
            {
                UserName = "user",
                Email = testEmail,
                EmailConfirmed = true,
                FullName = "Utente Test",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(testUser, "User123!");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(testUser, PlatformConstants.Roles.User);
            }
        }
        else
        {
            if (!await userManager.IsInRoleAsync(testUser, PlatformConstants.Roles.User))
            {
                await userManager.AddToRoleAsync(testUser, PlatformConstants.Roles.User);
            }
            
            // Forza reset password a User123! per sicurezza
            var token = await userManager.GeneratePasswordResetTokenAsync(testUser);
            await userManager.ResetPasswordAsync(testUser, token, "User123!");
        }
    }
}
