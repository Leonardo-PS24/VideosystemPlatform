using System;
using System.ComponentModel.DataAnnotations;

namespace Platform.Portal.Models;

/// <summary>
/// Rappresenta un ruolo personalizzato nel sistema.
/// </summary>
public class Role
{
    /// <summary>
    /// Identificatore univoco del ruolo.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Nome del ruolo (univoco).
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Descrizione opzionale del ruolo.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Se valorizzato, il ruolo è limitato a una specifica azienda.
    /// </summary>
    public Guid? CompanyId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
