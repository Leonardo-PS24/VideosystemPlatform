namespace Platform.Portal.Models.ViewModels;

/// <summary>
/// ViewModel per la modifica dei permessi di un singolo utente
/// </summary>
public class UserPermissionsViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public List<ApplicationPermissionItem> Applications { get; set; } = new();
}

/// <summary>
/// Singola applicazione con i suoi permessi
/// </summary>
public class ApplicationPermissionItem
{
    public int Id { get; set; }
    public string ApplicationName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}

/// <summary>
/// DTO per il salvataggio dei permessi
/// </summary>
public class SavePermissionsDto
{
    public string UserId { get; set; } = string.Empty;
    public List<PermissionDto> Permissions { get; set; } = new();
}

/// <summary>
/// Singolo permesso nel DTO
/// </summary>
public class PermissionDto
{
    public string ApplicationName { get; set; } = string.Empty;
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}