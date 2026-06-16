namespace Dashboard.Web.Models;

/// <summary>
/// Résultat d'une mesure HTTP effectuée par le MonitoringBackgroundService.
/// Chaque appel à un endpoint génère exactement un MetricRecord.
/// </summary>
public sealed class MetricRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..12];
    public string EndpointId { get; set; } = "";
    public string EndpointName { get; set; } = "";
    public string Url { get; set; } = "";
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Latence mesurée avec Stopwatch (Round-Trip Time réel en ms).</summary>
    public double LatencyMs { get; set; }

    public int? HttpStatusCode { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>Taille de la réponse en octets (-1 si inconnue ou erreur).</summary>
    public long ResponseSizeBytes { get; set; } = -1;

    public string StatusLabel => IsSuccess ? "Success" : "Error";
    public string LatencyCategory =>
        LatencyMs > 1000 ? "high" : LatencyMs > 400 ? "medium" : "low";
}
