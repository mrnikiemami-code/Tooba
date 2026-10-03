using Tooba.BuildingBlocks;
using Tooba.Persistence;

namespace Tooba.OperatorProfile.Infrastructure.Persistence;

/// <summary>ثبت Outbox پروفایل اپراتور؛ نسخهٔ فعلی رویداد بیرونی تعریف نمی‌کند.</summary>
public sealed class OperatorProfileOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => OperatorProfileDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(OperatorProfileDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata) => null;

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType) =>
        throw new InvalidOperationException("OperatorProfile integration event is not registered.");

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) => null;
}
