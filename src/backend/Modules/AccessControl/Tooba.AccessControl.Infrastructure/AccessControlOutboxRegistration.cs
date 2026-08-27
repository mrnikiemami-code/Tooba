using Tooba.BuildingBlocks;
using Tooba.AccessControl.Infrastructure.Persistence;
using Tooba.Persistence;

namespace Tooba.AccessControl.Infrastructure;

/// <summary>
/// ثبت Outbox ماژول AccessControl. فعلاً emit Integration ندارد.
/// </summary>
public sealed class AccessControlOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => AccessControlDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(AccessControlDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata) => null;

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType) =>
        throw new InvalidOperationException("AccessControl ماژول رویداد Integration صادر نمی‌کند.");

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) => null;
}
