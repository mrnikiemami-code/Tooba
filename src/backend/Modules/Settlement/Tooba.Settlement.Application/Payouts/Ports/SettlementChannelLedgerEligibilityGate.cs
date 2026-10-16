namespace Tooba.Settlement.Application.Payouts.Ports;

/// <summary>
/// نتیجهٔ دروازهٔ per-event ثبت ledger تسویه برای یک رویداد پرداخت موفق. این دروازه فقط اجازهٔ ثبت را
/// می‌دهد یا رد می‌کند؛ هیچ مقدار مالی، سهم carrier/فروشنده/فروشگاه یا payout نمی‌سازد.
/// </summary>
public enum SettlementChannelLedgerEligibility
{
    /// <summary>فروش این پرداخت کانال بازار (Marketplace) نیست؛ هیچ ثبتی مجاز نیست.</summary>
    SaleChannelNotMarketplace = 0,

    /// <summary>schema پایدار تسویه فعال نشده؛ ثبت ممکن نیست و باید fail-closed و بدون payout رد شود.</summary>
    SchemaNotReady = 1,

    /// <summary>شرایط کانال و آمادگی schema هر دو تأیید شد؛ ثبت ledger مجاز است.</summary>
    Eligible = 2,
}

/// <summary>
/// دروازهٔ per-event: آیا می‌توان ledger تسویه را برای این پرداخت ثبت کرد؟ تصمیم از حقیقت پایدار Order
/// (کانال فروش) و آمادگی schema تسویه گرفته می‌شود و هیچ relation ناموجودی پرس‌وجو نمی‌شود تا شکست
/// handler، حمل‌ونقل retrying را مسموم نکند.
/// </summary>
public interface ISettlementChannelLedgerEligibilityGate
{
    /// <summary>
    /// تصمیم را برای checkout پایدار رویداد می‌سازد. <c>null</c> نباید بازگردد تا مصرف‌کننده مجبور به
    /// حدس نباشد.
    /// </summary>
    Task<SettlementChannelLedgerEligibility> EvaluateAsync(
        Guid checkoutId,
        CancellationToken cancellationToken);
}

/// <summary>
/// درز صریح آمادگی schema پایدار تسویه. اگر relation ledger فعال نشده باشد، ثبت باید fail-closed و
/// بدون استثنا رد شود تا حمل‌ونقل retrying مسموم نشود.
/// </summary>
public interface ISettlementLedgerSchemaReadiness
{
    /// <summary>آیا relation پایدار ledger هزینهٔ ارسال برای ثبت آماده است.</summary>
    Task<bool> IsLedgerReadyAsync(CancellationToken cancellationToken);
}
