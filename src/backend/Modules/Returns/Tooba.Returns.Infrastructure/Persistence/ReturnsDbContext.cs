using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Persistence;
using Tooba.Returns.Domain.Aggregates;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Infrastructure.Persistence;

/// <summary>
/// DbContext مالک schema <c>returns</c>. سفارش و پرداخت را نگه نمی‌دارد.
/// </summary>
public sealed class ReturnsDbContext : DbContext
{
    /// <summary>
    /// schema اختصاصی Returns.
    /// </summary>
    public const string Schema = "returns";

    /// <summary>
    /// DbContext را با گزینه‌های Host می‌سازد.
    /// </summary>
    public ReturnsDbContext(DbContextOptions<ReturnsDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// درخواست‌های مرجوعی.
    /// </summary>
    public DbSet<ReturnRequest> ReturnRequests => Set<ReturnRequest>();

    /// <summary>
    /// خطوط مرجوعی.
    /// </summary>
    public DbSet<ReturnItem> ReturnItems => Set<ReturnItem>();

    /// <summary>
    /// تلاش‌های refund.
    /// </summary>
    public DbSet<RefundAttempt> RefundAttempts => Set<RefundAttempt>();

    /// <summary>
    /// TB-265-R2: تصمیم پایدار و سریال‌شدهٔ تأیید/رد هر پرونده.
    /// </summary>
    public DbSet<ReturnDecisionRecord> ReturnDecisions => Set<ReturnDecisionRecord>();

    /// <summary>
    /// TB-274: رسیدهای فیزیکی مشتری برای پرونده‌های تحویل‌شده (یک ردیف برای هر رخداد دریافت).
    /// </summary>
    public DbSet<ReturnReceipt> ReturnReceipts => Set<ReturnReceipt>();

    /// <summary>
    /// TB-274: خطوط بازرسی‌شدهٔ هر رسید (مقدار دریافت‌شده به تفکیک وضعیت).
    /// </summary>
    public DbSet<ReturnReceiptLine> ReturnReceiptLines => Set<ReturnReceiptLine>();

    /// <summary>
    /// Outbox همین ماژول.
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<ReturnRequest>(entity =>
        {
            entity.ToTable("return_requests");
            entity.HasKey(x => x.ReturnRequestId);
            entity.Property(x => x.ReturnRequestId).ValueGeneratedNever();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.IdempotencyKey).HasMaxLength(128);
            entity.Property(x => x.Reason).HasMaxLength(512);
            entity.Property(x => x.Currency).HasMaxLength(8);
            entity.Property(x => x.RefundAmount).HasPrecision(18, 4);
            entity.Property(x => x.RefundDestination).HasConversion<string>().HasMaxLength(32);
            // TB-265: explicit origin discriminator + courier-origin source linkage and closure phases.
            entity.Property(x => x.Origin).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.SourceOccurrenceKey).HasMaxLength(128);
            entity.Property(x => x.ClosurePhase).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.ReleasedHeldQuantity).HasPrecision(18, 6);
            entity.Ignore(x => x.DomainEvents);
            entity.Ignore(x => x.Items);
            entity.Ignore(x => x.RefundAttempts);
            entity.HasIndex(x => x.IdempotencyKey).IsUnique();
            entity.HasIndex(x => x.SellerOrderId);
            entity.HasIndex(x => x.SellerPartyId);
            entity.HasIndex(x => x.CheckoutId);
            entity.HasIndex(x => x.RequestedByUserId);
        });
        modelBuilder.Entity<ReturnItem>(entity =>
        {
            entity.ToTable("return_items");
            entity.HasKey(x => x.ReturnItemId);
            entity.Property(x => x.ReturnItemId).ValueGeneratedNever();
            entity.Property(x => x.Currency).HasMaxLength(8);
            entity.Property(x => x.Quantity).HasColumnType("numeric(18,6)");
            entity.Property(x => x.UnitPriceSnapshot).HasPrecision(18, 4);
            // TB-265: recorded physical condition and explicit disposition of the received quantity.
            entity.Property(x => x.Condition).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Disposition).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(x => x.ReturnRequestId);
            entity.HasIndex(x => x.OrderLineId);
            // TB-274-R1 (B): the owning case is a real composite identity of a requested line, so a receipt line can be
            // tied locally to BOTH its receipt header and its requested item without importing a cross-module key.
            entity.HasAlternateKey(x => new { x.ReturnRequestId, x.ReturnItemId })
                .HasName("ak_return_items_return_request_id_return_item_id");
        });
        modelBuilder.Entity<RefundAttempt>(entity =>
        {
            entity.ToTable("refund_attempts");
            entity.HasKey(x => x.RefundAttemptId);
            entity.Property(x => x.RefundAttemptId).ValueGeneratedNever();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Currency).HasMaxLength(8);
            entity.Property(x => x.Amount).HasPrecision(18, 4);
            entity.Property(x => x.IdempotencyKey).HasMaxLength(128);
            entity.Property(x => x.ProviderReference).HasMaxLength(256);
            entity.Property(x => x.FailureCode).HasMaxLength(64);
            entity.HasIndex(x => x.ReturnRequestId);
            entity.HasIndex(x => x.IdempotencyKey).IsUnique();
        });
        modelBuilder.Entity<ReturnDecisionRecord>(entity =>
        {
            entity.ToTable("return_request_decisions");
            entity.HasKey(x => x.ReturnRequestId).HasName("pk_return_request_decisions");
            entity.Property(x => x.ReturnRequestId).ValueGeneratedNever().HasColumnName("return_request_id");
            entity.Property(x => x.Decision).HasMaxLength(32).HasColumnName("decision");
            entity.Property(x => x.DecidedByUserId).HasColumnName("decided_by_user_id");
            entity.Property(x => x.DecidedAt).HasColumnName("decided_at");
            // TB-274: the reviewed rejection reason is part of the winning decision row, so the exact operator
            // statement is durable with the decision itself (never a second, separately-writable note).
            entity.Property(x => x.Reason).HasMaxLength(512).HasColumnName("reason");
        });
        modelBuilder.Entity<ReturnReceipt>(entity =>
        {
            entity.ToTable("customer_return_receipts");
            entity.HasKey(x => x.ReturnReceiptId).HasName("pk_customer_return_receipts");
            entity.Property(x => x.ReturnReceiptId).ValueGeneratedNever().HasColumnName("return_receipt_id");
            entity.Property(x => x.ReturnRequestId).HasColumnName("return_request_id");
            entity.Property(x => x.OccurrenceKey)
                .HasMaxLength(ReturnReceipt.OccurrenceKeyMaxLength)
                .HasColumnName("occurrence_key");
            entity.Property(x => x.ReceivedByUserId).HasColumnName("received_by_user_id");
            entity.Property(x => x.RecordedAt).HasColumnName("recorded_at");
            entity.Ignore(x => x.Lines);
            // ONE receipt per reviewed occurrence, and the occurrence belongs to exactly one case.
            entity.HasIndex(x => new { x.ReturnRequestId, x.OccurrenceKey })
                .IsUnique()
                .HasDatabaseName("ix_customer_return_receipts_return_request_id_occurrence_key");
            entity.HasIndex(x => x.ReturnRequestId)
                .HasDatabaseName("ix_customer_return_receipts_return_request_id");
            // TB-274-R1 (B): the header identity a receipt line must match is (case, header), so a line can never be
            // attached to a header of a different case.
            entity.HasAlternateKey(x => new { x.ReturnRequestId, x.ReturnReceiptId })
                .HasName("ak_customer_return_receipts_return_request_id_return_receipt_id");
            entity.HasOne<ReturnRequest>()
                .WithMany()
                .HasForeignKey(x => x.ReturnRequestId)
                .HasConstraintName("fk_customer_return_receipts_return_requests_return_request_id")
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ReturnReceiptLine>(entity =>
        {
            entity.ToTable(
                "customer_return_receipt_lines",
                table =>
                {
                    // TB-274-R1 (B): the two stored facts every later gate trusts are enforced by the store itself, so
                    // no future writer can persist a zero/negative quantity or an inspection token that is neither
                    // usable nor damaged. The cumulative upper bound stays in the serialized owning code by design.
                    table.HasCheckConstraint(
                        "ck_customer_return_receipt_lines_quantity_positive",
                        "quantity > 0");
                    table.HasCheckConstraint(
                        "ck_customer_return_receipt_lines_condition_defined",
                        "condition IN ('Usable', 'Damaged')");
                });
            entity.HasKey(x => x.ReturnReceiptLineId).HasName("pk_customer_return_receipt_lines");
            entity.Property(x => x.ReturnReceiptLineId).ValueGeneratedNever().HasColumnName("return_receipt_line_id");
            entity.Property(x => x.ReturnReceiptId).HasColumnName("return_receipt_id");
            entity.Property(x => x.ReturnRequestId).HasColumnName("return_request_id");
            entity.Property(x => x.ReturnItemId).HasColumnName("return_item_id");
            entity.Property(x => x.Quantity).HasColumnType("numeric(18,6)").HasColumnName("quantity");
            entity.Property(x => x.Condition)
                .HasConversion<string>()
                .HasMaxLength(32)
                .HasColumnName("condition");
            // One condition per line per occurrence; a mixed return is two rows, never a split requested line.
            entity.HasIndex(x => new { x.ReturnReceiptId, x.ReturnItemId, x.Condition })
                .IsUnique()
                .HasDatabaseName("ix_customer_return_receipt_lines_receipt_item_condition");
            entity.HasIndex(x => x.ReturnRequestId)
                .HasDatabaseName("ix_customer_return_receipt_lines_return_request_id");
            entity.HasIndex(x => x.ReturnItemId)
                .HasDatabaseName("ix_customer_return_receipt_lines_return_item_id");
            // TB-274-R1 (B): the two composite FK indexes are named here so the store never carries a convention-
            // truncated identifier.
            entity.HasIndex(x => new { x.ReturnRequestId, x.ReturnReceiptId })
                .HasDatabaseName("ix_customer_return_receipt_lines_owning_receipt");
            entity.HasIndex(x => new { x.ReturnRequestId, x.ReturnItemId })
                .HasDatabaseName("ix_customer_return_receipt_lines_owning_item");
            // TB-274-R1 (B): LOCAL composite ownership. The line's own case id must match BOTH its receipt header's
            // case and its requested item's case, so headerCaseA + itemCaseB can never be stored.
            entity.HasOne<ReturnReceipt>()
                .WithMany()
                .HasForeignKey(x => new { x.ReturnRequestId, x.ReturnReceiptId })
                .HasPrincipalKey(x => new { x.ReturnRequestId, x.ReturnReceiptId })
                .HasConstraintName("fk_customer_return_receipt_lines_receipts_owning_case")
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ReturnItem>()
                .WithMany()
                .HasForeignKey(x => new { x.ReturnRequestId, x.ReturnItemId })
                .HasPrincipalKey(x => new { x.ReturnRequestId, x.ReturnItemId })
                .HasConstraintName("fk_customer_return_receipt_lines_return_items_owning_case")
                .OnDelete(DeleteBehavior.Restrict);
        });
        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>
/// کارخانهٔ زمان طراحی مهاجرت.
/// </summary>
public sealed class ReturnsDbContextFactory : IDesignTimeDbContextFactory<ReturnsDbContext>
{
    /// <inheritdoc />
    public ReturnsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ReturnsDbContext>()
            .UseNpgsql("Host=127.0.0.1;Database=tooba_design;Username=tooba;Password=dev-placeholder")
            .Options;
        return new ReturnsDbContext(options);
    }
}
