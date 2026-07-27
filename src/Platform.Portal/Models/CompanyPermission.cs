using System;
using System.ComponentModel.DataAnnotations;

namespace Platform.Portal.Models;

/// <summary>
/// Permessi di un'applicazione per una specifica azienda (sotto‑marchio)
/// </summary>
public class CompanyPermission
{
    [Key]
    public int Id { get; set; }

    /// <summary>Id dell'azienda a cui appartiene il permesso</summary>
    [Required]
    public string CompanyId { get; set; } = string.Empty;

    /// <summary>Nome dell'applicazione (es. "ConfigurationKiosk")</summary>
    [Required]
    public string ApplicationName { get; set; } = string.Empty;

    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
