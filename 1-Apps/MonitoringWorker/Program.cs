using MonitoringWorker;

var builder = Host.CreateApplicationBuilder(args);

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
host.Run();
