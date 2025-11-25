using System.ComponentModel.DataAnnotations;

namespace Platform.Portal.Models.ViewModels;

/// <summary>
/// ViewModel per il form di login
/// </summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "Il nome utente è obbligatorio")]
    [Display(Name = "Nome utente o Email")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "La password è obbligatoria")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Ricordami")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
