using Platform.Portal.Models.PBAC;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Platform.Portal.Services.PBAC;

public interface IPbacService
{
    /// <summary>
    /// Verifica se l'utente ha uno specifico permesso atomico, opzionalmente filtrato per Sottoazienda e Reparto
    /// </summary>
    Task<bool> HasPermissionAsync(string userId, string permissionKey, string? companyId = null, string? departmentId = null);

    /// <summary>
    /// Ottiene tutte le chiavi di permesso concesse a un determinato utente con i relativi ambiti (Scope)
    /// </summary>
    Task<List<UserPermissionScopeDto>> GetUserEffectivePermissionsAsync(string userId);

    /// <summary>
    /// Registra o aggiorna un permesso atomico nel catalogo
    /// </summary>
    Task RegisterPermissionAsync(string key, string area, string module, string action, string description);

    /// <summary>
    /// Ottiene tutto il catalogo dei permessi atomici disponibili
    /// </summary>
    Task<List<PermissionCatalog>> GetPermissionCatalogAsync();

    /// <summary>
    /// Ottiene tutte le policy con i relativi permessi associati
    /// </summary>
    Task<List<PolicyDto>> GetAllPoliciesAsync();

    /// <summary>
    /// Ottiene i dettagli di una specifica policy
    /// </summary>
    Task<PolicyDto?> GetPolicyByIdAsync(int policyId);

    /// <summary>
    /// Crea o aggiorna una Policy (con la lista di ID dei permessi atomici)
    /// </summary>
    Task<PolicyDto> SavePolicyAsync(int id, string name, string? description, List<int> permissionCatalogIds);

    /// <summary>
    /// Elimina una Policy personalizzata (le policy di sistema sono protette)
    /// </summary>
    Task<bool> DeletePolicyAsync(int policyId);

    /// <summary>
    /// Assegna una Policy a un Utente con filtri di ambito (Company e Department)
    /// </summary>
    Task AssignPolicyToUserAsync(string userId, int policyId, string companyScope = "ALL", string departmentScope = "ALL");

    /// <summary>
    /// Rimuove un'assegnazione Policy da un Utente
    /// </summary>
    Task RemovePolicyFromUserAsync(int userPolicyAssignmentId);

    /// <summary>
    /// Assegna una Policy a un Ruolo con filtri di ambito (Company e Department)
    /// </summary>
    Task AssignPolicyToRoleAsync(string roleName, int policyId, string companyScope = "ALL", string departmentScope = "ALL");

    /// <summary>
    /// Rimuove un'assegnazione Policy da un Ruolo
    /// </summary>
    Task RemovePolicyFromRoleAsync(int rolePolicyAssignmentId);

    /// <summary>
    /// Ottiene le assegnazioni Policy di un determinato Utente
    /// </summary>
    Task<List<UserPolicyAssignmentDto>> GetUserPolicyAssignmentsAsync(string userId);

    /// <summary>
    /// Ottiene le assegnazioni Policy di tutti i Ruoli
    /// </summary>
    Task<List<RolePolicyAssignmentDto>> GetRolePolicyAssignmentsAsync(string? roleName = null);
}

public class UserPermissionScopeDto
{
    public string PermissionKey { get; set; } = string.Empty;
    public string CompanyScope { get; set; } = "ALL";
    public string DepartmentScope { get; set; } = "ALL";
}

public class PolicyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemPolicy { get; set; }
    public List<PermissionCatalogDto> Permissions { get; set; } = new();
}

public class PermissionCatalogDto
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class UserPolicyAssignmentDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int PolicyId { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public string CompanyScope { get; set; } = "ALL";
    public string DepartmentScope { get; set; } = "ALL";
}

public class RolePolicyAssignmentDto
{
    public int Id { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public int PolicyId { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public string CompanyScope { get; set; } = "ALL";
    public string DepartmentScope { get; set; } = "ALL";
}
