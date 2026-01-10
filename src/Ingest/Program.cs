using MediaTrust.Ingest.Accessors;
using MediaTrust.Ingest.Data;
using MediaTrust.Ingest.Managers;
using MediaTrust.Ingest.Storage;
using Microsoft.EntityFrameworkCore;
using Minio;
using Polly;
using Npgsql;
using Microsoft.Extensions.Options;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.Seq(
            context.Configuration["Seq:ServerUrl"] ?? "http://seq:5341");
});

// --------------------------------------------------
// Controllers
// --------------------------------------------------
builder.Services.AddControllers();

// --------------------------------------------------
// Postgres
// --------------------------------------------------
builder.Services.AddDbContext<IngestDbContext>(opt =>
{
    var cs = builder.Configuration.GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("Postgres connection string missing");

    opt.UseNpgsql(cs);
});

// --------------------------------------------------
// MinIO configuration
// --------------------------------------------------
builder.Services.Configure<MinioOptions>(
    builder.Configuration.GetSection("Minio"));

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

builder.Services.AddSingleton<MinioStorage>();

// --------------------------------------------------
// HTTP clients
// --------------------------------------------------
builder.Services.AddHttpClient("gateway", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Gateway:BaseUrl"]
        ?? "http://gateway:8080/");
});

// --------------------------------------------------
// Dependency Injection
// --------------------------------------------------
builder.Services.AddScoped<IMediaRepository, MediaRepository>();
builder.Services.AddScoped<IStorageAccessor, StorageAccessor>();
builder.Services.AddScoped<MediaIngestManager>();

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
                    delay.TotalSeconds);
            });

    await retryPolicy.ExecuteAsync(async () =>
    {
        await db.Database.MigrateAsync();
        logger.LogInformation("Ingest database migrated successfully");
    });
}

// --------------------------------------------------
// Map endpoints
// --------------------------------------------------
app.MapControllers();

app.Run();