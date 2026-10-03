using Tooba.BuildingBlocks;
using Tooba.Persistence;

namespace Tooba.Localization.Infrastructure.Persistence;

/// <summary>Localization Outbox registration; base version does not publish external events.</summary>
public sealed class LocalizationOutboxRegistration : IOutboxModuleRegistration
{
    public string Schema => LocalizationDbContext.Schema;
    public string TableName => OutboxMessageMapping.TableName;
    public Type DbContextType => typeof(LocalizationDbContext);
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata) => null;
    public string GetEventTypeName(Type integrationEventType) =>
        throw new InvalidOperationException("Localization integration event is not registered.");
    public Type? ResolveEventClrType(string eventTypeName) => null;
}
