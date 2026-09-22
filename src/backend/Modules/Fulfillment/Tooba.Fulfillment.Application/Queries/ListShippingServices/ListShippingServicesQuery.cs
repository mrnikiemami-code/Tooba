using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Fulfillment.Contracts.Errors;
using Tooba.Localization.Contracts;

namespace Tooba.Fulfillment.Application.Queries.ListShippingServices;

public sealed record ListShippingServicesQuery(string? Language)
    : IRequest<Result<IReadOnlyList<ShippingServiceListItemDto>>>;

public sealed class ListShippingServicesHandler
    : IRequestHandler<ListShippingServicesQuery, Result<IReadOnlyList<ShippingServiceListItemDto>>>
{
    private readonly IShippingCatalogReader _catalog;
    private readonly ILanguageLookup _languages;
    private readonly IShippingServiceDirectory _directory;

    public ListShippingServicesHandler(IShippingCatalogReader catalog, ILanguageLookup languages, IShippingServiceDirectory directory)
    { _catalog = catalog; _languages = languages; _directory = directory; }

    public async Task<Result<IReadOnlyList<ShippingServiceListItemDto>>> Handle(
        ListShippingServicesQuery request, CancellationToken cancellationToken)
    {
        await _directory.EnsureSeedAsync(cancellationToken);
        var langId = await ShippingServiceSemantic.ResolveLanguageIdAsync(_languages, request.Language, cancellationToken);
        var services = await _catalog.ListAsync(cancellationToken);
        var items = services.Select(s =>
        {
            var picked = s.Translations.FirstOrDefault(r => r.LanguageId == langId) ?? s.Translations.FirstOrDefault();
            return new ShippingServiceListItemDto(
                s.ShippingServiceId, s.Code, s.ProviderKind, s.IconKey, s.ColorKey,
                picked?.Name ?? s.Code, s.IsActive, s.SortOrder, s.Options.Count, s.Options.Count(o => o.IsActive));
        }).ToList();
        return Result.Success<IReadOnlyList<ShippingServiceListItemDto>>(items);
    }
}
