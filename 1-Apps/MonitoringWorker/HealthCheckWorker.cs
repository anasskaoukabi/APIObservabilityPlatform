using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Observability.Contracts.Data;
using Observability.Contracts.Entities;
using MailKit.Net.Smtp;
using MimeKit;

namespace MonitoringWorker;

/// <summary>
/// Worker Service automatique (Axe 2) — Health Check avec mesure de latence et gestion des timeouts.
/// Persistance SQLite (Axe 3) et Alerte SMTP réelle avec MailKit.
/// Seuils SLA :
///   SUCCESS  → HTTP 200 et temps < 1500 ms
///   WARNING  → HTTP 200 mais temps entre 1500 ms et 4000 ms (API lente)
///   TIMEOUT  → Pas de réponse dans les 4 secondes (TaskCanceledException)
///   CRITICAL → Code HTTP non-200 ou erreur réseau
/// </summary>
public sealed class HealthCheckWorker : BackgroundService
{
    private readonly ILogger<HealthCheckWorker> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly string _apiUrl;
    private readonly int _intervalSeconds;

    // Seuils SLA (en millisecondes)
    private const int SlaWarningMs  = 1500;
    private const int SlaCriticalMs = 4000;

    // État précédent pour éviter le spam d'alertes email (Axe 3 / MailKit)
    private bool _wasSuccess = true;

    public HealthCheckWorker(
        ILogger<HealthCheckWorker> logger,
        IHttpClientFactory httpClientFactory,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _scopeFactory = scopeFactory;
        _configuration = configuration;

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

    private async Task CheckApiAsync(CancellationToken stoppingToken)
    {
        var timestamp = DateTime.Now;
        var timestampStr = timestamp.ToString("dd/MM/yyyy HH:mm:ss");

        var client = _httpClientFactory.CreateClient("HealthCheckClient");

        int? statusCode = null;
        double latencyMs = 0;
        bool isSuccess = false;
        string? errorMessage = null;

        var sw = Stopwatch.StartNew();

        try
        {
            var response = await client.GetAsync(_apiUrl, stoppingToken);
            sw.Stop();
            latencyMs = sw.Elapsed.TotalMilliseconds;
            statusCode = (int)response.StatusCode;

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                isSuccess = true;
                if (latencyMs < SlaWarningMs)
                {
                    // ── SUCCESS ─────────────────────────────────────────────
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[{timestampStr}] [SUCCESS] API OK — Latence : {latencyMs:F0} ms");
                    Console.ResetColor();

                    _logger.LogInformation("[SUCCESS] API OK — Latence : {Latency:F0} ms", latencyMs);
                }
                else
                {
                    // ── WARNING : API lente ──────────────────────────────────
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[{timestampStr}] [WARNING] API LENTE — Latence : {latencyMs:F0} ms (seuil : {SlaWarningMs} ms)");
                    Console.ResetColor();

                    _logger.LogWarning("[WARNING] API LENTE — Latence : {Latency:F0} ms (seuil SLA : {Sla} ms)", latencyMs, SlaWarningMs);
                }
            }
            else
            {
                // ── CRITICAL : code HTTP non-200 ────────────────────────────
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{timestampStr}] [CRITICAL] API HORS SERVICE — HTTP {statusCode} {response.StatusCode} — Latence : {latencyMs:F0} ms");
                Console.ResetColor();

                errorMessage = $"HTTP {statusCode} {response.ReasonPhrase}";
                _logger.LogError("[CRITICAL] API HORS SERVICE — HTTP {StatusCode} — Latence : {Latency:F0} ms", statusCode, latencyMs);
            }
        }
        catch (TaskCanceledException) when (!stoppingToken.IsCancellationRequested)
        {
            // ── TIMEOUT : pas de réponse dans les 4 secondes ────────────────
            sw.Stop();
            latencyMs = sw.Elapsed.TotalMilliseconds;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{timestampStr}] [TIMEOUT / CRITICAL] L'API ne répond pas après {SlaCriticalMs / 1000}s — CRITIQUE !");
            Console.ResetColor();

