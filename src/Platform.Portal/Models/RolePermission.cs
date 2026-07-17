using System;
using System.ComponentModel.DataAnnotations;

namespace Platform.Portal.Models;

/// <summary>
/// Rappresenta i permessi di base associati a un ruolo/reparto
/// </summary>
public class RolePermission
{
    /// <summary>
    /// ID univoco del permesso di ruolo
    /// </summary>
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Nome del ruolo (es. "User", "Tecnico", "Developer")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string RoleName { get; set; } = string.Empty;
    
    /// <summary>
    /// Nome dell'applicazione a cui si riferisce il permesso
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ApplicationName { get; set; } = string.Empty;
    
    /// <summary>
    /// Permesso di visualizzazione
    /// </summary>
    public bool CanView { get; set; }
    
    /// <summary>
    /// Permesso di creazione
    /// </summary>
    public bool CanCreate { get; set; }
    
    /// <summary>
    /// Permesso di modifica
    /// </summary>
    public bool CanEdit { get; set; }
    
    /// <summary>
    /// Permesso di eliminazione
    /// </summary>
    public bool CanDelete { get; set; }
    
    /// <summary>
    /// Data e ora dell'ultimo aggiornamento del permesso
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
