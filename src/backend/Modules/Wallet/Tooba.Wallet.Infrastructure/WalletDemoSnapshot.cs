namespace Tooba.Wallet.Infrastructure;

/// <summary>شناسه‌های پایدار دانهٔ توسعه Wallet.</summary>
public static class WalletDemoIds
{
    /// <summary>حساب کیف پول مشتری demo.</summary>
    public static readonly Guid AccountId = Guid.Parse("01900000-0000-7000-9000-000000000001");

    /// <summary>سطر تعدیل Admin.</summary>
    public static readonly Guid AdminAdjustmentEntryId = Guid.Parse("01900000-0000-7000-9000-000000000011");

    /// <summary>سطر اعتبار کارت هدیهٔ بخشی.</summary>
    public static readonly Guid GiftCreditEntryId = Guid.Parse("01900000-0000-7000-9000-000000000012");

    /// <summary>کارت هدیهٔ استفاده‌نشده.</summary>
    public static readonly Guid UnusedGiftCardId = Guid.Parse("01900000-0000-7000-9000-000000000021");

    /// <summary>کارت بخشی مصرف‌شده.</summary>
    public static readonly Guid PartiallyRedeemedGiftCardId = Guid.Parse("01900000-0000-7000-9000-000000000022");

    /// <summary>کارت منقضی.</summary>
    public static readonly Guid ExpiredGiftCardId = Guid.Parse("01900000-0000-7000-9000-000000000023");

    /// <summary>کارت باطل‌شده.</summary>
    public static readonly Guid RevokedGiftCardId = Guid.Parse("01900000-0000-7000-9000-000000000024");

    /// <summary>بازخرید بخشی.</summary>
    public static readonly Guid PartialRedemptionId = Guid.Parse("01900000-0000-7000-9000-000000000031");

    /// <summary>کارت هدیهٔ استفاده‌نشدهٔ یدکی (اگر کارت اصلی در Dev مصرف شده باشد).</summary>
    public static readonly Guid SpareUnusedGiftCardId = Guid.Parse("01900000-0000-7000-9000-000000000025");

    /// <summary>کد نمایشی کارت استفاده‌نشده (فقط demo-preview).</summary>
    public const string UnusedGiftCardDemoCode = "TOOBA-DEMO-GIFT-500K";

    /// <summary>کد یدکی استفاده‌نشده برای USER-PREVIEW پس از smoke.</summary>
    public const string SpareUnusedGiftCardDemoCode = "TOOBA-DEMO-GIFT-SPARE";

    /// <summary>کد کارت منقضی (برای تست reject).</summary>
    public const string ExpiredGiftCardDemoCode = "TOOBA-DEMO-GIFT-EXPIRED";

    /// <summary>کد کارت باطل (برای تست reject).</summary>
    public const string RevokedGiftCardDemoCode = "TOOBA-DEMO-GIFT-REVOKED";

    /// <summary>کد کارت بخشی (دیگر قابل بازخرید کامل نیست اگر مانده صفر).</summary>
    public const string PartialGiftCardDemoCode = "TOOBA-DEMO-GIFT-PARTIAL";
}

/// <summary>snapshot پیش‌نمایش توسعه.</summary>
public sealed record WalletDemoSnapshot(
    Guid CustomerActorUserId,
    Guid AccountId,
    decimal Balance,
    Guid UnusedGiftCardId,
    string UnusedGiftCardDemoCode,
    Guid PartiallyRedeemedGiftCardId,
    Guid ExpiredGiftCardId,
    Guid RevokedGiftCardId,
    string Note);

/// <summary>نگهدارندهٔ snapshot پس از دانه.</summary>
public static class WalletDemoSnapshotStore
{
    private static WalletDemoSnapshot? _current;

    /// <summary>آخرین snapshot؛ null اگر آماده نباشد.</summary>
    public static WalletDemoSnapshot? Current => _current;

    /// <summary>snapshot را منتشر می‌کند.</summary>
    public static void Publish(WalletDemoSnapshot snapshot) => _current = snapshot;
}
