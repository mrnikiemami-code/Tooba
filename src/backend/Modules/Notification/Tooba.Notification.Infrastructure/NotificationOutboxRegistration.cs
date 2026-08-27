using Tooba.BuildingBlocks;
using Tooba.Notification.Infrastructure.Persistence;
using Tooba.Persistence;

namespace Tooba.Notification.Infrastructure;

/// <summary>
/// ثبت Outbox ماژول Notification. فعلاً emit ندارد؛ فقط مصرف‌کنندهٔ رویدادهای تجاری است.
/// </summary>
public sealed class NotificationOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => NotificationDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(NotificationDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata) => null;

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType) =>
        throw new InvalidOperationException("Notification ماژول رویداد Integration صادر نمی‌کند.");

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) => null;
}
