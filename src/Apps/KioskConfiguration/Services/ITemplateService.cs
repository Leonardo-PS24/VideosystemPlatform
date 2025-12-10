using Platform.Apps.KioskConfiguration.Models;

namespace Platform.Apps.KioskConfiguration.Services
{
    /// <summary>
    /// Service per gestire i template di configurazione JSON
    /// </summary>
    public interface ITemplateService
    {
        /// <summary>
        /// Carica tutti i template disponibili dalla cartella ConfigurationTemplates
        /// </summary>
        Task<List<ConfigurationTemplate>> GetAllTemplatesAsync();

        /// <summary>
        /// Carica un template specifico per ID
        /// </summary>
        Task<ConfigurationTemplate?> GetTemplateByIdAsync(string templateId);

        /// <summary>
        /// Carica un template da file JSON
        /// </summary>
        Task<ConfigurationTemplate?> LoadTemplateFromFileAsync(string filePath);

        /// <summary>
        /// Valida un template JSON
        /// </summary>
        (bool IsValid, List<string> Errors) ValidateTemplate(ConfigurationTemplate template);

        /// <summary>
        /// Salva un template nel database
        /// </summary>
        Task<Guid> SaveTemplateToDbAsync(ConfigurationTemplate template);

        /// <summary>
        /// Ottiene tutti i template salvati nel database
        /// </summary>
        Task<List<ConfigurationTemplateEntity>> GetDbTemplatesAsync(bool activeOnly = true);

        /// <summary>
        /// Ottiene un template dal database e lo deserializza
        /// </summary>
        Task<ConfigurationTemplate?> GetTemplateFromDbAsync(Guid id);

        /// <summary>
        /// Disattiva un template nel database
        /// </summary>
        Task<bool> DeactivateTemplateAsync(Guid id);

        /// <summary>
        /// Ottiene lista di applicazioni/modelli disponibili per dropdown
        /// </summary>
        Task<List<(string TemplateId, string TemplateName)>> GetTemplateOptionsAsync();
    }
}