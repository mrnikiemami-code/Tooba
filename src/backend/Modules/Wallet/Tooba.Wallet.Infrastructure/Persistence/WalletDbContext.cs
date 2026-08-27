using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Persistence;
using Tooba.Wallet.Domain;

namespace Tooba.Wallet.Infrastructure.Persistence;

/// <summary>DbContext مالک schema مستقل wallet.</summary>
public sealed class WalletDbContext : DbContext
{
    /// <summary>schema اختصاصی Wallet.</summary>
    public const string Schema = "wallet";

    /// <summary>DbContext را می‌سازد.</summary>
    public WalletDbContext(DbContextOptions<WalletDbContext> options) : base(options)
    {
    }

    /// <summary>حساب‌ها.</summary>
    public DbSet<WalletAccount> Accounts => Set<WalletAccount>();

    /// <summary>سطرهای دفتر.</summary>
    public DbSet<WalletLedgerEntry> LedgerEntries => Set<WalletLedgerEntry>();

    /// <summary>کارت‌های هدیه.</summary>
    public DbSet<GiftCard> GiftCards => Set<GiftCard>();

    /// <summary>بازخریدها.</summary>
    public DbSet<GiftCardRedemption> Redemptions => Set<GiftCardRedemption>();

    /// <summary>Outbox ماژول.</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<WalletAccount>(entity =>
        {
            entity.ToTable("wallet_accounts");
            entity.HasKey(x => x.AccountId);
            entity.Property(x => x.AccountId).ValueGeneratedNever();
            entity.Property(x => x.Currency).HasMaxLength(8);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
            entity.HasIndex(x => x.CustomerActorUserId).IsUnique();
        });
        modelBuilder.Entity<WalletLedgerEntry>(entity =>
        {
            entity.ToTable("wallet_ledger_entries");
            entity.HasKey(x => x.EntryId);
            entity.Property(x => x.EntryId).ValueGeneratedNever();
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Amount).HasPrecision(18, 0);
            entity.Property(x => x.Currency).HasMaxLength(8);
            entity.Property(x => x.Direction).HasConversion<string>().HasMaxLength(8);
            entity.Property(x => x.SourceType).HasMaxLength(WalletLedgerEntry.SourceTypeMaxLength);
            entity.Property(x => x.IdempotencyKey).HasMaxLength(WalletLedgerEntry.IdempotencyKeyMaxLength);
            entity.Property(x => x.Metadata).HasMaxLength(WalletLedgerEntry.MetadataMaxLength);
            entity.HasIndex(x => x.IdempotencyKey).IsUnique();
            entity.HasIndex(x => new { x.AccountId, x.CreatedAt });
        });
        modelBuilder.Entity<GiftCard>(entity =>
        {
            entity.ToTable("gift_cards");
            entity.HasKey(x => x.CardId);
            entity.Property(x => x.CardId).ValueGeneratedNever();
            entity.Property(x => x.CodeHash).HasMaxLength(GiftCard.CodeHashMaxLength);
            entity.Property(x => x.Currency).HasMaxLength(8);
            entity.Property(x => x.InitialAmount).HasPrecision(18, 0);
            entity.Property(x => x.RemainingAmount).HasPrecision(18, 0);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.IdempotencyKey).HasMaxLength(GiftCard.IdempotencyKeyMaxLength);
            entity.HasIndex(x => x.CodeHash).IsUnique();
            entity.HasIndex(x => x.IdempotencyKey).IsUnique();
            entity.HasIndex(x => new { x.Status, x.IssuedAt });
        });
        modelBuilder.Entity<GiftCardRedemption>(entity =>
        {
            entity.ToTable("gift_card_redemptions");
            entity.HasKey(x => x.RedemptionId);
            entity.Property(x => x.RedemptionId).ValueGeneratedNever();
            entity.Property(x => x.Amount).HasPrecision(18, 0);
            entity.Property(x => x.IdempotencyKey).HasMaxLength(GiftCardRedemption.IdempotencyKeyMaxLength);
            entity.HasIndex(x => x.IdempotencyKey).IsUnique();
            entity.HasIndex(x => new { x.CardId, x.CreatedAt });
            entity.HasIndex(x => new { x.AccountId, x.CreatedAt });
        });
        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>کارخانهٔ design-time مهاجرت Wallet.</summary>
public sealed class WalletDbContextFactory : IDesignTimeDbContextFactory<WalletDbContext>
{
    /// <inheritdoc />
    public WalletDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<WalletDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, ToobaNpgsql.DesignTimeConnectionString(), WalletDbContext.Schema, typeof(WalletDbContext));
        return new WalletDbContext(options.Options);
    }
}
