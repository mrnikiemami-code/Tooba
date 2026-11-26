using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.Inventory.Contracts.Errors;
using Tooba.Inventory.Contracts.Seller;
using Tooba.Inventory.Domain.Aggregates;
using Tooba.Inventory.Domain.ValueObjects;
using Tooba.Inventory.Infrastructure.Persistence;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;

namespace Tooba.Inventory.Infrastructure.Directories;

/// <summary>
/// Inventory-owned read of active eligible locations and idempotent provisioning of an offer
/// stock position. Provisioning opens an empty position (OnHand=0, Reserved=0); it never mutates
/// physical stock, never creates a warehouse and never touches another module's DbContext.
/// </summary>
public sealed class InventoryProvisioningDirectory : IInventoryProvisioningGateway
{
    private const string PositionOfferLocationIndex = "ix_stock_positions_offer_id_location_id";

    private readonly InventoryDbContext _db;
    private readonly IIdGenerator _ids;
    private readonly IClock _clock;
    private readonly ICurrentCommerceContext? _commerce;
    private readonly IOfferLookupGateway? _offers;

    /// <summary>Binds the directory to the Inventory schema and the shared id/clock seams.</summary>
    public InventoryProvisioningDirectory(InventoryDbContext db, IIdGenerator ids, IClock clock,
        ICurrentCommerceContext? commerce = null, IOfferLookupGateway? offers = null)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(ids);
        ArgumentNullException.ThrowIfNull(clock);
        _db = db;
        _ids = ids;
        _clock = clock;
        _commerce = commerce;
        _offers = offers;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<InventoryEligibleLocation>> ListEligibleLocationsAsync(
        Guid? sellerPartyId,
        CancellationToken cancellationToken) =>
        await EligibleLocations(sellerPartyId)
            .OrderBy(x => x.Code)
            .Select(x => new InventoryEligibleLocation(x.LocationId, x.Code, x.Name))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<ProvisionedStockPosition> EnsurePositionAsync(
        Guid offerId,
        Guid catalogVariantId,
        Guid locationId,
        Guid? sellerPartyId,
        CancellationToken cancellationToken)
    {
        if (offerId == Guid.Empty || catalogVariantId == Guid.Empty || locationId == Guid.Empty)
        {
            throw new ContractOperationException(InventoryErrorCodes.PositionIdsRequired);
        }

        if (_offers is not null)
        {
            var offer = await _offers.FindOfferAsync(offerId, cancellationToken);
            if (offer is null || offer.CatalogVariantId != catalogVariantId
                || (sellerPartyId is Guid seller && seller != offer.SellerPartyId))
                throw new ContractOperationException(InventoryErrorCodes.LocationNotEligible);
            if (_commerce?.Current?.Edition.Edition == ToobaEdition.Marketplace
                && ((offer.FulfillmentResponsibility == OfferFulfillmentResponsibility.Platform) != (sellerPartyId is null)))
                throw new ContractOperationException(InventoryErrorCodes.LocationNotEligible);
        }
        var locationIsActive = await EligibleLocations(sellerPartyId)
            .AnyAsync(x => x.LocationId == locationId, cancellationToken)
            .ConfigureAwait(false);
        if (!locationIsActive)
        {
            throw new ContractOperationException(InventoryErrorCodes.LocationNotFound);
        }

        var existing = await FindPositionAsync(offerId, locationId, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return Map(existing);
        }

        var position = StockPosition.Open(_ids.NewId(), offerId, catalogVariantId, locationId, _clock.UtcNow);
        _db.Positions.Add(position);
        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (IsPositionOfferLocationConflict(ex))
        {
            // A concurrent request created the same (OfferId, LocationId) position first; the unique
            // index is the idempotency authority, so drop our losing insert and return the winner.
            _db.Entry(position).State = EntityState.Detached;
            var raced = await FindPositionAsync(offerId, locationId, cancellationToken).ConfigureAwait(false);
            if (raced is null)
            {
                throw;
            }

            return Map(raced);
        }

        return Map(position);
    }

    private IQueryable<InventoryLocation> EligibleLocations(Guid? sellerPartyId)
    {
        var active = _db.Locations.AsNoTracking().Where(x => x.Status == InventoryLocationStatus.Active);
        // Store locations are scoped by the trusted tenant's physical database, never a browser tenant id.
        if (_commerce?.Current is { Edition.Edition: ToobaEdition.SingleStore, Tenant.Status: TenantStatus.Active })
            return active.Where(x => x.OwnerKind == InventoryLocationOwnerKind.Store && x.SellerPartyId == null);
        if (_commerce?.Current is { Edition.Edition: ToobaEdition.Marketplace, Tenant: null })
            return sellerPartyId is Guid seller && seller != Guid.Empty
                ? active.Where(x => x.OwnerKind == InventoryLocationOwnerKind.Seller && x.SellerPartyId == seller)
                : active.Where(x => x.OwnerKind == InventoryLocationOwnerKind.Marketplace && x.SellerPartyId == null);
        // Direct owner fixtures may explicitly exercise the seller boundary without a Host context.
        if (_commerce is null && sellerPartyId is Guid fixtureSeller && fixtureSeller != Guid.Empty)
            return active.Where(x => x.OwnerKind == InventoryLocationOwnerKind.Seller && x.SellerPartyId == fixtureSeller);
        return active.Where(x => false);
    }

    private Task<StockPosition?> FindPositionAsync(Guid offerId, Guid locationId, CancellationToken cancellationToken) =>
        _db.Positions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.OfferId == offerId && x.LocationId == locationId, cancellationToken);

    private static ProvisionedStockPosition Map(StockPosition position) =>
        new(position.StockItemId, position.LocationId, position.OfferId, position.OnHand, position.Reserved, position.UpdatedAt.ToString("O"));

    private static bool IsPositionOfferLocationConflict(DbUpdateException ex)
    {
        for (var inner = ex.InnerException; inner is not null; inner = inner.InnerException)
        {
            if (inner is PostgresException pg
                && pg.SqlState == PostgresErrorCodes.UniqueViolation
                && string.Equals(pg.ConstraintName, PositionOfferLocationIndex, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
