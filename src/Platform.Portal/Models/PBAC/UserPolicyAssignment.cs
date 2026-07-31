using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Portal.Models.PBAC;

/// <summary>
/// Assegnazione di una Policy a uno specifico Utente con filtri di ambito (Company e Department)
/// </summary>
public class UserPolicyAssignment
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }

    [Required]
    public int PolicyId { get; set; }

    [ForeignKey(nameof(PolicyId))]
    public virtual Policy? Policy { get; set; }

    /// <summary>
    /// Ambito Sottoazienda ("ALL", "Pharmaself24", "Skriptkiosk", ecc.)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string CompanyScope { get; set; } = "ALL";

    /// <summary>
    /// Ambito Reparto/Dipartimento ("ALL", "Magazzino", "Commerciale", "Assistenza", "Amministrazione", ecc.)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string DepartmentScope { get; set; } = "ALL";

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
