using MassTransit;
using MediaTrust.Contracts.Events;
using MediaTrust.Ingest.Data;
using MediaTrust.Ingest.Storage;
using Microsoft.EntityFrameworkCore;
using MediaTrust.Ingest.Managers;
using MediaTrust.Ingest.Accessors;
using Minio;
using Polly;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Postgres
builder.Services.AddDbContext<IngestDbContext>(opt =>
{
    var cs = builder.Configuration.GetConnectionString("Postgres");
    opt.UseNpgsql(cs);
});

// MinIO options
var minioOpt = builder.Configuration.GetSection("Minio").Get<MinioOptions>()!;
builder.Services.AddSingleton(minioOpt);

// ---------- MinIO ----------
builder.Services.AddSingleton<IMinioClient>(_ =>
{
    var client = new MinioClient()
        .WithEndpoint(minioOpt.Endpoint)
        .WithCredentials(minioOpt.AccessKey, minioOpt.SecretKey);

    if (minioOpt.UseSsl)
        client = client.WithSSL();

    return client.Build();
});

builder.Services.AddSingleton<MinioStorage>();

// DI
builder.Services.AddScoped<IMediaRepository, MediaRepository>();
builder.Services.AddScoped<IStorageAccessor, StorageAccessor>();
builder.Services.AddScoped<MediaIngestManager>();

// MassTransit / RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var host = builder.Configuration["RabbitMq:Host"] ?? "rabbitmq";
        var user = builder.Configuration["RabbitMq:Username"] ?? "guest";
        var pass = builder.Configuration["RabbitMq:Password"] ?? "guest";

        cfg.Host(host, "/", h =>
        {
            h.Username(user);
            h.Password(pass);
        });
    });
});

var app = builder.Build();

// ---------- DB migrate on startup (Polly retry) ----------
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<IngestDbContext>();

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
                    delay.TotalSeconds
                );
            });

    await retryPolicy.ExecuteAsync(async () =>
    {
        await db.Database.MigrateAsync();
    });
}

app.MapControllers();
app.Run();

