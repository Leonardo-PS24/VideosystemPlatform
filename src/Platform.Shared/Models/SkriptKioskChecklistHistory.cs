using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Shared.Models;

/// <summary>
/// Rappresenta una voce nello storico delle modifiche di una checklist SkriptKiosk
/// </summary>
public class SkriptKioskChecklistHistory
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int InstanceId { get; set; }

    [ForeignKey("InstanceId")]
    public virtual SkriptKioskChecklistInstance Instance { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    public string DataJson { get; set; } = "{}";

    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public string? Notes { get; set; }
}
