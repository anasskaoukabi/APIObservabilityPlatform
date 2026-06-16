using System.Diagnostics;
using Dashboard.Web.Models;

namespace Dashboard.Web.Services;

/// <summary>
/// Service de fond (.NET IHostedService) qui interroge réellement chaque endpoint HTTP
/// via HttpClient + Stopwatch et enregistre les métriques dans MetricsStore.
/// 
/// Chaque endpoint possède son propre intervalle de polling (PollingIntervalSeconds).
/// La boucle principale s'exécute toutes les secondes et déclenche les checks dus.
/// </summary>
public sealed class MonitoringBackgroundService : BackgroundService
{
    private readonly IHttpClientFactory     _http;
    private readonly EndpointConfigStore    _endpoints;
    private readonly MetricsStore           _metrics;
    private readonly AlertsStore            _alerts;
    private readonly ILogger<MonitoringBackgroundService> _logger;

    // Dernière exécution par endpoint
    private readonly Dictionary<string, DateTimeOffset> _lastRun = new();

    public MonitoringBackgroundService(
        IHttpClientFactory  http,
        EndpointConfigStore endpoints,
        MetricsStore        metrics,
        AlertsStore         alerts,
        ILogger<MonitoringBackgroundService> logger)
    {
        _http      = http;
        _endpoints = endpoints;
        _metrics   = metrics;
        _alerts    = alerts;
        _logger    = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("[MonitoringService] 🚀 Démarrage du service de monitoring.");

        while (!ct.IsCancellationRequested)
        {
            var now      = DateTimeOffset.UtcNow;
            var allEps   = _endpoints.GetAll();
            var dueEps   = allEps.Where(ep =>
                ep.IsEnabled &&
                (!_lastRun.TryGetValue(ep.Id, out var last) ||
                 now - last >= TimeSpan.FromSeconds(ep.PollingIntervalSeconds)));

            // Exécuter les checks en parallèle (max 5 simultanés)
            var tasks = dueEps.Select(ep => CheckEndpointAsync(ep, ct));
            await Task.WhenAll(tasks);

            await Task.Delay(1000, ct); // tick toutes les secondes
        }

        _logger.LogInformation("[MonitoringService] 🛑 Arrêt du service de monitoring.");
    }

    private async Task CheckEndpointAsync(MonitoredEndpoint ep, CancellationToken ct)
    {
        _lastRun[ep.Id] = DateTimeOffset.UtcNow;
        _endpoints.UpdateLastChecked(ep.Id);

        var client  = _http.CreateClient("MonitoringClient");
        var sw      = Stopwatch.StartNew();
        var record  = new MetricRecord
        {
            EndpointId   = ep.Id,
            EndpointName = ep.Name,
            Url          = ep.Url,
        };

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(ep.TimeoutSeconds));

            var response = await client.GetAsync(ep.Url, cts.Token);
            sw.Stop();

            record.LatencyMs      = sw.Elapsed.TotalMilliseconds;
            record.HttpStatusCode = (int)response.StatusCode;
            record.IsSuccess      = response.IsSuccessStatusCode;

            // Lire le contenu de la réponse
            var contentBytes = await response.Content.ReadAsByteArrayAsync(ct);
            record.ResponseSizeBytes = contentBytes.Length;
            string contentStr = System.Text.Encoding.UTF8.GetString(contentBytes);

            if (record.IsSuccess)
            {
                // 1. Validation SOAP / WSDL
                if (ep.Type == EndpointType.SoapWsdl || ep.IsWsdl)
                {
                    try
                    {
                        var doc = System.Xml.Linq.XDocument.Parse(contentStr);
                        var rootName = doc.Root?.Name.LocalName;
                        bool hasWsdlIndicator = rootName == "definitions" || 
                                               rootName == "schema" || 
                                               contentStr.Contains("wsdl:definitions") || 
                                               contentStr.Contains("<wsdl:") || 
                                               contentStr.Contains("<definitions");
                        if (!hasWsdlIndicator)
                        {
                            record.IsSuccess = false;
                            record.ErrorMessage = "XML valide, mais ne ressemble pas à un WSDL.";
                        }
                    }
                    catch (System.Xml.XmlException ex)
                    {
                        record.IsSuccess = false;
                        record.ErrorMessage = $"XML WSDL non valide : {ex.Message}";
                    }
                }
                // 2. Validation GraphQL
                else if (ep.Type == EndpointType.GraphQL)
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(contentStr);
                    }
                    catch (System.Text.Json.JsonException ex)
                    {
                        record.IsSuccess = false;
                        record.ErrorMessage = $"JSON GraphQL non valide : {ex.Message}";
                    }
                }

                // 3. Validation du contenu attendu
                if (record.IsSuccess && !string.IsNullOrWhiteSpace(ep.ExpectedContentContains))
                {
                    if (!contentStr.Contains(ep.ExpectedContentContains, StringComparison.OrdinalIgnoreCase))
                    {
                        record.IsSuccess = false;
                        record.ErrorMessage = $"Contenu attendu '{ep.ExpectedContentContains}' introuvable.";
                    }
                }
            }
            else
            {
                record.ErrorMessage = $"Erreur HTTP {(int)response.StatusCode}";
            }

            if (record.IsSuccess)
            {
                _logger.LogDebug(
                    "[MonitoringService] ✅ {Name} → {Status} in {Ms:F0}ms",
                    ep.Name, record.HttpStatusCode, record.LatencyMs);
            }
            else
            {
                _logger.LogWarning(
                    "[MonitoringService] ❌ {Name} → {Status} (Échec: {Msg}) in {Ms:F0}ms",
                    ep.Name, record.HttpStatusCode, record.ErrorMessage, record.LatencyMs);
            }
        }
        catch (TaskCanceledException)
        {
            sw.Stop();
            record.LatencyMs   = sw.Elapsed.TotalMilliseconds;
            record.IsSuccess   = false;
            record.ErrorMessage = $"Timeout après {ep.TimeoutSeconds}s";
            _logger.LogWarning("[MonitoringService] ⏱ Timeout: {Name}", ep.Name);
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            record.LatencyMs    = sw.Elapsed.TotalMilliseconds;
            record.IsSuccess    = false;
            record.ErrorMessage = ex.Message;
            _logger.LogWarning("[MonitoringService] 🔴 Erreur réseau: {Name} — {Msg}", ep.Name, ex.Message);
        }
        catch (Exception ex)
        {
            sw.Stop();
            record.LatencyMs    = sw.Elapsed.TotalMilliseconds;
            record.IsSuccess    = false;
            record.ErrorMessage = ex.Message;
            _logger.LogError(ex, "[MonitoringService] ❌ Erreur inattendue: {Name}", ep.Name);
        }

        // Enregistrer la métrique
        _metrics.Add(record);

        // Évaluer les alertes
        var recentRecords    = _metrics.GetByEndpoint(ep.Id, 20);
        var failCount        = recentRecords.Count(r => !r.IsSuccess);
        var errorRatePct     = recentRecords.Count > 0
                               ? (double)failCount / recentRecords.Count * 100
                               : 0;
        _alerts.Evaluate(record, errorRatePct);
    }
}
