namespace Dashboard.Web.Models;

/// <summary>Type de protocole d'un endpoint surveillé.</summary>
public enum EndpointType { Rest, SoapWsdl, GraphQL }

/// <summary>
/// Configuration d'un endpoint à surveiller.
/// Persisté en mémoire (remplacé par BDD à l'étape MonitoringWorker).
/// </summary>
public sealed class MonitoredEndpoint
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public int PollingIntervalSeconds { get; set; } = 10;
    public bool IsEnabled { get; set; } = true;
    public EndpointType Type { get; set; } = EndpointType.Rest;
    public DateTimeOffset AddedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastChecked { get; set; }
    public string? ExpectedContentContains { get; set; }
    public int TimeoutSeconds { get; set; } = 10;

    public bool IsWsdl => Url.Contains("wsdl", StringComparison.OrdinalIgnoreCase);
}
