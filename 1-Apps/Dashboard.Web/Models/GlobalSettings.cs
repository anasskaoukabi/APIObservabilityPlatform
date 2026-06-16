namespace Dashboard.Web.Models;

/// <summary>Paramètres globaux de la plateforme (stockés en mémoire).</summary>
public sealed class GlobalSettings
{
    /// <summary>Intervalle de polling par défaut (secondes) pour les nouveaux endpoints.</summary>
    public int DefaultPollingIntervalSeconds { get; set; } = 10;

    /// <summary>Timeout HTTP global (secondes).</summary>
    public int HttpTimeoutSeconds { get; set; } = 10;

    /// <summary>Seuil de latence Warning (ms).</summary>
    public double LatencyWarningMs { get; set; } = 500;

    /// <summary>Seuil de latence Critical (ms).</summary>
    public double LatencyCriticalMs { get; set; } = 1500;

    /// <summary>Seuil de taux d'erreur Warning (%).</summary>
    public double ErrorRateWarningPercent { get; set; } = 10;

    /// <summary>Seuil de taux d'erreur Critical (%).</summary>
    public double ErrorRateCriticalPercent { get; set; } = 25;

    /// <summary>Nombre max de métriques conservées par endpoint (buffer circulaire).</summary>
    public int MaxRecordsPerEndpoint { get; set; } = 200;

    /// <summary>Email de destination pour les alertes (placeholder MailKit).</summary>
    public string AlertEmail { get; set; } = "";

    /// <summary>Activer les alertes par email.</summary>
    public bool EmailAlertsEnabled { get; set; } = false;

    /// <summary>Serveur SMTP (placeholder).</summary>
    public string SmtpHost { get; set; } = "smtp.gmail.com";

    /// <summary>Port SMTP.</summary>
    public int SmtpPort { get; set; } = 587;
}
