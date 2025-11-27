using System.Collections.Generic;

namespace Platform.Portal.Models;

/// <summary>
/// Costanti per i nomi delle applicazioni nel sistema
/// </summary>
public static class ApplicationName
{
    /// <summary>
    /// Applicazione di registrazione kiosk
    /// </summary>
    public const string KioskRegistration = "KioskRegistration";
    
    /// <summary>
    /// Sistema di tracciamento bug
    /// </summary>
    public const string BugTracking = "BugTracking";
    
    /// <summary>
    /// Sistema di richieste funzionalità
    /// </summary>
    public const string FeatureRequest = "FeatureRequest";
    
    /// <summary>
    /// Dashboard sviluppatori
    /// </summary>
    public const string DeveloperDashboard = "DeveloperDashboard";
    
    /// <summary>
    /// Ottiene tutte le applicazioni disponibili
    /// </summary>
    public static List<string> GetAll()
    {
        return new List<string>
        {
            KioskRegistration,
            BugTracking,
            FeatureRequest,
            DeveloperDashboard
        };
    }
    
    /// <summary>
    /// Ottiene il nome visualizzato per un'applicazione
    /// </summary>
    public static string GetDisplayName(string applicationName)
    {
        return applicationName switch
        {
            KioskRegistration => "Kiosk Registration",
            BugTracking => "Bug Tracking",
            FeatureRequest => "Feature Request",
            DeveloperDashboard => "Developer Dashboard",
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
            KioskRegistration => "devices",
            BugTracking => "bug_report",
            FeatureRequest => "lightbulb",
            DeveloperDashboard => "code",
            _ => "apps"
        };
    }
}