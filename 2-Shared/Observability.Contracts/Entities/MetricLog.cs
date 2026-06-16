using Observability.Contracts.Enums;

namespace Observability.Contracts.Entities;

/// <summary>
/// Entité principale persistée en base de données.
/// Représente une mesure de santé effectuée par le MonitoringWorker sur une API cible.
/// </summary>
/// <remarks>
/// Colonnes indexées :
///   - <see cref="Timestamp"/> : pour les requêtes temporelles (time-series, graphiques)
///   - <see cref="ApiEndpointId"/> : pour filtrer par API cible
/// Ces deux index sont définis dans la migration EF Core du MonitoringWorker.
/// </remarks>
public class MetricLog
{
    /// <summary>Identifiant unique auto-généré par la BDD (IDENTITY).</summary>
    public long Id { get; set; }

    /// <summary>
    /// Identifiant logique de l'endpoint surveillé.
    /// Ex : "products-api", "auth-service-health".
    /// Utilisé comme clé de filtrage — indexé en BDD.
    /// </summary>
    public string ApiEndpointId { get; set; } = string.Empty;

    /// <summary>URL complète appelée lors de la mesure.</summary>
    public string TargetUrl { get; set; } = string.Empty;

    /// <summary>
    /// Horodatage UTC de la collecte de la métrique.
    /// Indexé en BDD pour les requêtes chronologiques.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Latence mesurée avec <see cref="System.Diagnostics.Stopwatch"/>.
    /// Représente le Round-Trip Time (RTT) en millisecondes.
    /// </summary>
    public double LatencyMs { get; set; }

    /// <summary>Code HTTP retourné par l'API (200, 500, etc.). Null si timeout.</summary>
    public int? HttpStatusCode { get; set; }

    /// <summary>Catégorie sémantique du code HTTP pour faciliter les agrégations.</summary>
    public HttpStatusCategory StatusCategory { get; set; }

    /// <summary>Indique si la requête est considérée comme un succès fonctionnel (2xx).</summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Message d'erreur en cas d'échec (exception réseau, timeout, erreur serveur).
    /// Null si la requête a réussi.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Nombre de tentatives effectuées par Polly avant d'obtenir ce résultat.
    /// 0 = succès au premier essai.
    /// </summary>
    public int RetryCount { get; set; }
}
