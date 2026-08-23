using NodaTime;
using Tooba.BuildingBlocks;

namespace Tooba.Persistence;

/// <summary>
/// شکل CLR ردیف Outbox. جدول سراسری مشترک بین ماژول‌ها نیست؛ هر DbContext آن را به schema خودش map می‌کند.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>
    /// کلید ردیف؛ برابر EventId قرارداد Integration است.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// زمان وقوع رویداد؛ ترتیب سراسری بین Tenantها تضمین نمی‌شود.
    /// </summary>
    public Instant OccurredAt { get; set; }

    /// <summary>
    /// نام قراردادی type map (مثلاً <c>platform_probe.record_created.v1</c>) نه AssemblyQualifiedName.
    /// </summary>
    public string EventType { get; set; } = "";

    /// <summary>
    /// بدنهٔ JSON فیلدهای کسب‌وکار بدون $type؛ هرگز لاگ نشود.
    /// </summary>
    public string Payload { get; set; } = "";

    /// <summary>
    /// همبستگی اختیاری با درخواست.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// نسخهٔ قرارداد Integration.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// TenantId پایدار یا تهی برای Marketplace. از Host استخراج نشده است.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// برچسب استقرار در زمان persist.
    /// </summary>
    public string DeploymentId { get; set; } = "";

    /// <summary>
    /// نام Edition ذخیره‌شده (Marketplace / SingleStore).
    /// </summary>
    public string Edition { get; set; } = "";

    /// <summary>
    /// زمان موفقیت انتشار به transport پایدار؛ تهی یعنی هنوز pending. موفقیت مصرف‌کننده نیست.
    /// </summary>
    public Instant? ProcessedAt { get; set; }

    /// <summary>
    /// تعداد دفعات claim؛ برای سقف retry و dead-letter.
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// زودترین زمان مجاز claim بعدی پس از شکست.
    /// </summary>
    public Instant? NextAttemptAt { get; set; }

    /// <summary>
    /// زمان انتقال به dead-letter پس از عبور از MaxAttempts.
    /// </summary>
    public Instant? DeadLetteredAt { get; set; }

    /// <summary>
    /// خلاصهٔ خطای بهداشتی‌شده؛ بدون secret، stack و payload.
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// قفل نرم claim تا worker دیگر همان ردیف را با SKIP LOCKED برندارد.
    /// </summary>
    public Instant? LockedUntil { get; set; }
}

/// <summary>
/// ثبت ماژول برای Outbox بدون چسباندن نام PlatformProbe به هستهٔ عمومی.
/// Host فقط در ترکیب نمونه، پیاده‌سازی ماژول را DI می‌کند.
/// </summary>
public interface IOutboxModuleRegistration
{
    /// <summary>
    /// schema مالک ماژول؛ جدول Outbox داخل همین schema است.
    /// </summary>
    string Schema { get; }

    /// <summary>
    /// نام فیزیکی جدول؛ قرارداد Tooba برابر <c>outbox_messages</c> است.
    /// </summary>
    string TableName { get; }

    /// <summary>
    /// نوع DbContext همین ماژول تا interceptor فقط روی همان context بنویسد.
    /// </summary>
    Type DbContextType { get; }

    /// <summary>
    /// ترجمهٔ صریح Domain → Integration. null یعنی این واقعیت دامنه منتشر نشود.
    /// </summary>
    /// <param name="domainEvent">رویداد دامنه از ChangeTracker.</param>
    /// <param name="metadata">فرادادهٔ تکمیل‌شده از زمینهٔ تجارت.</param>
    IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata);

    /// <summary>
    /// نام قراردادی برای نوع Integration جهت ستون event_type.
    /// </summary>
    string GetEventTypeName(Type integrationEventType);

    /// <summary>
    /// CLR type از نام قراردادی؛ ناشناخته یعنی deserialization رد شود نه Type.GetType آزاد.
    /// </summary>
    Type? ResolveEventClrType(string eventTypeName);
}

/// <summary>
/// سریالایزر JSON با type map صریح. TypeNameHandling / polymorphic CLR ممنوع است.
/// </summary>
public interface IIntegrationEventSerializer
{
    /// <summary>
    /// فقط فیلدهای کسب‌وکار را JSON می‌کند؛ Metadata ستون جدا است.
    /// </summary>
    string SerializePayload(IIntegrationEvent integrationEvent);

    /// <summary>
    /// رویداد را با type map و فرادادهٔ ستون‌ها بازسازی می‌کند. payload منبع Tenant نیست.
    /// </summary>
    IIntegrationEvent Deserialize(OutboxMessage message);
}

/// <summary>
/// فروشگاه dispatcher با SQL خام و <c>FOR UPDATE SKIP LOCKED</c>. DbContext بین Tenantها reuse نمی‌شود.
/// </summary>
public interface IOutboxDispatcherStore
{
    /// <summary>
    /// دسته‌ای از ردیف‌های قابل‌پردازش را claim می‌کند.
    /// </summary>
    Task<IReadOnlyList<OutboxMessage>> ClaimAsync(
        string connectionString,
        string schema,
        string tableName,
        int batchSize,
        int lockSeconds,
        CancellationToken cancellationToken);

    /// <summary>
    /// انتشار موفق را با processed_at علامت می‌زند.
    /// </summary>
    Task MarkProcessedAsync(
        string connectionString,
        string schema,
        string tableName,
        Guid id,
        CancellationToken cancellationToken);

    /// <summary>
    /// شکست قابل‌retry: LastError بهداشتی، next_attempt_at، آزاد کردن قفل.
    /// </summary>
    Task MarkRetryAsync(
        string connectionString,
        string schema,
        string tableName,
        Guid id,
        Instant nextAttemptAt,
        string lastError,
        CancellationToken cancellationToken);

    /// <summary>
    /// عبور از سقف تلاش: dead_lettered_at بدون حذف payload از دیسک (اما لاگ نمی‌شود).
    /// </summary>
    Task MarkDeadLetterAsync(
        string connectionString,
        string schema,
        string tableName,
        Guid id,
        string lastError,
        CancellationToken cancellationToken);
}

/// <summary>
/// هدف polling کارگر: یک اتصال منطقی، نه Host درخواست.
/// </summary>
/// <param name="Edition">Edition فرآیند.</param>
/// <param name="TenantId">در Single-Store هویت پایدار؛ در Marketplace تهی.</param>
/// <param name="ConnectionReference">مرجع اتصال این پایگاه.</param>
/// <param name="DeploymentId">برچسب استقرار.</param>
public readonly record struct OutboxPollTarget(
    ToobaEdition Edition,
    string? TenantId,
    ConnectionReference ConnectionReference,
    string DeploymentId);

/// <summary>
/// منبع فهرست پایگاه‌هایی که کارگر باید جداگانه poll کند.
/// </summary>
public interface IOutboxPollTargetSource
{
    /// <summary>
    /// Marketplace: فقط DB مارکت. Single-Store: Tenantهای Active. Unset: خالی.
    /// </summary>
    IReadOnlyList<OutboxPollTarget> GetTargets();
}
