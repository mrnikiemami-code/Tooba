using Tooba.Identity.Infrastructure;
using Tooba.ModuleContracts;
using Tooba.PlatformProbe.Infrastructure;

namespace Tooba.Host;

/// <summary>
/// ریشهٔ ترکیب صریح ماژول‌ها. فهرست دستی است تا استخراج بعدی سرویس بدون وابستگی مصرف‌کننده به جزئیات in-process بماند.
/// منطق کسب‌وکار اینجا نوشته نمی‌شود.
/// </summary>
internal static class ToobaModuleComposition
{
    /// <summary>
    /// ماژول‌های ثبت‌شده در این استقرار. کشف اسمبلی انجام نمی‌شود.
    /// </summary>
    public static IReadOnlyList<IToobaModule> Modules { get; } =
    [
        new PlatformProbeModule(),
        new IdentityModule(),
    ];

    /// <summary>
    /// هر ماژول را به ترتیب فهرست صریح ثبت می‌کند. Host فقط ترکیب می‌کند.
    /// </summary>
    public static IServiceCollection AddToobaModules(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        foreach (var module in Modules)
        {
            module.AddServices(services, configuration, environment);
        }

        return services;
    }
}
