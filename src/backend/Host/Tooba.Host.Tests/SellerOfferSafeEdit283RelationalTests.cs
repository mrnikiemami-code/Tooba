using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.Catalog.Contracts;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Inventory.Application.Ports;
using Tooba.Inventory.Contracts.Errors;
using Tooba.Inventory.Contracts.Seller;
using Tooba.Inventory.Domain.Aggregates;
using Tooba.Inventory.Infrastructure.Directories;
using Tooba.Inventory.Infrastructure.Persistence;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Persistence;
using Xunit;
using Xunit.Abstractions;

namespace Tooba.Host.Tests;

[Collection("PostgresSerial")]
public sealed class SellerOfferSafeEdit283RelationalTests
{
    private static readonly Lazy<(bool Ok, string Reason)> Target = new(ValidateTarget);
    private readonly ITestOutputHelper _output;
    private readonly Guid _offerId = Guid.NewGuid();
    private readonly Guid _sellerId = Guid.NewGuid();
    private readonly Guid _variantId = Guid.NewGuid();

    public SellerOfferSafeEdit283RelationalTests(ITestOutputHelper output) => _output = output;

    private static string Connection =>
        Environment.GetEnvironmentVariable("TB283_CONNECTION") is { Length: > 0 } raw
            ? raw.Trim()
            : "Host=127.0.0.1;Port=5432;Username=admin;Password=123456;Database=tooba_alpha";

    [SkippableFact]
    public async Task Named_position_write_changes_only_that_position()
    {
        Skip.IfNot(Target.Value.Ok, Target.Value.Reason);
        await using var db = CreateDb();
        await using var tx = await db.Database.BeginTransactionAsync();
        var now = DateTimeOffset.UtcNow;
        var location1 = InventoryLocation.Create(Guid.NewGuid(), $"TB283-A-{Guid.NewGuid():N}"[..24], "A", now);
        var location2 = InventoryLocation.Create(Guid.NewGuid(), $"TB283-B-{Guid.NewGuid():N}"[..24], "B", now);
        var first = StockPosition.Open(Guid.NewGuid(), _offerId, _variantId, location1.LocationId, now);
        var second = StockPosition.Open(Guid.NewGuid(), _offerId, _variantId, location2.LocationId, now);
        first.SyncQuantities(3, 1, now);
        second.SyncQuantities(7, 2, now);
        db.Locations.AddRange(location1, location2);
        db.Positions.AddRange(first, second);
        await db.SaveChangesAsync();

        var directory = CreateDirectory(db, now);
        var updated = await directory.SetInventoryAsync(new SetSellerOfferInventory(
            _offerId, _sellerId, 8, "tb283-named-position", second.StockItemId), CancellationToken.None);
        Assert.True(updated.IsSuccess);
        await db.SaveChangesAsync();

        var rows = await db.Positions.AsNoTracking().Where(x => x.OfferId == _offerId).ToListAsync();
        Assert.Equal(3, rows.Single(x => x.StockItemId == first.StockItemId).OnHand);
        Assert.Equal(8, rows.Single(x => x.StockItemId == second.StockItemId).OnHand);
        Assert.Equal(1, rows.Single(x => x.StockItemId == first.StockItemId).Reserved);
        Assert.Equal(2, rows.Single(x => x.StockItemId == second.StockItemId).Reserved);
        Assert.Equal(11, rows.Sum(x => x.OnHand));

        var ambiguous = await directory.SetInventoryAsync(new SetSellerOfferInventory(
            _offerId, _sellerId, 9, "ambiguous"), CancellationToken.None);
        Assert.True(ambiguous.IsFailure);
        Assert.Equal(InventoryErrorCodes.PositionIdRequired, ambiguous.FirstError.Code);

        var foreign = await directory.SetInventoryAsync(new SetSellerOfferInventory(
            _offerId, _sellerId, 9, "foreign", Guid.NewGuid()), CancellationToken.None);
        Assert.True(foreign.IsFailure);
        Assert.Equal(InventoryErrorCodes.PositionIdUnknown, foreign.FirstError.Code);

        var below = await directory.SetInventoryAsync(new SetSellerOfferInventory(
            _offerId, _sellerId, 1, "below", second.StockItemId), CancellationToken.None);
        Assert.True(below.IsFailure);
        Assert.Equal(InventoryErrorCodes.AdjustmentQuantityExceeds, below.FirstError.Code);

        await tx.RollbackAsync();
        _output.WriteLine($"TB283 offer={_offerId:N} committedRows=0");
    }

    private InventoryDirectory CreateDirectory(InventoryDbContext db, DateTimeOffset now) =>
        new(db, new PassGuard(), new OfferLookup(_offerId, _variantId, _sellerId), new NeverCatalog(),
            new FixedClock(now), new UuidV7IdGenerator(), new ModuleCallTracer());

    private static InventoryDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, Connection, InventoryDbContext.Schema, typeof(InventoryDbContext));
        return new InventoryDbContext(options.Options);
    }

    private static (bool Ok, string Reason) ValidateTarget()
    {
        try
        {
            using var connection = new NpgsqlConnection(Connection);
            connection.Open();
            using var command = new NpgsqlCommand("SELECT current_database()", connection);
            var database = (string?)command.ExecuteScalar();
            return string.Equals(database, "tooba_alpha", StringComparison.Ordinal)
                ? (true, "ok")
                : (false, "TB283 target is not tooba_alpha");
        }
        catch (Exception ex)
        {
            return (false, $"TB283 target unavailable: {ex.GetType().Name}");
        }
    }

    private sealed class PassGuard : IInventoryUseCaseGuard
    {
        public Task EnsureCanMutateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class OfferLookup(Guid offerId, Guid variantId, Guid sellerId) : IOfferLookupGateway
    {
        private readonly OfferReference _reference = new(
            offerId, variantId, sellerId, SalesChannel.Marketplace, OfferStatus.Draft, null);

        public Task<OfferReference?> FindOfferAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<OfferReference?>(id == offerId ? _reference : null);

        public Task<IReadOnlyDictionary<Guid, OfferReference>> FindOffersBatchAsync(
            IReadOnlyCollection<Guid> offerIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, OfferReference>>(
                offerIds.Contains(offerId)
                    ? new Dictionary<Guid, OfferReference> { [offerId] = _reference }
                    : new Dictionary<Guid, OfferReference>());

        public Task<IReadOnlyDictionary<Guid, int>> CountOffersByCatalogVariantIdsAsync(
            IReadOnlyCollection<Guid> catalogVariantIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, int>>(new Dictionary<Guid, int>());
    }

    private sealed class NeverCatalog : ICatalogVariantLookup
    {
        public Task<CatalogVariantLookupResult?> FindVariantAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<CatalogVariantLookupResult?>(null);
        public Task<IReadOnlyDictionary<Guid, Guid?>> GetPrimaryCategoryIdsByVariantIdsAsync(
            IReadOnlyList<Guid> variantIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, Guid?>>(new Dictionary<Guid, Guid?>());
        public Task<IReadOnlyDictionary<Guid, string>> GetVariantTitlesAsync(
            IReadOnlyList<Guid> variantIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());
        public Task<IReadOnlyList<Guid>> GetPublishedVariantIdsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
    }
}
