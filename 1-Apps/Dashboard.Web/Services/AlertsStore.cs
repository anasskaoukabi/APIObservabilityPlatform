using Dashboard.Web.Models;

namespace Dashboard.Web.Services;

/// <summary>
/// Store singleton des alertes déclenchées + règles d'alerte.
/// Évalue les règles après chaque nouvelle métrique.
/// </summary>
public sealed class AlertsStore
{
    private readonly GlobalSettings    _settings;
    private readonly List<AlertEntry>  _entries = [];
    private readonly List<AlertRule>   _rules;
    private readonly object            _lock    = new();
    private const    int               MaxAlerts = 100;

    public event Action? AlertTriggered;

    public AlertsStore(GlobalSettings settings)
    {
        _settings = settings;

        // Règles par défaut utilisant GlobalSettings
        _rules =
        [
            new AlertRule
            {
                Id        = "rule-lat-warn",
                Name      = "Latence élevée (Warning)",
                Metric    = AlertMetric.LatencyMs,
                Threshold = settings.LatencyWarningMs,
                Severity  = AlertSeverityLevel.Warning,
                CooldownMinutes = 3,
            },
            new AlertRule
            {
                Id        = "rule-lat-crit",
                Name      = "Latence critique",
                Metric    = AlertMetric.LatencyMs,
                Threshold = settings.LatencyCriticalMs,
                Severity  = AlertSeverityLevel.Critical,
                CooldownMinutes = 2,
            },
            new AlertRule
            {
                Id        = "rule-err-warn",
                Name      = "Erreur HTTP (Warning)",
                Metric    = AlertMetric.ErrorRatePercent,
                Threshold = settings.ErrorRateWarningPercent,
                Severity  = AlertSeverityLevel.Warning,
                CooldownMinutes = 5,
            },
            new AlertRule
            {
                Id        = "rule-err-crit",
                Name      = "Taux d'erreur critique",
                Metric    = AlertMetric.ErrorRatePercent,
                Threshold = settings.ErrorRateCriticalPercent,
                Severity  = AlertSeverityLevel.Critical,
                CooldownMinutes = 5,
            },
        ];
    }

    // ── Rules ─────────────────────────────────────────────────────────────────

    public IReadOnlyList<AlertRule> GetRules()
    {
        lock (_lock) return [.. _rules];
    }

    public void AddRule(AlertRule rule)
    {
        lock (_lock) _rules.Add(rule);
    }

    public void DeleteRule(string id)
    {
        lock (_lock) _rules.RemoveAll(r => r.Id == id);
    }

    public void ToggleRule(string id)
    {
        lock (_lock)
        {
            var rule = _rules.FirstOrDefault(r => r.Id == id);
            if (rule is not null) rule.IsEnabled = !rule.IsEnabled;
        }
    }

    // ── Entries ───────────────────────────────────────────────────────────────

    public List<AlertEntry> GetEntries(int count = 50)
    {
        lock (_lock)
            return _entries
                .OrderByDescending(e => e.TriggeredAt)
                .Take(count)
                .ToList();
    }

    public int UnacknowledgedCount()
    {
        lock (_lock) return _entries.Count(e => !e.IsAcknowledged);
    }

    public void Acknowledge(string id)
    {
        lock (_lock)
        {
            var entry = _entries.FirstOrDefault(e => e.Id == id);
            if (entry is not null) entry.IsAcknowledged = true;
        }
        AlertTriggered?.Invoke();
    }

    public void AcknowledgeAll()
    {
        lock (_lock) _entries.ForEach(e => e.IsAcknowledged = true);
        AlertTriggered?.Invoke();
    }

    /// <summary>Évalue toutes les règles actives contre la métrique reçue.</summary>
    public void Evaluate(MetricRecord record, double errorRatePercent)
    {
        lock (_lock)
        {
            foreach (var rule in _rules.Where(r => r.IsEnabled))
            {
                // Filtre par endpoint si la règle est spécifique
                if (rule.EndpointId is not null && rule.EndpointId != record.EndpointId)
                    continue;

                // Cooldown check
                if (rule.LastTriggered.HasValue &&
                    DateTimeOffset.UtcNow - rule.LastTriggered.Value <
                    TimeSpan.FromMinutes(rule.CooldownMinutes))
                    continue;

                // Synchronisation dynamique des seuils par défaut depuis GlobalSettings
                if (rule.Id == "rule-lat-warn") rule.Threshold = _settings.LatencyWarningMs;
                else if (rule.Id == "rule-lat-crit") rule.Threshold = _settings.LatencyCriticalMs;
                else if (rule.Id == "rule-err-warn") rule.Threshold = _settings.ErrorRateWarningPercent;
                else if (rule.Id == "rule-err-crit") rule.Threshold = _settings.ErrorRateCriticalPercent;

                double measured = rule.Metric switch
                {
                    AlertMetric.LatencyMs       => record.LatencyMs,
                    AlertMetric.ErrorRatePercent => errorRatePercent,
                    AlertMetric.HttpStatusCode   => record.HttpStatusCode ?? 0,
                    _                            => 0,
                };

                if (measured < rule.Threshold) continue;

                // Déclencher l'alerte
                rule.LastTriggered = DateTimeOffset.UtcNow;

                var entry = new AlertEntry
                {
                    RuleName      = rule.Name,
                    EndpointName  = record.EndpointName,
                    Severity      = rule.Severity,
                    MeasuredValue = Math.Round(measured, 1),
                    ThresholdValue = rule.Threshold,
                    Message       = $"[{record.EndpointName}] {rule.Metric} = {measured:F0}" +
                                    $" dépasse le seuil de {rule.Threshold}",
                };

                _entries.Insert(0, entry);
                if (_entries.Count > MaxAlerts) _entries.RemoveAt(_entries.Count - 1);

                // Simulation de l'envoi d'e-mail pour les alertes critiques si activé
                if (_settings.EmailAlertsEnabled && !string.IsNullOrWhiteSpace(_settings.AlertEmail) && rule.Severity == AlertSeverityLevel.Critical)
                {
                    Console.WriteLine($"[EMAIL SIMULATION] 📧 Alert email sent to {_settings.AlertEmail} via {_settings.SmtpHost}:{_settings.SmtpPort}. Subject: [{rule.Severity}] {rule.Name} on {record.EndpointName}. Message: {entry.Message}");
                }
            }
        }

        AlertTriggered?.Invoke();
    }
}
