using System.ComponentModel.DataAnnotations;

namespace Platform.Portal.Models.PBAC;

/// <summary>
/// Catalogo di tutti i permessi atomici disponibili nel sistema
/// </summary>
public class PermissionCatalog
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Chiave univoca del permesso (es. "pharmaself:kiosk:create", "skript:checklist:view")
    /// </summary>
    [Required]
    [MaxLength(150)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Macro area o Azienda a cui si riferisce (es. "Pharmaself24", "SkriptKiosk", "Admin")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Area { get; set; } = string.Empty;

    /// <summary>
    /// Modulo o applicazione specifica (es. "ConfigurationKiosk", "Checklist", "Users")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// Tipo di azione (View, Create, Edit, Delete, Admin, Export, ecc.)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Descrizione leggibile del permesso
    /// </summary>
    [MaxLength(255)]
    public string Description { get; set; } = string.Empty;
}
