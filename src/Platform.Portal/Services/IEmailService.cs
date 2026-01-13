namespace Platform.Portal.Services;

/// <summary>
/// Servizio per l'invio di email
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Invia un'email
    /// </summary>
    /// <param name="toEmail">Email del destinatario</param>
    /// <param name="subject">Oggetto dell'email</param>
    /// <param name="htmlBody">Corpo dell'email in formato HTML</param>
    Task SendEmailAsync(string toEmail, string subject, string htmlBody);
}
