using ECommerce.Product.Data;
using ECommerce.Product.Interface;
using ECommerce.Product.Services;
using ECommerce.Shared.Middleware;
using Microsoft.EntityFrameworkCore;
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
           .Enrich.WithProperty("ServiceName", "product-service")
           .WriteTo.Console()
           .WriteTo.Seq(context.Configuration["Seq:Url"]
               ?? "http://localhost:5341"));

    // ── Controllers + Swagger ────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        // Adds the Authorize button to Swagger UI
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT token from /api/auth/login"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id   = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // ── Multipart form data limit ─────────────────────────────
    // Allow up to 10MB for image uploads
    // Default limit is 30MB but we set it explicitly
    builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB
    });

    // ── Database ─────────────────────────────────────────────
    builder.Services.AddDbContext<ProductDbContext>(options =>
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("Default")));

    // ── Services ─────────────────────────────────────────────
    builder.Services.AddScoped<IProductService, ProductService>();

    // ── CurrentUser middleware ────────────────────────────────
    builder.Services.AddCurrentUser();

    // ── Health checks ─────────────────────────────────────────
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    // ── Auto-apply migrations ─────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider
            .GetRequiredService<ProductDbContext>();
        db.Database.Migrate();

        Log.Information("Product database migrations applied");
    }

    // ── Middleware pipeline ───────────────────────────────────
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseSerilogRequestLogging();
    app.UseCurrentUser();
    app.MapControllers();
    app.MapHealthChecks("/health");

    Log.Information("Product service started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Product service failed to start");
}
finally
{
    Log.CloseAndFlush();
}




