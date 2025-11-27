using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Portal.Models;

/// <summary>
/// Rappresenta i permessi di un utente per una specifica applicazione
/// </summary>
public class ApplicationPermission
{
    /// <summary>
    /// ID univoco del permesso
    /// </summary>
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// ID dell'utente a cui appartiene il permesso
    /// </summary>
    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Utente a cui appartiene il permesso
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }
    
    /// <summary>
    /// Nome dell'applicazione
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
    /// ID dell'utente che ha concesso il permesso
    /// </summary>
    [MaxLength(450)]
    public string? GrantedBy { get; set; }
    
    /// <summary>
    /// Utente che ha concesso il permesso
    /// </summary>
    [ForeignKey(nameof(GrantedBy))]
    public virtual ApplicationUser? GrantedByUser { get; set; }
    
    /// <summary>
    /// Data e ora di creazione del permesso
    /// </summary>
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Data e ora dell'ultimo aggiornamento
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Verifica se l'utente ha un permesso specifico
    /// </summary>
    public bool HasPermission(PermissionType permission)
    {
        return permission switch
        {
            PermissionType.View => CanView,
            PermissionType.Create => CanCreate,
            PermissionType.Edit => CanEdit,
            PermissionType.Delete => CanDelete,
            _ => false
        };
    }
    
    /// <summary>
    /// Imposta un permesso specifico
    /// </summary>
    public void SetPermission(PermissionType permission, bool value)
    {
        switch (permission)
        {
            case PermissionType.View:
                CanView = value;
                break;
            case PermissionType.Create:
                CanCreate = value;
                break;
            case PermissionType.Edit:
                CanEdit = value;
                break;
            case PermissionType.Delete:
                CanDelete = value;
                break;
        }
        
        // Se il permesso ha flag multipli, imposta tutti
        if (permission.HasFlag(PermissionType.View)) CanView = value;
        if (permission.HasFlag(PermissionType.Create)) CanCreate = value;
        if (permission.HasFlag(PermissionType.Edit)) CanEdit = value;
        if (permission.HasFlag(PermissionType.Delete)) CanDelete = value;
        
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Ottiene i permessi come PermissionType
    /// </summary>
    public PermissionType GetPermissions()
    {
        var permissions = PermissionType.None;
        
        if (CanView) permissions |= PermissionType.View;
        if (CanCreate) permissions |= PermissionType.Create;
        if (CanEdit) permissions |= PermissionType.Edit;
        if (CanDelete) permissions |= PermissionType.Delete;
        
        return permissions;
    }
}