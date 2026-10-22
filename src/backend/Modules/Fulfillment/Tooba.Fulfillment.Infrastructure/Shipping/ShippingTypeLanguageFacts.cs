using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.ShippingTypes;
using Tooba.Fulfillment.Contracts.Errors;
using Tooba.Localization.Contracts.Ports;

namespace Tooba.Fulfillment.Infrastructure.Shipping;

/// <summary>
/// حقایق زبان authoritative برای نوشتن متن نوع ارسال، روی <see cref="ILanguageLookup"/> واقعی.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ILanguageLookup.ListAsync"/> همهٔ ردیف‌های شناخته‌شده (شامل غیرفعال) را می‌آورد؛ فیلتر
/// فعال‌بودن اینجا صریح انجام می‌شود و هیچ فهرست ثابت fa/en یا locale درخواست جای آن را نمی‌گیرد.
/// </para>
/// <para>
/// metadata ناقص/مبهم (فعال‌بودن نامعلوم) برای نوشتن متن قابل‌اعتماد نیست و fail-closed می‌شود. رجیستری
/// بدون پیش‌فرض فعال همان خطای پایداری است که Application بررسی می‌کند.
/// </para>
/// </remarks>
public sealed class ShippingTypeLanguageFacts(ILanguageLookup languages) : IShippingTypeLanguageFacts
{
    /// <inheritdoc />
    public async Task<Application.ShippingTypes.ShippingTypeLanguageFacts> ReadAsync(
        CancellationToken cancellationToken)
    {
        var rows = await languages.ListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            throw Fault(FulfillmentErrorCodes.ShippingTypeDefaultLanguageUnavailable);
        }

        var facts = new List<ShippingTypeLanguageFact>(rows.Count);
        foreach (var row in rows)
        {
            // metadata ناقص یعنی رجیستری برای نوشتن متن قابل‌اعتماد نیست؛ حدس نمی‌زنیم.
            if (row.IsActive is not { } isActive)
            {
                throw Fault(FulfillmentErrorCodes.ShippingTypeLanguageInvalid);
            }

            facts.Add(new ShippingTypeLanguageFact(
                row.LanguageId,
                isActive,
                row.IsDefault,
                row.SortOrder ?? int.MaxValue));
        }

        return new Application.ShippingTypes.ShippingTypeLanguageFacts(facts);
    }

    private static SemanticException Fault(string code) => new(new SemanticError(code));
}
