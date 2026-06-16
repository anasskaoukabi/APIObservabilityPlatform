namespace Dashboard.Web.Models;

public enum AlertMetric { LatencyMs, ErrorRatePercent, HttpStatusCode }
public enum AlertSeverityLevel { Warning, Critical }

/// <summary>Règle d'alerte évaluée après chaque mesure.</summary>
public sealed class AlertRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; set; } = "";

    /// <summary>null = s'applique à tous les endpoints.</summary>
    public string? EndpointId { get; set; }

    public AlertMetric Metric { get; set; } = AlertMetric.LatencyMs;
    public double Threshold { get; set; }
    public AlertSeverityLevel Severity { get; set; } = AlertSeverityLevel.Warning;
    public bool IsEnabled { get; set; } = true;

    /// <summary>Cooldown en minutes pour éviter le spam d'alertes.</summary>
    public int CooldownMinutes { get; set; } = 5;

    public DateTimeOffset? LastTriggered { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Instance d'une alerte déclenchée.</summary>
public sealed class AlertEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..12];
    public DateTimeOffset TriggeredAt { get; set; } = DateTimeOffset.UtcNow;
    public string RuleName { get; set; } = "";
    public string EndpointName { get; set; } = "";
    public AlertSeverityLevel Severity { get; set; }
    public string Message { get; set; } = "";
    public double MeasuredValue { get; set; }
    public double ThresholdValue { get; set; }
    public bool IsAcknowledged { get; set; }
}
