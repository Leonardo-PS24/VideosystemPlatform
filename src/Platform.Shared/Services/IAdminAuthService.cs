namespace Platform.Shared.Services;

public interface IAdminAuthService
{
    Task<bool> VerifyAdminCredentialsAsync(string username, string password);
}
