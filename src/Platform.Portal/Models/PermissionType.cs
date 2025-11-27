using System;

namespace Platform.Portal.Models;

/// <summary>
/// Tipi di permessi che possono essere assegnati agli utenti
/// Usa [Flags] per permettere combinazioni di permessi
/// </summary>
[Flags]
public enum PermissionType
{
    /// <summary>
    /// Nessun permesso
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Permesso di visualizzazione (lettura)
    /// </summary>
    View = 1,
    
    /// <summary>
    /// Permesso di creazione
    /// </summary>
    Create = 2,
    
    /// <summary>
    /// Permesso di modifica
    /// </summary>
    Edit = 4,
    
    /// <summary>
    /// Permesso di eliminazione
    /// </summary>
    Delete = 8,
    
    /// <summary>
    /// Tutti i permessi
    /// </summary>
    All = View | Create | Edit | Delete
}