using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NodaTime;
using Tooba.BuildingBlocks;
using Tooba.Persistence;

namespace Tooba.PlatformProbe.Infrastructure.Persistence;

/// <summary>
/// ردیف نمونهٔ خنثی در schema <c>platform_probe</c>. قرارداد Catalog/Identity نیست و FK به ماژول دیگر ندارد.
/// </summary>
public sealed class PlatformProbeRecord
{
    /// <summary>
    /// کلید UUID v7 تولیدشده در دامنه؛ پایگاه مقدار را تولید نمی‌کند.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// زمان ایجاد به‌صورت Instant (UTC مفهومی NodaTime).
    /// </summary>
    public Instant CreatedAt { get; set; }

    /// <summary>
    /// ارجاع اختیاری UUID بدون رابطهٔ دیتابیس؛ برای اثبات «بدون FK بین‌ماژول».
    /// </summary>
    public Guid? ExternalReference { get; set; }
}

/// <summary>
/// DbContext اختصاصی PlatformProbe. schema جدا است تا الگوی مالکیت داده قبل از ماژول‌های واقعی قفل شود.
/// این نمونه disposable است و الگوی تجاری Catalog نیست.
/// </summary>
public sealed class PlatformProbeDbContext : DbContext
{
    /// <summary>
    /// نام schema مالک این ماژول؛ ماژول دیگر نباید در آن بنویسد.
    /// </summary>
    public const string Schema = "platform_probe";

    /// <summary>
    /// DbContext را با گزینه‌های ازپیش‌پیکربندی‌شده می‌سازد.
    /// </summary>
    public PlatformProbeDbContext(DbContextOptions<PlatformProbeDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// مجموعهٔ ردیف‌های probe؛ جدول کسب‌وکار نیست.
    /// </summary>
    public DbSet<PlatformProbeRecord> Records => Set<PlatformProbeRecord>();

    /// <summary>
    /// نگاشت جدول <c>probe_records</c> بدون رابطه به schema دیگر.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<PlatformProbeRecord>(entity =>
        {
            entity.ToTable("probe_records");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.ExternalReference);
        });
    }
}

/// <summary>
/// کارخانهٔ design-time برای ابزار EF. از متغیر <see cref="ToobaNpgsql.DesignTimeConnectionVariable"/> می‌خواند نه از Tenant درخواست.
/// </summary>
public sealed class PlatformProbeDbContextFactory : IDesignTimeDbContextFactory<PlatformProbeDbContext>
{
    /// <inheritdoc />
    public PlatformProbeDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PlatformProbeDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            ToobaNpgsql.DesignTimeConnectionString(),
            PlatformProbeDbContext.Schema,
            typeof(PlatformProbeDbContext));
        return new PlatformProbeDbContext(options.Options);
    }
}

/// <summary>
/// سازندهٔ ردیف نمونه با UUID v7 و ساعت سیستم. منطق کسب‌وکار ندارد.
/// </summary>
public static class PlatformProbePersistence
{
    /// <summary>
    /// یک <see cref="PlatformProbeRecord"/> جدید با شناسهٔ v7 می‌سازد.
    /// </summary>
    /// <param name="externalReference">ارجاع اختیاری بدون FK.</param>
    public static PlatformProbeRecord NewRecord(Guid? externalReference = null) => new()
    {
        Id = UuidV7.New(),
        CreatedAt = SystemClock.Instance.GetCurrentInstant(),
        ExternalReference = externalReference,
    };
}
