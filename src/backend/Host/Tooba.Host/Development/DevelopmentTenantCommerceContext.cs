using Tooba.BuildingBlocks;

namespace Tooba.Host.Development;

/// <summary>
/// تنها دروازهٔ مشترک Host برای اجرای دانه‌های Development ماژول‌ها.
/// رفع تکرار هشت wrapper: resolve فروشگاه توسعه <c>store-alpha</c>، انتساب
/// <see cref="CommerceContext"/> روی scope و سپس صدا زدن دانهٔ ماژول‌محور.
/// این فایل هیچ داده یا policy ماژولی ندارد و فقط orchestration میزبان است.
/// </summary>
internal static class DevelopmentTenantCommerceContext
{
    /// <summary>شناسهٔ فروشگاهی که دانه‌های Development روی آن اجرا می‌شوند.</summary>
    internal const string DevelopmentTenantId = "store-alpha";

    /// <summary>
    /// روی فروشگاه توسعهٔ فعال یک scope با <see cref="CommerceContext"/> منتسب می‌سازد
    /// و کار را به دانهٔ ماژول واگذار می‌کند. اگر فروشگاه وجود نداشته باشد یا فعال نباشد،
    /// دانه اجرا نمی‌شود (رفتار قبلی wrapperها حفظ می‌شود).
    /// </summary>
    /// <param name="services">ریشهٔ سرویس‌ها.</param>
    /// <param name="traceLabel">برچسب TraceId زمینهٔ تجارت (فقط Development).</param>
    /// <param name="seed">بدنهٔ اجرای دانهٔ ماژول که روی scope آماده صدا زده می‌شود.</param>
    public static async Task RunForDevelopmentTenantAsync(
        IServiceProvider services,
        string traceLabel,
        Func<IServiceProvider, CancellationToken, Task> seed)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(seed);

        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var registry = provider.GetRequiredService<ControlPlaneRegistry>();
        if (!registry.Tenants.TryGetValue(DevelopmentTenantId, out var tenant)
            || tenant.Status != TenantStatus.Active)
        {
            return;
        }

        var assigner = provider.GetRequiredService<ICommerceContextAssigner>();
        assigner.Assign(new CommerceContext(
            new EditionContext(registry.Edition, registry.DeploymentId),
            new TenantContext(
                tenant.TenantId,
                tenant.Status,
                tenant.ConnectionReference,
                tenant.DisplayName,
                tenant.ThemeReference,
                tenant.DefaultMarketReference,
                tenant.Hosts[0],
                tenant.PrimaryDomain),
            tenant.ConnectionReference,
            traceLabel));

        await seed(provider, CancellationToken.None);
    }
}
