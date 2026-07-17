using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Platform.Portal.Models;
using Platform.Portal.Models.ViewModels;
using Platform.Portal.Services;
using System.Security.Claims;

namespace Platform.Portal.Controllers;

/// <summary>
/// Controller API per la gestione dei permessi delle applicazioni
/// </summary>
[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<PermissionsController> _logger;

    public PermissionsController(
        IPermissionService permissionService,
        UserManager<ApplicationUser> userManager,
        ILogger<PermissionsController> logger)
    {
        _permissionService = permissionService;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Restituisce la matrice dei permessi per tutti gli utenti, con filtri opzionali
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPermissionMatrix(string? roleFilter = null, string? applicationFilter = null)
    {
        try
        {
            var (users, applications) = await _permissionService.GetPermissionMatrixAsync(roleFilter, applicationFilter);
            
            return Ok(new
            {
                users = users,
                applications = applications,
                roleFilter = roleFilter,
                applicationFilter = applicationFilter
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading permission matrix");
            return StatusCode(500, new { message = "Errore durante il caricamento dei permessi." });
        }
    }

    /// <summary>
    /// Restituisce i permessi correnti di un singolo utente
    /// </summary>
    [HttpGet("User/{userId}")]
    public async Task<IActionResult> GetUserPermissions(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new { message = "UserId non valido" });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "Utente non trovato" });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var userPermissions = await _permissionService.GetUserPermissionsAsync(userId);

        var viewModel = new UserPermissionsViewModel
        {
            UserId = user.Id,
            Username = user.UserName ?? "",
            FullName = user.FullName,
            Email = user.Email ?? "",
            Role = roles.FirstOrDefault() ?? "User",
            Applications = new List<ApplicationPermissionItem>()
        };

        // Crea lista applicazioni con permessi
        foreach (var appName in ApplicationName.GetAll())
        {
            var permission = userPermissions.FirstOrDefault(p => p.ApplicationName == appName);

            viewModel.Applications.Add(new ApplicationPermissionItem
            {
                Id = permission?.Id ?? 0,
                ApplicationName = appName,
                DisplayName = ApplicationName.GetDisplayName(appName),
                Icon = ApplicationName.GetIcon(appName),
                CanView = permission?.CanView ?? false,
                CanCreate = permission?.CanCreate ?? false,
                CanEdit = permission?.CanEdit ?? false,
                CanDelete = permission?.CanDelete ?? false
            });
        }

        return Ok(viewModel);
    }

    /// <summary>
    /// Salva i permessi modificati per un utente
    /// </summary>
    [HttpPost("User/{userId}")]
    public async Task<IActionResult> SaveUserPermissions(string userId, [FromBody] UserPermissionsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }

            var permissions = new Dictionary<string, PermissionType>();

            foreach (var app in model.Applications)
            {
                var permissionType = PermissionType.None;

                if (app.CanView) permissionType |= PermissionType.View;
                if (app.CanCreate) permissionType |= PermissionType.Create;
                if (app.CanEdit) permissionType |= PermissionType.Edit;
                if (app.CanDelete) permissionType |= PermissionType.Delete;

                permissions[app.ApplicationName] = permissionType;
            }

            await _permissionService.SavePermissionsAsync(userId, permissions, currentUserId);

            return Ok(new { message = "Permessi salvati con successo" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving permissions for user {UserId}", userId);
            return BadRequest(new { message = "Errore nel salvataggio dei permessi" });
        }
    }

    /// <summary>
    /// Toggle di un singolo permesso (AJAX/API)
    /// </summary>
    [HttpPost("Toggle")]
    public async Task<IActionResult> TogglePermission([FromBody] TogglePermissionRequest request)
    {
        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return BadRequest(new { message = "Utente non autenticato" });
            }

            // Ottieni il permesso esistente
            var existingPermission = await _permissionService.GetPermissionAsync(
                request.UserId, 
                request.ApplicationName);

            var permissionType = GetPermissionType(request.PermissionType);
            var currentValue = GetPermissionValue(existingPermission, permissionType);

            if (currentValue)
            {
                // Revoca il permesso
                await _permissionService.RevokePermissionAsync(
                    request.UserId,
                    request.ApplicationName,
                    permissionType);
            }
            else
            {
                // Concedi il permesso
                await _permissionService.GrantPermissionAsync(
                    request.UserId,
                    request.ApplicationName,
                    permissionType,
                    currentUserId);
            }

            return Ok(new { success = true, newValue = !currentValue });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling permission");
            return BadRequest(new { success = false, message = "Errore nell'operazione" });
        }
    }

    /// <summary>
    /// Elimina tutti i permessi di un utente
    /// </summary>
    [HttpPost("Reset")]
    public async Task<IActionResult> DeleteAllUserPermissions([FromBody] DeletePermissionsRequest request)
    {
        try
        {
            await _permissionService.DeletePermissionsAsync(request.UserId);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting permissions for user {UserId}", request.UserId);
            return BadRequest(new { success = false, message = "Errore nell'eliminazione dei permessi" });
        }
    }

    private bool GetPermissionValue(ApplicationPermission? permission, PermissionType type)
    {
        if (permission == null) return false;

        return type switch
        {
            PermissionType.View => permission.CanView,
            PermissionType.Create => permission.CanCreate,
            PermissionType.Edit => permission.CanEdit,
            PermissionType.Delete => permission.CanDelete,
            _ => false
        };
    }

    private PermissionType GetPermissionType(string typeString)
    {
        return typeString.ToLower() switch
        {
            "view" => PermissionType.View,
            "create" => PermissionType.Create,
            "edit" => PermissionType.Edit,
            "delete" => PermissionType.Delete,
            _ => PermissionType.None
        };
    }

    /// <summary>
    /// Restituisce la matrice dei permessi per tutti i ruoli
    /// </summary>
    [HttpGet("Roles")]
    public async Task<IActionResult> GetRolesPermissions()
    {
        try
        {
            var roles = await _permissionService.GetAllRolesAsync();
            var applications = ApplicationName.GetAll();
            
            var roleMatrix = new List<object>();
            foreach (var role in roles)
            {
                var rolePerms = await _permissionService.GetRolePermissionsAsync(role);
                var appPermissions = applications.ToDictionary(
                    app => app,
                    app =>
                    {
                        var perm = rolePerms.FirstOrDefault(p => p.ApplicationName == app);
                        return new
                        {
                            canView = perm?.CanView ?? false,
                            canCreate = perm?.CanCreate ?? false,
                            canEdit = perm?.CanEdit ?? false,
                            canDelete = perm?.CanDelete ?? false
                        };
                    });
                    
                roleMatrix.Add(new
                {
                    roleName = role,
                    applications = appPermissions
                });
            }

            return Ok(new
            {
                roles = roleMatrix,
                applications = applications
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role permissions");
            return StatusCode(500, new { message = "Errore durante il recupero dei permessi dei ruoli." });
        }
    }

    /// <summary>
    /// Restituisce i permessi di un singolo ruolo
    /// </summary>
    [HttpGet("Role/{roleName}")]
    public async Task<IActionResult> GetRolePermissions(string roleName)
    {
        try
        {
            var rolePerms = await _permissionService.GetRolePermissionsAsync(roleName);
            
            var applications = new List<ApplicationPermissionItem>();
            foreach (var appName in ApplicationName.GetAll())
            {
                var permission = rolePerms.FirstOrDefault(p => p.ApplicationName == appName);
                applications.Add(new ApplicationPermissionItem
                {
                    ApplicationName = appName,
                    DisplayName = ApplicationName.GetDisplayName(appName),
                    Icon = ApplicationName.GetIcon(appName),
                    CanView = permission?.CanView ?? false,
                    CanCreate = permission?.CanCreate ?? false,
                    CanEdit = permission?.CanEdit ?? false,
                    CanDelete = permission?.CanDelete ?? false
                });
            }
            
            return Ok(new
            {
                roleName = roleName,
                applications = applications
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permissions for role {Role}", roleName);
            return StatusCode(500, new { message = "Errore durante il recupero dei permessi del ruolo." });
        }
    }

    /// <summary>
    /// Salva i permessi di un ruolo specifico
    /// </summary>
    [HttpPost("Role/{roleName}")]
    public async Task<IActionResult> SaveRolePermissions(string roleName, [FromBody] SaveRolePermissionsDto model)
    {
        try
        {
            var permissions = new Dictionary<string, PermissionType>();
            foreach (var app in model.Applications)
            {
                var permissionType = PermissionType.None;
                if (app.CanView) permissionType |= PermissionType.View;
                if (app.CanCreate) permissionType |= PermissionType.Create;
                if (app.CanEdit) permissionType |= PermissionType.Edit;
                if (app.CanDelete) permissionType |= PermissionType.Delete;
                
                permissions[app.ApplicationName] = permissionType;
            }
            
            await _permissionService.SaveRolePermissionsAsync(roleName, permissions);
            return Ok(new { message = "Permessi del ruolo salvati con successo" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving permissions for role {Role}", roleName);
            return BadRequest(new { message = "Errore nel salvataggio dei permessi del ruolo" });
        }
    }
}

public class SaveRolePermissionsDto
{
    public List<ApplicationPermissionItem> Applications { get; set; } = new();
}

public class TogglePermissionRequest
{
    public string UserId { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
    public string PermissionType { get; set; } = string.Empty;
}

public class DeletePermissionsRequest
{
    public string UserId { get; set; } = string.Empty;
}