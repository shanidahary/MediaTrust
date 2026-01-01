using MediaTrust.Report.Accessors;
using MediaTrust.Report.Data;
using MediaTrust.Report.Managers;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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