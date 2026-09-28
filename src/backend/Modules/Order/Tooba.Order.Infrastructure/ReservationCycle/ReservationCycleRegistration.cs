using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Infrastructure.Integrations.Payment;

namespace Tooba.Order.Infrastructure.ReservationCycle;

/// <summary>
/// ثبت متمرکز و محدود مرزهای رزرو/انقضای سفارش که از ریشهٔ Host تخلیه شدند.
/// این فایل فقط ترکیب DI است؛ منطق کسب‌وکار ندارد و به سطح ثبت عمومی OrderModule وابسته نمی‌شود
/// تا سطح ثبت موجود OrderModule دست‌نخورده بماند.
/// </summary>
public static class ReservationCycleRegistration
{
    /// <summary>
    /// سیاست مهلت رزرو checkout، تنظیمات worker انقضای سفارش پرداخت‌نشده و خود worker را ثبت می‌کند.
    /// </summary>
    public static IServiceCollection AddOrderReservationCycleBoundaries(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddScoped<ICheckoutReservationHoldPolicy, CheckoutReservationHoldPolicyAdapter>();
        services.Configure<UnpaidOrderExpiryWorkerOptions>(
            configuration.GetSection(UnpaidOrderExpiryWorkerOptions.SectionName));
        services.AddHostedService<UnpaidOrderExpiryWorker>();

        return services;
    }
}
