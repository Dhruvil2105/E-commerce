using ECommerce.Notification.Consumers;
using ECommerce.Notification.Services;
using MassTransit;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ──────────────────────────────────────────────
    builder.Host.UseSerilog((context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ServiceName", "notification-service")
            .WriteTo.Console()
            .WriteTo.Seq(context.Configuration["Seq:Url"]
                ?? "http://localhost:5341"));

    // ── MassTransit + RabbitMQ ───────────────────────────────
    // Notification Service ONLY consumes events.
    // No publishing — it is a pure event consumer.
    builder.Services.AddMassTransit(x =>
    {
        x.AddConsumer<OrderConfirmedConsumer>();
        x.AddConsumer<OrderCancelledConsumer>();
        x.AddConsumer<OrderShippedConsumer>();

        x.UsingRabbitMq((ctx, cfg) =>
        {
            cfg.Host(
                builder.Configuration["RabbitMQ:Host"] ?? "localhost",
                builder.Configuration["RabbitMQ:VHost"] ?? "/",
                h =>
                {
                    h.Username(builder.Configuration["RabbitMQ:Username"]
                        ?? "guest");
                    h.Password(builder.Configuration["RabbitMQ:Password"]
                        ?? "guest");
                });

            cfg.UseMessageRetry(r => r.Intervals(
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(30)));

            cfg.ConfigureEndpoints(ctx);
        });
    });

    // ── Email Service ─────────────────────────────────────────
    // Registered as singleton — stateless, safe to share
    builder.Services.AddSingleton<IEmailService, EmailService>();

    // ── Health checks ─────────────────────────────────────────
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    // ── Middleware pipeline ───────────────────────────────────
    app.MapHealthChecks("/health");

    Log.Information("Notification service started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Notification service failed to start");
}
finally
{
    Log.CloseAndFlush();
}