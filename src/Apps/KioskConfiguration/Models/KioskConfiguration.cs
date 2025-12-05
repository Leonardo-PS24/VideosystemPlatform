using System.ComponentModel.DataAnnotations;

namespace Platform.Apps.KioskConfiguration.Models
{
    /// <summary>
    /// Configurazione di un kiosk salvata nel database
    /// </summary>
    public class KioskConfigurationEntity
    {
        public Guid Id { get; set; }
        
        [Required]
        public Guid TemplateId { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string SerialNumber { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Draft"; // Draft, InProgress, Completed, Approved, Rejected
        
        public string? CompletedBy { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        
        public string? RejectionReason { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ConfigurationTemplateEntity? Template { get; set; }
        public virtual ICollection<ConfigurationFieldValue> FieldValues { get; set; } = new List<ConfigurationFieldValue>();
        public virtual ICollection<ConfigurationAttachment> Attachments { get; set; } = new List<ConfigurationAttachment>();
    }

    /// <summary>
    /// Template salvato nel database (contiene JSON)
    /// </summary>
    public class ConfigurationTemplateEntity
    {
        public Guid Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string TemplateId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200)]
        public string TemplateName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(20)]
        public string Version { get; set; } = string.Empty;
        
        [Required]
        public string JsonContent { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<KioskConfigurationEntity> Configurations { get; set; } = new List<KioskConfigurationEntity>();
    }

    /// <summary>
    /// Valore di un campo della configurazione
    /// </summary>
    public class ConfigurationFieldValue
    {
        public Guid Id { get; set; }
        
        [Required]
        public Guid ConfigurationId { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string FieldPath { get; set; } = string.Empty; // es: "hardware.serialNumber"
        
        public string? FieldValue { get; set; }
        
        public bool IsEncrypted { get; set; } = false;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property
        public virtual KioskConfigurationEntity? Configuration { get; set; }
    }

    /// <summary>
    /// File allegato a una configurazione
    /// </summary>
    public class ConfigurationAttachment
    {
        public Guid Id { get; set; }

        [Required]
        public Guid ConfigurationId { get; set; }

        [Required]
        [MaxLength(200)]
        public string FieldPath { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        public long FileSize { get; set; }

        [MaxLength(100)]
        public string MimeType { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual KioskConfigurationEntity? Configuration { get; set; }
    }

    /// <summary>
    /// Enum per gli stati della configurazione
    /// </summary>
    public enum ConfigurationStatus
    {
        Draft,
        InProgress,
        Completed,
        Approved,
        Rejected
    }

    /// <summary>
    /// Enum per i tipi di campo supportati
    /// </summary>
    public enum FieldType
    {
        Text,
        Textarea,
        Checkbox,
        Radio,
        Dropdown,
        Number,
        Date,
        DateTime,
        Password,
        Email,
        Url,
        Tel,
        File
    }
}