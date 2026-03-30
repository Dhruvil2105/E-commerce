using System.Text;
using ECommerce.Payment.Consumers;
using ECommerce.Payment.Data;
using ECommerce.Payment.Services;
using ECommerce.Shared.Middleware;
using MassTransit;
using MassTransit.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ServiceName", "payment-service")
            .WriteTo.Console()
            .WriteTo.Seq(context.Configuration["Seq:Url"]
                ?? "http://localhost:5341"));

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT token"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    },

                },
                Array.Empty<string>()
            }
        });

        builder.Services.AddDbContext<PaymentDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

        builder.Services.AddMassTransit(x =>
        {
            x.AddConsumer<OrderPlacedConsumer>();
            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", builder.Configuration["RabbitMQ:VHost"] ?? "/",
                    h =>
                    {
                        h.Username(builder.Configuration["RabbitMQ:UserName"] ?? "guest");
                        h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
                    });
                cfg.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(30)));

                cfg.ConfigureEndpoints(ctx);
            });
        });
        var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "super-secret-key-change-in-production-minimum-32-chars!!";

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ValidateIssuer = true,
                    ValidIssuer = "ecommerce-identity",
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        builder.Services.AddAuthorization();
        builder.Services.AddScoped<IPaymentService, PaymentService>();
        builder.Services.AddCurrentUser();
        builder.Services.AddHealthChecks();

        var app = builder.Build();

        // ── Auto-apply migrations ─────────────────────────────────
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<PaymentDbContext>();
            db.Database.Migrate();
            Log.Information("Payment database migrations applied");
        }
        // ── Middleware pipeline ───────────────────────────────────
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseSerilogRequestLogging();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseCurrentUser();
        app.MapControllers();
        app.MapHealthChecks("/health");

        Log.Information("Payment service started successfully");
        app.Run();
    });
}
catch (Exception ex)
{

    Log.Fatal(ex, "Payment service failed to start");
}
finally
{
    Log.CloseAndFlush();
}