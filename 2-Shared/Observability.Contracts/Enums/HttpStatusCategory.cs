namespace Observability.Contracts.Enums;

/// <summary>
/// Catégorise les réponses HTTP selon les familles de codes standard.
/// Utilisé pour les agrégations statistiques et le filtrage dans le Dashboard.
/// </summary>
public enum HttpStatusCategory
{
    /// <summary>Réponses informatives (1xx)</summary>
    Informational = 1,

    /// <summary>Réponses de succès (2xx)</summary>
    Success = 2,

    /// <summary>Redirections (3xx)</summary>
    Redirection = 3,

    /// <summary>Erreurs client (4xx)</summary>
    ClientError = 4,

    /// <summary>Erreurs serveur (5xx)</summary>
    ServerError = 5,

    /// <summary>Timeout ou absence de réponse (ex : TaskCanceledException)</summary>
    Timeout = 0
}
