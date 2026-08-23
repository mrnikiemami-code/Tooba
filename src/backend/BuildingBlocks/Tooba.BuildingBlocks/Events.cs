namespace Tooba.BuildingBlocks;

/// <summary>
/// فرادادهٔ پایدار رویداد برای همبستگی تله‌متری و بازسازی زمینهٔ کارگر.
/// TenantId از Host ساخته نمی‌شود؛ اگر تهی باشد یعنی Marketplace یا رویداد بدون Tenant.
/// </summary>
/// <param name="EventId">شناسهٔ پایدار رویداد؛ معمولاً همان کلید Outbox.</param>
/// <param name="OccurredAt">زمان وقوع به‌وقت UTC؛ منبع ترتیب سراسری تضمین‌شده نیست.</param>
/// <param name="EventType">نام قراردادی ثبت‌شده در type map؛ نام CLR برای deserialization نیست.</param>
/// <param name="CorrelationId">همبستگی اختیاری با درخواست/فرآیند؛ جایگزین Audit نیست.</param>
/// <param name="Version">نسخهٔ قرارداد رویداد برای سازگاری روبه‌عقب.</param>
/// <param name="TenantId">هویت پایدار Tenant در Single-Store یا تهی در Marketplace.</param>
/// <param name="DeploymentId">برچسب استقرار فرآیند؛ هویت Tenant نیست.</param>
/// <param name="Edition">Edition فرآیند در زمان وقوع؛ کارگر نباید Edition را از Host حدس بزند.</param>
public sealed record EventMetadata(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string EventType,
    string? CorrelationId,
    int Version,
    string? TenantId,
    string DeploymentId,
    ToobaEdition Edition);

/// <summary>
/// واقعیت کسب‌وکار داخل مرز یک ماژول. هر Domain Event به‌طور خودکار Integration Event نیست و نباید از SaveChanges به مصرف‌کننده برسد.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// فرادادهٔ رویداد دامنه در زمان Raise؛ Tenant نهایی روی ردیف Outbox از زمینهٔ تجارت تکمیل می‌شود.
    /// </summary>
    EventMetadata Metadata { get; }
}

/// <summary>
/// واقعیت پایدار و نسخه‌بندی‌شده برای ماژول‌ها/سرویس‌های دیگر. فقط از روی ترجمهٔ صریح و پس از persist در Outbox منتشر می‌شود.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>
    /// فرادادهٔ قرارداد خارجی؛ کارگر زمینه را از این مقادیر و registry بازسازی می‌کند نه از Host.
    /// </summary>
    EventMetadata Metadata { get; }
}

/// <summary>
/// جمع‌آوری رویدادهای دامنه قبل از SaveChanges. ترکیب است نه ارث‌بری اجباری موجودیت.
/// </summary>
public interface IDomainEventCollector
{
    /// <summary>
    /// رویدادهای هنوز تخلیه‌نشده به ترتیب Raise.
    /// </summary>
    IReadOnlyList<IDomainEvent> Events { get; }

    /// <summary>
    /// یک واقعیت دامنه را برای همان تراکنش محلی صف می‌کند؛ انتشار نیست.
    /// </summary>
    /// <param name="domainEvent">رویداد داخل مرز ماژول.</param>
    void Add(IDomainEvent domainEvent);

    /// <summary>
    /// صف را پس از موفقیت SaveChanges خالی می‌کند تا retry دامنه رویداد تکراری نسازد.
    /// </summary>
    void Clear();
}

/// <summary>
/// موجودیتی که رویداد دامنه نگه می‌دارد تا interceptor همان تراکنش Outbox را پر کند.
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>
    /// صف رویدادهای دامنهٔ هنوز persistنشده به‌عنوان Integration.
    /// </summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// پس از commit موفق فراخوانی می‌شود؛ در rollback نباید صدا زده شود.
    /// </summary>
    void ClearDomainEvents();
}

/// <summary>
/// پیاده‌سازی سادهٔ صف رویداد دامنه برای ترکیب در موجودیت‌ها.
/// </summary>
public sealed class DomainEventCollector : IDomainEventCollector
{
    private readonly List<IDomainEvent> _events = [];

    /// <inheritdoc />
    public IReadOnlyList<IDomainEvent> Events => _events;

