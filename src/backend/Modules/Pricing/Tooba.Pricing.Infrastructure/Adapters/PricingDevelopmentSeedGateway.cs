using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Offer.Contracts.Errors;
using Tooba.Offer.Contracts.Ports;
using Tooba.Pricing.Application;
using Tooba.Pricing.Contracts;
using Tooba.Pricing.Domain;
using Tooba.Pricing.Infrastructure.Persistence;
using OfferChannel = Tooba.Offer.Contracts.Dtos.SalesChannel;

namespace Tooba.Pricing.Infrastructure.Adapters;

/// <summary>Development-only Pricing seed capability owned by Pricing.Infrastructure.</summary>
public sealed class PricingDevelopmentSeedGateway(
    PricingDbContext db,
    IPricingUseCaseGuard guard,
    IOfferLookupGateway offers,
    IClock clock,
    IIdGenerator ids) : IPricingDevelopmentSeedGateway
{
    /// <inheritdoc />
    public async Task<Result> EnsureDevelopmentBasePriceAsync(
        SetDevelopmentBasePrice request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var offer = await offers.FindOfferAsync(request.OfferId, cancellationToken);
        if (offer is null)
        {
            return Result.Failure(new SemanticError(OfferErrorCodes.NotFound));
        }

        if (!MarketCode.TryParse(request.Market, out var market, out var marketError))
        {
            return Result.Failure(marketError!);
        }

        if (!CurrencyCode.TryParse(request.Currency, out var currency, out var currencyError))
        {
            return Result.Failure(currencyError!);
        }

        var channel = ToPriceChannel(request.Channel);
        var existing = await db.Prices.AsNoTracking()
            .Where(x => x.OfferId == request.OfferId
                        && x.Market == market.Value
                        && x.Channel == channel
                        && x.Currency == currency.Value
                        && x.QualifierKind == PriceQualifierKind.Base)
            .OrderByDescending(x => x.PriceId)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            return Result.Success();
        }

        await guard.EnsureCanMutateAsync(cancellationToken);
        var price = AuthoredPrice.Create(
            ids.NewId(),
            request.OfferId,
            market.Value,
            channel,
            request.Amount,
            currency.Value,
            request.ValidFrom,
            null,
            clock.UtcNow);
        db.Prices.Add(price);
        await db.SaveChangesAsync(cancellationToken);
        price.Activate(clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static PriceChannel ToPriceChannel(OfferChannel channel) =>
        channel switch
        {
            OfferChannel.Direct => PriceChannel.Direct,
            OfferChannel.Marketplace => PriceChannel.Marketplace,
            OfferChannel.Agency => PriceChannel.Agency,
            OfferChannel.Corporate => PriceChannel.Corporate,
            OfferChannel.Affiliate => PriceChannel.Affiliate,
            OfferChannel.Api => PriceChannel.Api,
            _ => PriceChannel.Marketplace,
        };
}
