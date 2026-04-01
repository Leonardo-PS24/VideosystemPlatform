namespace Platform.Portal.Models;

public class CompanyInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PrimaryColor { get; set; } = "#000000";
    public string SecondaryColor { get; set; } = "#FFFFFF";
    public List<ApplicationInfo> Applications { get; set; } = new();
}
