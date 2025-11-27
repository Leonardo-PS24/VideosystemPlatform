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

namespace Platform.Portal.Services;

/// <summary>
/// Implementazione del servizio di gestione permessi
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<PermissionService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Verifica se un utente ha un permesso specifico
    /// </summary>
    public async Task<bool> HasPermissionAsync(string userId, string applicationName, PermissionType permission)
    {
        // Admin ha sempre tutti i permessi
        if (await IsAdminAsync(userId))
        {
            return true;
        }

        var userPermission = await GetPermissionAsync(userId, applicationName);
        
        if (userPermission == null)
        {
            return false;
        }

        return userPermission.HasPermission(permission);
    }

    /// <summary>
    /// Verifica se un utente è amministratore
    /// </summary>
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

    /// <summary>
    /// Ottiene tutti i permessi di un utente
    /// </summary>
    public async Task<List<ApplicationPermission>> GetUserPermissionsAsync(string userId)
    {
        return await _context.ApplicationPermissions
            .Where(p => p.UserId == userId)
            .ToListAsync();
    }

    /// <summary>
    /// Ottiene il permesso specifico di un utente per un'applicazione
    /// </summary>
    public async Task<ApplicationPermission?> GetPermissionAsync(string userId, string applicationName)
    {
        return await _context.ApplicationPermissions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.ApplicationName == applicationName);
    }

    /// <summary>
    /// Concede un permesso a un utente
    /// </summary>
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

    /// <summary>
    /// Revoca un permesso da un utente
    /// </summary>
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

    /// <summary>
    /// Salva i permessi di un utente (batch)
    /// </summary>
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

    /// <summary>
    /// Elimina tutti i permessi di un utente
    /// </summary>
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
            // Query base: tutti gli utenti con i loro permessi
            var query = _context.ApplicationPermissions
                .Include(p => p.User)
                .AsQueryable();

            // Filtro per ruolo
            List<string>? filteredUserIds = null;
            if (!string.IsNullOrEmpty(roleFilter))
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(roleFilter);
                filteredUserIds = usersInRole.Select(u => u.Id).ToList();
                query = query.Where(p => filteredUserIds.Contains(p.UserId));
            }

            // Filtro per applicazione
            if (!string.IsNullOrEmpty(applicationFilter))
            {
                query = query.Where(p => p.ApplicationName == applicationFilter);
            }

            var permissions = await query.ToListAsync();

            // Ottieni tutti gli utenti unici
            var uniqueUserIds = permissions.Select(p => p.UserId).Distinct().ToList();
            var users = await _context.Users
                .Where(u => uniqueUserIds.Contains(u.Id))
                .ToListAsync();

            // Ottieni ruoli per ogni utente
            var userRoles = new Dictionary<string, string>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles.FirstOrDefault() ?? "User";
            }

            // Applicazioni da mostrare
            var applications = string.IsNullOrEmpty(applicationFilter)
                ? ApplicationName.GetAll()
                : new List<string> { applicationFilter };

            // Crea le righe del ViewModel
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
                        var perm = permissions.FirstOrDefault(p => p.UserId == user.Id && p.ApplicationName == app);
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
}