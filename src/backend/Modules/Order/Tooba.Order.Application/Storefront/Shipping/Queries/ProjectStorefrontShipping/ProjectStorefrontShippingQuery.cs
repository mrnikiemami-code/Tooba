using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Application.Storefront.Shipping.RateAuthority;

namespace Tooba.Order.Application.Storefront.Shipping.Queries.ProjectStorefrontShipping;

public sealed record ProjectStorefrontShippingQuery(
    Guid CartId,
    string? GuestSecret,
    string? ProvinceName,
    string? MethodCode,
    string? Language) : IRequest<Result<StorefrontShippingProjection>>;

public sealed class ProjectStorefrontShippingHandler(
    StorefrontShippingService shipping,
    StorefrontShippingShadowObserver shadow)
    : IRequestHandler<ProjectStorefrontShippingQuery, Result<StorefrontShippingProjection>>
{
    public async Task<Result<StorefrontShippingProjection>> Handle(
        ProjectStorefrontShippingQuery request,
        CancellationToken cancellationToken)
    {
        var result = await StorefrontOrderResult.ExecuteAsync(() => shipping.ProjectAsync(
            request.CartId, request.GuestSecret, request.ProvinceName, request.MethodCode, request.Language, cancellationToken));

        // مشاهدهٔ سایه روی همان projection واقعی: فقط-مشاهده. نتیجهٔ بازگشتی دست‌نخورده است و
        // هیچ مقدار مشاهده‌شده به روش انتخاب، قیمت مشتری، promise یا draft راه نمی‌یابد.
        if (result.IsSuccess && result.Value is { } projection)
        {
            await shadow.ObserveAsync(
                new StorefrontShippingShadowObservationInput(
                    request.ProvinceName,
                    projection.SubtotalExclusiveOfTax,
                    projection.Currency,
                    projection.Methods
                        .Select(m => new StorefrontShippingShadowCandidate(m.ServiceCode, m.PriceAmount))
                        .ToArray()),
                cancellationToken);
        }

        return result;
    }
}
