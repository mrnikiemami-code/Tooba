using Tooba.BuildingBlocks;
using Tooba.Promotion.Contracts.Errors;

namespace Tooba.Promotion.Domain.Merchandising;

/// <summary>
/// گونهٔ مرجع مرچندایزینگ. هویت پایدار ماشین با Code است نه enum ذخیره‌شده.
/// </summary>
public sealed class MerchandisingPromotionType
{
    /// <summary>کد سیستمی پیشنهاد شگفت‌انگیز.</summary>
    public const string AmazingCode = "AMAZING";

    /// <summary>سازندهٔ EF.</summary>
    private MerchandisingPromotionType()
    {
    }

    /// <summary>شناسهٔ UuidV7.</summary>
    public Guid Id { get; init; }

    /// <summary>کد پایدار ماشین (مثلاً AMAZING).</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>آیا گونهٔ سیستمی است و قابل تغییر/حذف عادی نیست.</summary>
    public bool IsSystem { get; private set; }

    /// <summary>فعال بودن برای استفادهٔ جدید.</summary>
    public bool IsActive { get; private set; }

    /// <summary>ترتیب نمایش مرجع.</summary>
    public int SortOrder { get; private set; }

    /// <summary>ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>به‌روزرسانی UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// گونهٔ سیستمی می‌سازد. Code پس از ایجاد برای سیستم قابل تغییر نیست.
    /// </summary>
    public static MerchandisingPromotionType CreateSystem(
        Guid id,
        string code,
        int sortOrder,
        DateTimeOffset now)
    {
        if (id == Guid.Empty)
        {
            throw new ContractOperationException(PromotionErrorCodes.TypeIdRequired);
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ContractOperationException(PromotionErrorCodes.TypeCodeRequired);
        }

        return new MerchandisingPromotionType
        {
            Id = id,
            Code = code.Trim().ToUpperInvariant(),
            IsSystem = true,
            IsActive = true,
            SortOrder = sortOrder,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>
    /// گونهٔ غیراجباری می‌سازد.
    /// </summary>
    public static MerchandisingPromotionType Create(
        Guid id,
        string code,
        int sortOrder,
        bool isActive,
        DateTimeOffset now)
    {
        if (id == Guid.Empty)
        {
            throw new ContractOperationException(PromotionErrorCodes.TypeIdRequired);
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ContractOperationException(PromotionErrorCodes.TypeCodeRequired);
        }

        return new MerchandisingPromotionType
        {
            Id = id,
            Code = code.Trim().ToUpperInvariant(),
            IsSystem = false,
            IsActive = isActive,
            SortOrder = sortOrder,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>
    /// کد را عوض می‌کند. گونه‌های سیستمی قابل تغییر کد نیستند.
    /// </summary>
    public void RenameCode(string newCode, DateTimeOffset now)
    {
        if (IsSystem)
        {
            throw new ContractOperationException(PromotionErrorCodes.TypeSystemCodeImmutable);
        }

        if (string.IsNullOrWhiteSpace(newCode))
        {
            throw new ContractOperationException(PromotionErrorCodes.TypeCodeRequired);
        }

        Code = newCode.Trim().ToUpperInvariant();
        UpdatedAt = now;
    }

    /// <summary>
    /// حذف منطقی را برای گونهٔ سیستمی رد می‌کند.
    /// </summary>
    public void EnsureCanDelete()
    {
        if (IsSystem)
        {
            throw new ContractOperationException(PromotionErrorCodes.TypeSystemDeleteForbidden);
        }
    }

    /// <summary>
    /// ترتیب یا فعال بودن را به‌روز می‌کند بدون دست زدن به Code سیستمی.
    /// </summary>
    public void UpdateMeta(int sortOrder, bool isActive, DateTimeOffset now)
    {
        SortOrder = sortOrder;
        IsActive = isActive;
        UpdatedAt = now;
    }
}