    /// <inheritdoc />
    public void Add(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _events.Add(domainEvent);
    }

    /// <inheritdoc />
    public void Clear() => _events.Clear();
}

/// <summary>
/// مصرف‌کنندهٔ درون‌فرآیندی یک نوع Integration Event. از SaveChanges صدا زده نمی‌شود.
/// تحویل at-least-once است؛ مصرف‌کننده بایدidempotent باشد یا بعداً از Inbox استفاده کند.
/// </summary>
/// <typeparam name="TEvent">نوع قرارداد Integration ثبت‌شده در type map.</typeparam>
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    /// <summary>
    /// پردازش قرارداد خارجی پس از claim از Outbox. payload نباید لاگ شود.
    /// </summary>
    /// <param name="integrationEvent">رویداد بازسازی‌شده با فرادادهٔ ستون‌های Outbox.</param>
    /// <param name="cancellationToken">لغو حلقهٔ dispatcher.</param>
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken);
}

/// <summary>
/// مرز Tooba برای انتشار Integration Event پس از persist در Outbox. پیاده‌سازی پیش‌فرض تولید MassTransit است؛ کد کسب‌وکار به IBus وابسته نمی‌شود.
/// </summary>
public interface IIntegrationEventPublisher
{
    /// <summary>
    /// رویداد را به transport پایدار می‌سپارد. شکست اینجا یعنی retry/dead-letter Outbox، نه retry مصرف‌کننده.
    /// </summary>
    /// <param name="integrationEvent">رویداد از type map؛ deserialization چندریختی CLR نیست.</param>
    /// <param name="cancellationToken">لغو انتشار.</param>
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}

/// <summary>
/// درز Inbox برای جلوگیری از پردازش تکراری در مصرف‌کننده. Outbox جایگزین Inbox نیست و جدول Inbox در این Task پیاده نمی‌شود.
/// </summary>
public interface IInboxProcessedStore
{
    /// <summary>
    /// تلاش برای ثبت «این مصرف‌کننده این EventId را دیده است». پیاده‌سازی کامل به Task بعدی موکول است.
    /// </summary>
    /// <param name="consumerName">نام پایدار مصرف‌کننده داخل یک ماژول.</param>
    /// <param name="eventId">شناسهٔ رویداد Integration.</param>
    /// <param name="cancellationToken">لغو.</param>
    /// <returns>true اگر اجازهٔ پردازش داده شود؛ قرارداد نهایی با جدول Inbox مشخص می‌شود.</returns>
    Task<bool> TryMarkProcessedAsync(string consumerName, Guid eventId, CancellationToken cancellationToken);
}

/// <summary>
/// انتساب زمینهٔ تجارت بدون resolve از Host. مخصوص کارگر Outbox و تست؛ هدر درخواست مرجع Tenant نیست.
/// </summary>
public interface ICommerceContextAssigner
{
    /// <summary>
    /// زمینهٔ بازسازی‌شده از Outbox + registry را روی scope جاری می‌گذارد تا handlerها Host نخوانند.
    /// </summary>
    /// <param name="context">زمینهٔ immutable ساخته‌شده از TenantId پایدار پیام.</param>
    void Assign(CommerceContext context);
}

/// <summary>
/// سازندهٔ فرادادهٔ اولیه در زمان Raise دامنه؛ Tenant/Edition نهایی هنگام نوشتن Outbox تکمیل می‌شود.
/// </summary>
public static class EventMetadataFactory
{
    /// <summary>
    /// فرادادهٔ موقت دامنه با شناسهٔ UUID v7. Edition/Tenant در interceptor از <see cref="CommerceContext"/> می‌آید.
    /// </summary>
    /// <param name="eventType">نام قراردادی یا نام موقت دامنه؛ Integration از type map مقدار نهایی می‌گیرد.</param>
    /// <param name="version">نسخهٔ قرارداد.</param>
    /// <param name="correlationId">همبستگی اختیاری.</param>
    public static EventMetadata ForDomain(string eventType, int version = 1, string? correlationId = null) =>
        new(
            EventId: UuidV7.New(),
            OccurredAt: DateTimeOffset.UtcNow,
            EventType: eventType,
            CorrelationId: correlationId,
            Version: version,
            TenantId: null,
            DeploymentId: string.Empty,
            Edition: ToobaEdition.Unset);
}
