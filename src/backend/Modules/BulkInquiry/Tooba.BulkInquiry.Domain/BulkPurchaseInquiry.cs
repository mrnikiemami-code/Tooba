using System.Text.RegularExpressions;
using Tooba.BuildingBlocks;

namespace Tooba.BulkInquiry.Domain;

/// <summary>وضعیت چرخهٔ درخواست خرید عمده.</summary>
public enum BulkInquiryStatus
{
    /// <summary>ثبت‌شده و در انتظار پیگیری.</summary>
    Submitted = 0,
}

/// <summary>درخواست خرید عمده برای یک محصول منتشرشده؛ بدون قیمت یا تخفیف.</summary>
public sealed class BulkPurchaseInquiry
{
    /// <summary>حداقل طول نام فارسی/عربی پس از trim.</summary>
    public const int FullNameMinLength = 2;
    /// <summary>حداکثر طول نام پس از trim.</summary>
    public const int FullNameMaxLength = 100;
    /// <summary>طول دقیق شمارهٔ موبایل ایران.</summary>
    public const int PhoneLength = 11;
    /// <summary>حداکثر طول ایمیل.</summary>
    public const int EmailMaxLength = 256;
    /// <summary>حداکثر طول نام شرکت.</summary>
    public const int CompanyNameMaxLength = 200;
    /// <summary>حداقل طول نشانی.</summary>
    public const int AddressMinLength = 10;
    /// <summary>حداکثر طول نشانی.</summary>
    public const int AddressMaxLength = 512;
    /// <summary>حداقل مقدار سفارش.</summary>
    public const int QuantityMin = 10;
    /// <summary>حداکثر مقدار سفارش.</summary>
    public const int QuantityMax = 1000;
    /// <summary>حداکثر طول یادداشت.</summary>
    public const int NotesMaxLength = 2000;

    private static readonly Regex PersianNamePattern = new(@"^[\p{IsArabic}\s]{2,100}$", RegexOptions.Compiled);

    private BulkPurchaseInquiry() { }

    /// <summary>شناسهٔ پایدار درخواست.</summary>
    public Guid InquiryId { get; init; }
    /// <summary>مرجع opaque محصول در Catalog.</summary>
    public Guid ProductId { get; init; }
    /// <summary>نام کامل درخواست‌کننده.</summary>
    public string FullName { get; private set; } = string.Empty;
    /// <summary>شمارهٔ تماس ۱۱ رقمی شروع با ۰۹.</summary>
    public string Phone { get; private set; } = string.Empty;
    /// <summary>ایمیل اختیاری.</summary>
    public string? Email { get; private set; }
    /// <summary>نام شرکت اختیاری.</summary>
    public string? CompanyName { get; private set; }
    /// <summary>نشانی تحویل.</summary>
    public string Address { get; private set; } = string.Empty;
    /// <summary>مقدار درخواستی.</summary>
    public int Quantity { get; init; }
    /// <summary>یادداشت اختیاری.</summary>
    public string? Notes { get; private set; }
    /// <summary>وضعیت درخواست.</summary>
    public BulkInquiryStatus Status { get; init; }
    /// <summary>زمان ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>درخواست Submitted معتبر می‌سازد.</summary>
    public static BulkPurchaseInquiry Create(
        Guid productId,
        string fullName,
        string phone,
        string? email,
        string? companyName,
        string address,
        int quantity,
        string? notes,
        DateTimeOffset now)
    {
        if (productId == Guid.Empty) throw new InvalidOperationException("شناسهٔ محصول الزامی است.");

        var trimmedName = fullName?.Trim() ?? string.Empty;
        if (!PersianNamePattern.IsMatch(trimmedName))
            throw new InvalidOperationException("نام معتبر نیست.");

        var trimmedPhone = phone?.Trim() ?? string.Empty;
        if (trimmedPhone.Length != PhoneLength || !trimmedPhone.StartsWith("09", StringComparison.Ordinal) || !trimmedPhone.All(char.IsDigit))
            throw new InvalidOperationException("شمارهٔ تماس معتبر نیست.");

        if (quantity is < QuantityMin or > QuantityMax)
            throw new InvalidOperationException("مقدار درخواست معتبر نیست.");

        var trimmedAddress = address?.Trim() ?? string.Empty;
        if (trimmedAddress.Length < AddressMinLength || trimmedAddress.Length > AddressMaxLength)
            throw new InvalidOperationException("نشانی معتبر نیست.");

        var trimmedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        if (trimmedEmail?.Length > EmailMaxLength) throw new InvalidOperationException("ایمیل معتبر نیست.");

        var trimmedCompany = string.IsNullOrWhiteSpace(companyName) ? null : companyName.Trim();
        if (trimmedCompany?.Length > CompanyNameMaxLength) throw new InvalidOperationException("نام شرکت معتبر نیست.");

        var trimmedNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (trimmedNotes?.Length > NotesMaxLength) throw new InvalidOperationException("یادداشت معتبر نیست.");

        return new BulkPurchaseInquiry
        {
            InquiryId = UuidV7.New(),
            ProductId = productId,
            FullName = trimmedName,
            Phone = trimmedPhone,
            Email = trimmedEmail,
            CompanyName = trimmedCompany,
            Address = trimmedAddress,
            Quantity = quantity,
            Notes = trimmedNotes,
            Status = BulkInquiryStatus.Submitted,
            CreatedAt = now,
        };
    }
}
