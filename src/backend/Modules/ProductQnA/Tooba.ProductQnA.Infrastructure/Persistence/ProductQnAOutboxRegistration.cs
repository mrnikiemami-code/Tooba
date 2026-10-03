using Tooba.BuildingBlocks;
using Tooba.Persistence;

namespace Tooba.ProductQnA.Infrastructure.Persistence;

/// <summary>ثبت Outbox ProductQnA؛ نسخهٔ پایه هنوز رویداد بیرونی منتشر نمی‌کند.</summary>
public sealed class ProductQnAOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => ProductQnADbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(ProductQnADbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata) => null;

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType) =>
        throw new InvalidOperationException("ProductQnA integration event is not registered.");

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) => null;
}
