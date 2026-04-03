using Microsoft.EntityFrameworkCore;
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

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAngular");

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

