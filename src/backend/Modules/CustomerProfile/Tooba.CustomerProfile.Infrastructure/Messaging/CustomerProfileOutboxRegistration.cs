using Tooba.BuildingBlocks;
using Tooba.CustomerProfile.Infrastructure.Persistence;
using Tooba.ModuleContracts;
using Tooba.Persistence;

namespace Tooba.CustomerProfile.Infrastructure.Messaging;

/// <summary>ثبت Outbox پروفایل مشتری؛ نسخهٔ فعلی رویداد بیرونی تعریف نمی‌کند.</summary>
public sealed class CustomerProfileOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => CustomerProfileDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(CustomerProfileDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata) => null;

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType) =>
        throw new InvalidOperationException("CustomerProfile integration event is not registered.");

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) => null;
}
