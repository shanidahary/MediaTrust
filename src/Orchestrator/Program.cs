using MassTransit;
using MediaTrust.Orchestrator.Accessors;
using MediaTrust.Orchestrator.Consumers;
using MediaTrust.Orchestrator.Data;
using MediaTrust.Orchestrator.Managers;
using Microsoft.EntityFrameworkCore;
using Polly;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Postgres
builder.Services.AddDbContext<OrchestratorDbContext>(opt =>
{
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"));
});

// DI
builder.Services.AddScoped<IAnalysisJobRepository, AnalysisJobRepository>();
builder.Services.AddScoped<AnalysisJobManager>();

// MassTransit
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<MediaUploadedConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host("rabbitmq", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ConfigureEndpoints(ctx);
    });
});

var app = builder.Build();

// -------------------- DB migrate on startup (Polly) --------------------
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();

    var retryPolicy = Policy
        .Handle<NpgsqlException>()
        .Or<TimeoutException>()
        .WaitAndRetryAsync(
            retryCount: 5,
            sleepDurationProvider: _ => TimeSpan.FromSeconds(5),
            onRetry: (ex, delay, retry, _) =>
            {
                logger.LogWarning(
                    "Postgres not ready. Retry {Retry}/5 in {Delay}s",
                    retry,
                    delay.TotalSeconds);
            });

    await retryPolicy.ExecuteAsync(() => db.Database.MigrateAsync());
}

app.MapControllers();
app.Run();
