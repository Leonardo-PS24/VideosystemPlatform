namespace Platform.Shared.Models;

/// <summary>
/// Rappresenta una singola differenza rilevata tra due versioni dei dati di una checklist
/// </summary>
public class KioskChecklistDiff
{
    public string FieldId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string OldValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
}

public class TemplateStructureDto
{
    public List<TemplateSectionDto>? Sections { get; set; }
}

public class TemplateSectionDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<TemplateFieldDto>? Fields { get; set; }
}

public class TemplateFieldDto
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}
