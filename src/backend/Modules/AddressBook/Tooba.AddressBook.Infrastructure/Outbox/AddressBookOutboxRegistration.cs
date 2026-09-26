using Tooba.BuildingBlocks;
using Tooba.ModuleContracts;
using Tooba.AddressBook.Infrastructure.Persistence;
using Tooba.Persistence;

namespace Tooba.AddressBook.Infrastructure.Outbox;

/// <summary>ثبت Outbox دفترچهٔ آدرس؛ نسخهٔ فعلی رویداد بیرونی تعریف نمی‌کند.</summary>
public sealed class AddressBookOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => AddressBookDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(AddressBookDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata) => null;

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType) =>
        throw new InvalidOperationException("AddressBook integration event is not registered.");

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) => null;
}
