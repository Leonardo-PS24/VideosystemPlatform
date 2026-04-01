using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Platform.Portal.Data;

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
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        // Se l'utente è un Admin, ha tutti i permessi
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var permissionParts = requirement.Permission.Split('.');
        if (permissionParts.Length != 2)
        {
            return; // Formato permesso non valido
        }

        var applicationName = permissionParts[0];
        var permissionType = permissionParts[1];

        var hasPermission = await dbContext.ApplicationPermissions
            .AnyAsync(p => p.UserId == userId && p.ApplicationName == applicationName &&
                           (permissionType == "View" && p.CanView ||
                            permissionType == "Create" && p.CanCreate ||
                            permissionType == "Edit" && p.CanEdit ||
                            permissionType == "Delete" && p.CanDelete));

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}
