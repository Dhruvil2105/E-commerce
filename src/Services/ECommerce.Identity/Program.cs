using ECommerce.Identity.Data;
using ECommerce.Identity.Interface;
using ECommerce.Identity.Services;
using ECommerce.Shared.Middleware;
using Microsoft.EntityFrameworkCore;
using Serilog;


// ── Bootstrap logger ─────────────────────────────────────────
// Created before the app builder so that any errors
// that happen DURING startup are also captured in logs.
// Without this, startup errors are lost silently.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog — structured logging ─────────────────────────
    // Replaces the default Microsoft logging with Serilog.
    // ReadFrom.Configuration reads settings from appsettings.json.
    // ReadFrom.Services allows Serilog to use DI-registered services.
    // Enrich.WithProperty adds "ServiceName" to EVERY log entry
    // so when all services log to Seq, you can filter by service.
    builder.Host.UseSerilog((hostingContext, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(hostingContext.Configuration)
            .ReadFrom.Services(services)
            .WriteTo.Console()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ServiceName", "identity-service")
            .WriteTo.Seq(hostingContext.Configuration["Seq:Url"] ?? "http://localhost:5341");
    });

    // ── Controllers + Swagger ────────────────────────────────
    // AddControllers registers all classes marked with [ApiController].
    // AddEndpointsApiExplorer + AddSwaggerGen enables the Swagger UI
    // at /swagger — lets you test endpoints in the browser.
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

    builder.Services.AddScoped<TokenService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddCurrentUser();
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    // ── Auto-apply EF Core migrations ────────────────────────
    // On startup, check if there are any pending migrations
    // and apply them automatically.
    //
    // WHY: Ensures the database schema is always in sync
    // with the code without manual steps.
    //
    // CreateScope() creates a temporary DI scope.
    // We need this because DbContext is scoped —
    // it cannot be resolved from the root container directly.
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        // Migrate() applies any pending migrations.
        // Creates the database if it does not exist yet.
        // Safe to call even if there are no pending migrations.
        dbContext.Database.Migrate();

        Log.Information("Database migrations applied successfully");
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // HTTP request logging — logs every incoming request
    // Serilog logs method, path, status code, and duration
    app.UseSerilogRequestLogging();

    // CurrentUser middleware — reads gateway headers
    // and populates the CurrentUser scoped service.
    // Must come before MapControllers so controllers
    // already have CurrentUser populated when they run.
    app.UseCurrentUser();

    // Map controller routes
    // Scans all classes with [ApiController] and registers their routes.
    app.MapControllers();

    // Map health check endpoint
    app.MapHealthChecks("/health");

    Log.Information(
            "Identity service started successfully on {Environment}",
            app.Environment.EnvironmentName);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Identity service failed to start");
   
}
finally
{
    // Always flush Serilog buffers before process exits.
    // Without this, the last few log entries may be lost.
    Log.CloseAndFlush();
}


