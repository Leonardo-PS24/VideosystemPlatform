using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Shared.Models;

/// <summary>
/// Rappresenta una voce nello storico delle modifiche di una checklist
/// </summary>
public class KioskChecklistHistory
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int InstanceId { get; set; }

    [ForeignKey("InstanceId")]
    public virtual KioskChecklistInstance Instance { get; set; } = null!;

    /// <summary>
    /// L'azione eseguita (es. "Created", "Updated", "StatusChanged")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Snapshot dei dati JSON in quel momento
    /// </summary>
    public string DataJson { get; set; } = "{}";

    /// <summary>
    /// Stato in quel momento
    /// </summary>
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Utente che ha effettuato l'azione
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Data dell'azione
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Note opzionali sulla modifica
    /// </summary>
    public string? Notes { get; set; }
}