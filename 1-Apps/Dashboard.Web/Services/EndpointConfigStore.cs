using Dashboard.Web.Models;

namespace Dashboard.Web.Services;

/// <summary>
/// Store singleton des endpoints configurés à surveiller.
/// Thread-safe via lock. Pré-chargé avec des endpoints réels de démonstration.
/// </summary>
public sealed class EndpointConfigStore
{
    private readonly List<MonitoredEndpoint> _endpoints;
    private readonly object _lock = new();

    public event Action? Changed;

    public EndpointConfigStore()
    {
        // Endpoints pré-configurés (REST + SOAP/WSDL réels)
        _endpoints =
        [
            new MonitoredEndpoint
            {
                Id   = "target-health",
                Name = "TargetAPI — Health",
                Url  = "http://localhost:5100/api/products/health",
                Type = EndpointType.Rest,
                PollingIntervalSeconds = 5,
            },
            new MonitoredEndpoint
            {
                Id   = "target-products",
                Name = "TargetAPI — Products",
                Url  = "http://localhost:5100/api/products",
                Type = EndpointType.Rest,
                PollingIntervalSeconds = 8,
            },
            new MonitoredEndpoint
            {
                Id   = "jsonplaceholder",
                Name = "JSONPlaceholder (REST public)",
                Url  = "https://jsonplaceholder.typicode.com/posts/1",
                Type = EndpointType.Rest,
                PollingIntervalSeconds = 15,
            },
            new MonitoredEndpoint
            {
                Id   = "httpbin",
                Name = "HTTPBin — GET Test",
                Url  = "https://httpbin.org/get",
                Type = EndpointType.Rest,
                PollingIntervalSeconds = 20,
            },
            new MonitoredEndpoint
            {
                Id   = "calculator-wsdl",
                Name = "Calculator SOAP (WSDL)",
                Url  = "http://www.dneonline.com/calculator.asmx?wsdl",
                Type = EndpointType.SoapWsdl,
                PollingIntervalSeconds = 30,
            },
        ];
    }

    public IReadOnlyList<MonitoredEndpoint> GetAll()
    {
        lock (_lock) return [.. _endpoints];
    }

    public MonitoredEndpoint? GetById(string id)
    {
        lock (_lock) return _endpoints.FirstOrDefault(e => e.Id == id);
    }

    public void Add(MonitoredEndpoint endpoint)
    {
        lock (_lock) _endpoints.Add(endpoint);
        Changed?.Invoke();
    }

    public void Update(MonitoredEndpoint endpoint)
    {
        lock (_lock)
        {
            var idx = _endpoints.FindIndex(e => e.Id == endpoint.Id);
            if (idx >= 0) _endpoints[idx] = endpoint;
        }
        Changed?.Invoke();
    }

    public void SetEnabled(string id, bool enabled)
    {
        lock (_lock)
        {
            var e = _endpoints.FirstOrDefault(x => x.Id == id);
            if (e is not null) e.IsEnabled = enabled;
        }
        Changed?.Invoke();
    }

    public void Delete(string id)
    {
        lock (_lock) _endpoints.RemoveAll(e => e.Id == id);
        Changed?.Invoke();
    }

    public void UpdateLastChecked(string id)
    {
        lock (_lock)
        {
            var e = _endpoints.FirstOrDefault(x => x.Id == id);
            if (e is not null) e.LastChecked = DateTimeOffset.UtcNow;
        }
    }
}
