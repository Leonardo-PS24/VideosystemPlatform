using Microsoft.AspNetCore.Identity;
using Platform.Portal.Models;
using Platform.Shared.Constants;

namespace Platform.Portal.Data;

/// <summary>
/// Inizializzatore del database con dati di seed
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Inizializza il database e crea utente admin di default
    /// </summary>
    public static async Task Initialize(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        // In un ambiente di produzione, la creazione automatica di utenti
        // e ruoli di test dovrebbe essere disabilitata.
        // Lo schema del database viene gestito interamente tramite le migrazioni.
        
        /*
        // Applica le migrazioni pending
        await context.Database.EnsureCreatedAsync();

        // Crea i ruoli se non esistono
        if (!await roleManager.RoleExistsAsync(PlatformConstants.Roles.Admin))
        {
            await roleManager.CreateAsync(new IdentityRole(PlatformConstants.Roles.Admin));
        }

        if (!await roleManager.RoleExistsAsync(PlatformConstants.Roles.User))
        {
            await roleManager.CreateAsync(new IdentityRole(PlatformConstants.Roles.User));
        }

        // Crea utente admin se non esiste
        var adminEmail = "admin@videosystem.it";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

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
            }
        }

        // Crea utente test se non esiste
        var testEmail = "user@videosystem.it";
        var testUser = await userManager.FindByEmailAsync(testEmail);

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
        */
        
        await Task.CompletedTask;
    }
}
