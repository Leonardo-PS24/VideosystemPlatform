using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Platform.Apps.KioskConfiguration.Data;
using Platform.Apps.KioskConfiguration.Models;

namespace Platform.Apps.KioskConfiguration.Services
{
    /// <summary>
    /// Service per gestire i template di configurazione JSON
    /// </summary>
    public class TemplateService : ITemplateService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<TemplateService> _logger;
        private readonly string _templatesPath;

        public TemplateService(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<TemplateService> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
            _templatesPath = Path.Combine(_environment.ContentRootPath, "ConfigurationTemplates");
        }

        /// <summary>
        /// Carica tutti i template disponibili dalla cartella ConfigurationTemplates
        /// </summary>
        public async Task<List<ConfigurationTemplate>> GetAllTemplatesAsync()
        {
            var templates = new List<ConfigurationTemplate>();

            try
            {
                if (!Directory.Exists(_templatesPath))
                {
                    _logger.LogWarning("ConfigurationTemplates directory not found: {Path}", _templatesPath);
                    return templates;
                }

                var jsonFiles = Directory.GetFiles(_templatesPath, "*.json");

                foreach (var file in jsonFiles)
                {
                    try
                    {
                        var template = await LoadTemplateFromFileAsync(file);
                        if (template != null)
                        {
                            templates.Add(template);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error loading template from file: {File}", file);
                    }
                }

                _logger.LogInformation("Loaded {Count} templates from directory", templates.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading templates directory");
            }

            return templates;
        }

        /// <summary>
        /// Carica un template specifico per ID
        /// </summary>
        public async Task<ConfigurationTemplate?> GetTemplateByIdAsync(string templateId)
        {
            var templates = await GetAllTemplatesAsync();
            return templates.FirstOrDefault(t => t.TemplateId == templateId);
        }

        /// <summary>
        /// Carica un template da file JSON
        /// </summary>
        public async Task<ConfigurationTemplate?> LoadTemplateFromFileAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Template file not found: {Path}", filePath);
                    return null;
                }

                var jsonContent = await File.ReadAllTextAsync(filePath);
                
                var template = JsonConvert.DeserializeObject<ConfigurationTemplate>(jsonContent);

                if (template == null)
                {
                    _logger.LogWarning("Failed to deserialize template from: {Path}", filePath);
                    return null;
                }

                // Valida il template
                var (isValid, errors) = ValidateTemplate(template);
                if (!isValid)
                {
                    _logger.LogWarning("Template validation failed for {File}: {Errors}", 
                        filePath, string.Join(", ", errors));
                    return null;
                }

                _logger.LogInformation("Successfully loaded template: {TemplateId} from {File}", 
                    template.TemplateId, filePath);

                return template;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error for file: {Path}", filePath);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error loading template from: {Path}", filePath);
                return null;
            }
        }

        /// <summary>
        /// Valida un template JSON
        /// </summary>
        public (bool IsValid, List<string> Errors) ValidateTemplate(ConfigurationTemplate template)
        {
            var errors = new List<string>();

            // Validazione header
            if (string.IsNullOrWhiteSpace(template.TemplateId))
                errors.Add("TemplateId is required");

            if (string.IsNullOrWhiteSpace(template.TemplateName))
                errors.Add("TemplateName is required");

            if (string.IsNullOrWhiteSpace(template.Version))
                errors.Add("Version is required");

            // Validazione sezioni
            if (template.Sections == null || !template.Sections.Any())
            {
                errors.Add("At least one section is required");
            }
            else
            {
                var sectionIds = new HashSet<string>();
                foreach (var section in template.Sections)
                {
                    if (string.IsNullOrWhiteSpace(section.SectionId))
                        errors.Add($"Section at order {section.Order} is missing SectionId");
                    else if (!sectionIds.Add(section.SectionId))
                        errors.Add($"Duplicate SectionId: {section.SectionId}");

                    if (string.IsNullOrWhiteSpace(section.SectionName))
                        errors.Add($"Section {section.SectionId} is missing SectionName");

                    // Validazione campi
                    if (section.Fields == null || !section.Fields.Any())
                    {
                        errors.Add($"Section {section.SectionId} has no fields");
                    }
                    else
                    {
                        var fieldIds = new HashSet<string>();
                        foreach (var field in section.Fields)
                        {
                            if (string.IsNullOrWhiteSpace(field.FieldId))
                                errors.Add($"Field in section {section.SectionId} is missing FieldId");
                            else if (!fieldIds.Add(field.FieldId))
                                errors.Add($"Duplicate FieldId in section {section.SectionId}: {field.FieldId}");

                            if (string.IsNullOrWhiteSpace(field.FieldName))
                                errors.Add($"Field {field.FieldId} is missing FieldName");

                            if (string.IsNullOrWhiteSpace(field.FieldType))
                                errors.Add($"Field {field.FieldId} is missing FieldType");

                            // Validazione opzioni per dropdown/radio
                            if ((field.FieldType == "dropdown" || field.FieldType == "radio") &&
                                (field.Options == null || !field.Options.Any()))
                            {
                                errors.Add($"Field {field.FieldId} is type {field.FieldType} but has no options");
                            }

                            // Validazione dependsOn
                            if (field.DependsOn != null)
                            {
                                if (string.IsNullOrWhiteSpace(field.DependsOn.Field))
                                    errors.Add($"Field {field.FieldId} has dependsOn but missing field reference");

                                if (string.IsNullOrWhiteSpace(field.DependsOn.Condition))
                                    errors.Add($"Field {field.FieldId} has dependsOn but missing condition");
                            }
                        }
                    }
                }
            }

            return (errors.Count == 0, errors);
        }

        /// <summary>
        /// Salva un template nel database
        /// </summary>
        public async Task<Guid> SaveTemplateToDbAsync(ConfigurationTemplate template)
        {
            try
            {
                // Verifica se esiste già un template con lo stesso TemplateId
                var existing = await _context.ConfigurationTemplates
                    .FirstOrDefaultAsync(t => t.TemplateId == template.TemplateId);

                var jsonContent = JsonConvert.SerializeObject(template, Formatting.Indented);

                if (existing != null)
                {
                    // Update existing
                    existing.TemplateName = template.TemplateName;
                    existing.Version = template.Version;
                    existing.JsonContent = jsonContent;
                    existing.UpdatedAt = DateTime.UtcNow;

                    _context.ConfigurationTemplates.Update(existing);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Updated template {TemplateId} in database", template.TemplateId);
                    return existing.Id;
                }
                else
                {
                    // Create new
                    var entity = new ConfigurationTemplateEntity
                    {
                        Id = Guid.NewGuid(),
                        TemplateId = template.TemplateId,
                        TemplateName = template.TemplateName,
                        Version = template.Version,
                        JsonContent = jsonContent,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.ConfigurationTemplates.Add(entity);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Saved new template {TemplateId} to database", template.TemplateId);
                    return entity.Id;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving template {TemplateId} to database", template.TemplateId);
                throw;
            }
        }

        /// <summary>
        /// Ottiene tutti i template salvati nel database
        /// </summary>
        public async Task<List<ConfigurationTemplateEntity>> GetDbTemplatesAsync(bool activeOnly = true)
        {
            try
            {
                var query = _context.ConfigurationTemplates.AsQueryable();

                if (activeOnly)
                {
                    query = query.Where(t => t.IsActive);
                }

                var templates = await query
                    .OrderBy(t => t.TemplateName)
                    .ToListAsync();

                return templates;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving templates from database");
                throw;
            }
        }

        /// <summary>
        /// Ottiene un template dal database e lo deserializza
        /// </summary>
        public async Task<ConfigurationTemplate?> GetTemplateFromDbAsync(Guid id)
        {
            try
            {
                var entity = await _context.ConfigurationTemplates
                    .FirstOrDefaultAsync(t => t.Id == id && t.IsActive);

                if (entity == null)
                {
                    _logger.LogWarning("Template not found in database: {Id}", id);
                    return null;
                }

                var template = JsonConvert.DeserializeObject<ConfigurationTemplate>(entity.JsonContent);
                return template;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing template from database: {Id}", id);
                return null;
            }
        }

        /// <summary>
        /// Disattiva un template nel database
        /// </summary>
        public async Task<bool> DeactivateTemplateAsync(Guid id)
        {
            try
            {
                var template = await _context.ConfigurationTemplates.FindAsync(id);
                if (template == null)
                {
                    return false;
                }

                template.IsActive = false;
                template.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Deactivated template: {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating template: {Id}", id);
                return false;
            }
        }

        /// <summary>
        /// Ottiene lista di applicazioni/modelli disponibili per dropdown
        /// </summary>
        public async Task<List<(string TemplateId, string TemplateName)>> GetTemplateOptionsAsync()
        {
            try
            {
                var dbTemplates = await GetDbTemplatesAsync(activeOnly: true);
                return dbTemplates
                    .Select(t => (t.TemplateId, t.TemplateName))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting template options");
                return new List<(string, string)>();
            }
        }
    }
}
