using Observability.Contracts.Enums;
using Observability.Contracts.Models;

namespace Dashboard.Web.Services;

/// <summary>
/// Simule des métriques en temps réel jusqu'à ce que le MonitoringWorker soit connecté via SignalR.
/// Génère des données réalistes avec de la variance aléatoire pour démontrer l'UI.
/// </summary>
public sealed class MetricsSimulatorService
{
    private static readonly Random _rng = Random.Shared;

    private static readonly string[] _endpoints =
    [
        "products-api",
        "auth-service",
        "inventory-api"
    ];

    private static readonly string[] _urls =
    [
        "http://localhost:5100/api/products/health",
        "http://localhost:5100/api/products",
        "http://localhost:5100/api/products/health"
    ];

    // Historique glissant par endpoint (max 30 points)
    private readonly Dictionary<string, Queue<double>> _latencyHistory = new()
    {
        ["products-api"]  = new Queue<double>(Enumerable.Repeat(120.0, 20)),
        ["auth-service"]  = new Queue<double>(Enumerable.Repeat(85.0, 20)),
        ["inventory-api"] = new Queue<double>(Enumerable.Repeat(200.0, 20)),
    };

    private readonly Dictionary<string, double> _baseLatency = new()
    {
        ["products-api"]  = 120,
        ["auth-service"]  = 85,
        ["inventory-api"] = 200,
    };

    private readonly Dictionary<string, double> _errorRate = new()
    {
        ["products-api"]  = 0.12,
        ["auth-service"]  = 0.04,
        ["inventory-api"] = 0.20,
    };

    public List<MetricSnapshot> GetCurrentSnapshots()
    {
        var snapshots = new List<MetricSnapshot>();

        for (int i = 0; i < _endpoints.Length; i++)
        {
            var id  = _endpoints[i];
            var url = _urls[i];

            // Simule une mesure avec de la variance
            var isError   = _rng.NextDouble() < _errorRate[id];
            var latency   = isError
                ? _rng.Next(800, 3000)
                : _baseLatency[id] + _rng.Next(-40, 80);

            var history = _latencyHistory[id];
            history.Enqueue(latency);
            if (history.Count > 30) history.Dequeue();

            var allLatencies = history.ToArray();

            snapshots.Add(new MetricSnapshot
            {
                ApiEndpointId      = id,
                TargetUrl          = url,
                LastChecked        = DateTimeOffset.UtcNow,
                LatestLatencyMs    = Math.Round(latency, 1),
                AverageLatencyMs   = Math.Round(allLatencies.Average(), 1),
                MaxLatencyMs       = Math.Round(allLatencies.Max(), 1),
                SuccessRatePercent = Math.Round((1 - _errorRate[id]) * 100, 1),
                LastHttpStatusCode = isError ? 500 : 200,
                LastStatusCategory = isError ? HttpStatusCategory.ServerError : HttpStatusCategory.Success,
                TotalChecks        = 300,
                FailedChecks       = (int)(300 * _errorRate[id]),
            });
        }

        return snapshots;
    }

    public Dictionary<string, double[]> GetLatencyHistory()
    {
        return _latencyHistory.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.ToArray()
        );
    }
}
