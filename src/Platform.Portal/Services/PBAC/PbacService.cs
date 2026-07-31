using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Platform.Portal.Data;
using Platform.Portal.Models;
using Platform.Portal.Models.PBAC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Platform.Portal.Services.PBAC;

public class PbacService : IPbacService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PbacService> _logger;

    public PbacService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PbacService> logger)
    {
        _context = context;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<bool> HasPermissionAsync(string userId, string permissionKey, string? companyId = null, string? departmentId = null)
    {
        if (string.IsNullOrEmpty(userId)) return false;

        // 1. Controllo Dev Mode per utenti Developer o Admin
        var httpContext = _httpContextAccessor?.HttpContext;
        bool isDevMode = httpContext?.Request.Cookies["dev_mode"] == "true";

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive) return false;

        var userRoles = await _userManager.GetRolesAsync(user);

        if (isDevMode && (userRoles.Contains("Developer") || userRoles.Contains("Admin")))
        {
            return true;
        }

        // 2. Gli Admin di sistema hanno sempre accesso totalizzante
        if (userRoles.Contains("Admin"))
        {
            return true;
        }

        // 3. Ottieni tutti i permessi efficaci dell'utente (assegnazioni dirette + assegnazioni da ruoli)
        var effectivePermissions = await GetUserEffectivePermissionsAsync(userId);

        foreach (var perm in effectivePermissions)
        {
            if (perm.PermissionKey.Equals(permissionKey, StringComparison.OrdinalIgnoreCase) || perm.PermissionKey == "*")
            {
                // Verifica Company Scope
                bool companyMatch = perm.CompanyScope == "ALL" || 
                                    string.IsNullOrEmpty(companyId) || 
                                    perm.CompanyScope.Equals(companyId, StringComparison.OrdinalIgnoreCase);

                // Verifica Department Scope
                bool departmentMatch = perm.DepartmentScope == "ALL" || 
                                       string.IsNullOrEmpty(departmentId) || 
                                       perm.DepartmentScope.Equals(departmentId, StringComparison.OrdinalIgnoreCase);

                if (companyMatch && departmentMatch)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public async Task<List<UserPermissionScopeDto>> GetUserEffectivePermissionsAsync(string userId)
    {
        var result = new List<UserPermissionScopeDto>();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return result;

        // A) Policy assegnate direttamente all'Utente
        var userAssignments = await _context.UserPolicyAssignments
            .Include(u => u.Policy!)
                .ThenInclude(p => p.PolicyPermissions)
                    .ThenInclude(pp => pp.PermissionCatalog)
            .Where(u => u.UserId == userId)
            .ToListAsync();

        foreach (var ua in userAssignments)
        {
            if (ua.Policy != null)
            {
                foreach (var pp in ua.Policy.PolicyPermissions)
                {
                    if (pp.PermissionCatalog != null)
                    {
                        result.Add(new UserPermissionScopeDto
                        {
                            PermissionKey = pp.PermissionCatalog.Key,
                            CompanyScope = ua.CompanyScope,
                            DepartmentScope = ua.DepartmentScope
                        });
                    }
                }
            }
        }

        // B) Policy ereditate dai Ruoli dell'Utente
        var userRoles = await _userManager.GetRolesAsync(user);
        if (userRoles.Any())
        {
            var roleAssignments = await _context.RolePolicyAssignments
                .Include(r => r.Policy!)
                    .ThenInclude(p => p.PolicyPermissions)
                        .ThenInclude(pp => pp.PermissionCatalog)
                .Where(r => userRoles.Contains(r.RoleName))
                .ToListAsync();

            foreach (var ra in roleAssignments)
            {
                if (ra.Policy != null)
                {
                    foreach (var pp in ra.Policy.PolicyPermissions)
                    {
                        if (pp.PermissionCatalog != null)
                        {
                            result.Add(new UserPermissionScopeDto
                            {
                                PermissionKey = pp.PermissionCatalog.Key,
                                CompanyScope = ra.CompanyScope,
                                DepartmentScope = ra.DepartmentScope
                            });
                        }
                    }
                }
            }
        }

        return result;
    }

    public async Task RegisterPermissionAsync(string key, string area, string module, string action, string description)
    {
        var existing = await _context.PermissionCatalogs.FirstOrDefaultAsync(p => p.Key == key);
        if (existing == null)
        {
            _context.PermissionCatalogs.Add(new PermissionCatalog
            {
                Key = key,
                Area = area,
                Module = module,
                Action = action,
                Description = description
            });
            await _context.SaveChangesAsync();
            _logger.LogInformation("Registrato nuovo permesso atomico nel catalogo PBAC: {Key}", key);
        }
        else
        {
            existing.Area = area;
            existing.Module = module;
            existing.Action = action;
            existing.Description = description;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<PermissionCatalog>> GetPermissionCatalogAsync()
    {
        var catalog = await _context.PermissionCatalogs
            .OrderBy(p => p.Area)
            .ThenBy(p => p.Module)
            .ThenBy(p => p.Action)
            .ToListAsync();

        if (!catalog.Any())
        {
            await RegisterPermissionAsync("pharmaself:kiosk:view", "Pharmaself24", "ConfigurationKiosk", "View", "Visualizza schede e checklist Configuration Kiosk");
            await RegisterPermissionAsync("pharmaself:kiosk:create", "Pharmaself24", "ConfigurationKiosk", "Create", "Crea nuove checklist Configuration Kiosk");
            await RegisterPermissionAsync("pharmaself:kiosk:edit", "Pharmaself24", "ConfigurationKiosk", "Edit", "Modifica checklist Configuration Kiosk");
            await RegisterPermissionAsync("pharmaself:kiosk:delete", "Pharmaself24", "ConfigurationKiosk", "Delete", "Elimina checklist Configuration Kiosk");

            await RegisterPermissionAsync("skript:checklist:view", "SkriptKiosk", "SkriptkioskChecklist", "View", "Visualizza schede e checklist SkriptKiosk");
            await RegisterPermissionAsync("skript:checklist:create", "SkriptKiosk", "SkriptkioskChecklist", "Create", "Crea nuove checklist SkriptKiosk");
            await RegisterPermissionAsync("skript:checklist:edit", "SkriptKiosk", "SkriptkioskChecklist", "Edit", "Modifica checklist SkriptKiosk");
            await RegisterPermissionAsync("skript:checklist:delete", "SkriptKiosk", "SkriptkioskChecklist", "Delete", "Elimina checklist SkriptKiosk");

            await RegisterPermissionAsync("admin:users:manage", "Admin", "Users", "Manage", "Gestione completa utenti e permessi");

            catalog = await _context.PermissionCatalogs
                .OrderBy(p => p.Area)
                .ThenBy(p => p.Module)
                .ThenBy(p => p.Action)
                .ToListAsync();
        }

        return catalog;
    }

    public async Task<List<PolicyDto>> GetAllPoliciesAsync()
    {
        var policies = await _context.Policies
            .Include(p => p.PolicyPermissions)
                .ThenInclude(pp => pp.PermissionCatalog)
            .OrderBy(p => p.Name)
            .ToListAsync();

        if (!policies.Any())
        {
            var catalog = await GetPermissionCatalogAsync();

            // 1. Amministratore Completo
            var adminPol = new Policy { Name = "Amministratore Completo", Description = "Accesso illimitato a tutti i moduli e sottoaziende", IsSystemPolicy = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            _context.Policies.Add(adminPol);
            await _context.SaveChangesAsync();
            foreach (var perm in catalog) { _context.PolicyPermissions.Add(new PolicyPermission { PolicyId = adminPol.Id, PermissionCatalogId = perm.Id }); }

            // 2. Operatore Pharmaself24
            var pharmaPol = new Policy { Name = "Operatore Pharmaself24", Description = "Gestione completa del modulo Configuration Kiosk per Pharmaself24", IsSystemPolicy = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            _context.Policies.Add(pharmaPol);
            await _context.SaveChangesAsync();
            foreach (var perm in catalog.Where(p => p.Area == "Pharmaself24")) { _context.PolicyPermissions.Add(new PolicyPermission { PolicyId = pharmaPol.Id, PermissionCatalogId = perm.Id }); }

            // 3. Operatore SkriptKiosk
            var skriptPol = new Policy { Name = "Operatore SkriptKiosk", Description = "Gestione completa del modulo SkriptKiosk Checklist", IsSystemPolicy = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            _context.Policies.Add(skriptPol);
            await _context.SaveChangesAsync();
            foreach (var perm in catalog.Where(p => p.Area == "SkriptKiosk")) { _context.PolicyPermissions.Add(new PolicyPermission { PolicyId = skriptPol.Id, PermissionCatalogId = perm.Id }); }

            // 4. Utente Standard Read-Only
            var readPol = new Policy { Name = "Utente Standard Read-Only", Description = "Permesso di sola lettura su tutti i moduli aziendali", IsSystemPolicy = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            _context.Policies.Add(readPol);
            await _context.SaveChangesAsync();
            foreach (var perm in catalog.Where(p => p.Action == "View")) { _context.PolicyPermissions.Add(new PolicyPermission { PolicyId = readPol.Id, PermissionCatalogId = perm.Id }); }

            await _context.SaveChangesAsync();

            policies = await _context.Policies
                .Include(p => p.PolicyPermissions)
                    .ThenInclude(pp => pp.PermissionCatalog)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        return policies.Select(p => new PolicyDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            IsSystemPolicy = p.IsSystemPolicy,
            Permissions = p.PolicyPermissions
                .Where(pp => pp.PermissionCatalog != null)
                .Select(pp => new PermissionCatalogDto
                {
                    Id = pp.PermissionCatalog!.Id,
                    Key = pp.PermissionCatalog.Key,
                    Area = pp.PermissionCatalog.Area,
                    Module = pp.PermissionCatalog.Module,
                    Action = pp.PermissionCatalog.Action,
                    Description = pp.PermissionCatalog.Description
                }).ToList()
        }).ToList();
    }

    public async Task<PolicyDto?> GetPolicyByIdAsync(int policyId)
    {
        var p = await _context.Policies
            .Include(p => p.PolicyPermissions)
                .ThenInclude(pp => pp.PermissionCatalog)
            .FirstOrDefaultAsync(p => p.Id == policyId);

        if (p == null) return null;

        return new PolicyDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            IsSystemPolicy = p.IsSystemPolicy,
            Permissions = p.PolicyPermissions
                .Where(pp => pp.PermissionCatalog != null)
                .Select(pp => new PermissionCatalogDto
                {
                    Id = pp.PermissionCatalog!.Id,
                    Key = pp.PermissionCatalog.Key,
                    Area = pp.PermissionCatalog.Area,
                    Module = pp.PermissionCatalog.Module,
                    Action = pp.PermissionCatalog.Action,
                    Description = pp.PermissionCatalog.Description
                }).ToList()
        };
    }

    public async Task<PolicyDto> SavePolicyAsync(int id, string name, string? description, List<int> permissionCatalogIds)
    {
        Policy? policy;

        if (id > 0)
        {
            policy = await _context.Policies
                .Include(p => p.PolicyPermissions)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (policy == null)
            {
                throw new KeyNotFoundException($"Policy con ID {id} non trovata.");
            }

            policy.Name = name;
            policy.Description = description;
            policy.UpdatedAt = DateTime.UtcNow;

            // Aggiorna relazioni permessi
            _context.PolicyPermissions.RemoveRange(policy.PolicyPermissions);
        }
        else
        {
            policy = new Policy
            {
                Name = name,
                Description = description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Policies.Add(policy);
            await _context.SaveChangesAsync(); // salva per ottenere l'ID
        }

        foreach (var permId in permissionCatalogIds.Distinct())
        {
            _context.PolicyPermissions.Add(new PolicyPermission
            {
                PolicyId = policy.Id,
                PermissionCatalogId = permId
            });
        }

        await _context.SaveChangesAsync();

        return (await GetPolicyByIdAsync(policy.Id))!;
    }

    public async Task<bool> DeletePolicyAsync(int policyId)
    {
        var policy = await _context.Policies.FindAsync(policyId);
        if (policy == null) return false;

        if (policy.IsSystemPolicy)
        {
            throw new InvalidOperationException($"Non è possibile eliminare la Policy di sistema '{policy.Name}'.");
        }

        // Rimuovi relazioni
        var pPermissions = await _context.PolicyPermissions.Where(pp => pp.PolicyId == policyId).ToListAsync();
        _context.PolicyPermissions.RemoveRange(pPermissions);

        var uAssignments = await _context.UserPolicyAssignments.Where(ua => ua.PolicyId == policyId).ToListAsync();
        _context.UserPolicyAssignments.RemoveRange(uAssignments);

        var rAssignments = await _context.RolePolicyAssignments.Where(ra => ra.PolicyId == policyId).ToListAsync();
        _context.RolePolicyAssignments.RemoveRange(rAssignments);

        _context.Policies.Remove(policy);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task AssignPolicyToUserAsync(string userId, int policyId, string companyScope = "ALL", string departmentScope = "ALL")
    {
        var existing = await _context.UserPolicyAssignments
            .FirstOrDefaultAsync(u => u.UserId == userId && u.PolicyId == policyId && u.CompanyScope == companyScope && u.DepartmentScope == departmentScope);

        if (existing == null)
        {
            _context.UserPolicyAssignments.Add(new UserPolicyAssignment
            {
                UserId = userId,
                PolicyId = policyId,
                CompanyScope = companyScope,
                DepartmentScope = departmentScope,
                AssignedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
    }

    public async Task RemovePolicyFromUserAsync(int userPolicyAssignmentId)
    {
        var assignment = await _context.UserPolicyAssignments.FindAsync(userPolicyAssignmentId);
        if (assignment != null)
        {
            _context.UserPolicyAssignments.Remove(assignment);
            await _context.SaveChangesAsync();
        }
    }

    public async Task AssignPolicyToRoleAsync(string roleName, int policyId, string companyScope = "ALL", string departmentScope = "ALL")
    {
        var existing = await _context.RolePolicyAssignments
            .FirstOrDefaultAsync(r => r.RoleName == roleName && r.PolicyId == policyId && r.CompanyScope == companyScope && r.DepartmentScope == departmentScope);

        if (existing == null)
        {
            _context.RolePolicyAssignments.Add(new RolePolicyAssignment
            {
                RoleName = roleName,
                PolicyId = policyId,
                CompanyScope = companyScope,
                DepartmentScope = departmentScope,
                AssignedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
    }

    public async Task RemovePolicyFromRoleAsync(int rolePolicyAssignmentId)
    {
        var assignment = await _context.RolePolicyAssignments.FindAsync(rolePolicyAssignmentId);
        if (assignment != null)
        {
            _context.RolePolicyAssignments.Remove(assignment);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<UserPolicyAssignmentDto>> GetUserPolicyAssignmentsAsync(string userId)
    {
        var list = await _context.UserPolicyAssignments
            .Include(u => u.Policy)
            .Where(u => u.UserId == userId)
            .ToListAsync();

        return list.Select(u => new UserPolicyAssignmentDto
        {
            Id = u.Id,
            UserId = u.UserId,
            PolicyId = u.PolicyId,
            PolicyName = u.Policy?.Name ?? "",
            CompanyScope = u.CompanyScope,
            DepartmentScope = u.DepartmentScope
        }).ToList();
    }

    public async Task<List<RolePolicyAssignmentDto>> GetRolePolicyAssignmentsAsync(string? roleName = null)
    {
        var query = _context.RolePolicyAssignments.Include(r => r.Policy).AsQueryable();

        if (!string.IsNullOrEmpty(roleName))
        {
            query = query.Where(r => r.RoleName == roleName);
        }

        var list = await query.ToListAsync();

        return list.Select(r => new RolePolicyAssignmentDto
        {
            Id = r.Id,
            RoleName = r.RoleName,
            PolicyId = r.PolicyId,
            PolicyName = r.Policy?.Name ?? "",
            CompanyScope = r.CompanyScope,
            DepartmentScope = r.DepartmentScope
        }).ToList();
    }
}
