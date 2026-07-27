using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Platform.Portal.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Platform.Portal.Authorization;

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public PermissionHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();

        var httpContext = httpContextAccessor.HttpContext;
        bool isDevMode = httpContext?.Request.Cookies["dev_mode"] == "true";

        // 1. Bypass per Dev Mode attiva per Developer o Admin
        if (isDevMode && (context.User.IsInRole("Developer") || context.User.IsInRole("Admin")))
        {
            context.Succeed(requirement);
            return;
        }

        // 2. Accesso root automatico per l'amministratore (Admin)
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        var permissionParts = requirement.Permission.Split('.');
        if (permissionParts.Length != 2)
        {
            return; // Formato permesso non valido
        }

        var applicationName = permissionParts[0];
        var permissionType = permissionParts[1];

        // 3. Controllo Override specifico per Utente (ApplicationPermissions funge da override)
        var userOverride = await dbContext.ApplicationPermissions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.ApplicationName == applicationName);

        if (userOverride != null)
        {
            bool hasAccess = permissionType switch
            {
                "View" => userOverride.CanView,
                "Create" => userOverride.CanCreate,
                "Edit" => userOverride.CanEdit,
                "Delete" => userOverride.CanDelete,
                _ => false
            };

            if (hasAccess)
            {
                context.Succeed(requirement);
            }
            return; // L'override utente è definitivo, non controlliamo i ruoli
        }

        // 4. Controllo ereditarietà dai ruoli dell'utente (RolePermissions)
        var userRoles = context.User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
        if (userRoles.Any())
        {
            var rolePermissions = await dbContext.RolePermissions
                .Where(p => userRoles.Contains(p.RoleName) && p.ApplicationName == applicationName)
                .ToListAsync();

            if (rolePermissions.Any())
            {
                bool hasAccess = permissionType switch
                {
                    "View" => rolePermissions.Any(rp => rp.CanView),
                    "Create" => rolePermissions.Any(rp => rp.CanCreate),
                    "Edit" => rolePermissions.Any(rp => rp.CanEdit),
                    "Delete" => rolePermissions.Any(rp => rp.CanDelete),
                    _ => false
                };

                if (hasAccess)
                {
                    context.Succeed(requirement);
                }
            }
        }
    }
}
