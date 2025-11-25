namespace Platform.Shared.Models;

/// <summary>
/// Entità base per tutte le entità con tracciamento audit
/// </summary>
public abstract class AuditableEntity
{
    /// <summary>
    /// Data e ora di creazione del record
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Utente che ha creato il record
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Data e ora dell'ultima modifica
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Utente che ha effettuato l'ultima modifica
    /// </summary>
    public string? UpdatedBy { get; set; }
}
