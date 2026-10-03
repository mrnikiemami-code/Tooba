using Tooba.BuildingBlocks;
using Tooba.Persistence;

namespace Tooba.BulkInquiry.Infrastructure.Persistence;

/// <summary>ثبت Outbox BulkInquiry؛ نسخهٔ پایه هنوز رویداد بیرونی منتشر نمی‌کند.</summary>
public sealed class BulkInquiryOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => BulkInquiryDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(BulkInquiryDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata) => null;

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType) =>
        throw new InvalidOperationException("BulkInquiry integration event is not registered.");

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) => null;
}
