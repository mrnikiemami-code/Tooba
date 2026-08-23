using Tooba.BuildingBlocks;
using Tooba.Party.Domain;
using Tooba.Party.Infrastructure.Events;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.Persistence;

namespace Tooba.Party.Infrastructure;

/// <summary>
/// ثبت Outbox ماژول Party. ترجمه فقط برای برقراری عضویت است تا تصویرسازی مجوز بعد از persist انجام شود.
/// </summary>
public sealed class PartyOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => PartyDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(PartyDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata)
    {
        if (domainEvent is not PartyMembershipEstablishedDomainEvent established)
        {
            return null;
        }

        return new PartyMembershipEstablishedIntegrationEvent
        {
            Metadata = metadata with
            {
                EventType = PartyMembershipEstablishedIntegrationEvent.EventTypeName,
                Version = 1,
            },
            MembershipId = established.MembershipId,
            UserId = established.UserId,
            PartyId = established.PartyId,
            RelationCode = established.RelationCode,
        };
    }

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType)
    {
        if (integrationEventType == typeof(PartyMembershipEstablishedIntegrationEvent))
        {
            return PartyMembershipEstablishedIntegrationEvent.EventTypeName;
        }

        throw new InvalidOperationException("Unmapped Party integration event type.");
    }

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) =>
        eventTypeName == PartyMembershipEstablishedIntegrationEvent.EventTypeName
            ? typeof(PartyMembershipEstablishedIntegrationEvent)
            : null;
}
