namespace Platform.Shared.Constants;

/// <summary>
/// Costanti condivise per tutta la piattaforma Videosystem
/// </summary>
public static class PlatformConstants
{
    /// <summary>
    /// Colori aziendali Videosystem
    /// </summary>
    public static class BrandColors
    {
        public const string PrimaryGreen = "#00945E";
        public const string White = "#FFFFFF";
        public const string DarkGray = "#333333";
        public const string LightGray = "#F8F9FA";
    }

    /// <summary>
    /// Configurazioni JWT per autenticazione tra servizi
    /// </summary>
    public static class JwtSettings
    {
        public const string Issuer = "VideosystemPlatform";
        public const string Audience = "VideosystemApps";
        public const int TokenExpirationMinutes = 60;
    }

    /// <summary>
    /// Ruoli utente della piattaforma
    /// </summary>
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string User = "User";
    }

    /// <summary>
    /// Informazioni aziendali
    /// </summary>
    public static class CompanyInfo
    {
        public const string Name = "Videosystem S.r.l.";
        public const string Address = "Via Lago di Albano, 45 | 36015 Schio - Italia";
        public const string Phone = "+39 0445 500 500";
        public const string Website = "www.videosystem.it";
    }
}
