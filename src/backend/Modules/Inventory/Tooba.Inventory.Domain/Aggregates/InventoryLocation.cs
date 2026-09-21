using Tooba.BuildingBlocks;
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
    public static InventoryLocation Create(Guid locationId, string code, string name, DateTimeOffset now)
    {
        if (locationId == Guid.Empty)
        {
            throw new InvalidOperationException("inventory.location.id_required");
        }

        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length > 32)
        {
            throw new InvalidOperationException("inventory.location.code_invalid");
        }

        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 128)
        {
            throw new InvalidOperationException("inventory.location.name_required");
        }

        return new InventoryLocation
        {
            LocationId = locationId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Status = InventoryLocationStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }
}
