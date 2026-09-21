using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Localization.Contracts;

namespace Tooba.Fulfillment.Application.Shipping;

/// <summary>گزینهٔ سطح ۲ درخت روش ارسال فعال (شکل JSON پایدار برای UI).</summary>
public sealed record EnabledShippingMethodOptionDto(string Code, string LabelFa, string Name);

/// <summary>گره درخت روش ارسال فعال برای مودال ایجاد مرسوله.</summary>
public sealed record EnabledShippingMethodTreeItemDto(
    string Code,
    string LabelFa,
    string Name,
    string ProviderKind,
    string IconKey,
    string ColorKey,
    IReadOnlyList<EnabledShippingMethodOptionDto> Options);

/// <summary>درخت روش‌های ارسال فعال — مالکیت Application.</summary>
public sealed record ListEnabledShippingMethodsTreeQuery(string? Language)
    : IRequest<Result<IReadOnlyList<EnabledShippingMethodTreeItemDto>>>;

/// <summary>Handler درخت روش‌های ارسال فعال.</summary>
public sealed class ListEnabledShippingMethodsTreeHandler
    : IRequestHandler<ListEnabledShippingMethodsTreeQuery, Result<IReadOnlyList<EnabledShippingMethodTreeItemDto>>>
{
    private readonly IShippingCatalogReader _catalog;
    private readonly ILanguageLookup _languages;
    private readonly IShippingServiceDirectory _directory;
    private readonly ShippingMethodsOptions _options;

    /// <summary>Handler را می‌سازد.</summary>
    public ListEnabledShippingMethodsTreeHandler(
        IShippingCatalogReader catalog,
        ILanguageLookup languages,
        IShippingServiceDirectory directory,
        ShippingMethodsOptions options)
    {
        _catalog = catalog;
        _languages = languages;
        _directory = directory;
        _options = options;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<EnabledShippingMethodTreeItemDto>>> Handle(
        ListEnabledShippingMethodsTreeQuery request,
        CancellationToken cancellationToken)
    {
        await _directory.EnsureSeedAsync(cancellationToken);
        var langId = await ShippingServiceSemantic.ResolveLanguageIdAsync(_languages, request.Language, cancellationToken);
        var enabledCodes = ShippingMethodRegistry.Enabled(_options)
            .Select(x => x.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var catalogRows = await _catalog.ListAsync(cancellationToken);
        var services = catalogRows
            .Where(x => x.IsActive && enabledCodes.Contains(x.Code))
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Code)
            .ToList();
        if (services.Count == 0)
        {
            IReadOnlyList<EnabledShippingMethodTreeItemDto> fallback = ShippingMethodRegistry.Enabled(_options)
                .Select(x => new EnabledShippingMethodTreeItemDto(
                    x.Code,
                    x.LabelFa,
                    x.LabelFa,
                    x.ProviderKind,
                    x.Code,
                    DefaultColor(x.Code),
                    DefaultOptions(x.Code)))
                .ToList();
            return Result.Success(fallback);
        }

        IReadOnlyList<EnabledShippingMethodTreeItemDto> items = services.Select(s =>
        {
            var name = s.Translations.FirstOrDefault(r => r.LanguageId == langId)?.Name
                ?? s.Translations.FirstOrDefault()?.Name
                ?? s.Code;
            var mappedOptions = s.Options.Where(o => o.IsActive).Select(o =>
            {
                var optionName = o.Translations.FirstOrDefault(r => r.LanguageId == langId)?.Name
                    ?? o.Translations.FirstOrDefault()?.Name
                    ?? o.Code;
                return new EnabledShippingMethodOptionDto(o.Code, optionName, optionName);
            }).ToList();
            return new EnabledShippingMethodTreeItemDto(
                s.Code,
                name,
                name,
                s.ProviderKind,
                s.IconKey,
                s.ColorKey,
                mappedOptions);
        }).ToList();
        return Result.Success(items);
    }

    private static string DefaultColor(string code) => code switch
    {
        "post" => "blue",
        "tipax" => "amber",
        "snapp_courier" => "emerald",
        "store_courier" => "violet",
        "in_person" => "rose",
        _ => "blue",
    };

    private static IReadOnlyList<EnabledShippingMethodOptionDto> DefaultOptions(string code) =>
        code is "post" or "tipax"
            ?
            [
                new EnabledShippingMethodOptionDto("express", "پیشتاز", "پیشتاز"),
                new EnabledShippingMethodOptionDto("standard", "معمولی", "معمولی"),
            ]
            : Array.Empty<EnabledShippingMethodOptionDto>();
}
