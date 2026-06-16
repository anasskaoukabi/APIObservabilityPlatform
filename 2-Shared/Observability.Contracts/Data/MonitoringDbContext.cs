using Microsoft.EntityFrameworkCore;
using Observability.Contracts.Entities;

namespace Observability.Contracts.Data;

/// <summary>
/// DbContext pour la persistance des logs de monitoring SQLite.
/// </summary>
public class MonitoringDbContext : DbContext
{
    /// <summary>Constructeur.</summary>
    public MonitoringDbContext(DbContextOptions<MonitoringDbContext> options)
        : base(options)
    {
    }

    /// <summary>Table des logs de health checks.</summary>
    public DbSet<HealthCheckLog> HealthCheckLogs => Set<HealthCheckLog>();
}
