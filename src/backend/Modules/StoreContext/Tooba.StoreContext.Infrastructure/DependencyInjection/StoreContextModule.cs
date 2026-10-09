using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.ModuleContracts;
using Tooba.StoreContext.Contracts.Current;
using Tooba.StoreContext.Infrastructure.Current;

namespace Tooba.StoreContext.Infrastructure.DependencyInjection;

/// <summary>
/// ترکیب ماژول StoreContext: ثبت accessor زمینهٔ تجارت مؤثر فروشگاه در scope و ارائهٔ آن
/// از طریق هر دو درز Contracts (خواندن و تخصیص).
/// در این فاز foundation هیچ DB، schema، migration، endpoint یا application use-case وجود ندارد.
/// </summary>
public sealed class StoreContextModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "StoreContext";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddScoped<StoreCommerceContextAccessor>();
        services.AddScoped<ICurrentStoreCommerceContext>(sp => sp.GetRequiredService<StoreCommerceContextAccessor>());
        services.AddScoped<IStoreCommerceContextAssigner>(sp => sp.GetRequiredService<StoreCommerceContextAccessor>());
    }
}
