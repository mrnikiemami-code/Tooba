namespace Tooba.Payment.Infrastructure;

/// <summary>
/// پیکربندی درگاه پرداخت: Payment:Gateway
/// </summary>
public sealed class PaymentGatewayOptions
{
    /// <summary>
    /// نام بخش پیکربندی.
    /// </summary>
    public const string SectionName = "Payment:Gateway";

    /// <summary>
    /// Sandbox (dev/test)، Webhook (production provider)، Disabled.
    /// </summary>
    public string Mode { get; set; } = "Sandbox";

    /// <summary>
    /// کد درگاه پیش‌فرض برای شروع پرداخت.
    /// </summary>
    public string DefaultProvider { get; set; } = "fake";

    /// <summary>
    /// راز امضای webhook/callback. در Production از env تزریق می‌شود.
    /// </summary>
    public string WebhookSigningSecret { get; set; } = "";

    /// <summary>
    /// پایهٔ URL شروع پرداخت نزد PSP (بدون انتخاب تجاری در کد).
    /// </summary>
    public string InitiateBaseUrl { get; set; } = "";

    /// <summary>
    /// پایهٔ URL پرس‌وجوی وضعیت PSP برای Verify مستقل از متن callback.
    /// </summary>
    public string StatusQueryBaseUrl { get; set; } = "";

    /// <summary>
    /// Bearer/API key برای StatusQuery.
    /// </summary>
    public string StatusQueryApiKey { get; set; } = "";

    /// <summary>
    /// میزبان‌های مجاز برای StatusQuery (SSRF fail-closed). خالی = فقط https و رد localhost/private.
    /// </summary>
    public string[] AllowedStatusQueryHosts { get; set; } = [];

    /// <summary>
    /// مهلت HTTP به ثانیه.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 15;

    /// <summary>
    /// حداکثر تلاش Verify برای خطاهای موقت (timeout/rate-limit/unavailable).
    /// </summary>
    public int VerifyMaxAttempts { get; set; } = 3;

    /// <summary>
    /// فعال‌سازی درگاه کارت‌به‌کارت/دستی برای این فروشگاه/محیط. پیش‌فرض خاموش.
    /// بدون دادهٔ کارت/حساب در سورس؛ فقط قابلیت روش پرداخت.
    /// </summary>
    public bool ManualCardToCardEnabled { get; set; }

    /// <summary>
    /// الزام آپلود مدرک واریز: Disabled / Optional / Required. سراسری سخت‌کد نیست.
    /// </summary>
    public string ManualProofRequirement { get; set; } = "Optional";

    /// <summary>
    /// دستورالعمل کارت/حساب قابل نمایش مشتری. شناسهٔ داخلی پیکربندی نیست.
    /// </summary>
    public string ManualPaymentInstructions { get; set; } =
        "مبلغ را به حساب اعلام‌شده فروشگاه واریز کنید و شماره پیگیری پرداخت را ثبت کنید.";

    /// <summary>
    /// نام فروشگاه برای صفحهٔ شبیه‌ساز سندباکس.
    /// </summary>
    public string StoreDisplayName { get; set; } = "Tooba";

    /// <summary>
    /// مدت نگهداری موجودی پس از ثبت مدرک/پیگیری کارت‌به‌کارت تا تأیید ادمین (ساعت). پیش‌فرض تجاری ۲۴.
    /// </summary>
    public int ManualPaymentReviewHoldHours { get; set; } = 24;

    /// <summary>
    /// مهلت‌های تأمین سفارش (platform/store). اولویت با override صریح روش پرداخت است.
    /// </summary>
    public OrderSupplyHoldOverrideOptions? OrderSupplyHoldOverrides { get; set; }

    /// <summary>
    /// مهلت اولیهٔ نگهداری قبل از ثبت مدرک دستی (ساعت) — foundation R7؛ زمان شروع Cart را عوض نمی‌کند.
    /// </summary>
    public int ManualPaymentInitialHoldHours { get; set; } = 2;

    /// <summary>
    /// مهلت نگهداری پس از commit سفارش آنلاین تا تکمیل درگاه (ساعت).
    /// </summary>
    public int OnlinePaymentHoldHours { get; set; } = 2;

    /// <summary>
    /// مهلت سبد/پیش‌سفارش (دقیقه) — فقط مستندسازی/تنظیم؛ شروع رزرو سبد در R7 تغییر نمی‌کند.
    /// </summary>
    public int CartHoldMinutes { get; set; } = 30;

    /// <summary>
    /// الزام مدرک را به مقدار پایدار نگاشت می‌کند.
    /// </summary>
    public string NormalizedManualProofRequirement()
    {
        var raw = (ManualProofRequirement ?? string.Empty).Trim();
        if (raw.Equals("Disabled", StringComparison.OrdinalIgnoreCase)
            || raw.Equals("Required", StringComparison.OrdinalIgnoreCase)
            || raw.Equals("Optional", StringComparison.OrdinalIgnoreCase))
        {
            return char.ToUpperInvariant(raw[0]) + raw[1..].ToLowerInvariant();
        }

        return "Optional";
    }
}

/// <summary>Override سطح فروشگاه برای مهلت‌های تأمین.</summary>
public sealed class OrderSupplyHoldOverrideOptions
{
    /// <summary>بازبینی دستی.</summary>
    /// <summary>بازبینی دستی.</summary>
    public int? ManualPaymentReviewHoldHours { get; set; }
    /// <summary>مهلت اولیه دستی.</summary>
    public int? ManualPaymentInitialHoldHours { get; set; }
    /// <summary>مهلت آنلاین.</summary>
    public int? OnlinePaymentHoldHours { get; set; }
    /// <summary>مهلت سبد.</summary>
    public int? CartHoldMinutes { get; set; }
}
