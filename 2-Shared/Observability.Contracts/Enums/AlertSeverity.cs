namespace Observability.Contracts.Enums;

/// <summary>
/// Niveau de sévérité d'une alerte générée par le moteur de règles du MonitoringWorker.
/// Permet de prioriser les notifications (email, log, SignalR push).
/// </summary>
public enum AlertSeverity
{
    /// <summary>Information — aucune action requise</summary>
    Info = 0,

    /// <summary>Avertissement — latence ou taux d'erreur en hausse</summary>
    Warning = 1,

    /// <summary>Erreur critique — seuil dépassé, action immédiate requise</summary>
    Critical = 2
}
