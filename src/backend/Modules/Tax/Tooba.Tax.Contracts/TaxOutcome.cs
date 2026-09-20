namespace Tooba.Tax.Domain;

/// <summary>
/// نتیجهٔ محاسبهٔ مالیات. معافیت، نرخ صفر، نبودن قاعده و خطای محاسبه یکی نیستند.
/// Owned public contract type (Tooba.Tax.Contracts assembly) for cross-module use.
/// </summary>
public enum TaxOutcome
{
    /// <summary>
    /// قاعدهٔ درصدی اعمال شد و مبلغ مالیات جدا از قیمت پایه است.
    /// </summary>
    Taxable = 0,

    /// <summary>
    /// معافیت صریح؛ صفر شدن مبلغ به‌معنای نرخ صفر یا نبودن قاعده نیست.
    /// </summary>
    Exempt = 1,

    /// <summary>
    /// قاعدهٔ قابل‌اعمال با نرخ صفر؛ معافیت یا خطای پیکربندی نیست.
    /// </summary>
    ZeroRated = 2,

    /// <summary>
    /// هیچ قاعدهٔ قابل‌اعمالی برای حوزه/بازار/طبقه در زمان محاسبات پیدا نشد.
    /// </summary>
    NoApplicableRule = 3,

    /// <summary>
    /// محاسبه به‌خاطر ابهام قاعده، ارز ناسازگار یا دادهٔ نامعتبر شکست خورد.
    /// </summary>
    CalculationError = 4,
}
