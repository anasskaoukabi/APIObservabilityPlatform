namespace TargetAPI.Settings;

/// <summary>
/// Paramètres de configuration du middleware de simulation de pannes.
/// Injectés via <c>IOptions&lt;FaultInjectionSettings&gt;</c> — jamais codés en dur.
/// </summary>
/// <remarks>
/// Ces valeurs sont lues depuis <c>appsettings.json</c> (section "FaultInjection").
/// Elles peuvent être modifiées à chaud via <c>IOptionsSnapshot</c> si besoin.
/// </remarks>
public sealed class FaultInjectionSettings
{
    /// <summary>Nom de la section dans appsettings.json.</summary>
    public const string SectionName = "FaultInjection";

    /// <summary>
    /// Active ou désactive globalement le middleware.
    /// Mettre à <c>false</c> en production pour court-circuiter sans le retirer du pipeline.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Probabilité (entre 0.0 et 1.0) d'injecter une erreur 500 sur une requête.
    /// Exemple : 0.2 = 20% des requêtes retourneront HTTP 500.
    /// </summary>
    public double ErrorRate { get; set; } = 0.0;

    /// <summary>
    /// Probabilité (entre 0.0 et 1.0) d'injecter un délai artificiel.
    /// Exemple : 0.3 = 30% des requêtes seront ralenties.
    /// </summary>
    public double LatencyRate { get; set; } = 0.0;

    /// <summary>
    /// Délai minimal (en millisecondes) injecté lors d'une simulation de latence.
    /// </summary>
    public int MinLatencyMs { get; set; } = 300;

    /// <summary>
    /// Délai maximal (en millisecondes) injecté lors d'une simulation de latence.
    /// Le délai réel est tiré aléatoirement entre MinLatencyMs et MaxLatencyMs.
    /// </summary>
    public int MaxLatencyMs { get; set; } = 2000;

    /// <summary>
    /// Préfixe de chemin sur lequel les pannes sont injectées.
    /// Par défaut "/api" — Swagger UI, static files et autres routes sont exclues.
    /// </summary>
    public string ApiPathPrefix { get; set; } = "/api";
}
