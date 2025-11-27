using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Platform.Portal.Models;
using Platform.Portal.Models.ViewModels;
using Platform.Portal.Services;
using System.Security.Claims;


namespace Platform.Portal.Controllers;

/// <summary>
/// Controller per la gestione dei permessi delle applicazioni
/// </summary>
[Authorize(Roles = "Admin")]
public class PermissionsController : Controller
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

    [HttpGet]
    public async Task<IActionResult> Index(string? roleFilter = null, string? applicationFilter = null)
    {
        try
        {
            var (users, applications) = await _permissionService.GetPermissionMatrixAsync(roleFilter, applicationFilter);
            
            var viewModel = new PermissionMatrixViewModel
            {
                Users = users,
                Applications = applications,
                RoleFilter = roleFilter,
                ApplicationFilter = applicationFilter
            };
            
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading permission matrix");
            TempData["ErrorMessage"] = "Errore durante il caricamento dei permessi.";
            return RedirectToAction("Index", "Home");
        }
    }

    /// <summary>
    /// Visualizza il form di modifica permessi per un utente
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> EditUser(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
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

        return View(viewModel);
    }

    /// <summary>
    /// Salva i permessi modificati per un utente
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(UserPermissionsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }

            // Converti i permessi in dictionary
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

            await _permissionService.SavePermissionsAsync(model.UserId, permissions, currentUserId);

            TempData["Success"] = "Permessi salvati con successo";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving permissions for user {UserId}", model.UserId);
            TempData["Error"] = "Errore nel salvataggio dei permessi";
            return View(model);
        }
    }

    /// <summary>
    /// Toggle di un singolo permesso (AJAX)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> TogglePermission([FromBody] TogglePermissionRequest request)
    {
        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Json(new { success = false, message = "Utente non autenticato" });
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

            return Json(new { success = true, newValue = !currentValue });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling permission");
            return Json(new { success = false, message = "Errore nell'operazione" });
        }
    }

    /// <summary>
    /// Elimina tutti i permessi di un utente (AJAX)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> DeleteAllUserPermissions([FromBody] DeletePermissionsRequest request)
    {
        try
        {
            await _permissionService.DeletePermissionsAsync(request.UserId);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting permissions for user {UserId}", request.UserId);
            return Json(new { success = false, message = "Errore nell'eliminazione dei permessi" });
        }
    }

    /// <summary>
    /// Helper: Ottiene il valore di un permesso specifico
    /// </summary>
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

    /// <summary>
    /// Helper: Converte stringa in PermissionType
    /// </summary>
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
}

/// <summary>
/// Request per toggle permesso
/// </summary>
public class TogglePermissionRequest
{
    public string UserId { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
    public string PermissionType { get; set; } = string.Empty;
}

/// <summary>
/// Request per eliminazione permessi
/// </summary>
public class DeletePermissionsRequest
{
    public string UserId { get; set; } = string.Empty;
}