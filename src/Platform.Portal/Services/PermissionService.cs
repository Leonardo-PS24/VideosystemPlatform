using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Platform.Portal.Data;
using Platform.Portal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Platform.Portal.Models.ViewModels;
using Microsoft.AspNetCore.Http;

namespace Platform.Portal.Services;

/// <summary>
/// Implementazione del servizio di gestione permessi
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PermissionService> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    // ... (metodi esistenti non modificati) ...
    public async Task<bool> HasPermissionAsync(string userId, string applicationName, PermissionType permission)
    {
        // 1. Controllo Dev Mode attiva per Developer o Admin
        var httpContext = _httpContextAccessor?.HttpContext;
        bool isDevMode = httpContext?.Request.Cookies["dev_mode"] == "true";

        if (isDevMode)
        {
            var devUser = await _userManager.FindByIdAsync(userId);
            if (devUser != null)
            {
                var roles = await _userManager.GetRolesAsync(devUser);
                if (roles.Contains("Developer") || roles.Contains("Admin"))
                {
                    return true;
                }
            }
        }

        // 2. Admin ha sempre tutti i permessi
        if (await IsAdminAsync(userId))
        {
            return true;
        }

        // Check company authorization toggle first to prevent access if the company is disabled
        if (!applicationName.StartsWith("Company_"))
        {
            var companyId = applicationName switch
            {
                "ConfigurationKiosk" => "Pharmaself24",
                "SkriptkioskChecklist" => "Skriptkiosk",
                _ => ""
            };

            if (!string.IsNullOrEmpty(companyId))
            {
                var companyPermission = $"Company_{companyId}";
                var hasCompanyAccess = await HasCompanyPermissionDirectAsync(userId, companyPermission);
                if (!hasCompanyAccess)
                {
                    return false;
                }
            }
        }

        // 3. Controllo Override specifico per Utente
        var userOverride = await GetPermissionAsync(userId, applicationName);
        if (userOverride != null)
        {
            return userOverride.HasPermission(permission);
        }

        // 4. Controllo ereditarietà dai ruoli dell'utente (reparti)
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            if (userRoles.Any())
            {
                var rolePermissions = await _context.RolePermissions
                    .Where(p => userRoles.Contains(p.RoleName) && p.ApplicationName == applicationName)
                    .ToListAsync();

                if (rolePermissions.Any())
                {
                    return permission switch
                    {
                        PermissionType.View => rolePermissions.Any(rp => rp.CanView),
                        PermissionType.Create => rolePermissions.Any(rp => rp.CanCreate),
                        PermissionType.Edit => rolePermissions.Any(rp => rp.CanEdit),
                        PermissionType.Delete => rolePermissions.Any(rp => rp.CanDelete),
                        _ => false
                    };
                }
            }
        }

        return false;
    }
    public async Task<bool> IsAdminAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        var roles = await _userManager.GetRolesAsync(user);
        return roles.Contains("Admin");
    }
    public async Task<List<ApplicationPermission>> GetUserPermissionsAsync(string userId)
    {
        return await _context.ApplicationPermissions
            .Where(p => p.UserId == userId)
            .ToListAsync();
    }
    public async Task<ApplicationPermission?> GetPermissionAsync(string userId, string applicationName)
    {
        return await _context.ApplicationPermissions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.ApplicationName == applicationName);
    }
    public async Task GrantPermissionAsync(string userId, string applicationName, PermissionType permission, string grantedBy)
    {
        var existingPermission = await GetPermissionAsync(userId, applicationName);

        if (existingPermission == null)
        {
            // Crea nuovo permesso
            existingPermission = new ApplicationPermission
            {
                UserId = userId,
                ApplicationName = applicationName,
                GrantedBy = grantedBy,
                GrantedAt = DateTime.UtcNow
            };
            
            existingPermission.SetPermission(permission, true);
            
            _context.ApplicationPermissions.Add(existingPermission);
            
            _logger.LogInformation(
                "Granted permission {Permission} for {Application} to user {UserId} by {GrantedBy}",
                permission, applicationName, userId, grantedBy);
        }
        else
        {
            // Aggiorna permesso esistente
            existingPermission.SetPermission(permission, true);
            existingPermission.UpdatedAt = DateTime.UtcNow;
            
            _logger.LogInformation(
                "Updated permission {Permission} for {Application} for user {UserId}",
                permission, applicationName, userId);
        }

        await _context.SaveChangesAsync();
    }
    public async Task RevokePermissionAsync(string userId, string applicationName, PermissionType permission)
    {
        var existingPermission = await GetPermissionAsync(userId, applicationName);

        if (existingPermission != null)
        {
            existingPermission.SetPermission(permission, false);
            existingPermission.UpdatedAt = DateTime.UtcNow;

            // Se tutti i permessi sono false, elimina il record
            if (!existingPermission.CanView && !existingPermission.CanCreate && 
                !existingPermission.CanEdit && !existingPermission.CanDelete)
            {
                _context.ApplicationPermissions.Remove(existingPermission);
                
                _logger.LogInformation(
                    "Removed all permissions for {Application} from user {UserId}",
                    applicationName, userId);
            }
            else
            {
                _logger.LogInformation(
                    "Revoked permission {Permission} for {Application} from user {UserId}",
                    permission, applicationName, userId);
            }

            await _context.SaveChangesAsync();
        }
    }
    public async Task SavePermissionsAsync(string userId, Dictionary<string, PermissionType> permissions, string grantedBy)
    {
        foreach (var kvp in permissions)
        {
            var applicationName = kvp.Key;
            var permission = kvp.Value;

            var existingPermission = await GetPermissionAsync(userId, applicationName);

            if (permission == PermissionType.None)
            {
                // Rimuovi permesso se esiste
                if (existingPermission != null)
                {
                    _context.ApplicationPermissions.Remove(existingPermission);
                }
            }
            else
            {
                if (existingPermission == null)
                {
                    // Crea nuovo permesso
                    existingPermission = new ApplicationPermission
                    {
                        UserId = userId,
                        ApplicationName = applicationName,
                        GrantedBy = grantedBy,
                        GrantedAt = DateTime.UtcNow
                    };
                    
                    existingPermission.SetPermission(permission, true);
                    _context.ApplicationPermissions.Add(existingPermission);
                }
                else
                {
                    // Aggiorna permesso esistente
                    existingPermission.CanView = permission.HasFlag(PermissionType.View);
                    existingPermission.CanCreate = permission.HasFlag(PermissionType.Create);
                    existingPermission.CanEdit = permission.HasFlag(PermissionType.Edit);
                    existingPermission.CanDelete = permission.HasFlag(PermissionType.Delete);
                    existingPermission.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        await _context.SaveChangesAsync();
        
        _logger.LogInformation(
            "Saved permissions for user {UserId} by {GrantedBy}",
            userId, grantedBy);
    }
    public async Task DeletePermissionsAsync(string userId)
    {
        var permissions = await GetUserPermissionsAsync(userId);
        
        if (permissions.Any())
        {
            _context.ApplicationPermissions.RemoveRange(permissions);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation(
                "Deleted all permissions for user {UserId}",
                userId);
        }
    }


    /// <summary>
    /// Ottiene la matrice permessi per tutti gli utenti
    /// </summary>
    public async Task<(List<UserPermissionRow> Users, List<string> Applications)> GetPermissionMatrixAsync(string? roleFilter = null, string? applicationFilter = null)
    {
        try
        {
            // 1. Ottieni tutti gli utenti o filtrali per ruolo
            var usersQuery = _userManager.Users;
            if (!string.IsNullOrEmpty(roleFilter))
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(roleFilter);
                var userIdsInRole = usersInRole.Select(u => u.Id).ToList();
                usersQuery = usersQuery.Where(u => userIdsInRole.Contains(u.Id));
            }
            var users = await usersQuery.ToListAsync();

            // 2. Ottieni tutti i permessi necessari in una sola query
            var userIds = users.Select(u => u.Id).ToList();
            var permissionsQuery = _context.ApplicationPermissions.Where(p => userIds.Contains(p.UserId));
            if (!string.IsNullOrEmpty(applicationFilter))
            {
                permissionsQuery = permissionsQuery.Where(p => p.ApplicationName == applicationFilter);
            }
            var allPermissions = await permissionsQuery.ToListAsync();

            // 3. Ottieni i ruoli per ogni utente
            var userRoles = new Dictionary<string, string>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles.FirstOrDefault() ?? "User";
            }

            // 4. Definisci le applicazioni da mostrare
            var applications = string.IsNullOrEmpty(applicationFilter)
                ? ApplicationName.GetAll()
                : new List<string> { applicationFilter };

            // 5. Costruisci il ViewModel
            var userRows = users.Select(user => new UserPermissionRow
            {
                UserId = user.Id,
                Username = user.UserName ?? "",
                FullName = user.FullName ?? "",
                Role = userRoles[user.Id],
                IsActive = user.IsActive,
                Permissions = applications.ToDictionary(
                    app => app,
                    app =>
                    {
                        var perm = allPermissions.FirstOrDefault(p => p.UserId == user.Id && p.ApplicationName == app);
                        return new AppPermissions
                        {
                            PermissionId = perm?.Id ?? 0,
                            CanView = perm?.CanView ?? false,
                            CanCreate = perm?.CanCreate ?? false,
                            CanEdit = perm?.CanEdit ?? false,
                            CanDelete = perm?.CanDelete ?? false,
                            HasAnyPermission = perm != null && (perm.CanView || perm.CanCreate || perm.CanEdit || perm.CanDelete)
                        };
                    })
            }).ToList();

            return (userRows, applications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permission matrix");
            throw;
        }
    }

    public async Task<List<RolePermission>> GetRolePermissionsAsync(string roleName)
    {
        return await _context.RolePermissions
            .Where(rp => rp.RoleName == roleName)
            .ToListAsync();
    }

    public async Task SaveRolePermissionsAsync(string roleName, Dictionary<string, PermissionType> permissions)
    {
        foreach (var kvp in permissions)
        {
            var applicationName = kvp.Key;
            var permission = kvp.Value;

            var existingPermission = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleName == roleName && rp.ApplicationName == applicationName);

            if (permission == PermissionType.None)
            {
                if (existingPermission != null)
                {
                    _context.RolePermissions.Remove(existingPermission);
                }
            }
            else
            {
                if (existingPermission == null)
                {
                    existingPermission = new RolePermission
                    {
                        RoleName = roleName,
                        ApplicationName = applicationName
                    };
                    _context.RolePermissions.Add(existingPermission);
                }

                existingPermission.CanView = permission.HasFlag(PermissionType.View);
                existingPermission.CanCreate = permission.HasFlag(PermissionType.Create);
                existingPermission.CanEdit = permission.HasFlag(PermissionType.Edit);
                existingPermission.CanDelete = permission.HasFlag(PermissionType.Delete);
                existingPermission.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Saved role-based permissions for role {RoleName}", roleName);
    }

    public async Task<List<string>> GetAllRolesAsync()
    {
        return await _roleManager.Roles
            .Select(r => r.Name!)
            .OrderBy(r => r)
            .ToListAsync();
    }

    public async Task<bool> CreateRoleAsync(string roleName, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            throw new ArgumentException("Il nome del ruolo è obbligatorio.", nameof(roleName));
        }

        var normalizedRoleName = roleName.Trim();

        if (await _roleManager.RoleExistsAsync(normalizedRoleName))
        {
            throw new InvalidOperationException($"Il ruolo '{normalizedRoleName}' esiste già.");
        }

        var result = await _roleManager.CreateAsync(new IdentityRole(normalizedRoleName));
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Errore durante la creazione del ruolo Identity: {errors}");
        }

        try
        {
            var customRole = new Role
            {
                Name = normalizedRoleName,
                Description = description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.CustomRoles.Add(customRole);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Impossibile salvare i metadati aggiuntivi per il ruolo {RoleName} nella tabella CustomRoles.", normalizedRoleName);
        }

        _logger.LogInformation("Creato nuovo ruolo personalizzato: {RoleName}", normalizedRoleName);
        return true;
    }

    public async Task<bool> DeleteRoleAsync(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            throw new ArgumentException("Il nome del ruolo è obbligatorio.", nameof(roleName));
        }

        var protectedRoles = new[] { "Admin", "Developer", "User" };
        if (protectedRoles.Contains(roleName, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Non è possibile eliminare il ruolo di sistema '{roleName}'.");
        }

        var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
        if (usersInRole.Any())
        {
            throw new InvalidOperationException($"Impossibile eliminare il ruolo '{roleName}' perché è attualmente assegnato a {usersInRole.Count} utenti.");
        }

        // Rimuovi permessi associati al ruolo
        var permissions = await _context.RolePermissions
            .Where(rp => rp.RoleName == roleName)
            .ToListAsync();
        if (permissions.Any())
        {
            _context.RolePermissions.RemoveRange(permissions);
        }

        try
        {
            // Rimuovi entity Role personalizzata
            var customRole = await _context.CustomRoles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (customRole != null)
            {
                _context.CustomRoles.Remove(customRole);
            }
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Nota: Impossibile eliminare i metadati da CustomRoles per il ruolo {RoleName}", roleName);
        }

        // Rimuovi IdentityRole
        var identityRole = await _roleManager.FindByNameAsync(roleName);
        if (identityRole != null)
        {
            await _roleManager.DeleteAsync(identityRole);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Eliminato ruolo: {RoleName}", roleName);
        return true;
    }

    private async Task<bool> HasCompanyPermissionDirectAsync(string userId, string companyPermission)
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        bool isDevMode = httpContext?.Request.Cookies["dev_mode"] == "true";
        if (isDevMode)
        {
            var devUser = await _userManager.FindByIdAsync(userId);
            if (devUser != null)
            {
                var roles = await _userManager.GetRolesAsync(devUser);
                if (roles.Contains("Developer") || roles.Contains("Admin"))
                {
                    return true;
                }
            }
        }

        if (await IsAdminAsync(userId))
        {
            return true;
        }

        var userOverride = await GetPermissionAsync(userId, companyPermission);
        if (userOverride != null)
        {
            return userOverride.CanView;
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            if (userRoles.Any())
            {
                var rolePermissions = await _context.RolePermissions
                    .Where(p => userRoles.Contains(p.RoleName) && p.ApplicationName == companyPermission)
                    .ToListAsync();
                if (rolePermissions.Any())
                {
                    return rolePermissions.Any(rp => rp.CanView);
                }
            }
        }

        return false;
    }
}
