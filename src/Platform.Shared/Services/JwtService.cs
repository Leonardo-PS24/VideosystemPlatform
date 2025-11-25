using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Platform.Shared.Constants;

namespace Platform.Shared.Services;

/// <summary>
/// Servizio per la generazione e validazione di JWT token
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Genera un JWT token per l'utente specificato
    /// </summary>
    string GenerateToken(string userId, string username, string email, IEnumerable<string> roles);

    /// <summary>
    /// Valida un JWT token
    /// </summary>
    ClaimsPrincipal? ValidateToken(string token);
}

/// <summary>
/// Implementazione del servizio JWT
/// </summary>
public class JwtService : IJwtService
{
    private readonly string _secretKey;

    public JwtService(string secretKey)
    {
        _secretKey = secretKey;
    }

    /// <summary>
    /// Genera un JWT token per l'utente specificato
    /// </summary>
    public string GenerateToken(string userId, string username, string email, IEnumerable<string> roles)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_secretKey);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Email, email)
        };

        // Aggiunge i ruoli come claim
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(PlatformConstants.JwtSettings.TokenExpirationMinutes),
            Issuer = PlatformConstants.JwtSettings.Issuer,
            Audience = PlatformConstants.JwtSettings.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Valida un JWT token
    /// </summary>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_secretKey);

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = PlatformConstants.JwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = PlatformConstants.JwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
