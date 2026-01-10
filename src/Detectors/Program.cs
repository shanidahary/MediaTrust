using MediaTrust.Detectors.Accessors;
using MediaTrust.Detectors.Data;
using MediaTrust.Detectors.Managers;
using MediaTrust.Detectors.Messaging;
using Microsoft.EntityFrameworkCore;
using Polly;
using Npgsql;
using Minio;
using Microsoft.Extensions.Options;
using MediaTrust.Detectors.Storage;
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

builder.Services.AddHttpClient("gateway", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Gateway:BaseUrl"]
        ?? "http://gateway:8080/");
});

// --------------------------------------------------
// Postgres
// --------------------------------------------------
builder.Services.AddDbContext<DetectorsDbContext>(opt =>
{
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"));
});

//Minio
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

// --------------------------------------------------
// Dependency Injection
// --------------------------------------------------
builder.Services.AddScoped<IDetectorResultRepository, DetectorResultRepository>();
builder.Services.AddScoped<DetectorManager>();
builder.Services.AddSingleton<MinioStorage>();

// RabbitMQ (manual client, no consumers)
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
    var db = scope.ServiceProvider.GetRequiredService<DetectorsDbContext>();

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
        logger.LogInformation("Detectors database migrated successfully");
    });
}

// --------------------------------------------------
// Map endpoints
// --------------------------------------------------
app.MapControllers();

app.Run();