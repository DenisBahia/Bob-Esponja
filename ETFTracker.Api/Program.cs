using Scalar.AspNetCore;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ETFTracker.Api.Data;
using ETFTracker.Api.Services;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();


// Add DbContext
var connectionString = ResolveConnectionString(builder.Configuration)
    ?? throw new InvalidOperationException("Database connection is not configured. Set ConnectionStrings__DefaultConnection or DATABASE_URL.");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, b => b.MigrationsAssembly("ETFTracker.Api")));

// Register services
builder.Services.AddScoped<IPriceService, PriceService>();
builder.Services.AddScoped<IHoldingsService, HoldingsService>();
builder.Services.AddScoped<IProjectionService, ProjectionService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<ISharingContextService, SharingContextService>();
builder.Services.AddScoped<IGoalService, GoalService>();
builder.Services.AddScoped<ISellService, SellService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<PriceService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ── Authentication ────────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");

var isProduction = builder.Environment.IsProduction();

builder.Services
    .AddAuthentication(options =>
    {
        // API endpoints use JWT by default
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
        // OAuth callbacks sign-in through the cookie scheme
        options.DefaultSignInScheme       = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, o =>
    {
        o.Cookie.SameSite    = SameSiteMode.Lax;
        o.Cookie.HttpOnly    = true;
        o.Cookie.SecurePolicy = isProduction ? CookieSecurePolicy.Always : CookieSecurePolicy.None;
        o.ExpireTimeSpan     = TimeSpan.FromMinutes(15);
    })
    .AddGitHub("GitHub", o =>
    {
        o.ClientId     = builder.Configuration["OAuth:GitHub:ClientId"]     ?? "";
        o.ClientSecret = builder.Configuration["OAuth:GitHub:ClientSecret"] ?? "";
        o.CallbackPath = "/signin-github";
        o.Scope.Add("user:email");
        o.CorrelationCookie.SameSite     = SameSiteMode.Lax;
        o.CorrelationCookie.SecurePolicy = isProduction ? CookieSecurePolicy.Always : CookieSecurePolicy.None;
        o.CorrelationCookie.Path         = "/";
    })
    .AddGoogle("Google", o =>
    {
        o.ClientId     = builder.Configuration["OAuth:Google:ClientId"]     ?? "";
        o.ClientSecret = builder.Configuration["OAuth:Google:ClientSecret"] ?? "";
        o.CallbackPath = "/signin-google";
        o.Scope.Add("email");
        o.Scope.Add("profile");
        o.CorrelationCookie.SameSite     = SameSiteMode.Lax;
        o.CorrelationCookie.SecurePolicy = isProduction ? CookieSecurePolicy.Always : CookieSecurePolicy.None;
        o.CorrelationCookie.Path         = "/";
        // In production, force the redirect_uri sent to Google to use https://
        // This is a safety net in case forwarded-headers middleware hasn't run yet.
        if (isProduction)
        {
            o.Events.OnRedirectToAuthorizationEndpoint = ctx =>
            {
                var uri = new UriBuilder(ctx.RedirectUri) { Scheme = "https", Port = -1 };
                ctx.Response.Redirect(uri.ToString());
                return System.Threading.Tasks.Task.CompletedTask;
            };
        }
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"]   ?? "ETFTracker",
            ValidAudience            = builder.Configuration["Jwt:Audience"] ?? "ETFTracker",
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Trust the reverse-proxy headers from Render (X-Forwarded-For, X-Forwarded-Proto)
// This ensures OAuth redirect URIs use https:// in production.
// KnownNetworks/KnownProxies are cleared so Render's non-loopback proxy is trusted.
var forwardedOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedOptions.KnownIPNetworks.Clear();
forwardedOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedOptions);

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "ETF Tracker API v1";
        options.Theme = ScalarTheme.Mars;
    });
}

app.UseCors("AllowAngular");

app.UseAuthentication();
app.UseAuthorization();

// Serve static files (Angular app)
app.UseDefaultFiles();
app.UseStaticFiles();

// Public liveness endpoint for container health checks.
app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

app.MapControllers();

// Fallback to index.html for SPA routing
app.MapFallbackToFile("index.html");

await ApplyDatabaseMigrationsAndSeedAsync(app);

app.Run();

static string? ResolveConnectionString(IConfiguration configuration)
{
    var configuredConnection = configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrWhiteSpace(configuredConnection))
    {
        return configuredConnection;
    }

    var databaseUrl = configuration["DATABASE_URL"];
    if (string.IsNullOrWhiteSpace(databaseUrl))
    {
        return null;
    }

    if (databaseUrl.Contains("Host=", StringComparison.OrdinalIgnoreCase))
    {
        return databaseUrl;
    }

    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':', 2);

    var builder = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Database = uri.AbsolutePath.Trim('/'),
        Username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : string.Empty,
        Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
        SslMode = SslMode.Require
    };

    return builder.ConnectionString;
}

static async Task ApplyDatabaseMigrationsAndSeedAsync(WebApplication app)
{
    const int maxAttempts = 5;
    var delay = TimeSpan.FromSeconds(2);

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

            await db.Database.MigrateAsync();

            if (!await db.Users.AnyAsync())
            {
                var utcNow = new DateTime(DateTime.UtcNow.Ticks, DateTimeKind.Utc);
                db.Users.Add(new ETFTracker.Api.Models.User
                {
                    Email = "demo@example.com",
                    FirstName = "Demo",
                    LastName = "User",
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow
                });

                await db.SaveChangesAsync();
            }

            logger.LogInformation("Database migration check completed successfully.");
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            Console.WriteLine($"Database migration attempt {attempt} failed. Retrying in {delay.TotalSeconds}s. Error: {ex.Message}");
            await Task.Delay(delay);
            delay += delay;
        }
    }

    throw new InvalidOperationException("Database migrations failed after multiple attempts. Check database configuration and migration files.");
}

