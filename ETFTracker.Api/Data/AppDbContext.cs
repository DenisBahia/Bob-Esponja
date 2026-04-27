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
    public DbSet<UserSettings> UserSettings { get; set; }
    public DbSet<ProfileShare> ProfileShares { get; set; }
    public DbSet<UserGoal> UserGoals { get; set; }
    public DbSet<SellRecord> SellRecords { get; set; }
    public DbSet<SellLotAllocation> SellLotAllocations { get; set; }
    public DbSet<TaxEvent> TaxEvents { get; set; }
    public DbSet<AssetTypeDeemedDisposalDefault> AssetTypeDeemedDisposalDefaults { get; set; }
    public DbSet<AnnualTaxSummary> AnnualTaxSummaries { get; set; }

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
            .Property(u => u.Username).HasColumnName("username").HasMaxLength(50);
        modelBuilder.Entity<User>()
            .Property(u => u.Email).HasColumnName("email");
        modelBuilder.Entity<User>()
            .Property(u => u.FirstName).HasColumnName("first_name");
        modelBuilder.Entity<User>()
            .Property(u => u.LastName).HasColumnName("last_name");
        modelBuilder.Entity<User>()
            .Property(u => u.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        modelBuilder.Entity<User>()
            .Property(u => u.LastLoginAt).HasColumnName("last_login_at");
        modelBuilder.Entity<User>()
            .Property(u => u.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique()
            .HasFilter("username IS NOT NULL");
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique()
            .HasFilter("email IS NOT NULL");
        modelBuilder.Entity<User>()
            .HasMany(u => u.Holdings)
            .WithOne(h => h.User)
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<User>()
            .HasMany(u => u.SharedByMe)
            .WithOne(s => s.Owner)
            .HasForeignKey(s => s.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<User>()
            .HasMany(u => u.SharedWithMe)
            .WithOne(s => s.GuestUser)
            .HasForeignKey(s => s.GuestUserId)
            .OnDelete(DeleteBehavior.SetNull);

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
            .Property(t => t.DeemedDisposalDue).HasColumnName("deemed_disposal_due").HasDefaultValue(false);
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
            .Property(ps => ps.UpdatedAt).HasColumnName("updated_at");
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
            .Property(ps => ps.StartAmount).HasColumnName("start_amount").HasColumnType("decimal(15,2)").IsRequired(false);
        modelBuilder.Entity<ProjectionSettings>()
            .Property(ps => ps.ApplyDeemedDisposal).HasColumnName("apply_deemed_disposal").HasDefaultValue(false);
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

        // UserSettings
        modelBuilder.Entity<UserSettings>().ToTable("user_settings");
        modelBuilder.Entity<UserSettings>().HasKey(us => us.Id);
        modelBuilder.Entity<UserSettings>().Property(us => us.Id).HasColumnName("id");
        modelBuilder.Entity<UserSettings>().Property(us => us.UserId).HasColumnName("user_id");
        modelBuilder.Entity<UserSettings>().Property(us => us.IsIrishInvestor).HasColumnName("is_irish_investor").HasDefaultValue(false);
        modelBuilder.Entity<UserSettings>().Property(us => us.ExitTaxPercent).HasColumnName("exit_tax_percent").HasColumnType("decimal(5,2)").HasDefaultValue(41m);
        modelBuilder.Entity<UserSettings>().Property(us => us.DeemedDisposalPercent).HasColumnName("deemed_disposal_percent").HasColumnType("decimal(5,2)").HasDefaultValue(41m);
        modelBuilder.Entity<UserSettings>().Property(us => us.SiaAnnualPercent).HasColumnName("sia_annual_percent").HasColumnType("decimal(5,2)").HasDefaultValue(0m);
        modelBuilder.Entity<UserSettings>().Property(us => us.DeemedDisposalEnabled).HasColumnName("deemed_disposal_enabled").HasDefaultValue(false);
        modelBuilder.Entity<UserSettings>().Property(us => us.CgtPercent).HasColumnName("cgt_percent").HasColumnType("decimal(5,2)").HasDefaultValue(33m);
        modelBuilder.Entity<UserSettings>().Property(us => us.TaxFreeAllowancePerYear).HasColumnName("tax_free_allowance_per_year").HasColumnType("decimal(12,2)").HasDefaultValue(0m);
        modelBuilder.Entity<UserSettings>().Property(us => us.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        modelBuilder.Entity<UserSettings>().Property(us => us.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        modelBuilder.Entity<UserSettings>().HasIndex(us => us.UserId).IsUnique();
        modelBuilder.Entity<UserSettings>()
            .HasOne(us => us.User)
            .WithOne(u => u.UserSettings)
            .HasForeignKey<UserSettings>(us => us.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ProjectionVersion
        modelBuilder.Entity<ProjectionVersion>().ToTable("projection_versions");
        modelBuilder.Entity<ProjectionVersion>().HasKey(pv => pv.Id);
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.Id).HasColumnName("id");
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.UserId).HasColumnName("user_id");
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.VersionName).HasColumnName("version_name").HasMaxLength(200);
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.IsDefault).HasColumnName("is_default").HasDefaultValue(false);
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.SavedAt).HasColumnName("saved_at");
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.YearlyReturnPercent).HasColumnName("yearly_return_percent").HasColumnType("decimal(5,2)");
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.MonthlyBuyAmount).HasColumnName("monthly_buy_amount").HasColumnType("decimal(12,2)");
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.AnnualBuyIncreasePercent).HasColumnName("annual_buy_increase_percent").HasColumnType("decimal(5,2)");
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.ProjectionYears).HasColumnName("projection_years");
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.InflationPercent).HasColumnName("inflation_percent").HasColumnType("decimal(5,2)");
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.CgtPercent).HasColumnName("cgt_percent").HasColumnType("decimal(5,2)");
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.StartAmount).HasColumnName("start_amount").HasColumnType("decimal(15,2)").IsRequired(false);
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.ApplyDeemedDisposal).HasColumnName("apply_deemed_disposal").HasDefaultValue(false);
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.DeemedDisposalPercent).HasColumnName("deemed_disposal_percent").HasColumnType("decimal(5,2)").HasDefaultValue(0m);
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.DataPointsJson).HasColumnName("data_points_json").HasColumnType("text");
        modelBuilder.Entity<ProjectionVersion>().HasIndex(pv => pv.UserId);
        modelBuilder.Entity<ProjectionVersion>()
            .HasOne(pv => pv.User)
            .WithMany()
            .HasForeignKey(pv => pv.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // AssetTypeDeemedDisposalDefault
        modelBuilder.Entity<AssetTypeDeemedDisposalDefault>().ToTable("asset_type_deemed_disposal_defaults");
        modelBuilder.Entity<AssetTypeDeemedDisposalDefault>().HasKey(a => a.Id);
        modelBuilder.Entity<AssetTypeDeemedDisposalDefault>().Property(a => a.Id).HasColumnName("id");
        modelBuilder.Entity<AssetTypeDeemedDisposalDefault>().Property(a => a.UserId).HasColumnName("user_id");
        modelBuilder.Entity<AssetTypeDeemedDisposalDefault>().Property(a => a.AssetType).HasColumnName("asset_type").HasMaxLength(50);
        modelBuilder.Entity<AssetTypeDeemedDisposalDefault>().Property(a => a.DeemedDisposalDue).HasColumnName("deemed_disposal_due").HasDefaultValue(false);
        modelBuilder.Entity<AssetTypeDeemedDisposalDefault>().Property(a => a.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        modelBuilder.Entity<AssetTypeDeemedDisposalDefault>().HasIndex(a => new { a.UserId, a.AssetType }).IsUnique();
        modelBuilder.Entity<AssetTypeDeemedDisposalDefault>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // AnnualTaxSummary
        modelBuilder.Entity<AnnualTaxSummary>().ToTable("annual_tax_summary");
        modelBuilder.Entity<AnnualTaxSummary>().HasKey(a => a.Id);
        modelBuilder.Entity<AnnualTaxSummary>().Property(a => a.Id).HasColumnName("id");
        modelBuilder.Entity<AnnualTaxSummary>().Property(a => a.UserId).HasColumnName("user_id");
        modelBuilder.Entity<AnnualTaxSummary>().Property(a => a.TaxYear).HasColumnName("tax_year");
        modelBuilder.Entity<AnnualTaxSummary>().Property(a => a.TaxType).HasColumnName("tax_type").HasMaxLength(20);
        modelBuilder.Entity<AnnualTaxSummary>().Property(a => a.HoldingId).HasColumnName("holding_id").IsRequired(false);
        modelBuilder.Entity<AnnualTaxSummary>().Property(a => a.TotalProfits).HasColumnName("total_profits").HasColumnType("decimal(12,2)");
        modelBuilder.Entity<AnnualTaxSummary>().Property(a => a.TotalLosses).HasColumnName("total_losses").HasColumnType("decimal(12,2)");
        modelBuilder.Entity<AnnualTaxSummary>().Property(a => a.NetGain).HasColumnName("net_gain").HasColumnType("decimal(12,2)");
        modelBuilder.Entity<AnnualTaxSummary>().Property(a => a.AllowanceApplied).HasColumnName("allowance_applied").HasColumnType("decimal(12,2)");
        modelBuilder.Entity<AnnualTaxSummary>().Property(a => a.DeemedDisposalCredit).HasColumnName("deemed_disposal_credit").HasColumnType("decimal(12,2)");
        modelBuilder.Entity<AnnualTaxSummary>().Property(a => a.TaxableGain).HasColumnName("taxable_gain").HasColumnType("decimal(12,2)");
        modelBuilder.Entity<AnnualTaxSummary>().Property(a => a.TaxDue).HasColumnName("tax_due").HasColumnType("decimal(12,2)");
        modelBuilder.Entity<AnnualTaxSummary>().Property(a => a.TaxRateUsed).HasColumnName("tax_rate_used").HasColumnType("decimal(5,2)");
        modelBuilder.Entity<AnnualTaxSummary>().Property(a => a.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("Pending");
        modelBuilder.Entity<AnnualTaxSummary>().Property(a => a.PaidTaxAmount).HasColumnName("paid_tax_amount").HasColumnType("decimal(12,2)").HasDefaultValue(0m);
        modelBuilder.Entity<AnnualTaxSummary>().Property(a => a.RecalculatedAt).HasColumnName("recalculated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        modelBuilder.Entity<AnnualTaxSummary>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AnnualTaxSummary>()
            .HasOne(a => a.Holding)
            .WithMany()
            .HasForeignKey(a => a.HoldingId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);
    }
}
