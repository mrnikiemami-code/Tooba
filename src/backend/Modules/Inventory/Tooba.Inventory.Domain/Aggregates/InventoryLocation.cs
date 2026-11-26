using Tooba.BuildingBlocks;
using Tooba.Inventory.Contracts.Errors;
using Tooba.Inventory.Domain.ValueObjects;

namespace Tooba.Inventory.Domain.Aggregates;

/// <summary>
/// محل نگهداری حداقل. انبار، فروشگاه یا محل مجازی بعداً گسترش می‌یابد.
/// </summary>
public sealed class InventoryLocation : IHasDomainEvents
{
    private readonly DomainEventCollector _domainEvents = new();

    /// <summary>
    /// شناسهٔ پایدار محل.
    /// </summary>
    public Guid LocationId { get; init; }

    /// <summary>
    /// کد کوتاه محل داخل Tenant.
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// نام نمایشی عملیاتی. آدرس لجستیک کامل نیست.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Explicit owner kind; unknown/null is never treated as shared access.</summary>
    public InventoryLocationOwnerKind OwnerKind { get; init; } = InventoryLocationOwnerKind.Store;

    /// <summary>Owning seller only when <see cref="OwnerKind"/> is Seller.</summary>
    public Guid? SellerPartyId { get; init; }

    /// <summary>
    /// وضعیت محل.
    /// </summary>
    public InventoryLocationStatus Status { get; private set; }

    /// <summary>
    /// زمان ایجاد UTC.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// زمان آخرین تغییر UTC.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.Events;

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// محل فعال می‌سازد. موجودی Offer را صفر نمی‌کند.
    /// </summary>
    public static InventoryLocation Create(Guid locationId, string code, string name, DateTimeOffset now) =>
        Create(locationId, code, name, now, InventoryLocationOwnerKind.Store, null);

    /// <summary>Creates a location with an explicit owner; unknown/shared fallback is forbidden.</summary>
    public static InventoryLocation Create(
        Guid locationId, string code, string name, DateTimeOffset now,
        InventoryLocationOwnerKind ownerKind, Guid? sellerPartyId)
    {
        if (locationId == Guid.Empty)
        {
            throw new ContractOperationException(InventoryErrorCodes.LocationIdRequired);
        }

        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length > 32)
        {
            throw new ContractOperationException(InventoryErrorCodes.LocationCodeInvalid);
        }

        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 128)
        {
            throw new ContractOperationException(InventoryErrorCodes.LocationNameRequired);
        }

        if (ownerKind == InventoryLocationOwnerKind.Seller && (sellerPartyId is null || sellerPartyId == Guid.Empty))
        {
            throw new ContractOperationException(InventoryErrorCodes.LocationOwnerRequired);
        }

        if (ownerKind != InventoryLocationOwnerKind.Seller && sellerPartyId is not null)
        {
            throw new ContractOperationException(InventoryErrorCodes.LocationOwnerInvalid);
        }

        return new InventoryLocation
        {
            LocationId = locationId,
            OwnerKind = ownerKind,
            SellerPartyId = sellerPartyId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Status = InventoryLocationStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }
}