            errorMessage = $"Timeout de {SlaCriticalMs / 1000} secondes dépassé";
            _logger.LogCritical("[TIMEOUT / CRITICAL] L'API ne répond pas après {Timeout}s", SlaCriticalMs / 1000);
        }
        catch (HttpRequestException ex)
        {
            // ── CRITICAL : erreur réseau (hôte injoignable, DNS…) ───────────
            sw.Stop();
            latencyMs = sw.Elapsed.TotalMilliseconds;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{timestampStr}] [CRITICAL] Erreur réseau — {ex.Message}");
            Console.ResetColor();

            errorMessage = ex.Message;
            statusCode = (int?)ex.StatusCode;
            _logger.LogError(ex, "[CRITICAL] Erreur réseau lors du Health Check");
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            // ── CRITICAL : erreur inattendue ────────────────────────────────
            sw.Stop();
            latencyMs = sw.Elapsed.TotalMilliseconds;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{timestampStr}] [CRITICAL] Erreur inattendue — {ex.GetType().Name}: {ex.Message}");
            Console.ResetColor();

            errorMessage = $"{ex.GetType().Name}: {ex.Message}";
            _logger.LogError(ex, "[CRITICAL] Erreur inattendue lors du Health Check");
        }

        // ── Persistance dans la base de données SQLite ──────────────────────
        await SaveToDatabaseAsync(timestamp, statusCode, latencyMs, isSuccess, errorMessage);

        // ── Système de notification SMTP avec MailKit (Axe 3) ────────────────
        if (!isSuccess)
        {
            if (_wasSuccess)
            {
                // Transition : de En Ligne à Hors Service
                _wasSuccess = false;
                await SendAlertEmailAsync(errorMessage ?? "Inconnu", statusCode);
            }
        }
        else
        {
            // Réinitialisation du flag de succès pour les futures pannes
            _wasSuccess = true;
        }
    }

    private async Task SaveToDatabaseAsync(DateTime timestamp, int? statusCode, double latencyMs, bool isSuccess, string? errorMessage)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();

            var log = new HealthCheckLog
            {
                Timestamp = timestamp,
                StatusCode = statusCode,
                ResponseTimeMs = Math.Round(latencyMs, 1),
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage
            };

            db.HealthCheckLogs.Add(log);
            await db.SaveChangesAsync();
            _logger.LogDebug("💾 Health check log enregistré avec succès en base de données.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Échec de la persistance du log en base de données SQLite");
        }
    }

    private async Task SendAlertEmailAsync(string errorMsg, int? statusCode)
    {
        try
        {
            var smtpHost = _configuration["Smtp:Host"] ?? "localhost";
            var smtpPort = int.TryParse(_configuration["Smtp:Port"], out var port) ? port : 1025;
            var smtpUser = _configuration["Smtp:Username"] ?? "";
            var smtpPass = _configuration["Smtp:Password"] ?? "";
            var fromAddress = _configuration["Smtp:From"] ?? "alert@observability.local";
            var toAddress = _configuration["Smtp:To"] ?? "admin@example.com";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Plateforme Observabilité", fromAddress));
            message.To.Add(new MailboxAddress("Administrateur", toAddress));
            message.Subject = "🚨 [ALERTE CRITIQUE] API CRUD Témoin Hors Service !";

            message.Body = new TextPart("plain")
            {
                Text = $"Bonjour,\n\n" +
                       $"L'API CRUD Témoin est actuellement HORS SERVICE ou inaccessible.\n\n" +
                       $"Détails de l'alerte :\n" +
                       $"- URL : {_apiUrl}\n" +
                       $"- Date/Heure : {DateTime.Now:dd/MM/yyyy HH:mm:ss}\n" +
                       $"- Code Statut : {(statusCode.HasValue ? statusCode.Value.ToString() : "N/A (Timeout/Réseau)")}\n" +
                       $"- Erreur : {errorMsg}\n\n" +
                       $"Veuillez vérifier l'état du service immédiatement.\n\n" +
                       $"Cordialement,\n" +
                       $"Votre Plateforme d'Observabilité."
            };

            using var client = new SmtpClient();

            var useSsl = bool.TryParse(_configuration["Smtp:EnableSsl"], out var ssl) && ssl;
            var options = useSsl 
                ? MailKit.Security.SecureSocketOptions.SslOnConnect 
                : MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable;

            // Permettre les certificats auto-signés pour le développement local
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;

            await client.ConnectAsync(smtpHost, smtpPort, options);

            if (!string.IsNullOrEmpty(smtpUser))
            {
                await client.AuthenticateAsync(smtpUser, smtpPass);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("📧 Email d'alerte envoyé avec succès à {To} via {Host}:{Port}", toAddress, smtpHost, smtpPort);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Échec de l'envoi de l'email d'alerte SMTP");
        }
    }
}
