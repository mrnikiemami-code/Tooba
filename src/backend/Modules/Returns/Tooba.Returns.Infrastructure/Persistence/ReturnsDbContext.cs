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

    /// <summary>TB-279: تاریخچهٔ افزایشی تصمیم‌های بازبینی‌شدهٔ هر پرونده.</summary>
    public DbSet<ReturnDecisionRevision> ReturnDecisionRevisions => Set<ReturnDecisionRevision>();

    /// <summary>TB-279: برنامهٔ حل‌وفصل مقداری هر خط (پذیرفته/ردشده).</summary>
    public DbSet<ReturnItemResolution> ReturnItemResolutions => Set<ReturnItemResolution>();

    /// <summary>TB-279: تحویل واقعی مقدار ردشده به مشتری مالک.</summary>
    public DbSet<ReturnItemCustody> ReturnItemCustodies => Set<ReturnItemCustody>();

    /// <summary>TB-279: تخصیص مقدار مرجوعی به برش‌های تحویل مالک.</summary>
    public DbSet<ReturnDeliveryAllocation> ReturnDeliveryAllocations => Set<ReturnDeliveryAllocation>();

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
            // TB-279: the reviewed approved money, frozen before the first money step; null = never frozen (legacy).
            // TB-279-R1 (F4): the column name is pinned to the module's canonical snake_case so the model and the applied
            // schema can never disagree again (the original migration wrote "ApprovedAmount" and needed a repair).
            entity.Property(x => x.ApprovedAmount).HasPrecision(18, 4).HasColumnName("approved_amount");
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
            // TB-279-R1 (F2): per-portion durable physical completion. Null = not recorded yet; a mixed line can never
            // prove two conditions from one coarse enum.
            entity.Property(x => x.UsableRestockedQuantity).HasColumnType("numeric(18,6)")
                .HasColumnName("usable_restocked_quantity");
            entity.Property(x => x.DamagedRecordedQuantity).HasColumnType("numeric(18,6)")
                .HasColumnName("damaged_recorded_quantity");
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

        modelBuilder.Entity<ReturnDecisionRevision>(entity =>
        {
            entity.ToTable("return_request_decision_revisions");
            entity.HasKey(x => new { x.ReturnRequestId, x.Version })
                .HasName("pk_return_request_decision_revisions");
            entity.Property(x => x.ReturnRequestId).ValueGeneratedNever().HasColumnName("return_request_id");
            entity.Property(x => x.Version).ValueGeneratedNever().HasColumnName("revision_version");
            entity.Property(x => x.Decision).HasMaxLength(32).HasColumnName("decision");
            entity.Property(x => x.Reason).HasMaxLength(512).HasColumnName("reason");
            entity.Property(x => x.ActorUserId).HasColumnName("actor_user_id");
            entity.Property(x => x.OccurrenceKey).HasMaxLength(128).HasColumnName("occurrence_key");
            entity.Property(x => x.RecordedAt).HasColumnName("recorded_at");
            // One reviewed occurrence records exactly one revision, so a duplicate/lost-response POST replays its own row.
            entity.HasIndex(x => new { x.ReturnRequestId, x.OccurrenceKey })
                .IsUnique()
                .HasDatabaseName("ix_return_request_decision_revisions_occurrence_key");
            entity.HasOne<ReturnRequest>()
                .WithMany()
                .HasForeignKey(x => x.ReturnRequestId)
                .HasConstraintName("fk_return_request_decision_revisions_requests")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReturnItemResolution>(entity =>
        {
            entity.ToTable(
                "return_item_resolutions",
                table =>
                {
                    // A resolution states a real non-negative split of one requested line, and it can never accept more
                    // decimals than the persisted quantity scale stores.
                    table.HasCheckConstraint(
                        "ck_return_item_resolutions_accepted_non_negative",
                        "accepted_quantity >= 0");
                    table.HasCheckConstraint(
                        "ck_return_item_resolutions_rejected_non_negative",
                        "rejected_quantity >= 0");
                    table.HasCheckConstraint(
                        "ck_return_item_resolutions_reason_present",
                        "length(btrim(reason)) > 0");
                });
            entity.HasKey(x => new { x.ReturnRequestId, x.ReturnItemId, x.Version })
                .HasName("pk_return_item_resolutions");
            entity.Property(x => x.ReturnRequestId).ValueGeneratedNever().HasColumnName("return_request_id");
            entity.Property(x => x.ReturnItemId).ValueGeneratedNever().HasColumnName("return_item_id");
            entity.Property(x => x.Version).ValueGeneratedNever().HasColumnName("resolution_version");
            entity.Property(x => x.AcceptedQuantity).HasColumnType("numeric(18,6)").HasColumnName("accepted_quantity");
            entity.Property(x => x.RejectedQuantity).HasColumnType("numeric(18,6)").HasColumnName("rejected_quantity");
            // TB-279-R1 (F2/F3): the explicit per-condition portions. Nullable so a pre-R1 row keeps its original bytes
            // and is read as an incomplete plan instead of a guessed usable-first split.
            entity.Property(x => x.AcceptedUsableQuantity).HasColumnType("numeric(18,6)")
                .HasColumnName("accepted_usable_quantity");
            entity.Property(x => x.AcceptedDamagedQuantity).HasColumnType("numeric(18,6)")
                .HasColumnName("accepted_damaged_quantity");
            entity.Property(x => x.RejectedReceivedUsableQuantity).HasColumnType("numeric(18,6)")
                .HasColumnName("rejected_received_usable_quantity");
            entity.Property(x => x.RejectedReceivedDamagedQuantity).HasColumnType("numeric(18,6)")
                .HasColumnName("rejected_received_damaged_quantity");
            entity.Property(x => x.RejectedUnreceivedQuantity).HasColumnType("numeric(18,6)")
                .HasColumnName("rejected_unreceived_quantity");
            entity.Property(x => x.Reason).HasMaxLength(512).HasColumnName("reason");
            entity.Property(x => x.ActorUserId).HasColumnName("actor_user_id");
            entity.Property(x => x.OccurrenceKey).HasMaxLength(128).HasColumnName("occurrence_key");
            entity.Property(x => x.RecordedAt).HasColumnName("recorded_at");
            entity.HasIndex(x => new { x.ReturnRequestId, x.OccurrenceKey })
                .HasDatabaseName("ix_return_item_resolutions_occurrence_key");
            entity.HasOne<ReturnRequest>()
                .WithMany()
                .HasForeignKey(x => x.ReturnRequestId)
                .HasConstraintName("fk_return_item_resolutions_requests")
                .OnDelete(DeleteBehavior.Cascade);
            // LOCAL composite ownership: a resolution line always belongs to an item of ITS OWN case.
            entity.HasOne<ReturnItem>()
                .WithMany()
                .HasForeignKey(x => new { x.ReturnRequestId, x.ReturnItemId })
                .HasPrincipalKey(x => new { x.ReturnRequestId, x.ReturnItemId })
                .HasConstraintName("fk_return_item_resolutions_items_owning_case")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReturnItemCustody>(entity =>
        {
            entity.ToTable(
                "return_item_custody",
                table =>
                {
                    table.HasCheckConstraint("ck_return_item_custody_quantity_positive", "quantity > 0");
                    table.HasCheckConstraint(
                        "ck_return_item_custody_condition_defined",
                        "condition IN ('Usable', 'Damaged')");
                    table.HasCheckConstraint(
                        "ck_return_item_custody_proof_present",
                        "length(btrim(proof_reference)) > 0");
                });
            entity.HasKey(x => x.ReturnCustodyId).HasName("pk_return_item_custody");
            entity.Property(x => x.ReturnCustodyId).ValueGeneratedNever().HasColumnName("return_custody_id");
            entity.Property(x => x.ReturnRequestId).HasColumnName("return_request_id");
            entity.Property(x => x.ReturnItemId).HasColumnName("return_item_id");
            entity.Property(x => x.Quantity).HasColumnType("numeric(18,6)").HasColumnName("quantity");
            entity.Property(x => x.Condition).HasMaxLength(32).HasColumnName("condition");
            entity.Property(x => x.RecipientUserId).HasColumnName("recipient_user_id");
            entity.Property(x => x.ProofReference).HasMaxLength(256).HasColumnName("proof_reference");
            // TB-279-R1 (F3): the reviewed proof KIND. Nullable so a pre-R1 row keeps its historical bytes.
            entity.Property(x => x.ProofKind).HasMaxLength(32).HasColumnName("proof_kind");
            entity.Property(x => x.ActorUserId).HasColumnName("actor_user_id");
            entity.Property(x => x.OccurrenceKey).HasMaxLength(128).HasColumnName("occurrence_key");
            entity.Property(x => x.RecordedAt).HasColumnName("recorded_at");
            entity.HasIndex(x => new { x.ReturnRequestId, x.OccurrenceKey })
                .HasDatabaseName("ix_return_item_custody_occurrence_key");
            entity.HasIndex(x => new { x.ReturnRequestId, x.ReturnItemId })
                .HasDatabaseName("ix_return_item_custody_owning_item");
            entity.HasOne<ReturnRequest>()
                .WithMany()
                .HasForeignKey(x => x.ReturnRequestId)
                .HasConstraintName("fk_return_item_custody_requests")
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ReturnItem>()
                .WithMany()
                .HasForeignKey(x => new { x.ReturnRequestId, x.ReturnItemId })
                .HasPrincipalKey(x => new { x.ReturnRequestId, x.ReturnItemId })
                .HasConstraintName("fk_return_item_custody_items_owning_case")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReturnDeliveryAllocation>(entity =>
        {
            entity.ToTable(
                "return_delivery_allocations",
                table =>
                {
                    table.HasCheckConstraint("ck_return_delivery_allocations_quantity_positive", "quantity > 0");
                    table.HasCheckConstraint(
                        "ck_return_delivery_allocations_slice_present",
                        "length(btrim(slice_key)) > 0");
                });
            entity.HasKey(x => x.ReturnAllocationId).HasName("pk_return_delivery_allocations");
            entity.Property(x => x.ReturnAllocationId).ValueGeneratedNever().HasColumnName("return_allocation_id");
            entity.Property(x => x.ReturnRequestId).HasColumnName("return_request_id");
            entity.Property(x => x.ReturnItemId).HasColumnName("return_item_id");
            entity.Property(x => x.OrderLineId).HasColumnName("order_line_id");
            entity.Property(x => x.SliceKey).HasMaxLength(128).HasColumnName("slice_key");
            entity.Property(x => x.Quantity).HasColumnType("numeric(18,6)").HasColumnName("quantity");
            entity.Property(x => x.DeliveredAt).HasColumnName("delivered_at");
            entity.Property(x => x.ReleasedAt).HasColumnName("released_at");
            entity.Property(x => x.RecordedAt).HasColumnName("recorded_at");
            entity.HasIndex(x => new { x.ReturnRequestId, x.ReturnItemId })
                .HasDatabaseName("ix_return_delivery_allocations_owning_item");
            entity.HasIndex(x => new { x.OrderLineId, x.SliceKey })
                .HasDatabaseName("ix_return_delivery_allocations_owning_slice");
            // The same case line can never claim the same source slice twice.
            entity.HasIndex(x => new { x.ReturnRequestId, x.ReturnItemId, x.SliceKey })
                .IsUnique()
                .HasDatabaseName("ix_return_delivery_allocations_owning_case_slice");
            entity.HasOne<ReturnRequest>()
                .WithMany()
                .HasForeignKey(x => x.ReturnRequestId)
                .HasConstraintName("fk_return_delivery_allocations_requests")
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ReturnItem>()
                .WithMany()
                .HasForeignKey(x => new { x.ReturnRequestId, x.ReturnItemId })
                .HasPrincipalKey(x => new { x.ReturnRequestId, x.ReturnItemId })
                .HasConstraintName("fk_return_delivery_allocations_items_owning_case")
                .OnDelete(DeleteBehavior.Restrict);
        });
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
