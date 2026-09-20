using Tooba.BuildingBlocks;

namespace Tooba.Tax.Domain;

/// <summary>
/// گونهٔ قاعده. نرخ درصد در کد حوزهٔ مالیاتی قفل نمی‌شود.
/// </summary>
public enum TaxRuleKind
{
    /// <summary>
    /// درصد پیکربندی‌شده روی مبلغ بدون مالیات.
    /// </summary>
    Percentage = 0,

    /// <summary>
    /// معافیت صریح با دلیل؛ نرخ صفر ساختگی نیست.
    /// </summary>
    Exempt = 1,

    /// <summary>
    /// نرخ صفرِ قابل‌اعمال.
    /// </summary>
    ZeroRated = 2,
}

/// <summary>
/// وضعیت انتشار قاعده.
/// </summary>
public enum TaxRuleStatus
{
    /// <summary>
    /// پیش‌نویس؛ در محاسبه شرکت نمی‌کند.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// فعال در پنجرهٔ اعتبار.
    /// </summary>
    Active = 1,

    /// <summary>
    /// بازنشسته.
    /// </summary>
    Retired = 2,
}

/// <summary>
/// سیاست بازنویسی نرخ. مشتری/درخواست HTTP نرخ را تزریق نمی‌کند.
/// </summary>
public enum TaxOverridePolicy
{
    /// <summary>
    /// بازنویسی ممنوع است.
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// فقط مسیر داخلی معتمد با پرچم صریح سرور.
    /// </summary>
    TrustedInternal = 1,
}
