using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.Localization.Contracts.Ports;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.StoreContext.Contracts.Current;

namespace Tooba.Shipping.Infrastructure.Authoring;

/// <summary>
/// آداپتور Shipping برای واقعیت‌های زمینهٔ نویسندگی — فقط-خواندنی و Contracts-only.
/// </summary>
/// <remarks>
/// <para>
/// منابع: <see cref="ILanguageLookup"/> (Localization.Contracts) برای زبان‌ها،
/// <see cref="ICurrentStoreCommerceContext"/> (StoreContext.Contracts) برای ارز پیش‌فرض و
/// <see cref="ICurrentEdition"/> (BuildingBlocks) برای حالت مؤثر نصب. هیچ DbContext خارجی، هیچ
/// join میان‌طرح‌واره‌ای، هیچ فراخوانی HTTP داخلی و هیچ Application/Domain خارجی خوانده نمی‌شود.
/// </para>
/// <para>
/// TB-SHIPPING-ADMIN-CONFIGURATION-CONTEXT-188: هر فراخوانی واقعی <see cref="ILanguageLookup.ListAsync"/>
/// یک span فرزند canonical <c>module.Shipping.Localization.ListConfiguredLanguages</c> می‌گیرد.
/// اگر زمینهٔ trusted (edition/ارز) resolve نشده باشد، خواندن خارجی انجام نمی‌شود و snapshot
/// ناقص برگردانده می‌شود تا handler fail-closed کند؛ هیچ زبان/ارز پیش‌فرضی ساخته نمی‌شود.
/// </para>
/// <para>
/// هیچ cache تازه‌ای اینجا نیست: هر فراخوانی همان مرجع authoritative همان scope را می‌خواند.
/// </para>
/// </remarks>
public sealed class ShippingAuthoringContextSource(
    ILanguageLookup languages,
    ICurrentStoreCommerceContext storeCommerce,
    ICurrentEdition edition,
    IModuleCallTracer moduleCallTracer) : IShippingAuthoringContextSource
{
    private const string SourceModule = "Shipping";
    private const string TargetModule = "Localization";
    private const string ListOperation = "ListConfiguredLanguages";

    /// <inheritdoc />
    public async Task<ShippingAuthoringContextSourceSnapshot> ReadAsync(CancellationToken cancellationToken)
    {
        var effectiveEdition = edition.Current?.Edition ?? ToobaEdition.Unset;
        var defaultCurrency = storeCommerce.Current?.DefaultCurrency;

        // زمینهٔ resolve‌نشده یک شکست مصرف‌کننده است، نه دلیل خواندن خارجی: بدون trusted context هیچ
        // زبان/ارزی ساخته نمی‌شود و هیچ span فراخوانی ماژولی هم ساخته نمی‌شود.
        if (effectiveEdition is not (ToobaEdition.SingleStore or ToobaEdition.Marketplace)
            || string.IsNullOrWhiteSpace(defaultCurrency))
        {
            return new ShippingAuthoringContextSourceSnapshot(effectiveEdition, defaultCurrency, []);
        }

        IReadOnlyList<LanguageLookupSnapshot> rows;
        using (var moduleCall = moduleCallTracer.Begin(SourceModule, TargetModule, ListOperation))
        {
            try
            {
                rows = await languages.ListAsync(cancellationToken);
                moduleCall.SetOk();
            }
            catch (Exception exception)
            {
                // فقط نوع استثنا ثبت می‌شود؛ همان استثنا/لغو بدون تبدیل دوباره پرتاب می‌شود.
                moduleCall.SetError(exception);
                throw;
            }
        }

        return new ShippingAuthoringContextSourceSnapshot(
            effectiveEdition,
            defaultCurrency,
            rows.Select(row => new ShippingAuthoringLanguageSource(
                row.LanguageId,
                row.Code,
                row.Culture,
                row.UrlPrefix,
                row.IsActive,
                row.IsDefault,
                row.Direction,
                row.DisplayName,
                row.NativeName,
                row.SortOrder))
                .ToList());
    }
}
