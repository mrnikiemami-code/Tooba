using Microsoft.Extensions.Configuration;

namespace Tooba.Settlement.Infrastructure.DependencyInjection;

/// <summary>
/// قابلیت استقرار برای ثبت ledger تسویهٔ فروش‌های کانال بازار در استقراری که Edition آن
/// Marketplace نیست. پیش‌فرض خاموش است تا معنای قدیمی SingleStore دست‌نخورده بماند و هیچ استقرار
/// SingleStore بدون فعال‌سازی صریح، handler تسویه را ثبت نکند.
/// </summary>
public sealed class SettlementMarketplaceChannelLedgerOptions
{
    /// <summary>مسیر پیکربندی صریح قابلیت.</summary>
    public const string SectionName = "Tooba:Settlement:MarketplaceChannelLedger";

    /// <summary>
    /// آیا این استقرار صریحاً ledger تسویهٔ فروش‌های کانال بازار را فعال کرده است. خاموش = هیچ ثبتی.
    /// </summary>
    public bool Enabled { get; set; }
}

/// <summary>
/// تصمیم‌های ثبت‌نام (deployment capability) و دامنهٔ per-event دروازهٔ ledger تسویه.
/// </summary>
public static class SettlementMarketplaceChannelLedgerPolicy
{
    /// <summary>
    /// آیا Edition استقرار Marketplace است (مسیر قدیمی؛ behavior موجود دست‌نخورده می‌ماند).
    /// </summary>
    public static bool IsMarketplaceEdition(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var edition = configuration.GetSection("Tooba")["Edition"];
        return string.Equals(edition, "Marketplace", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// آیا handler تسویه در این استقرار ثبت شود؟ Marketplace همیشه (رفتار موجود) و سایر Edition‌ها
    /// فقط با فعال‌سازی صریح قابلیت.
    /// </summary>
    public static bool ShouldRegisterHandlers(IConfiguration configuration) =>
        IsMarketplaceEdition(configuration) || IsLedgerCapabilityEnabled(configuration);

    /// <summary>آیا قابلیت صریح ledger کانال بازار فعال است.</summary>
    public static bool IsLedgerCapabilityEnabled(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var section = configuration.GetSection(SettlementMarketplaceChannelLedgerOptions.SectionName);
        return bool.TryParse(section["Enabled"], out var enabled) && enabled;
    }

    /// <summary>
    /// آیا این استقرار فقط فروش‌های کانال بازار را ثبت می‌کند؟ فقط وقتی قابلیت صریح (نه Edition
    /// Marketplace) مسیر را باز کرده باشد true است؛ Edition=Marketplace رفتار قدیمی خود را نگه می‌دارد.
    /// </summary>
    public static bool RequiresMarketplaceChannelOnly(IConfiguration configuration) =>
        !IsMarketplaceEdition(configuration) && IsLedgerCapabilityEnabled(configuration);
}
