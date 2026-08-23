using Tooba.BuildingBlocks;
using Tooba.Persistence;
using Tooba.Tax.Domain;
using Tooba.Tax.Infrastructure.Events;
using Tooba.Tax.Infrastructure.Persistence;

namespace Tooba.Tax.Infrastructure;

/// <summary>
/// ثبت Outbox ماژول Tax.
/// </summary>
public sealed class TaxOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => TaxDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(TaxDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata)
    {
        return domainEvent switch
        {
            TaxRuleCreatedDomainEvent created => new TaxRuleCreatedIntegrationEvent
            {
                Metadata = metadata with { EventType = TaxRuleCreatedIntegrationEvent.EventTypeName, Version = 1 },
                RuleId = created.RuleId,
            },
            TaxRuleActivatedDomainEvent activated => new TaxRuleActivatedIntegrationEvent
            {
                Metadata = metadata with { EventType = TaxRuleActivatedIntegrationEvent.EventTypeName, Version = 1 },
                RuleId = activated.RuleId,
            },
            TaxRuleChangedDomainEvent changed => new TaxRuleChangedIntegrationEvent
            {
                Metadata = metadata with { EventType = TaxRuleChangedIntegrationEvent.EventTypeName, Version = 1 },
                RuleId = changed.RuleId,
            },
            TaxCalculationFailedDomainEvent failed => new TaxCalculationFailedIntegrationEvent
            {
                Metadata = metadata with { EventType = TaxCalculationFailedIntegrationEvent.EventTypeName, Version = 1 },
                Outcome = failed.Outcome.ToString(),
            },
            _ => null,
        };
    }

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType)
    {
        if (integrationEventType == typeof(TaxRuleCreatedIntegrationEvent))
        {
            return TaxRuleCreatedIntegrationEvent.EventTypeName;
        }

        if (integrationEventType == typeof(TaxRuleActivatedIntegrationEvent))
        {
            return TaxRuleActivatedIntegrationEvent.EventTypeName;
        }

        if (integrationEventType == typeof(TaxRuleChangedIntegrationEvent))
        {
            return TaxRuleChangedIntegrationEvent.EventTypeName;
        }

        if (integrationEventType == typeof(TaxCalculationFailedIntegrationEvent))
        {
            return TaxCalculationFailedIntegrationEvent.EventTypeName;
        }

        throw new InvalidOperationException("Unmapped Tax integration event type.");
    }

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) =>
        eventTypeName switch
        {
            TaxRuleCreatedIntegrationEvent.EventTypeName => typeof(TaxRuleCreatedIntegrationEvent),
            TaxRuleActivatedIntegrationEvent.EventTypeName => typeof(TaxRuleActivatedIntegrationEvent),
            TaxRuleChangedIntegrationEvent.EventTypeName => typeof(TaxRuleChangedIntegrationEvent),
            TaxCalculationFailedIntegrationEvent.EventTypeName => typeof(TaxCalculationFailedIntegrationEvent),
            _ => null,
        };
}
