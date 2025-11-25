namespace Platform.Shared.Models;

/// <summary>
/// Risposta API standardizzata per comunicazione tra servizi
/// </summary>
/// <typeparam name="T">Tipo dei dati restituiti</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Indica se l'operazione è andata a buon fine
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Messaggio descrittivo del risultato
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Dati restituiti dall'operazione
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Lista di errori, se presenti
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Crea una risposta di successo
    /// </summary>
    public static ApiResponse<T> SuccessResponse(T data, string message = "Operazione completata con successo")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    /// <summary>
    /// Crea una risposta di errore
    /// </summary>
    public static ApiResponse<T> ErrorResponse(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<string>()
        };
    }
}
