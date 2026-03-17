
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ECommerce.Shared.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((hostingContext, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(hostingContext.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ServiceName", "api-gateway")
            .WriteTo.Console()
            .WriteTo.Seq(hostingContext.Configuration["Seq:Url"] ?? "http://localhost:5341");
    });

    // ── YARP Reverse Proxy ───────────────────────────────────
    // Reads routing config from appsettings.json (ReverseProxy section)
    // Routes every /api/identity/* → Identity Service
    //              /api/products/* → Product Service  etc.
    builder.Services.AddReverseProxy()
        .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

    // ── JWT Authentication ───────────────────────────────────
    // Gateway validates the JWT token on every request.
    // If invalid → 401 immediately, request never reaches services.
    // If valid   → strip JWT, inject trusted headers, forward request.
    var jwtSecret = builder.Configuration["Jwt:Secret"]
        ?? throw new InvalidOperationException("Jwt:Secret not configured");

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "ecommerce-identity",
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ClockSkew = TimeSpan.Zero
            };
        });

    builder.Services.AddAuthorization();
    builder.Services.AddCurrentUser();
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseAuthentication();
    app.UseAuthorization();



    // ── JWT header injection middleware ──────────────────────
    // Runs AFTER authentication so we know the token is valid.
    // Reads claims from the validated JWT and injects them
    // as trusted headers for downstream services.

    app.Use(async (context, next) =>
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? string.Empty;

            var email = context.User.FindFirst(ClaimTypes.Email)?.Value
                ?? context.User.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                ?? string.Empty;

            var role = context.User.FindFirst(ClaimTypes.Role)?.Value
                ?? string.Empty;

            var tenantId = context.User.FindFirst("tenantId")?.Value
                ?? "default";

            var traceId = System.Diagnostics.Activity.Current?.TraceId.ToString()
                ?? Guid.NewGuid().ToString("N");

            // Inject trusted headers — downstream services read these
            // They never validate JWT themselves
            context.Request.Headers["X-User-Id"] = userId;
            context.Request.Headers["X-User-Email"] = email;
            context.Request.Headers["X-Roles"] = role;
            context.Request.Headers["X-Tenant-Id"] = tenantId;
            context.Request.Headers["X-Trace-Id"] = traceId;

            // Strip the JWT — downstream services must NOT receive it
            // They should only trust the injected headers
            context.Request.Headers.Remove("Authorization");

            Log.Information(
                "Authenticated request: {UserId} {Email} {Role} → {Path}",
                userId, email, role,
                context.Request.Path);

            

        }

        await next();
    });
    app.MapHealthChecks("/health");

    app.MapReverseProxy();
   
    Log.Information("API Gateway started successfully");
    app.Run();

}
catch (Exception ex)
{

    Log.Fatal(ex, "API Gateway failed to start");
}
finally
{
    Log.CloseAndFlush();
}

