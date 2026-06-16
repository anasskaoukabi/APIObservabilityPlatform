using Dashboard.Web.Components;
using Dashboard.Web.Models;
using Dashboard.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Blazor ────────────────────────────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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

// ── Compatibilité (supprimé — remplacé par les vrais services) ─────────────────
// builder.Services.AddSingleton<MetricsSimulatorService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
