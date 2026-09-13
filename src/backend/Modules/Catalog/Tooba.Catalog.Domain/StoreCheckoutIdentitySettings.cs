namespace Tooba.Catalog.Domain;

/// <summary>سیاست هویت مشتری در فرایند خرید. پیش‌فرض ورود الزامی است.</summary>
public enum CheckoutIdentityPolicyKind
{
    /// <summary>ارسال، تسویه و پرداخت نیازمند نشست مشتری است.</summary>
    AuthenticatedOnly = 0,

    /// <summary>خرید مهمان مجاز است.</summary>
    GuestAllowed = 1,
}

/// <summary>یک ردیف تنظیم فروشگاه برای هویت فرایند خرید.</summary>
public sealed class StoreCheckoutIdentitySettings
{
    /// <summary>شناسه تک‌ردیفی.</summary>
    public static readonly Guid SingletonId = Guid.Parse("01900000-0000-7000-8000-00000000cc01");

    /// <summary>کلید ردیف.</summary>
    public Guid SettingsId { get; init; }

    /// <summary>سیاست مؤثر فروشگاه.</summary>
    public CheckoutIdentityPolicyKind Policy { get; private set; } = CheckoutIdentityPolicyKind.AuthenticatedOnly;

    /// <summary>زمان به‌روزرسانی.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>ردیف پیش‌فرض AuthenticatedOnly.</summary>
    public static StoreCheckoutIdentitySettings CreateDefault(DateTimeOffset now) => new()
    {
        SettingsId = SingletonId,
        Policy = CheckoutIdentityPolicyKind.AuthenticatedOnly,
        UpdatedAt = now,
    };

    /// <summary>سیاست را جایگزین می‌کند.</summary>
    public void Replace(CheckoutIdentityPolicyKind policy, DateTimeOffset now)
    {
        Policy = policy is CheckoutIdentityPolicyKind.GuestAllowed
            ? CheckoutIdentityPolicyKind.GuestAllowed
            : CheckoutIdentityPolicyKind.AuthenticatedOnly;
        UpdatedAt = now;
    }
}
