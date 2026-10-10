using Tooba.BuildingBlocks;
using Tooba.Persistence;
using Tooba.Wishlist.Contracts.Errors;

namespace Tooba.Wishlist.Infrastructure.Persistence;

/// <summary>ثبت Outbox Wishlist؛ نسخهٔ فعلی رویداد بیرونی تعریف نمی‌کند.</summary>
public sealed class WishlistOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => WishlistDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(WishlistDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata) => null;

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType) =>
        throw new ContractOperationException(WishlistErrorCodes.OutboxUnmappedEventType);

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) => null;
}
