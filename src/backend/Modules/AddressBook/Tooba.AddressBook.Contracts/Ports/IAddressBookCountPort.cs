namespace Tooba.AddressBook.Contracts.Ports;

/// <summary>Narrow read port: actor address count for customer-account dashboard.</summary>
public interface IAddressBookCountPort
{
    /// <summary>Returns the number of addresses owned by the actor.</summary>
    Task<long> CountAsync(Guid actorUserId, CancellationToken cancellationToken);
}
