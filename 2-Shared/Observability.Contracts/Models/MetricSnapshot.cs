using Observability.Contracts.Enums;

namespace Observability.Contracts.Models;

/// <summary>
/// DTO (Data Transfer Object) de lecture rapide envoyé en temps réel via SignalR au Dashboard.
/// Contient les métriques agrégées d'une fenêtre temporelle récente pour un endpoint donné.
/// </summary>
/// <remarks>
/// Ce modèle est intentionnellement découplé de <c>MetricLog</c> (l'entité BDD)
/// afin de ne pas exposer les détails internes de persistance au client.
/// </remarks>
public sealed record MetricSnapshot
{
    /// <summary>Identifiant logique de l'API surveillée.</summary>
    public string ApiEndpointId { get; init; } = string.Empty;

    /// <summary>URL de l'endpoint mesuré.</summary>
    public string TargetUrl { get; init; } = string.Empty;

    /// <summary>Horodatage UTC de la dernière mesure collectée.</summary>
    public DateTimeOffset LastChecked { get; init; }

    /// <summary>Latence du dernier appel en millisecondes.</summary>
    public double LatestLatencyMs { get; init; }

    /// <summary>Latence moyenne sur la fenêtre glissante configurée (ex : 5 minutes).</summary>
    public double AverageLatencyMs { get; init; }

    /// <summary>Latence maximale observée sur la fenêtre glissante.</summary>
    public double MaxLatencyMs { get; init; }

    /// <summary>
    /// Taux de succès sur la fenêtre glissante, exprimé en pourcentage [0.0 – 100.0].
    /// </summary>
    public double SuccessRatePercent { get; init; }

    /// <summary>Dernier code HTTP reçu. Null si timeout.</summary>
    public int? LastHttpStatusCode { get; init; }

    /// <summary>Catégorie sémantique du dernier code HTTP.</summary>
    public HttpStatusCategory LastStatusCategory { get; init; }

    /// <summary>Nombre total de mesures dans la fenêtre glissante.</summary>
    public int TotalChecks { get; init; }

    /// <summary>Nombre d'échecs (non-2xx ou timeout) dans la fenêtre glissante.</summary>
    public int FailedChecks { get; init; }
}
