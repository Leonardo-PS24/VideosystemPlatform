using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Portal.Models.PBAC;

/// <summary>
/// Tabella ponte per associare permessi atomici a una Policy
/// </summary>
public class PolicyPermission
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PolicyId { get; set; }

    [ForeignKey(nameof(PolicyId))]
    public virtual Policy? Policy { get; set; }

    [Required]
    public int PermissionCatalogId { get; set; }

    [ForeignKey(nameof(PermissionCatalogId))]
    public virtual PermissionCatalog? PermissionCatalog { get; set; }
}
