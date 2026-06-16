using System;

namespace Observability.Contracts.Entities;

/// <summary>
/// Log d'exécution de health check persistant en base SQLite.
/// </summary>
public class HealthCheckLog
{
    /// <summary>Identifiant unique.</summary>
    public int Id { get; set; }

    /// <summary>Timestamp UTC ou local de l'opération.</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>Code statut HTTP renvoyé (200, 500, etc.) ou null en cas d'erreur de connexion.</summary>
    public int? StatusCode { get; set; }

    /// <summary>Temps de réponse en millisecondes.</summary>
    public double ResponseTimeMs { get; set; }

    /// <summary>Indique si la vérification a réussi.</summary>
    public bool IsSuccess { get; set; }

    /// <summary>Message d'erreur associé en cas d'échec.</summary>
    public string? ErrorMessage { get; set; }
}
