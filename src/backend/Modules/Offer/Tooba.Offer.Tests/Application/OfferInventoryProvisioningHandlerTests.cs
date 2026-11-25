using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Inventory.Contracts.Errors;
using Tooba.Inventory.Contracts.Seller;
using Tooba.Offer.Application.Offers.Commands.EnsureOfferInventoryPosition;
using Tooba.Offer.Application.Offers.Ports;
using Tooba.Offer.Application.Offers.Queries.ListEligibleInventoryLocations;
using Tooba.Offer.Contracts.Errors;
using Tooba.Offer.Domain.Aggregates;
using Xunit;
using OfferChannel = Tooba.Offer.Domain.ValueObjects.SalesChannel;

namespace Tooba.Offer.Tests.Application;

/// <summary>
/// R10-03: Inventory-owned eligible-location read and idempotent position provisioning are reached
/// through Offer Application handlers over the Inventory contracts gateway. The gateway stub keeps the
/// same position for one (offer, location) pair; real uniqueness/DB proof is intentionally not faked here.
/// </summary>
public sealed class OfferInventoryProvisioningHandlerTests
{
    private static readonly Guid OfferId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid VariantId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid SellerId = Guid.Parse("33333333-3333-4333-8333-333333333333");
    private static readonly Guid OtherSellerId = Guid.Parse("44444444-4444-4444-8444-444444444444");
    private static readonly Guid LocationId = Guid.Parse("55555555-5555-4555-8555-555555555555");
    private static readonly Guid StockItemId = Guid.Parse("66666666-6666-4666-8666-666666666666");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-21T00:00:00Z");

    [Fact]
    public async Task Ensure_position_is_idempotent_for_same_offer_and_location()
    {
        var gateway = new FakeProvisioningGateway();
        var sender = BuildSender(OfferFor(SellerId), gateway);

        var first = await sender.Send(new EnsureOfferInventoryPositionCommand(OfferId, LocationId, SellerId));
        var second = await sender.Send(new EnsureOfferInventoryPositionCommand(OfferId, LocationId, SellerId));

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(StockItemId, first.Value.StockItemId);
        Assert.Equal(StockItemId, second.Value.StockItemId);
        Assert.Equal(LocationId, first.Value.LocationId);
        Assert.Equal(OfferId, first.Value.OfferId);
        Assert.Equal(0m, first.Value.OnHand);
        Assert.Equal(0m, first.Value.Reserved);
        Assert.Equal(2, gateway.EnsureCalls);
        Assert.All(gateway.Ensured, call => Assert.Equal((OfferId, VariantId, LocationId), call));
    }

    [Fact]
    public async Task Ensure_position_rejects_offer_owned_by_another_seller()
    {
        var gateway = new FakeProvisioningGateway();
        var sender = BuildSender(OfferFor(SellerId), gateway);

        var result = await sender.Send(new EnsureOfferInventoryPositionCommand(OfferId, LocationId, OtherSellerId));

        Assert.True(result.IsFailure);
        Assert.Equal(OfferErrorCodes.NotFound, result.FirstError.Code);
        Assert.Equal(0, gateway.EnsureCalls);
    }

