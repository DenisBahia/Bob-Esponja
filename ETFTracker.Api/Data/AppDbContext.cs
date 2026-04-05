using Microsoft.EntityFrameworkCore;
using ETFTracker.Api.Models;

namespace ETFTracker.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Holding> Holdings { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<PriceSnapshot> PriceSnapshots { get; set; }
    public DbSet<ProjectionSettings> ProjectionSettings { get; set; }
    public DbSet<ProjectionVersion> ProjectionVersions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure table names to lowercase for PostgreSQL
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<Holding>().ToTable("holdings");
        modelBuilder.Entity<Transaction>().ToTable("transactions");
        modelBuilder.Entity<PriceSnapshot>().ToTable("price_snapshots");

        // User
        modelBuilder.Entity<User>()
            .HasKey(u => u.Id);
        modelBuilder.Entity<User>()
            .Property(u => u.Id).HasColumnName("id");
        modelBuilder.Entity<User>()
            .Property(u => u.Email).HasColumnName("email");
        modelBuilder.Entity<User>()
            .Property(u => u.FirstName).HasColumnName("first_name");
        modelBuilder.Entity<User>()
            .Property(u => u.LastName).HasColumnName("last_name");
        modelBuilder.Entity<User>()
            .Property(u => u.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        modelBuilder.Entity<User>()
            .Property(u => u.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
        modelBuilder.Entity<User>()
            .HasMany(u => u.Holdings)
            .WithOne(h => h.User)
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Holding
        modelBuilder.Entity<Holding>()
            .HasKey(h => h.Id);
        modelBuilder.Entity<Holding>()
            .Property(h => h.Id).HasColumnName("id");
        modelBuilder.Entity<Holding>()
            .Property(h => h.UserId).HasColumnName("user_id");
        modelBuilder.Entity<Holding>()
            .Property(h => h.Ticker).HasColumnName("ticker");
        modelBuilder.Entity<Holding>()
            .Property(h => h.EtfName).HasColumnName("etf_name");
        modelBuilder.Entity<Holding>()
            .Property(h => h.Quantity).HasColumnName("quantity");
        modelBuilder.Entity<Holding>()
            .Property(h => h.AverageCost).HasColumnName("average_cost");
        modelBuilder.Entity<Holding>()
            .Property(h => h.Broker).HasColumnName("broker");
        modelBuilder.Entity<Holding>()
            .Property(h => h.PriceSource).HasColumnName("price_source");
        modelBuilder.Entity<Holding>()
            .Property(h => h.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        modelBuilder.Entity<Holding>()
            .Property(h => h.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        modelBuilder.Entity<Holding>()
            .HasIndex(h => new { h.UserId, h.Ticker })
            .IsUnique();
        modelBuilder.Entity<Holding>()
            .HasIndex(h => h.Ticker);
        modelBuilder.Entity<Holding>()
            .HasMany(h => h.Transactions)
            .WithOne(t => t.Holding)
            .HasForeignKey(t => t.HoldingId)
            .OnDelete(DeleteBehavior.Cascade);

        // Transaction
        modelBuilder.Entity<Transaction>()
            .HasKey(t => t.Id);
        modelBuilder.Entity<Transaction>()
            .Property(t => t.Id).HasColumnName("id");
        modelBuilder.Entity<Transaction>()
            .Property(t => t.HoldingId).HasColumnName("holding_id");
        modelBuilder.Entity<Transaction>()
            .Property(t => t.Quantity).HasColumnName("quantity");
        modelBuilder.Entity<Transaction>()
            .Property(t => t.PurchasePrice).HasColumnName("purchase_price");
        modelBuilder.Entity<Transaction>()
            .Property(t => t.PurchaseDate).HasColumnName("purchase_date").HasColumnType("date");
        modelBuilder.Entity<Transaction>()
            .Property(t => t.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        modelBuilder.Entity<Transaction>()
            .Property(t => t.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.HoldingId);
        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.PurchaseDate);

        // PriceSnapshot
        modelBuilder.Entity<PriceSnapshot>()
            .HasKey(ps => ps.Id);
        modelBuilder.Entity<PriceSnapshot>()
            .Property(ps => ps.Id).HasColumnName("id");
        modelBuilder.Entity<PriceSnapshot>()
            .Property(ps => ps.Ticker).HasColumnName("ticker");
        modelBuilder.Entity<PriceSnapshot>()
            .Property(ps => ps.Price).HasColumnName("price");
        modelBuilder.Entity<PriceSnapshot>()
            .Property(ps => ps.SnapshotDate).HasColumnName("snapshot_date");
        modelBuilder.Entity<PriceSnapshot>()
            .Property(ps => ps.Source).HasColumnName("source");
        modelBuilder.Entity<PriceSnapshot>()
            .Property(ps => ps.CreatedAt).HasColumnName("created_at");
        modelBuilder.Entity<PriceSnapshot>()
            .HasIndex(ps => new { ps.Ticker, ps.SnapshotDate })
            .IsUnique();
        modelBuilder.Entity<PriceSnapshot>()
            .HasIndex(ps => ps.Ticker);
        modelBuilder.Entity<PriceSnapshot>()
            .HasIndex(ps => ps.SnapshotDate);

        // ProjectionSettings
        modelBuilder.Entity<ProjectionSettings>().ToTable("projection_settings");
        modelBuilder.Entity<ProjectionSettings>()
            .HasKey(ps => ps.Id);
        modelBuilder.Entity<ProjectionSettings>()
            .Property(ps => ps.Id).HasColumnName("id");
        modelBuilder.Entity<ProjectionSettings>()
            .Property(ps => ps.UserId).HasColumnName("user_id");
        modelBuilder.Entity<ProjectionSettings>()
            .Property(ps => ps.YearlyReturnPercent).HasColumnName("yearly_return_percent").HasColumnType("decimal(5,2)");
        modelBuilder.Entity<ProjectionSettings>()
            .Property(ps => ps.MonthlyBuyAmount).HasColumnName("monthly_buy_amount").HasColumnType("decimal(12,2)");
        modelBuilder.Entity<ProjectionSettings>()
            .Property(ps => ps.AnnualBuyIncreasePercent).HasColumnName("annual_buy_increase_percent").HasColumnType("decimal(5,2)");
        modelBuilder.Entity<ProjectionSettings>()
            .Property(ps => ps.ProjectionYears).HasColumnName("projection_years");
        modelBuilder.Entity<ProjectionSettings>()
            .Property(ps => ps.InflationPercent).HasColumnName("inflation_percent").HasColumnType("decimal(5,2)");
        modelBuilder.Entity<ProjectionSettings>()
            .Property(ps => ps.CgtPercent).HasColumnName("cgt_percent").HasColumnType("decimal(5,2)");
        modelBuilder.Entity<ProjectionSettings>()
            .Property(ps => ps.ExitTaxPercent).HasColumnName("exit_tax_percent").HasColumnType("decimal(5,2)");
        modelBuilder.Entity<ProjectionSettings>()
            .Property(ps => ps.ExcludePreExistingFromTax).HasColumnName("exclude_pre_existing_from_tax").HasDefaultValue(false);
        modelBuilder.Entity<ProjectionSettings>()
            .Property(ps => ps.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        modelBuilder.Entity<ProjectionSettings>()
            .Property(ps => ps.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        modelBuilder.Entity<ProjectionSettings>()
            .HasIndex(ps => ps.UserId).IsUnique();
        modelBuilder.Entity<ProjectionSettings>()
            .HasOne(ps => ps.User)
            .WithOne(u => u.ProjectionSettings)
            .HasForeignKey<ProjectionSettings>(ps => ps.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ProjectionVersion
        modelBuilder.Entity<ProjectionVersion>().ToTable("projection_versions");
        modelBuilder.Entity<ProjectionVersion>().HasKey(pv => pv.Id);
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.Id).HasColumnName("id");
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.UserId).HasColumnName("user_id");
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.VersionNumber).HasColumnName("version_number");
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.SavedAt).HasColumnName("saved_at");
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.YearlyReturnPercent).HasColumnName("yearly_return_percent").HasColumnType("decimal(5,2)");
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.MonthlyBuyAmount).HasColumnName("monthly_buy_amount").HasColumnType("decimal(12,2)");
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.AnnualBuyIncreasePercent).HasColumnName("annual_buy_increase_percent").HasColumnType("decimal(5,2)");
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.ProjectionYears).HasColumnName("projection_years");
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.InflationPercent).HasColumnName("inflation_percent").HasColumnType("decimal(5,2)");
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.CgtPercent).HasColumnName("cgt_percent").HasColumnType("decimal(5,2)");
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.ExitTaxPercent).HasColumnName("exit_tax_percent").HasColumnType("decimal(5,2)");
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.ExcludePreExistingFromTax).HasColumnName("exclude_pre_existing_from_tax").HasDefaultValue(false);
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.DataPointsJson).HasColumnName("data_points_json").HasColumnType("text");
        modelBuilder.Entity<ProjectionVersion>().HasIndex(pv => new { pv.UserId, pv.VersionNumber }).IsUnique();
        modelBuilder.Entity<ProjectionVersion>()
            .HasOne(pv => pv.User)
            .WithMany()
            .HasForeignKey(pv => pv.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
