using Tooba.BuildingBlocks;
using Tooba.ModuleContracts;
using Tooba.Persistence;
using Tooba.Story.Infrastructure.Persistence;

namespace Tooba.Story.Infrastructure.Messaging;

/// <summary>
/// ثبت Outbox ماژول Story.
/// نسخهٔ پایهٔ این ماژول هنوز رویداد یکپارچه‌سازی بیرونی منتشر نمی‌کند، بنابراین ترجمهٔ رویداد دامنه
/// به رویداد یکپارچه <c>null</c> است و نگاشت نوع رویداد بیرونی عمداً خطا می‌دهد تا مصرف ناخواسته
/// به‌جای رفتار مبهم، fail-fast شود.
/// </summary>
public sealed class StoryOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => StoryDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(StoryDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata) => null;

    /// <summary>
    /// نام نوع رویداد یکپارچه را برمی‌گرداند؛ Story هیچ رویداد یکپارچهٔ ثبت‌شده‌ای ندارد و این مسیر
    /// باید به‌صورت صریح خطا بدهد تا انتشار ناخواسته رخ ندهد.
    /// </summary>
    /// <param name="integrationEventType">نوع رویداد یکپارچهٔ درخواستی.</param>
    /// <returns>هرگز مقدار برنمی‌گرداند.</returns>
    /// <exception cref="InvalidOperationException">همیشه، چون Story رویداد یکپارچه ثبت‌شده ندارد.</exception>
    public string GetEventTypeName(Type integrationEventType) =>
        throw new InvalidOperationException("Story integration event is not registered.");

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) => null;
}
