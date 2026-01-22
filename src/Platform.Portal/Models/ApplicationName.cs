using System.Collections.Generic;

namespace Platform.Portal.Models;

/// <summary>
/// Costanti per i nomi delle applicazioni nel sistema
/// </summary>
public static class ApplicationName
{
    /// <summary>
    /// Applicazione Configuration Kiosk
    /// </summary>
    public const string ConfigurationKiosk = "ConfigurationKiosk";
    
    /// <summary>
    /// Ottiene tutte le applicazioni disponibili
    /// </summary>
    public static List<string> GetAll()
    {
        return new List<string>
        {
            ConfigurationKiosk
        };
    }
    
    /// <summary>
    /// Ottiene il nome visualizzato per un'applicazione
    /// </summary>
    public static string GetDisplayName(string applicationName)
    {
        return applicationName switch
        {
            ConfigurationKiosk => "Configuration Kiosk",
            _ => applicationName
        };
    }
    
    /// <summary>
    /// Ottiene l'icona Material per un'applicazione
    /// </summary>
    public static string GetIcon(string applicationName)
    {
        return applicationName switch
        {
            ConfigurationKiosk => "fact_check",
            _ => "apps"
        };
    }
}