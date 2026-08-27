using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Persistence;
using Tooba.Settlement.Domain;

namespace Tooba.Settlement.Infrastructure.Persistence;

/// <summary>
/// DbContext مالک schema <c>settlement</c>. سفارش و پرداخت را نگه نمی‌دارد.
/// </summary>
public sealed class SettlementDbContext : DbContext
{
    /// <summary>
    /// schema اختصاصی Settlement.
    /// </summary>
    public const string Schema = "settlement";

    /// <summary>
    /// DbContext را با گزینه‌های Host می‌سازد.
    /// </summary>
    public SettlementDbContext(DbContextOptions<SettlementDbContext> options)
        : base(options)
    {
    }

    /// <summary>سیاست‌های کارمزد.</summary>
    public DbSet<CommissionPolicy> CommissionPolicies => Set<CommissionPolicy>();

    /// <summary>حساب‌های تسویه.</summary>
    public DbSet<SettlementAccount> SettlementAccounts => Set<SettlementAccount>();

    /// <summary>سطرهای posted.</summary>
    public DbSet<SettlementEntry> SettlementEntries => Set<SettlementEntry>();

    /// <summary>صورت‌حساب‌ها.</summary>
    public DbSet<SettlementStatement> SettlementStatements => Set<SettlementStatement>();

    /// <summary>پروفایل payout فروشنده.</summary>
    public DbSet<SellerPayoutProfile> SellerPayoutProfiles => Set<SellerPayoutProfile>();

    /// <summary>درخواست‌های payout.</summary>
    public DbSet<PayoutRequest> PayoutRequests => Set<PayoutRequest>();

    /// <summary>تلاش‌های payout.</summary>
    public DbSet<PayoutAttempt> PayoutAttempts => Set<PayoutAttempt>();

    /// <summary>inbox payment.</summary>
    public DbSet<SettlementPaymentInboxRecord> PaymentInbox => Set<SettlementPaymentInboxRecord>();

    /// <summary>inbox refund.</summary>
    public DbSet<SettlementRefundInboxRecord> RefundInbox => Set<SettlementRefundInboxRecord>();

