using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MonitoringWorker;

/// <summary>
/// Worker Service automatique (Axe 2) — Health Check avec mesure de latence et gestion des timeouts.
/// Interroge régulièrement l'URL de l'API (toutes les 10 secondes par défaut).
/// Seuils SLA :
///   SUCCESS  → HTTP 200 et temps &lt; 1500 ms
///   WARNING  → HTTP 200 mais temps entre 1500 ms et 4000 ms (API lente)
///   TIMEOUT  → Pas de réponse dans les 4 secondes (TaskCanceledException)
///   CRITICAL → Code HTTP non-200 ou erreur réseau
/// </summary>
public sealed class HealthCheckWorker : BackgroundService
{
    private readonly ILogger<HealthCheckWorker> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiUrl;
    private readonly int _intervalSeconds;

    // Seuils SLA (en millisecondes)
    private const int SlaWarningMs  = 1500;
    private const int SlaCriticalMs = 4000;

    public HealthCheckWorker(
        ILogger<HealthCheckWorker> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;

        // Lecture de la configuration (appsettings.json → section HealthCheck)
        _apiUrl         = configuration["HealthCheck:Url"] ?? "http://localhost:5100/api/products";
        _intervalSeconds = int.TryParse(configuration["HealthCheck:IntervalSeconds"], out var val) ? val : 10;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "🚀 [HealthCheckWorker] Démarrage — URL : {Url} | Intervalle : {Interval}s | Timeout : {Timeout}s | Seuil WARNING : {Warn}ms",
            _apiUrl, _intervalSeconds, SlaCriticalMs / 1000, SlaWarningMs);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_intervalSeconds));

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CheckApiAsync(stoppingToken);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Core health-check logic
    // ─────────────────────────────────────────────────────────────────────────
    private async Task CheckApiAsync(CancellationToken stoppingToken)
    {
        var timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

        // Le client est configuré avec un timeout de 4 s dans Program.cs
        var client = _httpClientFactory.CreateClient("HealthCheckClient");

        // ── Mesure de latence ────────────────────────────────────────────────
        var sw = Stopwatch.StartNew();

        try
        {
            var response = await client.GetAsync(_apiUrl, stoppingToken);
            sw.Stop();
            long latencyMs = sw.ElapsedMilliseconds;

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                if (latencyMs < SlaWarningMs)
                {
                    // ── SUCCESS ─────────────────────────────────────────────
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[{timestamp}] [SUCCESS] API OK — Latence : {latencyMs} ms");
                    Console.ResetColor();

                    _logger.LogInformation("[SUCCESS] API OK — Latence : {Latency} ms", latencyMs);
                }
                else
                {
                    // ── WARNING : API lente ──────────────────────────────────
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[{timestamp}] [WARNING] API LENTE — Latence : {latencyMs} ms (seuil : {SlaWarningMs} ms)");
                    Console.ResetColor();

                    _logger.LogWarning("[WARNING] API LENTE — Latence : {Latency} ms (seuil SLA : {Sla} ms)", latencyMs, SlaWarningMs);
                }
            }
            else
            {
                // ── CRITICAL : code HTTP non-200 ────────────────────────────
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{timestamp}] [CRITICAL] API HORS SERVICE — HTTP {(int)response.StatusCode} {response.StatusCode} — Latence : {latencyMs} ms");
                Console.ResetColor();

                _logger.LogError("[CRITICAL] API HORS SERVICE — HTTP {StatusCode} — Latence : {Latency} ms",
                    (int)response.StatusCode, latencyMs);
            }
        }
        catch (TaskCanceledException) when (!stoppingToken.IsCancellationRequested)
        {
            // ── TIMEOUT : pas de réponse dans les 4 secondes ────────────────
            sw.Stop();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{timestamp}] [TIMEOUT / CRITICAL] L'API ne répond pas après {SlaCriticalMs / 1000}s — CRITIQUE !");
            Console.ResetColor();

            _logger.LogCritical("[TIMEOUT / CRITICAL] L'API ne répond pas après {Timeout}s", SlaCriticalMs / 1000);
        }
        catch (HttpRequestException ex)
        {
            // ── CRITICAL : erreur réseau (hôte injoignable, DNS…) ───────────
            sw.Stop();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{timestamp}] [CRITICAL] Erreur réseau — {ex.Message}");
            Console.ResetColor();

            _logger.LogError(ex, "[CRITICAL] Erreur réseau lors du Health Check");
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            // ── CRITICAL : erreur inattendue ────────────────────────────────
            sw.Stop();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{timestamp}] [CRITICAL] Erreur inattendue — {ex.GetType().Name}: {ex.Message}");
            Console.ResetColor();

            _logger.LogError(ex, "[CRITICAL] Erreur inattendue lors du Health Check");
        }
    }
}
