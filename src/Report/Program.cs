using MediaTrust.Report.Accessors;
using MediaTrust.Report.Data;
using MediaTrust.Report.Managers;
using Microsoft.EntityFrameworkCore;
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
// Postgres (READ-ONLY)
// --------------------------------------------------
builder.Services.AddDbContext<ReportDbContext>(opt =>
{
    opt.UseNpgsql(
        builder.Configuration.GetConnectionString("Postgres"));
});

// --------------------------------------------------
// Dependency Injection
// --------------------------------------------------
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<ReportManager>();

// --------------------------------------------------
// Detectors HTTP Client
// --------------------------------------------------
builder.Services.AddHttpClient("detectors", client =>
{
    client.BaseAddress = new Uri("http://detectors:8080/");
});

// --------------------------------------------------
// Accessors
// --------------------------------------------------
builder.Services.AddScoped<IDetectorsAccessor, DetectorsAccessor>();

// --------------------------------------------------
// AI Accessor
// --------------------------------------------------
builder.Services.AddScoped<IAiAccessor, GeminiAiAccessor>();

// --------------------------------------------------
// Gemini HTTP Client
// --------------------------------------------------
builder.Services.AddHttpClient("gemini", client =>
{
    client.BaseAddress =
        new Uri("https://generativelanguage.googleapis.com/");
});

// --------------------------------------------------
// CORS
// --------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// --------------------------------------------------
// Build app
// --------------------------------------------------
var app = builder.Build();

app.UseCors("ReactFrontend");
// --------------------------------------------------
// Map endpoints
// --------------------------------------------------
app.MapControllers();

app.Run();