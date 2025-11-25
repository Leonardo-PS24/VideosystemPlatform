using Microsoft.AspNetCore.Identity;

namespace Platform.Portal.Models;

/// <summary>
/// Utente dell'applicazione esteso con campi personalizzati
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Nome completo dell'utente
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Indica se l'utente è attivo
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Data di creazione dell'utente
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Data dell'ultima modifica
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
