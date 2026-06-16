using Observability.Contracts.Enums;

namespace Observability.Contracts.Models;

/// <summary>
/// Modèle représentant un événement d'alerte déclenché par le moteur de règles du MonitoringWorker.
/// Transmis via SignalR au Dashboard et utilisé par le service d'email (MailKit).
/// </summary>
public sealed record AlertEvent
{
    /// <summary>Identifiant unique de l'alerte (GUID pour éviter les doublons SignalR).</summary>
    public Guid AlertId { get; init; } = Guid.NewGuid();

    /// <summary>Horodatage UTC de déclenchement de l'alerte.</summary>
    public DateTimeOffset TriggeredAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Identifiant de l'API ayant déclenché l'alerte.</summary>
    public string ApiEndpointId { get; init; } = string.Empty;

    /// <summary>Niveau de sévérité de l'alerte.</summary>
    public AlertSeverity Severity { get; init; }

    /// <summary>
    /// Message lisible décrivant la cause de l'alerte.
    /// Ex : "Latence de 1523 ms dépasse le seuil de 1000 ms".
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Valeur mesurée ayant déclenché l'alerte (latence, taux d'erreur, etc.).</summary>
    public double MeasuredValue { get; init; }

    /// <summary>Seuil configuré qui a été dépassé.</summary>
    public double ThresholdValue { get; init; }
}
