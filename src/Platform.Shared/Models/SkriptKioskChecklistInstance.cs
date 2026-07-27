using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Shared.Models;

/// <summary>
/// Rappresenta una checklist compilata per una specifica macchina SkriptKiosk
/// </summary>
public class SkriptKioskChecklistInstance : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int TemplateId { get; set; }

    [ForeignKey("TemplateId")]
    public virtual SkriptKioskChecklistTemplate Template { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string MachineSerialNumber { get; set; } = string.Empty;

    [Required]
    public string DataJson { get; set; } = "{}";

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "InProgress";

    public int Progress { get; set; } = 0;
    
    public int Revision { get; set; } = 0;

    public DateTime? CompletedAt { get; set; }
}
