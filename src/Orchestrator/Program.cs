using MediaTrust.Orchestrator.Accessors;
using MediaTrust.Orchestrator.Data;
using MediaTrust.Orchestrator.Managers;
using MediaTrust.Orchestrator.Messaging;
using Microsoft.EntityFrameworkCore;
using Polly;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// --------------------------------------------------
// Controllers
// --------------------------------------------------
builder.Services.AddControllers();

// --------------------------------------------------
// Postgres
// --------------------------------------------------
builder.Services.AddDbContext<OrchestratorDbContext>(opt =>
{
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"));
});

// --------------------------------------------------
// Dependency Injection
// --------------------------------------------------
builder.Services.AddScoped<IAnalysisJobRepository, AnalysisJobRepository>();
builder.Services.AddScoped<AnalysisJobManager>();

// RabbitMQ (publisher only, exchange-based)
builder.Services.AddSingleton<RabbitMqClient>();

// --------------------------------------------------
// Build app
// --------------------------------------------------
var app = builder.Build();

// --------------------------------------------------
// DB migrate on startup (Polly retry)
// --------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

    var retryPolicy = Policy
        .Handle<NpgsqlException>()
        .Or<TimeoutException>()
        .WaitAndRetryAsync(
            retryCount: 5,
            sleepDurationProvider: retry => TimeSpan.FromSeconds(5),
            onRetry: (ex, delay, retry, _) =>
            {
                logger.LogWarning(
                    "Postgres not ready. Retry {Retry}/5 in {Delay}s",
                    retry,
                    delay.TotalSeconds);
            });

    await retryPolicy.ExecuteAsync(async () =>
    {
        await db.Database.MigrateAsync();
        logger.LogInformation("Orchestrator database migrated successfully");
    });
}

// --------------------------------------------------
// Map endpoints
// --------------------------------------------------
app.MapControllers();

app.Run();