    [Fact]
    public async Task Ensure_position_admin_scope_targets_any_existing_offer()
    {
        var gateway = new FakeProvisioningGateway();
        var sender = BuildSender(OfferFor(SellerId), gateway);

        var result = await sender.Send(new EnsureOfferInventoryPositionCommand(OfferId, LocationId));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, gateway.EnsureCalls);
    }

    [Fact]
    public async Task Ensure_position_maps_a_known_inventory_failure_to_a_typed_result()
    {
        var gateway = new FakeProvisioningGateway { ThrowOnEnsure = InventoryErrorCodes.LocationNotFound };
        var sender = BuildSender(OfferFor(SellerId), gateway);

        var result = await sender.Send(new EnsureOfferInventoryPositionCommand(OfferId, LocationId, SellerId));

        Assert.True(result.IsFailure);
        Assert.Equal(InventoryErrorCodes.LocationNotFound, result.FirstError.Code);
    }

    [Fact]
    public async Task List_eligible_locations_returns_the_inventory_projection()
    {
        var gateway = new FakeProvisioningGateway();
        gateway.Locations.Add(new InventoryEligibleLocation(LocationId, "MAIN", "Main warehouse"));
        var sender = BuildSender(OfferFor(SellerId), gateway);

        var result = await sender.Send(new ListEligibleInventoryLocationsQuery());

        Assert.True(result.IsSuccess);
        var location = Assert.Single(result.Value);
        Assert.Equal(LocationId, location.LocationId);
        Assert.Equal("MAIN", location.Code);
        Assert.Equal("Main warehouse", location.Name);
    }

    [Fact]
    public void Eligible_location_active_filter_and_code_order_are_source_verified()
    {
        var source = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "backend", "Modules", "Inventory", "Tooba.Inventory.Infrastructure",
            "Directories", "InventoryProvisioningDirectory.cs"));

        Assert.Contains("x.Status == InventoryLocationStatus.Active", source, StringComparison.Ordinal);
        Assert.Contains(".OrderBy(x => x.Code)", source, StringComparison.Ordinal);
        Assert.Contains("x.LocationId == locationId && x.Status == InventoryLocationStatus.Active", source, StringComparison.Ordinal);
        Assert.Contains("StockPosition.Open(", source, StringComparison.Ordinal);
        Assert.Contains("ix_stock_positions_offer_id_location_id", source, StringComparison.Ordinal);
    }

    private static SellerOffer OfferFor(Guid sellerId) =>
        SellerOffer.Create(OfferId, VariantId, sellerId, OfferChannel.Marketplace, "SKU-1", Now);

    private static ISender BuildSender(SellerOffer offer, IInventoryProvisioningGateway gateway)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(EnsureOfferInventoryPositionCommand).Assembly);
        services.AddSingleton<IOfferStore>(new FakeStore(offer));
        services.AddSingleton(gateway);
        return services.BuildServiceProvider().GetRequiredService<ISender>();
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }

    private sealed class FakeStore(SellerOffer offer) : IOfferStore
    {
        public Task<SellerOffer?> GetByIdAsync(Guid offerId, CancellationToken cancellationToken) =>
            Task.FromResult<SellerOffer?>(offerId == offer.OfferId ? offer : null);

        public Task<IReadOnlyList<SellerOffer>> ListBySellerAsync(Guid sellerPartyId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SellerOffer>>([offer]);

        public Task AddAsync(SellerOffer value, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<SellerOffer?> PersistNewAsync(SellerOffer value, CancellationToken cancellationToken) =>
            Task.FromResult<SellerOffer?>(null);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> ExistsActiveListingAsync(
            Guid sellerPartyId,
            Guid catalogVariantId,
            Tooba.Offer.Domain.ValueObjects.SalesChannel channel,
            CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<Guid?> FindActiveListingOfferIdAsync(
            Guid sellerPartyId,
            Guid catalogVariantId,
            Tooba.Offer.Domain.ValueObjects.SalesChannel channel,
            CancellationToken cancellationToken) => Task.FromResult<Guid?>(null);

        public Task<bool> ExistsSellerSkuAsync(
            Guid sellerPartyId,
            string sellerSku,
            Guid? excludingOfferId,
            CancellationToken cancellationToken) => Task.FromResult(false);
    }

    private sealed class FakeProvisioningGateway : IInventoryProvisioningGateway
    {
        public int EnsureCalls { get; private set; }

        public List<(Guid OfferId, Guid CatalogVariantId, Guid LocationId)> Ensured { get; } = [];

        public List<InventoryEligibleLocation> Locations { get; } = [];

        public string? ThrowOnEnsure { get; init; }

        public Task<IReadOnlyList<InventoryEligibleLocation>> ListEligibleLocationsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<InventoryEligibleLocation>>(Locations);

        public Task<ProvisionedStockPosition> EnsurePositionAsync(
            Guid offerId,
            Guid catalogVariantId,
            Guid locationId,
            CancellationToken cancellationToken)
        {
            EnsureCalls++;
            Ensured.Add((offerId, catalogVariantId, locationId));
            if (ThrowOnEnsure is not null)
            {
                throw new ContractOperationException(ThrowOnEnsure);
            }

            return Task.FromResult(new ProvisionedStockPosition(StockItemId, locationId, offerId, 0m, 0m));
        }
    }
}
