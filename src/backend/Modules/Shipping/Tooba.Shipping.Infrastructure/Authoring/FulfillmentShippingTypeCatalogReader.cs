using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Shipping.Application.Authoring.Ports;

namespace Tooba.Shipping.Infrastructure.Authoring;

/// <summary>
/// آداپتور فقط-خواندنی کاتالوگ نوع ارسال: تنها مصرف‌کنندهٔ <see cref="IShippingTypeReader"/> در
/// Shipping و تنها نقطه‌ای که قرارداد Fulfillment به فکت Application نگاشت می‌شود.
/// </summary>
/// <remarks>
/// <para>
/// <b>Contracts-only.</b> هیچ DbContext، جدول، اتصال یا نگاشت مستقیم دیتابیس ماژول دیگر اینجا نیست و
/// هیچ نوع/کد/متن تعریفی در Shipping کپی یا cache نمی‌شود. نتیجه فقط فکت همان لحظهٔ خواندن است، نه
/// یک قفل یا تراکنش توزیع‌شده در برابر غیرفعال‌شدن بعدی تعریف.
/// </para>
/// <para>
/// <b>Canonical tracing.</b> هر فراخوانی <b>واقعی</b> مقصد یک span فرزند
/// <c>module.Shipping.Fulfillment.*</c> با نام عملیات ثابت و متمایز می‌گیرد. فقط <em>نوع</em> استثنا
/// ثبت می‌شود؛ هیچ شناسه/کد/نام/ترجمه/مالک در telemetry نمی‌رود. استثنا و لغو عیناً منتشر می‌شوند و
/// هرگز به فهرست خالی تبدیل نمی‌شوند.
/// </para>
/// </remarks>
public sealed class FulfillmentShippingTypeCatalogReader(
    IShippingTypeReader reader,
    IModuleCallTracer moduleCallTracer) : IShippingTypeCatalogReader
{
    private const string SourceModule = "Shipping";
    private const string TargetModule = "Fulfillment";
    private const string ListActiveOperation = "ListActiveShippingTypes";
    private const string GetByIdsOperation = "GetShippingTypesByIds";

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShippingTypeFact>> ListActiveAsync(CancellationToken cancellationToken)
    {
        var snapshots = await CallFulfillmentAsync(
            ListActiveOperation,
            () => reader.ListActiveAsync(cancellationToken));
        return snapshots.Select(Map).ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShippingTypeFact>> GetByIdsAsync(
        IReadOnlyCollection<Guid> shippingTypeIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shippingTypeIds);

        // An empty request is not a foreign call: nothing is asked, so no child span is created.
        if (shippingTypeIds.Count == 0)
        {
            return [];
        }

        var snapshots = await CallFulfillmentAsync(
            GetByIdsOperation,
            () => reader.GetByIdsAsync(shippingTypeIds, cancellationToken));
        return snapshots.Select(Map).ToArray();
    }

    private static ShippingTypeFact Map(ShippingTypeSnapshot snapshot)
        => new(
            snapshot.ShippingTypeId,
            snapshot.Code,
            snapshot.IsActive,
            snapshot.SortOrder,
            snapshot.Revision,
            snapshot.Translations
                .Select(translation => new ShippingTypeFactTranslation(
                    translation.LanguageId,
                    translation.Name,
                    translation.Description))
                .ToArray());

    /// <summary>
    /// یک فراخوانی واقعی Fulfillment را با span فرزند canonical می‌پوشاند. مقدار بازگشتی، آرگومان‌ها و
    /// انتشار استثنا/لغو عیناً حفظ می‌شود؛ تنها <em>نوع</em> استثنا ثبت می‌گردد.
    /// </summary>
    private async Task<T> CallFulfillmentAsync<T>(string operation, Func<Task<T>> call)
    {
        using var moduleCall = moduleCallTracer.Begin(SourceModule, TargetModule, operation);
        try
        {
            var result = await call();
            moduleCall.SetOk();
            return result;
        }
        catch (Exception exception)
        {
            moduleCall.SetError(exception);
            throw;
        }
    }
}
