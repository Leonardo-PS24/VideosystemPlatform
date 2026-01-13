using System.ComponentModel.DataAnnotations;

namespace Platform.Portal.Models.ViewModels;

/// <summary>
/// ViewModel per la pagina di impostazione della password
/// </summary>
public class SetPasswordViewModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nuova password è obbligatoria")]
    [DataType(DataType.Password)]
    [StringLength(100, ErrorMessage = "La password deve essere di almeno {2} caratteri", MinimumLength = 8)]
    [Display(Name = "Nuova Password")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Conferma nuova password")]
    [Compare("Password", ErrorMessage = "Le password non coincidono")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
