using Tooba.BuildingBlocks;
using Tooba.Party.Domain.Enums;
using Tooba.Party.Domain.Events;

namespace Tooba.Party.Domain.Aggregates;

/// <summary>
/// ریشهٔ کسب‌وکار شخص/سازمان. نام CLR با namespace ماژول یکی نیست تا ابهام Tooba.Party پیش نیاید.
/// Tenant نیست، UserAccount نیست، و credential ورود ندارد.
/// </summary>
public sealed class BusinessParty : IHasDomainEvents
{
    private readonly DomainEventCollector _domainEvents = new();

    /// <summary>
    /// شناسهٔ پایدار Party در schema همین ماژول.
    /// </summary>
    public Guid PartyId { get; init; }

    /// <summary>
    /// شخص یا سازمان. ارث از Identity نیست.
    /// </summary>
    public PartyKind Kind { get; init; }

    /// <summary>
    /// نام نمایشی کسب‌وکار؛ ایمیل ورود Identity نیست.
    /// </summary>
    public string DisplayName { get; set; } = "";

    /// <summary>
    /// نام حقوقی اختیاری. قانون مالیاتی کشور اینجا قفل نمی‌شود.
    /// </summary>
    public string? LegalName { get; set; }

    /// <summary>
    /// حداکثر طول توضیح عملیاتی سازمان.
    /// </summary>
    public const int DescriptionMaxLength = 1000;

    /// <summary>
    /// حداکثر طول تلفن پشتیبانی سازمان.
    /// </summary>
    public const int SupportPhoneMaxLength = 32;

    /// <summary>
    /// حداکثر طول ایمیل پشتیبانی سازمان.
    /// </summary>
    public const int SupportEmailMaxLength = 256;

    /// <summary>
    /// حداکثر طول نشانی عملیاتی سازمان.
    /// </summary>
    public const int AddressLineMaxLength = 512;

    /// <summary>
    /// توضیح کوتاه عملیاتی سازمان؛ برای Person معنا ندارد.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// تلفن پشتیبانی سازمان؛ شناسهٔ ورود Identity نیست.
    /// </summary>
    public string? SupportPhone { get; private set; }

    /// <summary>
    /// ایمیل پشتیبانی سازمان؛ credential ورود نیست.
    /// </summary>
    public string? SupportEmail { get; private set; }

    /// <summary>
    /// خط نشانی عملیاتی سازمان.
    /// </summary>
    public string? AddressLine { get; private set; }

    /// <summary>
    /// وضعیت Party در منبع حقیقت محلی.
    /// </summary>
    public PartyStatus Status { get; set; }

    /// <summary>
    /// زمان ایجاد.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// زمان آخرین تغییر فرادادهٔ Party.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// قابلیت‌های تجاری سازمان. برای Person معمولاً خالی می‌ماند.
    /// </summary>
    public List<PartyCapability> Capabilities { get; } = [];

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.Events;

    /// <summary>
    /// Person کسب‌وکار می‌سازد بدون کپی ایمیل/تلفن ورود.
    /// </summary>
    public static BusinessParty CreatePerson(string displayName, DateTimeOffset now) =>
        Create(PartyKind.Person, displayName, legalName: null, now);

    /// <summary>
    /// سازمان می‌سازد بدون قفل به یک نقش تجاری واحد.
    /// </summary>
    public static BusinessParty CreateOrganization(string displayName, string? legalName, DateTimeOffset now) =>
        Create(PartyKind.Organization, displayName, legalName, now);

    /// <summary>
    /// قابلیت تجاری را بدون enum SellerOnly به سازمان می‌چسباند.
    /// </summary>
    public PartyCapability GrantCapability(string capabilityCode, DateTimeOffset now)
    {
        if (Kind != PartyKind.Organization)
        {
            throw new InvalidOperationException("قابلیت تجاری فقط روی Organization معنا دارد.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(capabilityCode);
        var granted = new PartyCapability
        {
            Id = UuidV7.New(),
            PartyId = PartyId,
            CapabilityCode = capabilityCode.Trim().ToLowerInvariant(),
            CreatedAt = now,
        };
        Capabilities.Add(granted);
        UpdatedAt = now;
        return granted;
    }

    /// <summary>
    /// پروفایل عملیاتی Organization را به‌روز می‌کند؛ برای Person رد می‌شود.
    /// </summary>
    public void UpdateOrganizationProfile(
        string displayName,
        string? legalName,
        string? description,
        string? supportPhone,
        string? supportEmail,
        string? addressLine,
        DateTimeOffset now)
    {
        if (Kind != PartyKind.Organization)
        {
            throw new InvalidOperationException("پروفایل سازمانی فقط برای Organization معنا دارد.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        DisplayName = displayName.Trim();
        if (DisplayName.Length > 256)
        {
            throw new InvalidOperationException("نام نمایشی سازمان بیش از حد بلند است.");
        }

        LegalName = OptionalBounded(legalName, 256, "نام حقوقی بیش از حد بلند است.");
        Description = OptionalBounded(description, DescriptionMaxLength, "توضیح سازمان بیش از حد بلند است.");
        SupportPhone = OptionalBounded(supportPhone, SupportPhoneMaxLength, "تلفن پشتیبانی بیش از حد بلند است.");
        SupportEmail = OptionalBounded(supportEmail, SupportEmailMaxLength, "ایمیل پشتیبانی بیش از حد بلند است.");
        AddressLine = OptionalBounded(addressLine, AddressLineMaxLength, "نشانی سازمان بیش از حد بلند است.");
        UpdatedAt = now;
    }

    private static string? OptionalBounded(string? value, int max, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > max)
        {
            throw new InvalidOperationException(message);
        }

        return trimmed;
    }

    /// <summary>
    /// رویداد دامنه را صف می‌کند؛ تماس SpiceDB نیست.
    /// </summary>
    public void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();

    private static BusinessParty Create(PartyKind kind, string displayName, string? legalName, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        return new BusinessParty
        {
            PartyId = UuidV7.New(),
            Kind = kind,
            DisplayName = displayName.Trim(),
            LegalName = string.IsNullOrWhiteSpace(legalName) ? null : legalName.Trim(),
            Status = PartyStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }
}
