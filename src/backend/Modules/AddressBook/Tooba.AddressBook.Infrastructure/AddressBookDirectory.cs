using Microsoft.EntityFrameworkCore;
using Tooba.AddressBook.Application;
using Tooba.AddressBook.Domain;
using Tooba.AddressBook.Infrastructure.Persistence;

namespace Tooba.AddressBook.Infrastructure;

/// <summary>پیاده‌سازی دفترچه که فقط schema خود را لمس می‌کند و مالکیت را سرورمحور اعمال می‌کند.</summary>
public sealed class AddressBookDirectory : IAddressBookDirectory
{
    private readonly AddressBookDbContext _db;

    /// <summary>وابستگی مالک را بدون DbContext خارجی دریافت می‌کند.</summary>
    public AddressBookDirectory(AddressBookDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<CustomerAddressRecord> CreateAsync(
        Guid actorUserId,
        CustomerAddressWrite input,
        CancellationToken cancellationToken)
    {
        EnsureActor(actorUserId);
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        var address = CustomerAddress.Create(
            actorUserId,
            input.RecipientName,
            input.ContactMobile,
            input.Country,
            input.ProvinceName,
            input.CityName,
            input.PostalCode,
            input.PostalAddress,
            input.BuildingUnit,
            input.Label,
            input.IsDefault,
            DateTimeOffset.UtcNow);
        if (address.IsDefault)
        {
            await ClearOtherDefaultsAsync(actorUserId, address.AddressId, address.UpdatedAt, cancellationToken);
        }

        _db.Addresses.Add(address);
        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return Map(address);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CustomerAddressRecord>> ListAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        EnsureActor(actorUserId);
        var rows = await _db.Addresses.AsNoTracking()
            .Where(x => x.OwnerUserId == actorUserId)
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.UpdatedAt)
            .ThenBy(x => x.AddressId)
            .ToListAsync(cancellationToken);
        return rows.Select(Map).ToList();
    }

    /// <inheritdoc />
    public async Task<CustomerAddressRecord?> GetAsync(
        Guid actorUserId,
        Guid addressId,
        CancellationToken cancellationToken)
    {
        EnsureActor(actorUserId);
        var address = await _db.Addresses.AsNoTracking()
            .SingleOrDefaultAsync(x => x.AddressId == addressId && x.OwnerUserId == actorUserId, cancellationToken);
        return address is null ? null : Map(address);
    }

    /// <inheritdoc />
    public async Task<CustomerAddressRecord> UpdateAsync(
        Guid actorUserId,
        Guid addressId,
        CustomerAddressWrite input,
        CancellationToken cancellationToken)
    {
        EnsureActor(actorUserId);
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        var address = await RequireOwnAsync(actorUserId, addressId, cancellationToken);
        address.Update(
            input.RecipientName,
            input.ContactMobile,
            input.Country,
            input.ProvinceName,
            input.CityName,
            input.PostalCode,
            input.PostalAddress,
            input.BuildingUnit,
            input.Label,
            input.IsDefault,
            DateTimeOffset.UtcNow);
        if (address.IsDefault)
        {
            await ClearOtherDefaultsAsync(actorUserId, address.AddressId, address.UpdatedAt, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return Map(address);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid actorUserId, Guid addressId, CancellationToken cancellationToken)
    {
        EnsureActor(actorUserId);
        var address = await _db.Addresses.SingleOrDefaultAsync(
            x => x.AddressId == addressId && x.OwnerUserId == actorUserId,
            cancellationToken);
        if (address is null)
        {
            throw new InvalidOperationException("نشانی متعلق به این مشتری پیدا نشد.");
        }

        _db.Addresses.Remove(address);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CustomerAddressRecord> SetDefaultAsync(
        Guid actorUserId,
        Guid addressId,
        CancellationToken cancellationToken)
    {
        EnsureActor(actorUserId);
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        var address = await RequireOwnAsync(actorUserId, addressId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        await ClearOtherDefaultsAsync(actorUserId, address.AddressId, now, cancellationToken);
        address.MarkDefault(now);
        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return Map(address);
    }

    private async Task<CustomerAddress> RequireOwnAsync(
        Guid actorUserId,
        Guid addressId,
        CancellationToken cancellationToken)
    {
        var address = await _db.Addresses.SingleOrDefaultAsync(
            x => x.AddressId == addressId && x.OwnerUserId == actorUserId,
            cancellationToken);
        return address ?? throw new InvalidOperationException("نشانی متعلق به این مشتری پیدا نشد.");
    }

    private async Task ClearOtherDefaultsAsync(
        Guid actorUserId,
        Guid keepAddressId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await _db.Addresses
            .Where(x => x.OwnerUserId == actorUserId && x.IsDefault && x.AddressId != keepAddressId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.IsDefault, false)
                    .SetProperty(x => x.UpdatedAt, now),
                cancellationToken);
        foreach (var tracked in _db.ChangeTracker.Entries<CustomerAddress>()
            .Select(entry => entry.Entity)
            .Where(entity => entity.OwnerUserId == actorUserId && entity.AddressId != keepAddressId && entity.IsDefault)
            .ToList())
        {
            tracked.ClearDefault(now);
        }
    }

    /// <inheritdoc />
    public Task<long> CountAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        EnsureActor(actorUserId);
        return _db.Addresses.LongCountAsync(x => x.OwnerUserId == actorUserId, cancellationToken);
    }

    private static CustomerAddressRecord Map(CustomerAddress address) =>
        new(
            address.AddressId,
            address.RecipientName,
            address.ContactMobile,
            address.Country,
            address.ProvinceName,
            address.CityName,
            address.PostalCode,
            address.PostalAddress,
            address.BuildingUnit,
            address.Label,
            address.IsDefault,
            address.CreatedAt,
            address.UpdatedAt);

    private static void EnsureActor(Guid actorUserId)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new InvalidOperationException("Actor معتبر الزامی است.");
        }
    }
}
