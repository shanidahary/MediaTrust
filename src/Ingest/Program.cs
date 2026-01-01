using MassTransit;
using MediaTrust.Contracts.Events;
using MediaTrust.Ingest.Data;
using MediaTrust.Ingest.Storage;
using Microsoft.EntityFrameworkCore;
using Minio;
using Polly;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// ---------- Options ----------
var minioOpt = builder.Configuration
    .GetSection("Minio")
    .Get<MinioOptions>() ?? new MinioOptions();

builder.Services.AddSingleton(minioOpt);

// ---------- Postgres ----------
builder.Services.AddDbContext<IngestDbContext>(opt =>
{
    var cs = builder.Configuration.GetConnectionString("Postgres");
    opt.UseNpgsql(cs);
});

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

// ---------- MassTransit / RabbitMQ ----------
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

// ---------- Endpoints ----------
app.MapGet("/", () => Results.Ok(new
{
    service = "ingest",
    status = "ok"
}));

app.MapPost("/media", async (
    HttpRequest request,
    IngestDbContext db,
    MinioStorage storage,
    IPublishEndpoint bus,
    CancellationToken ct) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest("Expected multipart/form-data with a 'file' field.");

    var form = await request.ReadFormAsync(ct);
    var file = form.Files.GetFile("file");

    if (file is null || file.Length == 0)
        return Results.BadRequest("Missing file. Use form field name 'file'.");

    var mediaId = Guid.NewGuid();
    var safeName = Path.GetFileName(file.FileName);
    var objectKey = $"{DateTime.UtcNow:yyyy/MM/dd}/{mediaId}_{safeName}";

    // upload to MinIO
    await using var stream = file.OpenReadStream();
    await storage.PutObjectAsync(
        objectKey,
        stream,
        file.Length,
        file.ContentType ?? "application/octet-stream",
        ct);

    // save DB
    var item = new MediaItem
    {
        Id = mediaId,
        FileName = safeName,
        ContentType = file.ContentType ?? "application/octet-stream",
        SizeBytes = file.Length,
        ObjectKey = objectKey,
        UploadedAtUtc = DateTimeOffset.UtcNow
    };

    db.MediaItems.Add(item);
    await db.SaveChangesAsync(ct);

    // publish event
    await bus.Publish(new MediaUploaded(
        MediaId: mediaId,
        ObjectKey: objectKey,
        FileName: safeName,
        ContentType: item.ContentType,
        SizeBytes: item.SizeBytes,
        UploadedAtUtc: item.UploadedAtUtc
    ), ct);

    return Results.Ok(new
    {
        mediaId,
        objectKey,
        item.FileName,
        item.ContentType,
        item.SizeBytes,
        item.UploadedAtUtc
    });
});

app.Run();
