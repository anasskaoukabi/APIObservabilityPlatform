using Dashboard.Web.Components;
using Dashboard.Web.Models;
using Dashboard.Web.Services;
using Microsoft.EntityFrameworkCore;
using Observability.Contracts.Data;

var builder = WebApplication.CreateBuilder(args);

// ── Blazor ────────────────────────────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ── Enregistrer le DbContext SQLite (via DbContextFactory pour Blazor Server) ─
builder.Services.AddDbContextFactory<MonitoringDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=../../monitoring.db"));

// ── HttpClient pour le monitoring (avec timeout configurable) ─────────────────
builder.Services.AddHttpClient("MonitoringClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Add("User-Agent", "APIObservabilityPlatform/1.0");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    // Accepte les certificats self-signed en dev — à durcir en production
    ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
});

// ── Singletons de domaine ─────────────────────────────────────────────────────
builder.Services.AddSingleton<GlobalSettings>();
builder.Services.AddSingleton<EndpointConfigStore>();
builder.Services.AddSingleton<MetricsStore>();
builder.Services.AddSingleton<AlertsStore>();
builder.Services.AddScoped<AuthService>();

// ── Service de fond (vrai monitoring HTTP) ────────────────────────────────────
builder.Services.AddHostedService<MonitoringBackgroundService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

// ── Endpoint API d'export CSV (Axe 4) ─────────────────────────────────────────
app.MapGet("/api/export-logs", async (MonitoringDbContext db) =>
{
    var logs = await db.HealthCheckLogs.OrderByDescending(l => l.Timestamp).ToListAsync();
    var csv = new System.Text.StringBuilder();
    csv.AppendLine("Id,Timestamp,StatusCode,ResponseTimeMs,IsSuccess,ErrorMessage");
    foreach (var log in logs)
    {
        string safeError = log.ErrorMessage?.Replace("\"", "\"\"") ?? "";
        csv.AppendLine($"{log.Id},{log.Timestamp:yyyy-MM-dd HH:mm:ss},{log.StatusCode},{log.ResponseTimeMs},{log.IsSuccess},\"{safeError}\"");
    }
    var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
    return Results.File(bytes, "text/csv", $"health-check-logs-{DateTime.Now:yyyyMMddHHmmss}.csv");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
