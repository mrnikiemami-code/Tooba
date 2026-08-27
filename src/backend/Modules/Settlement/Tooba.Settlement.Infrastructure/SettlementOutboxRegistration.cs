using Tooba.BuildingBlocks;
using Tooba.Persistence;
using Tooba.Settlement.Application;
using Tooba.Settlement.Domain;
using Tooba.Settlement.Infrastructure.Persistence;

namespace Tooba.Settlement.Infrastructure;

/// <summary>
/// ثبت Outbox ماژول Settlement. سفارش/پرداخت را مستقیم به‌روز نمی‌کند؛ ترجمه فقط Integration پایدار است.
/// </summary>
public sealed class SettlementOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => SettlementDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(SettlementDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata) =>
        domainEvent switch
        {
            SettlementEntryPostedDomainEvent posted => new SettlementEntryPostedIntegrationEvent
            {
                Metadata = metadata with { EventType = SettlementEntryPostedIntegrationEvent.EventTypeName, Version = 1 },
                EntryId = posted.EntryId,
                SettlementAccountId = posted.SettlementAccountId,
                SellerPartyId = posted.SellerPartyId,
                EntryType = posted.EntryType,
                NetAmount = posted.NetAmount,
                Currency = posted.Currency,
                SourceType = posted.SourceType,
                SourceId = posted.SourceId,
            },
            PayoutSucceededDomainEvent succeeded => new PayoutSucceededIntegrationEvent
            {
                Metadata = metadata with { EventType = PayoutSucceededIntegrationEvent.EventTypeName, Version = 1 },
                PayoutRequestId = succeeded.PayoutRequestId,
                SettlementAccountId = succeeded.SettlementAccountId,
                SellerPartyId = succeeded.SellerPartyId,
                Amount = succeeded.Amount,
                Currency = succeeded.Currency,
            },
            PayoutFailedDomainEvent failed => new PayoutFailedIntegrationEvent
            {
                Metadata = metadata with { EventType = PayoutFailedIntegrationEvent.EventTypeName, Version = 1 },
                PayoutRequestId = failed.PayoutRequestId,
                SettlementAccountId = failed.SettlementAccountId,
                SellerPartyId = failed.SellerPartyId,
                Amount = failed.Amount,
                Currency = failed.Currency,
                FailureCode = failed.FailureCode,
            },
            _ => null,
        };

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType) =>
        integrationEventType switch
        {
            _ when integrationEventType == typeof(SettlementEntryPostedIntegrationEvent) => SettlementEntryPostedIntegrationEvent.EventTypeName,
            _ when integrationEventType == typeof(PayoutSucceededIntegrationEvent) => PayoutSucceededIntegrationEvent.EventTypeName,
            _ when integrationEventType == typeof(PayoutFailedIntegrationEvent) => PayoutFailedIntegrationEvent.EventTypeName,
            _ => throw new InvalidOperationException("Unmapped Settlement integration event type."),
        };

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) =>
        eventTypeName switch
        {
            SettlementEntryPostedIntegrationEvent.EventTypeName => typeof(SettlementEntryPostedIntegrationEvent),
            PayoutSucceededIntegrationEvent.EventTypeName => typeof(PayoutSucceededIntegrationEvent),
            PayoutFailedIntegrationEvent.EventTypeName => typeof(PayoutFailedIntegrationEvent),
            _ => null,
        };
}
