using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Contracts.Errors;
using Tooba.Localization.Contracts;

namespace Tooba.Fulfillment.Application.Shipping;

/// <summary>Handler فهرست سرویس ارسال.</summary>
public sealed class ListShippingServicesHandler
    : IRequestHandler<ListShippingServicesQuery, Result<IReadOnlyList<ShippingServiceListItemDto>>>
{
    private readonly IShippingCatalogReader _catalog;
    private readonly ILanguageLookup _languages;
    private readonly IShippingServiceDirectory _directory;

    /// <summary>Handler را می‌سازد.</summary>
    public ListShippingServicesHandler(
        IShippingCatalogReader catalog,
        ILanguageLookup languages,
        IShippingServiceDirectory directory)
    {
        _catalog = catalog;
        _languages = languages;
        _directory = directory;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ShippingServiceListItemDto>>> Handle(
        ListShippingServicesQuery request,
        CancellationToken cancellationToken)
    {
        await _directory.EnsureSeedAsync(cancellationToken);
        var langId = await ShippingServiceSemantic.ResolveLanguageIdAsync(_languages, request.Language, cancellationToken);
        var services = await _catalog.ListAsync(cancellationToken);
        var items = services.Select(s =>
        {
            var picked = s.Translations.FirstOrDefault(r => r.LanguageId == langId)
                ?? s.Translations.FirstOrDefault();
            return new ShippingServiceListItemDto(
                s.ShippingServiceId,
                s.Code,
                s.ProviderKind,
                s.IconKey,
                s.ColorKey,
                picked?.Name ?? s.Code,
                s.IsActive,
                s.SortOrder,
                s.Options.Count,
                s.Options.Count(o => o.IsActive));
        }).ToList();
        return Result.Success<IReadOnlyList<ShippingServiceListItemDto>>(items);
    }
}

/// <summary>Handler جزئیات سرویس ارسال.</summary>
public sealed class GetShippingServiceHandler
    : IRequestHandler<GetShippingServiceQuery, Result<ShippingServiceDetailDto>>
{
    private readonly IShippingCatalogReader _catalog;

    /// <summary>Handler را می‌سازد.</summary>
    public GetShippingServiceHandler(IShippingCatalogReader catalog) => _catalog = catalog;

    /// <inheritdoc />
    public async Task<Result<ShippingServiceDetailDto>> Handle(
        GetShippingServiceQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await _catalog.GetAsync(request.ServiceId, cancellationToken);
        return entity is null
            ? ShippingServiceSemantic.Failure<ShippingServiceDetailDto>(FulfillmentErrorCodes.ShippingServiceNotFound)
            : Result.Success(ShippingServiceSemantic.ToDetail(entity));
    }
}
