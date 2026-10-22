using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Shipping.Models;
using Tooba.Fulfillment.Contracts.Errors;
using Tooba.Localization.Contracts.Ports;

namespace Tooba.Fulfillment.Infrastructure.Shipping;

/// <summary>
/// حقایق زبان authoritative برای نوشتن/خواندن متن نوع ارسال، روی <see cref="ILanguageLookup"/> واقعی.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ILanguageLookup.ListAsync"/> همهٔ ردیف‌های شناخته‌شده (شامل غیرفعال) را می‌آورد؛ فیلتر
/// فعال‌بودن و حل پیش‌فرض در Application انجام می‌شود و هیچ فهرست ثابت fa/en یا locale درخواست جای آن را
/// نمی‌گیرد.
/// </para>
/// <para>
/// هر فراخوانی واقعی Localization با یک span فرزند canonical
/// <c>module.Fulfillment.Localization.ListLanguages</c> پوشیده می‌شود. برچسب‌ها هیچ شناسهٔ زبان/نوع، متن،
/// tenant یا توکنی ندارند؛ خطا فقط با نوع استثنا ثبت می‌شود و همان استثنا دوباره پرتاب می‌شود. لغو فراخوان
/// تغییر نمی‌کند و بدون span جایگزین/بدون‌کار منتشر می‌شود.
/// </para>
/// </remarks>
public sealed class LocalizationShippingTypeLanguageFacts(
    ILanguageLookup languages,
    IModuleCallTracer moduleCalls) : IShippingTypeLanguageFacts
{
    private const string SourceModule = "Fulfillment";
    private const string TargetModule = "Localization";
    private const string ListOperation = "ListLanguages";

    /// <inheritdoc />
    public async Task<ShippingTypeLanguageFacts> ReadAsync(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<LanguageLookupSnapshot> rows;
        using (var moduleCall = moduleCalls.Begin(SourceModule, TargetModule, ListOperation))
        {
            try
            {
                rows = await languages.ListAsync(cancellationToken);
                moduleCall.SetOk();
            }
            catch (Exception exception)
            {
                moduleCall.SetError(exception);
                throw;
            }
        }

        if (rows.Count == 0)
        {
            throw Fault(FulfillmentErrorCodes.ShippingTypeDefaultLanguageUnavailable);
        }

        var facts = new List<ShippingTypeLanguageFact>(rows.Count);
        foreach (var row in rows)
        {
            // metadata ناقص یعنی رجیستری برای نوشتن متن قابل‌اعتماد نیست؛ حدس/مقدار بدلی نمی‌سازیم.
            if (row.IsActive is not { } isActive || row.SortOrder is not { } sortOrder)
            {
                throw Fault(FulfillmentErrorCodes.ShippingTypeLanguageInvalid);
            }

            facts.Add(new ShippingTypeLanguageFact(row.LanguageId, isActive, row.IsDefault, sortOrder));
        }

        return new ShippingTypeLanguageFacts(facts);
    }

    private static SemanticException Fault(string code) => new(new SemanticError(code));
}
