using Microsoft.EntityFrameworkCore;
using TargetAPI.Data;
using TargetAPI.Middlewares;
using TargetAPI.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHealthChecks();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure In-Memory Database for TargetAPI
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("TargetApiDb"));

// Register SimulationState as a Singleton so both Middleware and Controller share the same state
builder.Services.AddSingleton<SimulationState>();

var app = builder.Build();

// Seed the In-Memory Database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Inject the Custom Simulation Middleware BEFORE Authorization and Controllers
app.UseMiddleware<SimulationMiddleware>();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/api/products/health");
app.MapGet("/", (HttpContext context) => Results.Redirect("/swagger"));
app.MapGet("/index.html", (HttpContext context) => Results.Redirect("/swagger"));

app.Run();
