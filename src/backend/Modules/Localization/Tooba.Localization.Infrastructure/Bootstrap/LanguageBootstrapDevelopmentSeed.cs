using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.Localization.Application.Ports;
using Tooba.Localization.Infrastructure.Persistence;

namespace Tooba.Localization.Infrastructure.Bootstrap;

/// <summary>
/// اجرای idempotent دانهٔ زبان روی زمینهٔ اتصال موجود. مالکیتش Localization است و هیچ
/// context/DbContext بیگانه‌ای را نام نمی‌برد.
/// </summary>
/// <remarks>
/// «موجود» یعنی زمینهٔ اتصال از قبل تثبیت شده باشد. اگر زمینه تثبیت نشده باشد، ثبت
/// <see cref="LocalizationDbContext"/> با fail-closed شکست می‌خورد؛ این کلاس خودش زمینه نمی‌سازد و
/// هرگز اتصال را دور نمی‌زند.
/// </remarks>
internal static class LanguageBootstrapRunner
{
    /// <summary>
    /// اگر جدول زبان خالی باشد fa-IR/en-US را می‌نویسد؛ روی دیتابیس غیرخالی هیچ کاری نمی‌کند.
    /// </summary>
    internal static async Task EnsureSeededAsync(IServiceProvider provider, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(provider);
        var directory = provider.GetRequiredService<ILanguageDirectory>();
        await directory.BootstrapAsync(cancellationToken);
    }

    /// <summary>مهاجرت schema زبان و سپس دانهٔ idempotent زبان.</summary>
    internal static async Task MigrateAndSeedAsync(IServiceProvider provider, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(provider);
        var db = provider.GetRequiredService<LocalizationDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
        await EnsureSeededAsync(provider, cancellationToken);
    }
}

/// <summary>
/// گام Development مالکِ Localization: پس از مهاجرت schema همین ماژول، زبان‌های کانونیکال
/// را داخل همان زمینهٔ تجارتی که میزبان تثبیت کرده است دانه می‌کند.
/// </summary>
/// <remarks>
/// پیش از این، دانهٔ زبان فقط در <see cref="LanguageBootstrapHostedService"/> انجام می‌شد. آن
/// سرویس در استارت میزبان و پیش از تثبیت زمینهٔ تجاری اجرا می‌شود، پس resolve کردن context با
/// ۵۰۳ fail-closed شکست می‌خورد و یک دیتابیس تازه‌ساخته بدون هیچ زبان کانونیکال باقی می‌ماند.
/// این گام همان دانه را در ترتیب پشتیبانی‌شدهٔ راه‌اندازی Development و داخل scope تثبیت‌شده
/// تکرار می‌کند؛ هیچ شناسه‌ای hardcode نمی‌شود و idempotence حفظ می‌شود.
/// </remarks>
public static class LanguageBootstrapDevelopmentSeed
{
    /// <summary>گام‌های الزامی Localization که باید پس از مهاجرت schema همین ماژول اجرا شوند.</summary>
    public static class PostMigration
    {
        /// <summary>
        /// دانهٔ زبان کانونیکال را idempotent اجرا می‌کند. بیرون از Development و همچنین وقتی
        /// زمینهٔ اتصال تثبیت نشده باشد، no-op است تا رفتار غیرتولیدی/غیر-Development تغییر نکند.
        /// </summary>
        public static async Task EnsureAsync(IServiceProvider provider, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(provider);

            var environment = provider.GetRequiredService<IHostEnvironment>();
            if (!environment.IsDevelopment())
            {
                return;
            }

            await LanguageBootstrapRunner.EnsureSeededAsync(provider, cancellationToken);
        }
    }
}
