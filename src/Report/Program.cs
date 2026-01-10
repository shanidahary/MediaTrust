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
// Build app
// --------------------------------------------------
var app = builder.Build();

// --------------------------------------------------
// Map endpoints
// --------------------------------------------------
app.MapControllers();

app.Run();