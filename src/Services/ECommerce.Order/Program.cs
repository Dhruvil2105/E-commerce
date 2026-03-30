using System.Text;
using ECommerce.Order.Consumers;
using ECommerce.Order.Data;
using ECommerce.Order.Interface;
using ECommerce.Order.Services;
using ECommerce.Shared.Middleware;
using MassTransit;
using MassTransit.Logging;
using Microsoft.AspNetCore.Authentication;
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
            .Enrich.WithProperty("ServiceName", "order - service")
            .WriteTo.Console()
            .WriteTo.Seq(context.Configuration["Seq:Url"] ?? "http://localhost:5341"));

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
            Description = "Enter your JWT tocken"
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
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    builder.Services.AddDbContext<OrderDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

    builder.Services.AddMassTransit(x =>
    {
        // Register consumers — MassTransit creates queues automatically
        x.AddConsumer<PaymentResultConsumer>();
        x.AddConsumer<StockResultConsumer>();

        x.UsingRabbitMq((ctx, cfg) =>
        {
            // Connect to RabbitMQ
            cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost",
                     builder.Configuration["RabbitMQ:VHost"] ?? "/", h =>
                     {
                         h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
                         h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
                     });

            // Configure retry — retry 3 times with 5 second intervals
            // before sending to dead letter queue
            cfg.UseMessageRetry(r => r.Intervals(
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(30)));

            // Auto-configure endpoints for all registered consumers
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
                   ValidIssuer = "ecommerce - identity",
                   ValidateAudience = false,
                   ValidateLifetime = true,
                   ClockSkew = TimeSpan.Zero,
               };
           });

    builder.Services.AddAuthorization();

    builder.Services.AddScoped<IOrderService, OrderService>();
    builder.Services.AddCurrentUser();
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    using(var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        db.Database.Migrate();

        Log.Information("Order database migrations applied");
    }

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

    Log.Information("Order service started successfully");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Order service failed to start");
}
finally
{
    Log.CloseAndFlush();
}