using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Tooba.ModuleContracts;

/// <summary>
/// قرارداد ثبت یک ماژول در ریشهٔ ترکیب Host. کشف بازتابی ندارد؛ Host فهرست را صریح می‌چیند.
/// این رابط سرویس، زیرساخت و کارگر پس‌زمینه را ثبت می‌کند. نگاشت endpoint اختیاری است و نباید این قرارداد را به خدای ترکیب تبدیل کند.
/// </summary>
public interface IToobaModule
{
    /// <summary>
    /// نام پایدار ماژول برای تله‌متری و مستند ترکیب؛ هویت Tenant یا hostname نیست.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// سرویس‌ها و زیرساخت متعلق به همین ماژول را ثبت می‌کند. نباید DbContext یا repository ماژول دیگر را لمس کند.
    /// </summary>
    /// <param name="services">ظرف DI فرآیند.</param>
    /// <param name="configuration">پیکربندی Host؛ ماژول TenantId را از Host خام نمی‌سازد.</param>
    /// <param name="environment">محیط اجرا برای محدود کردن مسیرهای تشخیصی.</param>
    void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment);
}

/// <summary>
/// نشانگر قراردادهای بین‌ماژولی پایدار. persistence و DbContext و join بین‌schema اینجا نمی‌آید.
/// قرارداد کسب‌وکار Catalog/Identity هنوز اضافه نمی‌شود؛ این اسمبلی محل تخلیهٔ انواع تصادفی نیست.
/// </summary>
public static class ModuleContractsMarker
{
}
