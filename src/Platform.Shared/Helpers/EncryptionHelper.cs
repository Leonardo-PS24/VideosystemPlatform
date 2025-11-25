using System.Security.Cryptography;
using System.Text;

namespace Platform.Shared.Helpers;

/// <summary>
/// Helper per la crittografia e decrittografia di dati sensibili
/// </summary>
public static class EncryptionHelper
{
    /// <summary>
    /// Cripta una stringa usando AES
    /// </summary>
    /// <param name="plainText">Testo in chiaro</param>
    /// <param name="key">Chiave di crittografia (deve essere di 32 caratteri)</param>
    /// <returns>Testo crittografato in Base64</returns>
    public static string Encrypt(string plainText, string key)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        byte[] keyBytes = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));

        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        
        // Scrive l'IV all'inizio dello stream
        ms.Write(aes.IV, 0, aes.IV.Length);

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    /// <summary>
    /// Decripta una stringa crittografata con AES
    /// </summary>
    /// <param name="cipherText">Testo crittografato in Base64</param>
    /// <param name="key">Chiave di crittografia (deve essere di 32 caratteri)</param>
    /// <returns>Testo in chiaro</returns>
    public static string Decrypt(string cipherText, string key)
    {
        if (string.IsNullOrEmpty(cipherText))
            return string.Empty;

        byte[] keyBytes = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
        byte[] buffer = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = keyBytes;

        // Legge l'IV dall'inizio dello stream
        byte[] iv = new byte[aes.IV.Length];
        Array.Copy(buffer, 0, iv, 0, iv.Length);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(buffer, iv.Length, buffer.Length - iv.Length);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        
        return sr.ReadToEnd();
    }

    /// <summary>
    /// Maschera una stringa per visualizzazione (mostra solo primi e ultimi caratteri)
    /// </summary>
    /// <param name="text">Testo da mascherare</param>
    /// <param name="visibleChars">Numero di caratteri visibili all'inizio e alla fine</param>
    /// <returns>Stringa mascherata</returns>
    public static string MaskString(string text, int visibleChars = 2)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= visibleChars * 2)
            return new string('*', text?.Length ?? 0);

        var start = text.Substring(0, visibleChars);
        var end = text.Substring(text.Length - visibleChars);
        var masked = new string('*', text.Length - (visibleChars * 2));

        return $"{start}{masked}{end}";
    }
}
