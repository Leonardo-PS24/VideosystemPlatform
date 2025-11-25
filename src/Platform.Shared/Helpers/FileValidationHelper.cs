using Microsoft.AspNetCore.Http;

namespace Platform.Shared.Helpers;

/// <summary>
/// Helper per la validazione dei file caricati
/// </summary>
public static class FileValidationHelper
{
    private static readonly Dictionary<string, List<byte[]>> FileSignatures = new()
    {
        {
            ".pdf", new List<byte[]>
            {
                new byte[] { 0x25, 0x50, 0x44, 0x46 } // %PDF
            }
        },
        {
            ".jpg", new List<byte[]>
            {
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 },
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE2 },
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE3 },
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE8 }
            }
        },
        {
            ".jpeg", new List<byte[]>
            {
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 },
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE2 },
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE3 },
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE8 }
            }
        },
        {
            ".png", new List<byte[]>
            {
                new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }
            }
        }
    };

    /// <summary>
    /// Valida se il file ha un'estensione permessa
    /// </summary>
    /// <param name="fileName">Nome del file</param>
    /// <param name="allowedExtensions">Estensioni permesse (con il punto, es: .pdf, .jpg)</param>
    /// <returns>True se l'estensione è valida</returns>
    public static bool HasValidExtension(string fileName, string[] allowedExtensions)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return allowedExtensions.Contains(extension);
    }

    /// <summary>
    /// Valida la dimensione del file
    /// </summary>
    /// <param name="fileSize">Dimensione del file in bytes</param>
    /// <param name="maxSizeInMb">Dimensione massima in MB</param>
    /// <returns>True se la dimensione è valida</returns>
    public static bool HasValidSize(long fileSize, int maxSizeInMb)
    {
        var maxSizeInBytes = maxSizeInMb * 1024 * 1024;
        return fileSize > 0 && fileSize <= maxSizeInBytes;
    }

    /// <summary>
    /// Valida il file in base alla signature (magic bytes)
    /// </summary>
    /// <param name="file">File da validare</param>
    /// <param name="allowedExtensions">Estensioni permesse</param>
    /// <returns>True se il file è valido</returns>
    public static async Task<bool> HasValidSignature(IFormFile file, string[] allowedExtensions)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!FileSignatures.ContainsKey(extension))
            return false;

        using var reader = new BinaryReader(file.OpenReadStream());
        var signatures = FileSignatures[extension];
        var headerBytes = reader.ReadBytes(signatures.Max(m => m.Length));

        return signatures.Any(signature =>
            headerBytes.Take(signature.Length).SequenceEqual(signature));
    }

    /// <summary>
    /// Genera un nome file unico e sicuro
    /// </summary>
    /// <param name="originalFileName">Nome file originale</param>
    /// <returns>Nome file sanitizzato con GUID</returns>
    public static string GenerateSafeFileName(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var fileName = Path.GetFileNameWithoutExtension(originalFileName);
        
        // Rimuove caratteri non sicuri
        fileName = string.Concat(fileName.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'));
        
        // Limita la lunghezza
        if (fileName.Length > 50)
            fileName = fileName.Substring(0, 50);

        return $"{fileName}_{Guid.NewGuid()}{extension}";
    }

    /// <summary>
    /// Formatta la dimensione del file in formato leggibile
    /// </summary>
    /// <param name="bytes">Dimensione in bytes</param>
    /// <returns>Stringa formattata (es: "1.5 MB")</returns>
    public static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}
