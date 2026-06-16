namespace TargetAPI.Models;

/// <summary>
/// Enveloppe générique pour toutes les réponses de l'API.
/// Fournit un format uniforme : succès/échec, données, message et timestamp.
/// </summary>
/// <typeparam name="T">Type de la donnée encapsulée.</typeparam>
public sealed class ApiResponse<T>
{
    /// <summary>Indique si l'opération a réussi.</summary>
    public bool Success { get; init; }

    /// <summary>Données retournées (null en cas d'erreur).</summary>
    public T? Data { get; init; }

    /// <summary>Message lisible décrivant le résultat ou l'erreur.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Horodatage UTC de la réponse.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    // ── Méthodes factory pour un code d'appel expressif ──────────────────────

    public static ApiResponse<T> Ok(T data, string message = "Success") =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string message) =>
        new() { Success = false, Data = default, Message = message };

    public static ApiResponse<T> NotFound(string message = "Resource not found") =>
        new() { Success = false, Data = default, Message = message };
}
