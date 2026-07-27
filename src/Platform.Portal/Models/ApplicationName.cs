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
    public const string SkriptkioskChecklist = "SkriptkioskChecklist";
    
    // Company master permissions
    public const string Company_Pharmaself24 = "Company_Pharmaself24";
    public const string Company_Skriptkiosk = "Company_Skriptkiosk";
    
    /// <summary>
    /// Ottiene tutte le applicazioni disponibili
    /// </summary>
    public static List<string> GetAll()
    {
        return new List<string>
        {
            Company_Pharmaself24,
            Company_Skriptkiosk,
            ConfigurationKiosk,
            SkriptkioskChecklist
        };
    }
    
    /// <summary>
    /// Ottiene il nome visualizzato per un'applicazione
    /// </summary>
    public static string GetDisplayName(string applicationName)
    {
        return applicationName switch
        {
            Company_Pharmaself24 => "Abilita Pharmaself24",
            Company_Skriptkiosk => "Abilita SkriptKiosk",
            ConfigurationKiosk => "Configuration Kiosk (Pharmaself24)",
            SkriptkioskChecklist => "Configuration Kiosk (SkriptKiosk)",
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
            Company_Pharmaself24 => "business",
            Company_Skriptkiosk => "business",
            ConfigurationKiosk => "fact_check",
            SkriptkioskChecklist => "fact_check",
            _ => "apps"
        };
    }
}