using Tooba.BuildingBlocks;

namespace Tooba.Party.Domain;

/// <summary>
/// گونهٔ Party. شخص کسب‌وکار است نه UserAccount؛ سازمان هم ردیف User نیست.
/// Seller/Agency/Customer اینجا enum نهایی مجوز یا نقش ورود نیستند.
/// </summary>
public enum PartyKind
{
    /// <summary>
    /// موجودیت انسانی کسب‌وکار. اعتبار ورود Identity را کپی نمی‌کند.
    /// </summary>
    Person = 1,

    /// <summary>
    /// موجودیت سازمانی. قابلیت‌های تجاری بعدی با کدهای گسترش‌پذیر ثبت می‌شوند نه با SellerOnly.
    /// </summary>
    Organization = 2,
}

/// <summary>
/// وضعیت چرخهٔ عمر Party در منبع حقیقت کسب‌وکار. مجوز SpiceDB نیست.
/// </summary>
public enum PartyStatus
{
    /// <summary>
    /// Party برای پیوند و عضویت قابل استفاده است.
    /// </summary>
    Active = 0,

    /// <summary>
    /// Party از جریان کسب‌وکار کنار گذاشته شده؛ حذف سخت هویت ورود نیست.
    /// </summary>
    Disabled = 1,
}

/// <summary>
/// وضعیت عضویت. وجود عضویت به‌تنهایی همهٔ مجوزها را نمی‌دهد.
/// </summary>
public enum MembershipStatus
{
    /// <summary>
    /// پیوند کسب‌وکار برقرار است و می‌تواند به SpiceDB تصویر شود.
    /// </summary>
    Active = 0,

    /// <summary>
    /// پیوند پایان یافته؛ مجوز را SpiceDB schema تعیین می‌کند نه این مقدار به‌تنهایی.
    /// </summary>
    Ended = 1,
}

/// <summary>
/// کدهای رابطهٔ عضویت به‌عنوان انجمن کسب‌وکار. ستون Role/Permission نهایی نیستند.
/// </summary>
public static class MembershipRelationCodes
{
    /// <summary>
    /// عضو سازمان یا مرتبط با Party؛ معادل ماتریس مجوز محصول نیست.
    /// </summary>
    public const string Member = "member";
}

/// <summary>
/// کدهای رابطهٔ سازمان‌به‌سازمان. فهرست بستهٔ B2B نیست؛ کد جدید بدون بازنویسی هسته اضافه می‌شود.
/// </summary>
public static class OrganizationRelationCodes
{
    /// <summary>
    /// رابطهٔ والد/فرزند سازمانی برای سلسله‌مراتب آینده.
    /// </summary>
    public const string ParentOf = "parent_of";

    /// <summary>
    /// درز «فروشنده توسط» بدون قفل Seller module.
    /// </summary>
    public const string OperatedBy = "operated_by";

    /// <summary>
    /// درز نمایندگی آژانس بدون پورتال آژانس.
    /// </summary>
    public const string Represents = "represents";
}

/// <summary>
/// کد قابلیت تجاری گسترش‌پذیر روی سازمان. یک سازمان می‌تواند چند قابلیت داشته باشد.
/// </summary>
public static class PartyCapabilityCodes
{
    /// <summary>
    /// درز فروشنده؛ در این foundation فعال‌سازی onboarding نیست.
    /// </summary>
    public const string Seller = "seller";

    /// <summary>
    /// درز آژانس.
    /// </summary>
    public const string Agency = "agency";

    /// <summary>
    /// درز خریدار سازمانی.
    /// </summary>
    public const string CorporateBuyer = "corporate_buyer";
}

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

/// <summary>
/// قابلیت تجاری جدا از نوع Party و جدا از مجوز SpiceDB.
/// </summary>
public sealed class PartyCapability
{
    /// <summary>
    /// کلید ردیف قابلیت.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// سازمان مالک قابلیت. FK به Identity نیست.
    /// </summary>
    public Guid PartyId { get; init; }

    /// <summary>
    /// کد گسترش‌پذیر مثل seller/agency؛ ماتریس مجوز نیست.
    /// </summary>
    public string CapabilityCode { get; init; } = "";

    /// <summary>
    /// زمان اعطای قابلیت.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// پیوند صریح UserId مبهم Identity به Party. ستون PartyId روی UserAccount گذاشته نمی‌شود و FK بین‌ماژولی نیست.
/// </summary>
public sealed class UserPartyLink
{
    /// <summary>
    /// کلید پیوند.
    /// </summary>
    public Guid LinkId { get; init; }

    /// <summary>
    /// اصل ورود به‌صورت Guid مات. جدول identity.users اینجا join نمی‌شود.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Party مقصد پیوند، معمولاً Person.
    /// </summary>
    public Guid PartyId { get; init; }

