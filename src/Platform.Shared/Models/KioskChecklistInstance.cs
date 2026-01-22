using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Shared.Models;

/// <summary>
/// Rappresenta una checklist compilata per una specifica macchina
/// </summary>
public class KioskChecklistInstance : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int TemplateId { get; set; }

    [ForeignKey("TemplateId")]
    public virtual KioskChecklistTemplate Template { get; set; } = null!;

    /// <summary>
    /// Identificativo della macchina (es. Numero di Serie)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string MachineSerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// I dati compilati in formato JSON
    /// </summary>
    [Required]
    public string DataJson { get; set; } = "{}";

    /// <summary>
    /// Stato della checklist (es. InProgress, Completed)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "InProgress";

    public DateTime? CompletedAt { get; set; }
}