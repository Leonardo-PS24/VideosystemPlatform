namespace Platform.Portal.Models.ViewModels;

/// <summary>
/// ViewModel per la matrice dei permessi
/// </summary>
public class PermissionMatrixViewModel
{
    public List<UserPermissionRow> Users { get; set; } = new();
    public List<string> Applications { get; set; } = new();
    public string? RoleFilter { get; set; }
    public string? ApplicationFilter { get; set; }
}

/// <summary>
/// Riga utente nella matrice permessi
/// </summary>
public class UserPermissionRow
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public Dictionary<string, AppPermissions> Permissions { get; set; } = new();
}

/// <summary>
/// Permessi per una singola applicazione
/// </summary>
public class AppPermissions
{
    public int PermissionId { get; set; }
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool HasAnyPermission { get; set; }
}