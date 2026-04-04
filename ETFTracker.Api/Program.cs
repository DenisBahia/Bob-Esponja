using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ETFTracker.Api.Data;
using ETFTracker.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, b => b.MigrationsAssembly("ETFTracker.Api")));

// Register services
builder.Services.AddScoped<IPriceService, PriceService>();
builder.Services.AddScoped<IHoldingsService, HoldingsService>();
builder.Services.AddScoped<IProjectionService, ProjectionService>();
builder.Services.AddScoped<JwtService>();
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
        // For production, set OAuth:GitHub:RedirectUri in configuration
        if (builder.Configuration["OAuth:GitHub:RedirectUri"] is string githubRedirectUri)
            o.AuthorizationEndpoint = o.AuthorizationEndpoint.Replace("http", githubRedirectUri.StartsWith("https") ? "https" : "http");
        o.Scope.Add("user:email");
        o.CorrelationCookie.SameSite     = SameSiteMode.Lax;
        o.CorrelationCookie.SecurePolicy = isProduction ? CookieSecurePolicy.Always : CookieSecurePolicy.None;
        o.CorrelationCookie.Path         = "/";   // cookie available to all paths
    })
    .AddGoogle("Google", o =>
    {
        o.ClientId     = builder.Configuration["OAuth:Google:ClientId"]     ?? "";
        o.ClientSecret = builder.Configuration["OAuth:Google:ClientSecret"] ?? "";
        o.CallbackPath = "/signin-google";
        // For production, the absolute redirect URI should be: https://etf-tracker-app.onrender.com/signin-google
        // Configure via OAuth:Google:RedirectUri environment variable or appsettings
        if (builder.Configuration["OAuth:Google:RedirectUri"] is string googleRedirectUri)
            o.Events.OnRedirectToAuthorizationEndpoint = ctx =>
            {
                ctx.RedirectUri = googleRedirectUri;
                return System.Threading.Tasks.Task.CompletedTask;
            };
        o.Scope.Add("email");
        o.Scope.Add("profile");
        o.CorrelationCookie.SameSite     = SameSiteMode.Lax;
        o.CorrelationCookie.SecurePolicy = isProduction ? CookieSecurePolicy.Always : CookieSecurePolicy.None;
        o.CorrelationCookie.Path         = "/";   // cookie available to all paths
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
// This ensures OAuth redirect URIs use https:// in production
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAngular");

app.UseAuthentication();
app.UseAuthorization();

// Serve static files (Angular app)
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

// Fallback to index.html for SPA routing
app.MapFallbackToFile("index.html");

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Use Migrate() instead of EnsureCreated() to apply EF Core migrations
    db.Database.Migrate();

    // Seed default user if it doesn't exist
    if (!db.Users.Any())
    {
        var utcNow = new DateTime(DateTime.UtcNow.Ticks, DateTimeKind.Utc);
        var defaultUser = new ETFTracker.Api.Models.User
        {
            Email = "demo@example.com",
            FirstName = "Demo",
            LastName = "User",
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
        db.Users.Add(defaultUser);
        db.SaveChanges();
    }
}

app.Run();

