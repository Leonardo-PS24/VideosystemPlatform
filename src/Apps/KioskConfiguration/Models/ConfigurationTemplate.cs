namespace Platform.Apps.KioskConfiguration.Models
{
    /// <summary>
    /// Rappresenta un template di configurazione caricato da JSON
    /// </summary>
    public class ConfigurationTemplate
    {
        public string TemplateId { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<ChecklistSection> Sections { get; set; } = new();
        public TemplateMetadata Metadata { get; set; } = new();
    }

    /// <summary>
    /// Sezione della checklist (es: Hardware, Software, Network)
    /// </summary>
    public class ChecklistSection
    {
        public string SectionId { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;
        public int Order { get; set; }
        public string Icon { get; set; } = "folder";
        public List<ChecklistField> Fields { get; set; } = new();
    }

    /// <summary>
    /// Campo della checklist con tipo, validazione e proprietà
    /// </summary>
    public class ChecklistField
    {
        public string FieldId { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty; // "text", "checkbox", "dropdown", etc.
        public bool Required { get; set; }
        public string? Placeholder { get; set; }
        public string? DefaultValue { get; set; }
        public FieldValidation? Validation { get; set; }
        public List<FieldOption>? Options { get; set; } // Per dropdown, radio
        public FieldDependency? DependsOn { get; set; } // Campo condizionale
        public bool Encrypted { get; set; } // Per password
        public string? Accept { get; set; } // Per file upload (es: ".jpg,.png")
        public long? MaxSize { get; set; } // Dimensione max file in bytes
        public int? MaxFiles { get; set; } // Numero massimo file caricabili
        public int? MaxLength { get; set; } // Lunghezza massima testo
        public int? MinLength { get; set; } // Lunghezza minima testo
    }

    /// <summary>
    /// Regole di validazione per un campo
    /// </summary>
    public class FieldValidation
    {
        public string? Pattern { get; set; } // Regex per validazione
        public string? ErrorMessage { get; set; }
        public int? Min { get; set; } // Per numeri
        public int? Max { get; set; } // Per numeri
    }

    /// <summary>
    /// Opzione per dropdown o radio button
    /// </summary>
    public class FieldOption
    {
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    /// <summary>
    /// Dipendenza condizionale (mostra campo solo se...)
    /// </summary>
    public class FieldDependency
    {
        public string Field { get; set; } = string.Empty; // FieldId del campo da cui dipende
        public string Condition { get; set; } = string.Empty; // "equals", "notEquals", "contains"
        public string Value { get; set; } = string.Empty; // Valore da confrontare
    }

    /// <summary>
    /// Metadati del template
    /// </summary>
    public class TemplateMetadata
    {
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string LastModified { get; set; } = string.Empty;
        public string Status { get; set; } = "active"; // active, archived, draft
    }
}
