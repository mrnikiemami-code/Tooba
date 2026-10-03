using Tooba.BuildingBlocks;
using Tooba.Persistence;

namespace Tooba.UserPreference.Infrastructure.Persistence;

/// <summary>ثبت Outbox ترجیح کاربر؛ نسخهٔ فعلی رویداد بیرونی تعریف نمی‌کند.</summary>
public sealed class UserPreferenceOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => UserPreferenceDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(UserPreferenceDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata) => null;

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType) =>
        throw new InvalidOperationException("UserPreference integration event is not registered.");

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) => null;
}
