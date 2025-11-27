using Platform.Portal.Models;
using Platform.Portal.Models.ViewModels;

namespace Platform.Portal.Services;

/// <summary>
/// Servizio per la gestione dei permessi delle applicazioni
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Verifica se un utente ha un permesso specifico per un'applicazione
    /// </summary>
    /// <param name="userId">ID dell'utente</param>
    /// <param name="applicationName">Nome dell'applicazione</param>
    /// <param name="permission">Tipo di permesso da verificare</param>
    /// <returns>True se l'utente ha il permesso, False altrimenti</returns>
    Task<bool> HasPermissionAsync(string userId, string applicationName, PermissionType permission);
    
    /// <summary>
    /// Verifica se un utente è amministratore
    /// </summary>
    /// <param name="userId">ID dell'utente</param>
    /// <returns>True se l'utente è Admin, False altrimenti</returns>
    Task<bool> IsAdminAsync(string userId);
    
    /// <summary>
    /// Ottiene tutti i permessi di un utente
    /// </summary>
    /// <param name="userId">ID dell'utente</param>
    /// <returns>Lista dei permessi dell'utente</returns>
    Task<List<ApplicationPermission>> GetUserPermissionsAsync(string userId);
    
    /// <summary>
    /// Ottiene il permesso specifico di un utente per un'applicazione
    /// </summary>
    /// <param name="userId">ID dell'utente</param>
    /// <param name="applicationName">Nome dell'applicazione</param>
    /// <returns>Il permesso se esiste, null altrimenti</returns>
    Task<ApplicationPermission?> GetPermissionAsync(string userId, string applicationName);
    
    /// <summary>
    /// Concede un permesso a un utente
    /// </summary>
    /// <param name="userId">ID dell'utente</param>
    /// <param name="applicationName">Nome dell'applicazione</param>
    /// <param name="permission">Tipo di permesso da concedere</param>
    /// <param name="grantedBy">ID dell'utente che concede il permesso</param>
    Task GrantPermissionAsync(string userId, string applicationName, PermissionType permission, string grantedBy);
    
    /// <summary>
    /// Revoca un permesso da un utente
    /// </summary>
    /// <param name="userId">ID dell'utente</param>
    /// <param name="applicationName">Nome dell'applicazione</param>
    /// <param name="permission">Tipo di permesso da revocare</param>
    Task RevokePermissionAsync(string userId, string applicationName, PermissionType permission);
    
    /// <summary>
    /// Salva i permessi di un utente (batch update)
    /// </summary>
    /// <param name="userId">ID dell'utente</param>
    /// <param name="permissions">Dictionary con ApplicationName -> PermissionType</param>
    /// <param name="grantedBy">ID dell'utente che modifica i permessi</param>
    Task SavePermissionsAsync(string userId, Dictionary<string, PermissionType> permissions, string grantedBy);
    
    /// <summary>
    /// Elimina tutti i permessi di un utente
    /// </summary>
    /// <param name="userId">ID dell'utente</param>
    Task DeletePermissionsAsync(string userId);
    
    /// <summary>
    /// Ottiene la matrice permessi per tutti gli utenti
    /// </summary>
    /// <param name="roleFilter">Filtro per ruolo (opzionale)</param>
    /// <param name="applicationFilter">Filtro per applicazione (opzionale)</param>
    /// <returns>Oggetto con la matrice permessi</returns>
    Task<(List<UserPermissionRow> Users, List<string> Applications)> GetPermissionMatrixAsync(string? roleFilter = null, string? applicationFilter = null);
}