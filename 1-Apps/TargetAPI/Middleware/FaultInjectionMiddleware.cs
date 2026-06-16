using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TargetAPI.Settings;

namespace TargetAPI.Middleware;

/// <summary>
/// Middleware ASP.NET Core de simulation de pannes (Chaos Engineering léger).
/// ⚠️ S'applique UNIQUEMENT aux routes sous <see cref="FaultInjectionSettings.ApiPathPrefix"/>
/// (par défaut : /api) — les fichiers statiques et Swagger UI ne sont jamais affectés.
/// </summary>
/// <remarks>
/// Deux modes de pannes disponibles (configurables indépendamment) :
/// <list type="bullet">
///   <item><b>Erreur 500</b> : Court-circuite la requête avec HTTP 500.</item>
///   <item><b>Latence injectée</b> : Ajoute un délai aléatoire avant de transmettre.</item>
/// </list>
/// Si les deux sont actifs, l'erreur est évaluée en premier.
/// </remarks>
public sealed class FaultInjectionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<FaultInjectionMiddleware> _logger;

    // IOptionsMonitor → hot-reload sans redémarrage de l'API.
    private readonly IOptionsMonitor<FaultInjectionSettings> _settingsMonitor;

    // Random.Shared → thread-safe, zéro allocation supplémentaire.
    private static readonly Random _random = Random.Shared;

    public FaultInjectionMiddleware(
        RequestDelegate next,
        ILogger<FaultInjectionMiddleware> logger,
        IOptionsMonitor<FaultInjectionSettings> settingsMonitor)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settingsMonitor = settingsMonitor ?? throw new ArgumentNullException(nameof(settingsMonitor));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var settings = _settingsMonitor.CurrentValue;

        // ── Guard 1 : middleware globalement désactivé ─────────────────────────
        if (!settings.Enabled)
        {
            await _next(context);
            return;
        }

        var requestPath = context.Request.Path;

        // ── Guard 2 : n'injecter des pannes QUE sur /api/* ────────────────────
        // Swagger UI (/swagger, /index.html), health checks, static files → exclus.
        if (!requestPath.StartsWithSegments(settings.ApiPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // ── 1. Simulation d'erreur 500 ────────────────────────────────────────
        if (settings.ErrorRate > 0 && _random.NextDouble() < settings.ErrorRate)
        {
            _logger.LogWarning(
                "[FaultInjection] 💥 Erreur 500 injectée sur {Method} {Path}",
                context.Request.Method, requestPath);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                error      = "Simulated Internal Server Error",
                injectedBy = "FaultInjectionMiddleware",
                path       = requestPath.Value,
                timestamp  = DateTimeOffset.UtcNow
            });

            return; // court-circuit — _next n'est PAS appelé
        }

        // ── 2. Simulation de latence ───────────────────────────────────────────
        if (settings.LatencyRate > 0 && _random.NextDouble() < settings.LatencyRate)
        {
            var delayMs = _random.Next(settings.MinLatencyMs, settings.MaxLatencyMs + 1);

            _logger.LogWarning(
                "[FaultInjection] ⏱ Latence de {DelayMs}ms injectée sur {Method} {Path}",
                delayMs, context.Request.Method, requestPath);

            await Task.Delay(delayMs, context.RequestAborted);
        }

        // ── 3. Transmission normale au contrôleur ─────────────────────────────
        await _next(context);
    }
}

/// <summary>Extension method pour enregistrer le middleware avec la syntaxe <c>UseXxx()</c>.</summary>
public static class FaultInjectionMiddlewareExtensions
{
    /// <summary>
    /// Ajoute le middleware de simulation de pannes au pipeline HTTP.
    /// Doit être appelé <b>avant</b> <c>UseRouting()</c> pour intercepter toutes les requêtes.
    /// </summary>
    public static IApplicationBuilder UseFaultInjection(this IApplicationBuilder app)
    {
        return app.UseMiddleware<FaultInjectionMiddleware>();
    }
}
