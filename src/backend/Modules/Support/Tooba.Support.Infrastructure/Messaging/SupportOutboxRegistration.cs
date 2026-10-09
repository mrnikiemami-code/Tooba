using Tooba.BuildingBlocks;
using Tooba.Persistence;
using Tooba.Support.Contracts.Errors;
using Tooba.Support.Infrastructure.Persistence;

namespace Tooba.Support.Infrastructure.Messaging;

/// <summary>ثبت Outbox Support؛ رویداد بیرونی در این نسخه منتشر نمی‌شود.</summary>
public sealed class SupportOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => SupportDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(SupportDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata) => null;

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType) =>
        throw new ContractOperationException(SupportErrorCodes.OutboxEmitNotSupported);

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) => null;
}
