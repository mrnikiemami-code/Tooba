using Tooba.BuildingBlocks;
using Tooba.Promotion.Contracts.Errors;

namespace Tooba.Promotion.Domain.Merchandising;

/// <summary>
/// کمپین مرچندایزینگ فروشگاهی؛ جدا از تعریف تخفیف تسویه (<see cref="PromotionDefinition"/>).
/// </summary>
public sealed class MerchandisingCampaign
{
    /// <summary>سازندهٔ EF.</summary>
    private MerchandisingCampaign()
    {
    }

    /// <summary>شناسهٔ UuidV7.</summary>
    public Guid Id { get; init; }

    /// <summary>گونهٔ مرچندایزینگ.</summary>
    public Guid PromotionTypeId { get; private set; }

    /// <summary>محدودهٔ فروشگاه.</summary>
    public Guid StoreId { get; init; }

    /// <summary>وضعیت چرخهٔ عمر.</summary>
    public MerchandisingCampaignLifecycleStatus LifecycleStatus { get; private set; }

    /// <summary>شروع پنجرهٔ UTC.</summary>
    public DateTimeOffset StartAt { get; private set; }

    /// <summary>پایان اختیاری UTC؛ تهی یعنی باز.</summary>
    public DateTimeOffset? EndAt { get; private set; }

    /// <summary>اولویت بین کمپین‌های هم‌نوع؛ عدد بزرگ‌تر زودتر.</summary>
    public int Priority { get; private set; }

    /// <summary>ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>به‌روزرسانی UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// کمپین پیش‌نویس می‌سازد.
    /// </summary>
    public static MerchandisingCampaign Create(
        Guid id,
        Guid promotionTypeId,
        Guid storeId,
        DateTimeOffset startAt,
        DateTimeOffset? endAt,
        int priority,
        DateTimeOffset now)
    {
        if (id == Guid.Empty)
        {
            throw new ContractOperationException(PromotionErrorCodes.CampaignIdRequired);
        }

        if (promotionTypeId == Guid.Empty)
        {
            throw new ContractOperationException(PromotionErrorCodes.CampaignTypeRequired);
        }

        if (storeId == Guid.Empty)
        {
            throw new ContractOperationException(PromotionErrorCodes.CampaignStoreRequired);
        }

        ValidateWindow(startAt, endAt);

        return new MerchandisingCampaign
        {
            Id = id,
            PromotionTypeId = promotionTypeId,
            StoreId = storeId,
            LifecycleStatus = MerchandisingCampaignLifecycleStatus.Draft,
            StartAt = startAt,
            EndAt = endAt,
            Priority = priority,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>
    /// وضعیت چرخهٔ عمر را برای upsert دانهٔ Development تنظیم می‌کند (فقط seed).
    /// </summary>
    public void ForceLifecycleForSeed(MerchandisingCampaignLifecycleStatus status, DateTimeOffset now)
    {
        LifecycleStatus = status;
        UpdatedAt = now;
    }

    /// <summary>
    /// پنجره و اولویت را به‌روز می‌کند.
    /// </summary>
    public void UpdateWindow(DateTimeOffset startAt, DateTimeOffset? endAt, int priority, DateTimeOffset now)
    {
        ValidateWindow(startAt, endAt);
        StartAt = startAt;
        EndAt = endAt;
        Priority = priority;
        UpdatedAt = now;
    }

    /// <summary>
    /// کمپین را منتشر می‌کند.
    /// </summary>
    public void Publish(DateTimeOffset now)
    {
        if (LifecycleStatus == MerchandisingCampaignLifecycleStatus.Archived)
        {
            throw new ContractOperationException(PromotionErrorCodes.CampaignArchivedCannotPublish);
        }

        LifecycleStatus = MerchandisingCampaignLifecycleStatus.Published;
        UpdatedAt = now;
    }

    /// <summary>
    /// کمپین را بایگانی می‌کند. عضویت Offer را حذف نمی‌کند مگر فراخوان صریح.
    /// </summary>
    public void Archive(DateTimeOffset now)
    {
        LifecycleStatus = MerchandisingCampaignLifecycleStatus.Archived;
        UpdatedAt = now;
    }

    /// <summary>
    /// آیا در لحظهٔ داده‌شده برای ریل فروشگاهی فعال است (مشتق از وضعیت + پنجره).
    /// </summary>
    public bool IsRuntimeActive(DateTimeOffset now) =>
        LifecycleStatus == MerchandisingCampaignLifecycleStatus.Published
        && now >= StartAt
        && (EndAt is null || now < EndAt);

    private static void ValidateWindow(DateTimeOffset startAt, DateTimeOffset? endAt)
    {
        if (endAt is not null && endAt <= startAt)
        {
            throw new ContractOperationException(PromotionErrorCodes.CampaignWindowInvalid);
        }
    }
}
