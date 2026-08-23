using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NodaTime;
using Tooba.BuildingBlocks;

namespace Tooba.Persistence;

/// <summary>
/// در SavingChanges رویداد دامنه را جمع می‌کند، فقط ترجمه‌های ثبت‌شده را به Outbox همان تراکنش می‌نویسد، و handler صدا نمی‌زند.
/// </summary>
public sealed class OutboxSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentCommerceContext _commerce;
    private readonly IEnumerable<IOutboxModuleRegistration> _modules;
    private readonly IIntegrationEventSerializer _serializer;

    /// <summary>
    /// interceptor را به زمینهٔ درخواست و ثبت ماژول‌ها وصل می‌کند.
    /// </summary>
    public OutboxSaveChangesInterceptor(
        ICurrentCommerceContext commerce,
        IEnumerable<IOutboxModuleRegistration> modules,
        IIntegrationEventSerializer serializer)
    {
        _commerce = commerce;
        _modules = modules;
        _serializer = serializer;
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        AppendOutbox(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AppendOutbox(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        ClearDomainEvents(eventData.Context);
        return base.SavedChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        ClearDomainEvents(eventData.Context);
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private void AppendOutbox(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var registration = _modules.FirstOrDefault(m => m.DbContextType.IsInstanceOfType(context));
        if (registration is null)
        {
            return;
        }

        var hosts = context.ChangeTracker.Entries()
            .Select(e => e.Entity)
            .OfType<IHasDomainEvents>()
            .ToArray();

        var domainEvents = hosts.SelectMany(e => e.DomainEvents).ToArray();
        if (domainEvents.Length == 0)
        {
            return;
        }

        var commerce = _commerce.Current
            ?? throw new PlatformHttpException(503, "Service Unavailable", "platform.edition.unconfigured");

        foreach (var domain in domainEvents.DistinctBy(item => item.Metadata.EventId))
        {
            var metadata = new EventMetadata(
                EventId: domain.Metadata.EventId,
                OccurredAt: domain.Metadata.OccurredAt,
                EventType: domain.Metadata.EventType,
                CorrelationId: domain.Metadata.CorrelationId ?? commerce.TraceId,
                Version: domain.Metadata.Version,
                TenantId: commerce.Tenant?.TenantId.Value,
                DeploymentId: commerce.Edition.DeploymentId,
                Edition: commerce.Edition.Edition);

            var integration = registration.Translate(domain, metadata);
            if (integration is null)
            {
                continue;
            }

            var eventType = registration.GetEventTypeName(integration.GetType());
            var finalMetadata = integration.Metadata with
            {
                EventType = eventType,
                TenantId = metadata.TenantId,
                DeploymentId = metadata.DeploymentId,
                Edition = metadata.Edition,
                CorrelationId = metadata.CorrelationId,
            };

            var writable = integration.GetType().GetProperty(nameof(IIntegrationEvent.Metadata));
            writable?.SetValue(integration, finalMetadata);

            if (context.Set<OutboxMessage>().Local.Any(row => row.Id == finalMetadata.EventId))
            {
                continue;
            }

            context.Set<OutboxMessage>().Add(new OutboxMessage
            {
                Id = finalMetadata.EventId,
                OccurredAt = Instant.FromDateTimeOffset(finalMetadata.OccurredAt),
                EventType = eventType,
                Payload = _serializer.SerializePayload(integration),
                CorrelationId = finalMetadata.CorrelationId,
                Version = finalMetadata.Version,
                TenantId = finalMetadata.TenantId,
                DeploymentId = finalMetadata.DeploymentId,
                Edition = finalMetadata.Edition.ToString(),
            });
        }
    }

    private static void ClearDomainEvents(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        foreach (var entity in context.ChangeTracker.Entries().Select(e => e.Entity).OfType<IHasDomainEvents>())
        {
            entity.ClearDomainEvents();
        }
    }
}
