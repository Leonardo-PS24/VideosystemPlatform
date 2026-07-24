using System.ComponentModel.DataAnnotations;

namespace Platform.Shared.Models;

/// <summary>
/// Rappresenta un modello di checklist (es. per un tipo di macchina) per SkriptKiosk
/// </summary>
public class SkriptKioskChecklistTemplate : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    public string StructureJson { get; set; } = "{}";

    public int Version { get; set; } = 1;

    public bool IsActive { get; set; } = true;
}
