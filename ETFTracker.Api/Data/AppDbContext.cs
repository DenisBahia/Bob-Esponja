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
    public DbSet<SellAllocation> SellAllocations { get; set; }
    public DbSet<AssetTaxRate> AssetTaxRates { get; set; }
    public DbSet<PriceSnapshot> PriceSnapshots { get; set; }
    public DbSet<ProjectionSettings> ProjectionSettings { get; set; }
    public DbSet<ProjectionVersion> ProjectionVersions { get; set; }
    public DbSet<ProfileShare> ProfileShares { get; set; }

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
            .Property(h => h.SecurityType).HasColumnName("security_type").HasMaxLength(50);
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
            .Property(t => t.TransactionType).HasColumnName("transaction_type").HasDefaultValue(TransactionType.Buy);
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
        modelBuilder.Entity<Transaction>()
            .HasMany(t => t.SellAllocations)
            .WithOne(a => a.SellTransaction)
            .HasForeignKey(a => a.SellTransactionId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Transaction>()
            .HasMany(t => t.BuyAllocations)
            .WithOne(a => a.BuyTransaction)
            .HasForeignKey(a => a.BuyTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        // SellAllocation
        modelBuilder.Entity<SellAllocation>().ToTable("sell_allocations");
        modelBuilder.Entity<SellAllocation>().HasKey(a => a.Id);
        modelBuilder.Entity<SellAllocation>().Property(a => a.Id).HasColumnName("id");
        modelBuilder.Entity<SellAllocation>().Property(a => a.SellTransactionId).HasColumnName("sell_transaction_id");
        modelBuilder.Entity<SellAllocation>().Property(a => a.BuyTransactionId).HasColumnName("buy_transaction_id");
        modelBuilder.Entity<SellAllocation>().Property(a => a.AllocatedQuantity).HasColumnName("allocated_quantity");
        modelBuilder.Entity<SellAllocation>().Property(a => a.BuyPrice).HasColumnName("buy_price");
        modelBuilder.Entity<SellAllocation>().HasIndex(a => a.SellTransactionId);
        modelBuilder.Entity<SellAllocation>().HasIndex(a => a.BuyTransactionId);

        // AssetTaxRate
        modelBuilder.Entity<AssetTaxRate>().ToTable("asset_tax_rates");
        modelBuilder.Entity<AssetTaxRate>().HasKey(r => r.SecurityType);
        modelBuilder.Entity<AssetTaxRate>().Property(r => r.SecurityType).HasColumnName("security_type").HasMaxLength(50);
        modelBuilder.Entity<AssetTaxRate>().Property(r => r.ExitTaxPercent).HasColumnName("exit_tax_percent").HasColumnType("decimal(5,2)");
        modelBuilder.Entity<AssetTaxRate>().Property(r => r.Label).HasColumnName("label").HasMaxLength(100);
        modelBuilder.Entity<AssetTaxRate>().Property(r => r.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        modelBuilder.Entity<AssetTaxRate>().Property(r => r.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

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
            .Property(ps => ps.CgtPercent).HasColumnName("cgt_percent").HasColumnType("decimal(5,2)");
        modelBuilder.Entity<ProjectionSettings>()
            .Property(ps => ps.ExitTaxPercent).HasColumnName("exit_tax_percent").HasColumnType("decimal(5,2)");
        modelBuilder.Entity<ProjectionSettings>()
            .Property(ps => ps.ExcludePreExistingFromTax).HasColumnName("exclude_pre_existing_from_tax").HasDefaultValue(false);
        modelBuilder.Entity<ProjectionSettings>()
            .Property(ps => ps.SiaAnnualPercent).HasColumnName("sia_annual_percent").HasColumnType("decimal(5,2)").HasDefaultValue(0m);
        modelBuilder.Entity<ProjectionSettings>()
            .Property(ps => ps.StartAmount).HasColumnName("start_amount").HasColumnType("decimal(15,2)").IsRequired(false);
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
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.VersionName).HasColumnName("version_name").HasMaxLength(200);
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.IsDefault).HasColumnName("is_default").HasDefaultValue(false);
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.SavedAt).HasColumnName("saved_at");
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.YearlyReturnPercent).HasColumnName("yearly_return_percent").HasColumnType("decimal(5,2)");
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.MonthlyBuyAmount).HasColumnName("monthly_buy_amount").HasColumnType("decimal(12,2)");
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.AnnualBuyIncreasePercent).HasColumnName("annual_buy_increase_percent").HasColumnType("decimal(5,2)");
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.ProjectionYears).HasColumnName("projection_years");
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.InflationPercent).HasColumnName("inflation_percent").HasColumnType("decimal(5,2)");
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.CgtPercent).HasColumnName("cgt_percent").HasColumnType("decimal(5,2)");
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.ExitTaxPercent).HasColumnName("exit_tax_percent").HasColumnType("decimal(5,2)");
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.ExcludePreExistingFromTax).HasColumnName("exclude_pre_existing_from_tax").HasDefaultValue(false);
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.SiaAnnualPercent).HasColumnName("sia_annual_percent").HasColumnType("decimal(5,2)").HasDefaultValue(0m);
        modelBuilder.Entity<ProjectionVersion>().Property(pv => pv.DataPointsJson).HasColumnName("data_points_json").HasColumnType("text");
        modelBuilder.Entity<ProjectionVersion>().HasIndex(pv => pv.UserId);
        modelBuilder.Entity<ProjectionVersion>()
            .HasOne(pv => pv.User)
            .WithMany()
            .HasForeignKey(pv => pv.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ProfileShare
        modelBuilder.Entity<ProfileShare>().ToTable("profile_shares");
        modelBuilder.Entity<ProfileShare>().HasKey(s => s.Id);
        modelBuilder.Entity<ProfileShare>().Property(s => s.Id).HasColumnName("id");
        modelBuilder.Entity<ProfileShare>().Property(s => s.OwnerId).HasColumnName("owner_id");
        modelBuilder.Entity<ProfileShare>().Property(s => s.GuestEmail).HasColumnName("guest_email").HasMaxLength(256);
        modelBuilder.Entity<ProfileShare>().Property(s => s.GuestUserId).HasColumnName("guest_user_id");
        modelBuilder.Entity<ProfileShare>().Property(s => s.IsReadOnly).HasColumnName("is_read_only").HasDefaultValue(true);
        modelBuilder.Entity<ProfileShare>().Property(s => s.Status).HasColumnName("status").HasDefaultValue(ShareStatus.Pending);
        modelBuilder.Entity<ProfileShare>().Property(s => s.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        modelBuilder.Entity<ProfileShare>().Property(s => s.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        modelBuilder.Entity<ProfileShare>()
            .HasIndex(s => new { s.OwnerId, s.GuestEmail })
            .IsUnique();
    }
}
