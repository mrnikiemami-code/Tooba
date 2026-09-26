using Tooba.AddressBook.Contracts.Dtos;

namespace Tooba.AddressBook.Contracts.Ports;

/// <summary>
/// Stable AddressBook read for Order checkout shipping imaging.
/// Write/list ownership remains AddressBook.Application.
/// </summary>
public interface IAddressBookCheckoutLookup
{
    /// <summary>Returns an address owned by the actor, or null when missing/foreign.</summary>
    Task<CustomerAddressRecord?> GetAsync(Guid actorUserId, Guid addressId, CancellationToken cancellationToken);
}
