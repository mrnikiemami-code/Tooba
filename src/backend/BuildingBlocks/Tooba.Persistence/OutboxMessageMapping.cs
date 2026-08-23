using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tooba.Persistence;

/// <summary>
/// نگاشت per-module جدول <c>outbox_messages</c> داخل schema همان ماژول. یک جدول فیزیکی سراسری نیست.
/// </summary>
public static class OutboxMessageMapping
{
    /// <summary>
    /// نام فیزیکی قرارداد Outbox در هر schema ماژول.
    /// </summary>
    public const string TableName = "outbox_messages";

    /// <summary>
    /// موجودیت <see cref="OutboxMessage"/> را به schema داده‌شده می‌چسباند.
    /// </summary>
    public static void Map(ModelBuilder modelBuilder, string schema, string tableName = TableName)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        SqlIdentifiers.EnsureSafe(schema);
        SqlIdentifiers.EnsureSafe(tableName);

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable(tableName, schema);
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.EventType).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Payload).IsRequired();
            entity.Property(x => x.CorrelationId).HasMaxLength(128);
            entity.Property(x => x.TenantId).HasMaxLength(128);
            entity.Property(x => x.DeploymentId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Edition).HasMaxLength(32).IsRequired();
            entity.Property(x => x.LastError).HasMaxLength(256);
            entity.HasIndex(x => x.OccurredAt)
                .HasDatabaseName("ix_outbox_messages_pending")
                .HasFilter("processed_at IS NULL AND dead_lettered_at IS NULL");
        });
    }
}

/// <summary>
/// اعتبارسنجی شناسهٔ SQL تا schema/table از پیکربندی به injection تبدیل نشود.
/// </summary>
public static class SqlIdentifiers
{
    private static readonly Regex Safe = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    /// <summary>
    /// شناسه را می‌پذیرد یا استثنا می‌اندازد. برای درونی‌سازی در SQL پویا است نه ورودی کاربر نهایی.
    /// </summary>
    public static void EnsureSafe(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier) || !Safe.IsMatch(identifier))
        {
            throw new InvalidOperationException("Unsafe SQL identifier rejected.");
        }
    }

    /// <summary>
    /// شناسه را با کوتیشن PostgreSQL برمی‌گرداند.
    /// </summary>
    public static string Quote(string identifier)
    {
        EnsureSafe(identifier);
        return "\"" + identifier + "\"";
    }
}