    /// <summary>
    /// زمان ایجاد پیوند.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// پیوند User به Party را در مالکیت همین ماژول می‌سازد.
    /// </summary>
    public static UserPartyLink Bind(Guid userId, Guid partyId, DateTimeOffset now)
    {
        if (userId == Guid.Empty || partyId == Guid.Empty)
        {
            throw new ArgumentException("UserId و PartyId باید پایدار و غیرتهی باشند.");
        }

        return new UserPartyLink
        {
            LinkId = UuidV7.New(),
            UserId = userId,
            PartyId = partyId,
            CreatedAt = now,
        };
    }
}

/// <summary>
/// عضویت User در Party/سازمان. Membership برابر Authorization نیست و ستون Role نهایی ندارد.
/// </summary>
public sealed class PartyMembership : IHasDomainEvents
{
    private readonly DomainEventCollector _domainEvents = new();

    /// <summary>
    /// شناسهٔ پایدار عضویت.
    /// </summary>
    public Guid MembershipId { get; init; }

    /// <summary>
    /// اصل ورود مبهم.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Party/سازمان مقصد عضویت.
    /// </summary>
    public Guid PartyId { get; init; }

    /// <summary>
    /// وضعیت عضویت در منبع حقیقت Party.
    /// </summary>
    public MembershipStatus Status { get; set; }

    /// <summary>
    /// رابطهٔ کسب‌وکار (مثلاً member). مجوز view/edit اینجا ذخیره نمی‌شود.
    /// </summary>
    public string RelationCode { get; init; } = MembershipRelationCodes.Member;

    /// <summary>
    /// زمان ایجاد عضویت.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.Events;

    /// <summary>
    /// عضویت فعال می‌سازد و رویداد تصویرسازی مجوز را صف می‌کند، بدون تماس شبکه SpiceDB.
    /// </summary>
    public static PartyMembership Establish(Guid userId, Guid partyId, string relationCode, DateTimeOffset now)
    {
        if (userId == Guid.Empty || partyId == Guid.Empty)
        {
            throw new ArgumentException("UserId و PartyId عضویت نباید تهی باشند.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(relationCode);
        var membership = new PartyMembership
        {
            MembershipId = UuidV7.New(),
            UserId = userId,
            PartyId = partyId,
            Status = MembershipStatus.Active,
            RelationCode = relationCode.Trim().ToLowerInvariant(),
            CreatedAt = now,
        };
        membership._domainEvents.Add(new PartyMembershipEstablishedDomainEvent(membership));
        return membership;
    }

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();
}

/// <summary>
/// رابطهٔ typed سازمان‌به‌سازمان برای ساختارهای آینده. قوانین کامل B2B اینجا پیاده نمی‌شود.
/// </summary>
public sealed class OrganizationRelationship
{
    /// <summary>
    /// کلید رابطه.
    /// </summary>
    public Guid RelationshipId { get; init; }

    /// <summary>
    /// Party مبدأ.
    /// </summary>
    public Guid FromPartyId { get; init; }

    /// <summary>
    /// Party مقصد.
    /// </summary>
    public Guid ToPartyId { get; init; }

    /// <summary>
    /// کد گسترش‌پذیر رابطه؛ فهرست بستهٔ SellerOnly نیست.
    /// </summary>
    public string RelationCode { get; init; } = "";

    /// <summary>
    /// وضعیت رابطه در منبع حقیقت Party.
    /// </summary>
    public MembershipStatus Status { get; set; }

    /// <summary>
    /// زمان ایجاد.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// رابطهٔ سازمانی را بدون اجرای workflow فروشنده/آژانس ثبت می‌کند.
    /// </summary>
    public static OrganizationRelationship Connect(Guid fromPartyId, Guid toPartyId, string relationCode, DateTimeOffset now)
    {
        if (fromPartyId == Guid.Empty || toPartyId == Guid.Empty || fromPartyId == toPartyId)
        {
            throw new ArgumentException("رابطهٔ سازمانی باید دو Party متمایز داشته باشد.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(relationCode);
        return new OrganizationRelationship
        {
            RelationshipId = UuidV7.New(),
            FromPartyId = fromPartyId,
            ToPartyId = toPartyId,
            RelationCode = relationCode.Trim().ToLowerInvariant(),
            Status = MembershipStatus.Active,
            CreatedAt = now,
        };
    }
}

/// <summary>
/// واقعیت دامنهٔ برقراری عضویت. تماس SpiceDB نیست؛ ترجمه به Integration فقط از Outbox است.
/// </summary>
public sealed class PartyMembershipEstablishedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد را از عضویت persistشونده می‌سازد.
    /// </summary>
    public PartyMembershipEstablishedDomainEvent(PartyMembership membership)
    {
        ArgumentNullException.ThrowIfNull(membership);
        MembershipId = membership.MembershipId;
        UserId = membership.UserId;
        PartyId = membership.PartyId;
        RelationCode = membership.RelationCode;
        Metadata = EventMetadataFactory.ForDomain("party.membership_established.domain");
    }

    /// <summary>
    /// عضویت منبع حقیقت.
    /// </summary>
    public Guid MembershipId { get; }

    /// <summary>
    /// اصل ورود برای تصویرسازی بعدی.
    /// </summary>
    public Guid UserId { get; }

    /// <summary>
    /// Party مقصد تصویرسازی.
    /// </summary>
    public Guid PartyId { get; }

    /// <summary>
    /// رابطهٔ کسب‌وکار؛ handler مجوز را از schema SpiceDB می‌گیرد نه از این رشته به‌عنوان Role.
    /// </summary>
    public string RelationCode { get; }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }
}