    /// <summary>Outbox همین ماژول.</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<CommissionPolicy>(entity =>
        {
            entity.ToTable("commission_policies");
            entity.HasKey(x => x.PolicyId);
            entity.Property(x => x.PolicyId).ValueGeneratedNever();
            entity.Property(x => x.Name).HasMaxLength(128);
            entity.Property(x => x.Rate).HasPrecision(19, 4);
            entity.HasIndex(x => x.IsDefault);
        });
        modelBuilder.Entity<SettlementAccount>(entity =>
        {
            entity.ToTable("settlement_accounts");
            entity.HasKey(x => x.SettlementAccountId);
            entity.Property(x => x.SettlementAccountId).ValueGeneratedNever();
            entity.Property(x => x.Currency).HasMaxLength(8);
            entity.HasIndex(x => x.SellerPartyId).IsUnique();
        });
        modelBuilder.Entity<SettlementEntry>(entity =>
        {
            entity.ToTable("settlement_entries");
            entity.HasKey(x => x.EntryId);
            entity.Property(x => x.EntryId).ValueGeneratedNever();
            entity.Property(x => x.EntryType).HasConversion<string>().HasMaxLength(16);
            entity.Property(x => x.GrossAmount).HasPrecision(19, 4);
            entity.Property(x => x.CommissionAmount).HasPrecision(19, 4);
            entity.Property(x => x.NetAmount).HasPrecision(19, 4);
            entity.Property(x => x.Currency).HasMaxLength(8);
            entity.Property(x => x.SourceType).HasMaxLength(32);
            entity.Property(x => x.IdempotencyKey).HasMaxLength(128);
            entity.OwnsOne(x => x.CommissionPolicySnapshot, owned =>
            {
                owned.Property(x => x.PolicyId).HasColumnName("commission_policy_id");
                owned.Property(x => x.PolicyName).HasColumnName("commission_policy_name").HasMaxLength(128);
                owned.Property(x => x.Rate).HasColumnName("commission_rate").HasPrecision(19, 4);
            });
            entity.Ignore(x => x.DomainEvents);
            entity.HasIndex(x => x.IdempotencyKey).IsUnique();
            entity.HasIndex(x => x.SettlementAccountId);
            entity.HasIndex(x => x.SellerPartyId);
            entity.HasIndex(x => x.SellerOrderId);
        });
        modelBuilder.Entity<SettlementStatement>(entity =>
        {
            entity.ToTable("settlement_statements");
            entity.HasKey(x => x.StatementId);
            entity.Property(x => x.StatementId).ValueGeneratedNever();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
            entity.Property(x => x.OpeningBalance).HasPrecision(19, 4);
            entity.Property(x => x.ClosingBalance).HasPrecision(19, 4);
            entity.Property(x => x.Currency).HasMaxLength(8);
            entity.HasIndex(x => x.SettlementAccountId);
        });
        modelBuilder.Entity<SellerPayoutProfile>(entity =>
        {
            entity.ToTable("seller_payout_profiles");
            entity.HasKey(x => x.SellerPayoutProfileId);
            entity.Property(x => x.SellerPayoutProfileId).ValueGeneratedNever();
            entity.Property(x => x.Iban).HasMaxLength(64);
            entity.Property(x => x.AccountHolderName).HasMaxLength(256);
            entity.HasIndex(x => x.SellerPartyId).IsUnique();
        });
        modelBuilder.Entity<PayoutRequest>(entity =>
        {
            entity.ToTable("payout_requests");
            entity.HasKey(x => x.PayoutRequestId);
            entity.Property(x => x.PayoutRequestId).ValueGeneratedNever();
            entity.Property(x => x.Amount).HasPrecision(19, 4);
            entity.Property(x => x.Currency).HasMaxLength(8);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
            entity.Property(x => x.IdempotencyKey).HasMaxLength(128);
            entity.Ignore(x => x.DomainEvents);
            entity.Ignore(x => x.Attempts);
            entity.HasIndex(x => x.IdempotencyKey).IsUnique();
            entity.HasIndex(x => x.SellerPartyId);
            entity.HasIndex(x => x.SettlementAccountId);
            entity.HasIndex(x => x.Status);
        });
        modelBuilder.Entity<PayoutAttempt>(entity =>
        {
            entity.ToTable("payout_attempts");
            entity.HasKey(x => x.PayoutAttemptId);
            entity.Property(x => x.PayoutAttemptId).ValueGeneratedNever();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
            entity.Property(x => x.IdempotencyKey).HasMaxLength(128);
            entity.Property(x => x.ProviderReference).HasMaxLength(256);
            entity.Property(x => x.FailureCode).HasMaxLength(64);
            entity.HasIndex(x => x.PayoutRequestId);
            entity.HasIndex(x => x.IdempotencyKey).IsUnique();
        });
        modelBuilder.Entity<SettlementPaymentInboxRecord>(entity =>
        {
            entity.ToTable("payment_inbox");
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.EventId).ValueGeneratedNever();
        });
        modelBuilder.Entity<SettlementRefundInboxRecord>(entity =>
        {
            entity.ToTable("refund_inbox");
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.EventId).ValueGeneratedNever();
        });
        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>
/// کارخانهٔ زمان طراحی مهاجرت.
/// </summary>
public sealed class SettlementDbContextFactory : IDesignTimeDbContextFactory<SettlementDbContext>
{
    /// <inheritdoc />
    public SettlementDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SettlementDbContext>()
            .UseNpgsql("Host=127.0.0.1;Database=tooba_design;Username=tooba;Password=dev-placeholder")
            .Options;
        return new SettlementDbContext(options);
    }
}
