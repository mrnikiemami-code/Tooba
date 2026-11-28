using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Pricing.Contracts.Ports;

namespace Tooba.Offer.Application.Offers.Queries.GetOfferCurrencies;

/// <summary>Returns the Pricing-owned authoritative active system currencies for the offer price editor.</summary>
public sealed record GetOfferCurrenciesQuery : IRequest<Result<IReadOnlyList<OfferCurrencyOption>>>;

internal sealed class GetOfferCurrenciesQueryHandler(ICurrencyReferenceReader currencies)
    : IRequestHandler<GetOfferCurrenciesQuery, Result<IReadOnlyList<OfferCurrencyOption>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<OfferCurrencyOption>>> Handle(
        GetOfferCurrenciesQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await currencies.ListActiveAsync(cancellationToken);
        return Result.Success<IReadOnlyList<OfferCurrencyOption>>(rows
            .Select(row => new OfferCurrencyOption(row.Code, row.EnglishName, row.NativeName, row.DecimalPlaces))
            .ToArray());
    }
}
