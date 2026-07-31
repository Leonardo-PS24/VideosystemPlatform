using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Platform.Portal.Models.PBAC;

/// <summary>
/// Rappresenta un pacchetto di permessi riutilizzabile
/// </summary>
public class Policy
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Nome della Policy (es. "Amministratore Completo", "Operatore Kiosk", "Visualizzatore Skript")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Descrizione dello scopo della Policy
    /// </summary>
    [MaxLength(255)]
    public string? Description { get; set; }

    /// <summary>
    /// Indica se è una Policy di sistema non eliminabile
    /// </summary>
    public bool IsSystemPolicy { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Permessi associati a questa Policy
    /// </summary>
    public virtual ICollection<PolicyPermission> PolicyPermissions { get; set; } = new List<PolicyPermission>();
}
