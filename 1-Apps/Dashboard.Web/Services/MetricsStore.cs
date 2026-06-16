using Dashboard.Web.Models;

namespace Dashboard.Web.Services;

/// <summary>
/// Buffer circulaire thread-safe de MetricRecords.
/// Notifie les composants Blazor via l'événement DataChanged.
/// </summary>
public sealed class MetricsStore
{
    private readonly GlobalSettings _settings;
    // Un buffer par endpoint, max records dynamique
    private readonly Dictionary<string, LinkedList<MetricRecord>> _buckets = new();
    private readonly object _lock = new();

    public event Action? DataChanged;

    public MetricsStore(GlobalSettings settings)
    {
        _settings = settings;
    }

    public void Add(MetricRecord record)
    {
        lock (_lock)
        {
            if (!_buckets.TryGetValue(record.EndpointId, out var list))
            {
                list = new LinkedList<MetricRecord>();
                _buckets[record.EndpointId] = list;
            }

            list.AddFirst(record);                     // plus récent en tête
            int capacity = _settings?.MaxRecordsPerEndpoint ?? 200;
            while (list.Count > capacity)
                list.RemoveLast();
        }

        DataChanged?.Invoke();
    }

    /// <summary>Retourne les N derniers enregistrements toutes sources confondues.</summary>
    public List<MetricRecord> GetRecent(int count = 100)
    {
        lock (_lock)
        {
            return _buckets.Values
                .SelectMany(l => l)
                .OrderByDescending(r => r.Timestamp)
                .Take(count)
                .ToList();
        }
    }

    /// <summary>Retourne tous les enregistrements d'un endpoint donné.</summary>
    public List<MetricRecord> GetByEndpoint(string endpointId, int count = 200)
    {
        lock (_lock)
        {
            if (!_buckets.TryGetValue(endpointId, out var list))
                return [];
            return list.Take(count).ToList();
        }
    }

    /// <summary>Retourne les enregistrements filtrés.</summary>
    public List<MetricRecord> GetFiltered(
        string? endpointId = null,
        bool? successOnly  = null,
        int   count        = 100)
    {
        lock (_lock)
        {
            IEnumerable<MetricRecord> query = _buckets.Values.SelectMany(l => l);

            if (endpointId is not null)
                query = query.Where(r => r.EndpointId == endpointId);

            if (successOnly.HasValue)
                query = query.Where(r => r.IsSuccess == successOnly.Value);

            return query
                .OrderByDescending(r => r.Timestamp)
                .Take(count)
                .ToList();
        }
    }

    /// <summary>Snapshot agrégé par endpoint pour le Dashboard.</summary>
    public List<EndpointSummary> GetSummaries()
    {
        lock (_lock)
        {
            var summaries = new List<EndpointSummary>();

            foreach (var (id, list) in _buckets)
            {
                if (list.Count == 0) continue;

                var records    = list.ToList();
                var latest     = records[0];
                var latencies  = records.Select(r => r.LatencyMs).ToArray();
                var successCnt = records.Count(r => r.IsSuccess);

                summaries.Add(new EndpointSummary
                {
                    EndpointId         = id,
                    EndpointName       = latest.EndpointName,
                    Url                = latest.Url,
                    LastChecked        = latest.Timestamp,
                    LatestLatencyMs    = Math.Round(latest.LatencyMs, 1),
                    AverageLatencyMs   = Math.Round(latencies.Average(), 1),
                    MaxLatencyMs       = Math.Round(latencies.Max(), 1),
                    MinLatencyMs       = Math.Round(latencies.Min(), 1),
                    LastHttpStatus     = latest.HttpStatusCode,
                    IsLastSuccess      = latest.IsSuccess,
                    SuccessRatePercent = Math.Round((double)successCnt / records.Count * 100, 1),
                    TotalChecks        = records.Count,
                    FailedChecks       = records.Count - successCnt,
                    LatencyHistory     = latencies.Take(30).Reverse().ToArray(),
                });
            }

            return summaries.OrderBy(s => s.EndpointName).ToList();
        }
    }

    public int TotalRecords()
    {
        lock (_lock) return _buckets.Values.Sum(l => l.Count);
    }
}

/// <summary>Vue agrégée d'un endpoint pour le Dashboard.</summary>
public sealed class EndpointSummary
{
    public string EndpointId { get; set; } = "";
    public string EndpointName { get; set; } = "";
    public string Url { get; set; } = "";
    public DateTimeOffset LastChecked { get; set; }
    public double LatestLatencyMs { get; set; }
    public double AverageLatencyMs { get; set; }
    public double MaxLatencyMs { get; set; }
    public double MinLatencyMs { get; set; }
    public int? LastHttpStatus { get; set; }
    public bool IsLastSuccess { get; set; }
    public double SuccessRatePercent { get; set; }
    public int TotalChecks { get; set; }
    public int FailedChecks { get; set; }
    public double[] LatencyHistory { get; set; } = [];

    public string HealthStatus =>
        SuccessRatePercent >= 95 && LatestLatencyMs < 600 ? "healthy" :
        SuccessRatePercent >= 80 || LatestLatencyMs < 1500 ? "degraded" : "critical";

    public string HealthLabel =>
        HealthStatus switch
        {
            "healthy"  => "Healthy",
            "degraded" => "Dégradé",
            _          => "Critique"
        };
}
