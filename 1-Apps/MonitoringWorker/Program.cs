using MonitoringWorker;
using Microsoft.EntityFrameworkCore;
using Observability.Contracts.Data;

var builder = Host.CreateApplicationBuilder(args);

// ── Enregistrer le DbContext SQLite ──────────────────────────────────────────
builder.Services.AddDbContext<MonitoringDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=../../monitoring.db"));

// ── Enregistrer le client HTTP avec un timeout de 4 secondes (Axe 2) ────────
// Le timeout déclenche une TaskCanceledException si l'API ne répond pas à temps.
builder.Services.AddHttpClient("HealthCheckClient", client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "HealthCheckWorker/1.0");
    client.Timeout = TimeSpan.FromSeconds(4); // Seuil TIMEOUT / CRITICAL
});

// ── Enregistrer le HealthCheckWorker en tant que Hosted Service (Axe 2) ──────
builder.Services.AddHostedService<HealthCheckWorker>();

var host = builder.Build();

// ── Assurer la création de la base de données SQLite au démarrage ───────────
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();
    db.Database.EnsureCreated();
}

host.Run();
