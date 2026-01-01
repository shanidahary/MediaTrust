using MassTransit;
using MediaTrust.Contracts.Events;
using MediaTrust.Ingest.Data;
using MediaTrust.Ingest.Storage;
using MediaTrust.Ingest.Managers;
using MediaTrust.Ingest.Accessors;
using Microsoft.EntityFrameworkCore;
using Minio;
using Polly;
using Npgsql;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// -------------------- Controllers --------------------
builder.Services.AddControllers();

// -------------------- Postgres --------------------
builder.Services.AddDbContext<IngestDbContext>(opt =>
{
    var cs = builder.Configuration.GetConnectionString("Postgres")
             ?? throw new InvalidOperationException("Postgres connection string is missing");

    opt.UseNpgsql(cs);
});

// MinIO options
builder.Services.Configure<MinioOptions>(
    builder.Configuration.GetSection("Minio"));

// MinIO client
builder.Services.AddSingleton<IMinioClient>(sp =>
{
    var opt = sp.GetRequiredService<IOptions<MinioOptions>>().Value;

    var client = new MinioClient()
        .WithEndpoint(opt.Endpoint)
        .WithCredentials(opt.AccessKey, opt.SecretKey);

    if (opt.UseSsl)
        client = client.WithSSL();

    return client.Build();
});

// Storage
builder.Services.AddSingleton<MinioStorage>();

// -------------------- DI --------------------
builder.Services.AddScoped<IMediaRepository, MediaRepository>();
builder.Services.AddScoped<IStorageAccessor, StorageAccessor>();
builder.Services.AddScoped<MediaIngestManager>();

// -------------------- MassTransit / RabbitMQ --------------------
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

// -------------------- DB migrate on startup (Polly) --------------------
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<IngestDbContext>();

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