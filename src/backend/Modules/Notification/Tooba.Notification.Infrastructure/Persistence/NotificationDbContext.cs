using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Notification.Domain;
using Tooba.Persistence;

namespace Tooba.Notification.Infrastructure.Persistence;

/// <summary>
/// DbContext مالک schema <c>notification</c>. سفارش و پرداخت را نگه نمی‌دارد.
/// </summary>
public sealed class NotificationDbContext : DbContext
{
    /// <summary>schema اختصاصی Notification.</summary>
    public const string Schema = "notification";

    /// <summary>DbContext را با گزینه‌های Host می‌سازد.</summary>
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
        : base(options)
    {
    }

    /// <summary>اعلان‌های تراکنشی.</summary>
    public DbSet<UserNotification> Notifications => Set<UserNotification>();

    /// <summary>Outbox همین ماژول (مصرف‌کننده؛ فعلاً emit ندارد).</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<UserNotification>(entity =>
        {
            entity.ToTable("user_notifications");
            entity.HasKey(x => x.NotificationId);
            entity.Property(x => x.NotificationId).ValueGeneratedNever();
            entity.Property(x => x.RecipientKind).HasConversion<int>();
            entity.Property(x => x.Type).HasMaxLength(64).IsRequired();
            entity.Property(x => x.PayloadJson).HasColumnType("text").IsRequired();
            entity.Property(x => x.TargetRoute).HasMaxLength(512).IsRequired();
            entity.Property(x => x.SourceEventId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.SourceType).HasMaxLength(128).IsRequired();
            // یک رویداد می‌تواند چند گیرنده داشته باشد؛ idempotency per-recipient است.
            entity.HasIndex(x => new { x.RecipientKind, x.RecipientPartyId, x.SourceEventId }).IsUnique();
            entity.HasIndex(x => new { x.RecipientKind, x.RecipientPartyId, x.CreatedAt });
            entity.HasIndex(x => new { x.RecipientKind, x.RecipientActorUserId, x.CreatedAt });
            entity.HasIndex(x => new { x.RecipientKind, x.RecipientPartyId, x.IsRead, x.IsDeleted });
        });
        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>کارخانهٔ زمان طراحی مهاجرت.</summary>
public sealed class NotificationDbContextFactory : IDesignTimeDbContextFactory<NotificationDbContext>
{
    /// <inheritdoc />
    public NotificationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<NotificationDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            ToobaNpgsql.DesignTimeConnectionString(),
            NotificationDbContext.Schema,
            typeof(NotificationDbContext));
        return new NotificationDbContext(options.Options);
    }
}
