using System.ComponentModel.DataAnnotations;

namespace Platform.Portal.Models.ViewModels;

/// <summary>
/// ViewModel per la creazione/modifica di un utente
/// </summary>
public class UserViewModel
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "Il nome utente è obbligatorio")]
    [StringLength(50, ErrorMessage = "Il nome utente non può superare i 50 caratteri")]
    [Display(Name = "Nome utente")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "L'email è obbligatoria")]
    [EmailAddress(ErrorMessage = "Email non valida")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Il nome completo è obbligatorio")]
    [StringLength(200, ErrorMessage = "Il nome completo non può superare i 200 caratteri")]
    [Display(Name = "Nome completo")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Attivo")]
    public bool IsActive { get; set; } = true;

    [Required(ErrorMessage = "Il ruolo è obbligatorio")]
    [Display(Name = "Ruolo")]
    public string Role { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [StringLength(100, ErrorMessage = "La password deve essere di almeno {2} caratteri", MinimumLength = 8)]
    [Display(Name = "Password")]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Conferma password")]
    [Compare("Password", ErrorMessage = "Le password non coincidono")]
    public string? ConfirmPassword { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// ViewModel per la lista degli utenti
/// </summary>
public class UserListViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
