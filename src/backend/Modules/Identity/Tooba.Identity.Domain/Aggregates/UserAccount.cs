using Tooba.BuildingBlocks;
using Tooba.Identity.Contracts;
using Tooba.Identity.Domain.Enums;
using Tooba.Identity.Domain.Rules;
using Tooba.Identity.Domain.Events;

namespace Tooba.Identity.Domain.Aggregates;

/// <summary>
/// اصل احراز هویت. پروفایل مشتری، سازمان فروشنده یا Tenant نیست.
/// </summary>
public sealed class UserAccount : IHasDomainEvents
{
    private readonly DomainEventCollector _domainEvents = new();

    /// <summary>
    /// شناسهٔ پایدار User برای تحویل به Authorization بعدی.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// وضعیت ورود.
    /// </summary>
    public UserAccountStatus Status { get; set; }

    /// <summary>
    /// زمان ایجاد.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// زمان آخرین تغییر وضعیت یا اعتبار.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// مهر امنیتی حساب. با تغییر رمز/بازنشانی/قفل افزایش می‌یابد تا نشست‌های قبلی Refresh نشوند. ماتریس مجوز محصول نیست.
    /// </summary>
    public int SecurityStamp { get; set; }

    /// <summary>
    /// اعتبار رمز در صورت ثبت؛ نبودن یعنی ورود رمزی ممکن نیست.
    /// </summary>
    public PasswordCredential? Password { get; set; }

    /// <summary>
    /// شناسه‌های ورود متعلق به همین User. جدول جدا است نه ستون ثابت روی User.
    /// </summary>
    public List<LoginIdentifier> Identifiers { get; } = [];

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.Events;

    /// <summary>
    /// User جدید با یک شناسه می‌سازد. فیلد کسب‌وکار Party اضافه نمی‌شود.
    /// </summary>
    public static UserAccount Register(LoginIdentifierKind kind, string rawIdentifier, DateTimeOffset now)
    {
        var (display, normalized) = LoginIdentifierNormalizer.Normalize(kind, rawIdentifier);
        var user = new UserAccount
        {
            UserId = UuidV7.New(),
            Status = UserAccountStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
        };
        user.Identifiers.Add(new LoginIdentifier
        {
            Id = UuidV7.New(),
            UserId = user.UserId,
            Kind = kind,
            DisplayValue = display,
            NormalizedValue = normalized,
            VerificationState = IdentifierVerificationState.Unverified,
            IsPreferred = true,
            CreatedAt = now,
        });
        user.Raise(new UserRegisteredDomainEvent(user.UserId));
        return user;
    }

    /// <summary>
    /// مهر امنیتی را جلو می‌برد تا Refresh نشست‌های قبلی با همان نسخه نامعتبر شود.
    /// </summary>
    public void BumpSecurityStamp(DateTimeOffset now)
    {
        SecurityStamp++;
        UpdatedAt = now;
    }

    /// <summary>
    /// شناسهٔ ورود اضافی به همین User وصل می‌کند.
    /// </summary>
    public LoginIdentifier AddIdentifier(LoginIdentifierKind kind, string rawIdentifier, DateTimeOffset now)
    {
        var (display, normalized) = LoginIdentifierNormalizer.Normalize(kind, rawIdentifier);
        var identifier = new LoginIdentifier
        {
            Id = UuidV7.New(),
            UserId = UserId,
            Kind = kind,
            DisplayValue = display,
            NormalizedValue = normalized,
            VerificationState = IdentifierVerificationState.Unverified,
            CreatedAt = now,
        };
        Identifiers.Add(identifier);
        UpdatedAt = now;
        return identifier;
    }

    /// <summary>
    /// رویداد دامنه را صف می‌کند؛ انتشار Integration نیست.
    /// </summary>
    public void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();
}
