using Tooba.BuildingBlocks;
using Tooba.Offer.Domain;

namespace Tooba.Pricing.Domain;

/// <summary>
/// وضعیت رکورد قیمت نوشته‌شده. موجودی، انتشار Catalog، و نرخ FX را نشان نمی‌دهد.
/// </summary>
public enum PriceStatus
{
    /// <summary>
    /// پیش‌نویس قیمت نوشته‌شده. هنوز برای انتخاب پایه فعال نیست.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// قیمت پایهٔ فعال در بازهٔ اعتبار. به‌تنهایی قابل‌خرید بودن Offer نیست.
    /// </summary>
    Active = 1,

    /// <summary>
    /// قیمت از انتخاب خارج شده است. حذف Product یا Offer نیست.
    /// </summary>
    Retired = 2,
}

/// <summary>
/// گونهٔ محدودکنندهٔ قیمت. فعلاً فقط پایه است تا قیمت مشتری/سازمان/قرارداد بعداً اضافه شود.
/// </summary>
public enum PriceQualifierKind
{
    /// <summary>
    /// قیمت پایهٔ کانال/بازار بدون مشتری یا قرارداد.
    /// </summary>
    Base = 0,
}

/// <summary>
/// کد ارز ISO. از Locale یا Market حدس زده نمی‌شود و تومان نمایشی با ریال مخلوط نمی‌شود.
/// </summary>
public readonly record struct CurrencyCode
{
    /// <summary>
    /// سه حرف بزرگ ISO مثل IRR یا USD.
    /// </summary>
    public string Value { get; }

    private CurrencyCode(string value) => Value = value;

    /// <summary>
    /// کد ارز را نرمال می‌کند. تومان/IRT ممنوع است چون منبع حقیقت ریال (IRR) است.
    /// </summary>
    public static CurrencyCode Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException("کد ارز خالی نیست؛ از Locale هم استنباط نمی‌شود.");
        }

        var code = raw.Trim().ToUpperInvariant();
        if (code is "TMN" or "IRT" or "TOMAN")
        {
            throw new InvalidOperationException("تومان واحد نمایش است نه ارز ذخیره‌شده؛ مبلغ نوشته‌شده باید IRR باشد.");
        }

        if (code.Length != 3 || !code.All(char.IsAsciiLetter))
        {
            throw new InvalidOperationException("ارز باید کد سه حرفی ISO باشد نه زبان UI.");
        }

        return new CurrencyCode(code);
    }

    /// <summary>
    /// مقیاس اعشار برای گرد کردن. IRR بدون اعشار؛ بیشتر ارزها دو رقم.
    /// </summary>
    public int Scale => Value is "IRR" or "JPY" or "KRW" ? 0 : 2;

    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>
/// مبلغ نوشته‌شده با ارز صریح. ممیز شناور نیست و مالیات داخل مبلغ نیست.
/// </summary>
public readonly record struct Money
{
    /// <summary>
    /// مقدار پس از گرد کردن با AwayFromZero مطابق مقیاس ارز.
    /// </summary>
    public decimal Amount { get; }

    /// <summary>
    /// ارز مبلغ نوشته‌شده. تبدیل FX اینجا ذخیره نمی‌شود.
    /// </summary>
    public CurrencyCode Currency { get; }

    private Money(decimal amount, CurrencyCode currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>
    /// Money می‌سازد و به مقیاس ارز گرد می‌کند. نرخ ارز خارجی را به‌جای مبلغ نوشته‌شده نمی‌گذارد.
    /// </summary>
    public static Money Create(decimal amount, string currencyCode)
    {
        if (amount < 0)
        {
            throw new InvalidOperationException("مبلغ پایه منفی نیست.");
        }

        var currency = CurrencyCode.Parse(currencyCode);
        var rounded = decimal.Round(amount, currency.Scale, MidpointRounding.AwayFromZero);
        return new Money(rounded, currency);
    }

    /// <summary>
    /// مبلغ بدون مالیات است؛ VAT جدا محاسبه می‌شود.
    /// </summary>
    public bool IsTaxExclusive => true;
}

/// <summary>
/// هویت بازار تجاری. زبان UI نیست و لزوماً یک ارز یکتا ندارد.
/// </summary>
public readonly record struct MarketCode
{
    /// <summary>
    /// کد پایدار بازار مثل IR یا UK.
    /// </summary>
    public string Value { get; }

    private MarketCode(string value) => Value = value;

    /// <summary>
    /// کد بازار را نرمال می‌کند. فقط ایران در schema قفل نمی‌شود.
    /// </summary>
    public static MarketCode Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException("بازار خالی نیست و با Locale یکی نیست.");
        }

        var code = raw.Trim().ToUpperInvariant();
        if (code.Length is < 2 or > 16 || !code.All(ch => char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_'))
        {
            throw new InvalidOperationException("کد بازار باید کوتاه و پایدار باشد نه نام زبان.");
        }

        return new MarketCode(code);
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>
/// قیمت نوشته‌شده برای یک Offer در بازار و کانال و ارز. موجودی و مالیات محاسبه‌شده اینجا نیست.
/// </summary>
public sealed class AuthoredPrice : IHasDomainEvents
{
    private readonly DomainEventCollector _domainEvents = new();

    /// <summary>
    /// شناسهٔ پایدار قیمت.
    /// </summary>
    public Guid PriceId { get; init; }

    /// <summary>
    /// Offer هدف؛ FK به schema offer نیست.
    /// </summary>
    public Guid OfferId { get; init; }

    /// <summary>
    /// بازار تجاری. Locale نیست.
    /// </summary>
    public string Market { get; init; } = string.Empty;

    /// <summary>
    /// کانال فروش همان مفهوم Offer است نه فهرست جدا.
    /// </summary>
    public SalesChannel Channel { get; init; }

    /// <summary>
    /// ارز نوشته‌شده.
    /// </summary>
    public string Currency { get; private set; } = string.Empty;

    /// <summary>
    /// مبلغ بدون مالیات پس از گرد کردن.
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// شروع اعتبار به UTC.
    /// </summary>
    public DateTimeOffset ValidFrom { get; init; }

    /// <summary>
    /// پایان اعتبار اختیاری به UTC. جلالی ذخیره نمی‌شود.
    /// </summary>
    public DateTimeOffset? ValidTo { get; private set; }

    /// <summary>
    /// وضعیت انتخاب.
    /// </summary>
    public PriceStatus Status { get; private set; }

    /// <summary>
    /// درز محدودکننده برای قیمت مشتری/سازمان/قرارداد آینده.
    /// </summary>
    public PriceQualifierKind QualifierKind { get; init; }

    /// <summary>
    /// کلید محدودکنندهٔ آینده؛ برای قیمت پایه تهی است.
    /// </summary>
    public string? QualifierKey { get; init; }

    /// <summary>
    /// زمان ایجاد.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// زمان به‌روزرسانی.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.Events;

    /// <summary>
    /// قیمت نوشته‌شده می‌سازد. قابل‌خرید بودن و نرخ FX را اعلام نمی‌کند.
    /// </summary>
    public static AuthoredPrice Create(
        Guid offerId,
        string marketCode,
        SalesChannel channel,
        decimal amount,
        string currencyCode,
        DateTimeOffset validFrom,
        DateTimeOffset? validTo,
        DateTimeOffset now)
    {
        var market = MarketCode.Parse(marketCode);
        var money = Money.Create(amount, currencyCode);
        if (validTo is { } to && to <= validFrom)
        {
            throw new InvalidOperationException("پایان اعتبار باید بعد از شروع باشد.");
        }

        var price = new AuthoredPrice
        {
            PriceId = UuidV7.New(),
            OfferId = offerId,
            Market = market.Value,
            Channel = channel,
            Currency = money.Currency.Value,
            Amount = money.Amount,
            ValidFrom = validFrom,
            ValidTo = validTo,
            Status = PriceStatus.Draft,
            QualifierKind = PriceQualifierKind.Base,
            QualifierKey = null,
            CreatedAt = now,
            UpdatedAt = now,
        };
        price._domainEvents.Add(new PriceCreatedDomainEvent(price));
        return price;
    }

    /// <summary>
    /// قیمت را برای انتخاب پایه فعال می‌کند. موجودی را تضمین نمی‌کند.
    /// </summary>
    public void Activate(DateTimeOffset now)
    {
        if (Status == PriceStatus.Retired)
        {
            throw new InvalidOperationException("قیمت بازنشسته دوباره فعال نمی‌شود؛ رکورد جدید بنویسید.");
        }

        Status = PriceStatus.Active;
        UpdatedAt = now;
        _domainEvents.Add(new PriceActivatedDomainEvent(this));
    }

    /// <summary>
    /// مبلغ نوشته‌شده را عوض می‌کند. نتیجهٔ FX را جای حقیقت نمی‌گذارد.
    /// </summary>
    public void ChangeAmount(decimal amount, string currencyCode, DateTimeOffset now)
    {
        if (Status == PriceStatus.Retired)
        {
            throw new InvalidOperationException("قیمت بازنشسته ویرایش نمی‌شود.");
        }

        var money = Money.Create(amount, currencyCode);
        if (!string.Equals(money.Currency.Value, Currency, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("تغییر ارز یک قیمت نوشته‌شده، قیمت جدیدی می‌خواهد نه تبدیل خاموش FX.");
        }

        Amount = money.Amount;
        UpdatedAt = now;
        _domainEvents.Add(new PriceChangedDomainEvent(this));
    }

    /// <summary>
    /// قیمت را از انتخاب خارج می‌کند.
    /// </summary>
    public void Expire(DateTimeOffset now)
    {
        if (Status == PriceStatus.Retired)
        {
            return;
        }

        ValidTo = ValidTo is { } existing && existing < now ? existing : now;
        Status = PriceStatus.Retired;
        UpdatedAt = now;
        _domainEvents.Add(new PriceExpiredDomainEvent(this));
    }

    /// <summary>
    /// آیا در Instant داده‌شده برای انتخاب پایه معتبر است.
    /// </summary>
    public bool IsEffectiveAt(DateTimeOffset at) =>
        Status == PriceStatus.Active
        && at >= ValidFrom
        && (ValidTo is null || at < ValidTo);

    /// <summary>
    /// همپوشانی بازه با رکورد دیگر روی همان کلید انتخاب.
    /// </summary>
    public bool Overlaps(AuthoredPrice other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var thisEnd = ValidTo ?? DateTimeOffset.MaxValue;
        var otherEnd = other.ValidTo ?? DateTimeOffset.MaxValue;
        return ValidFrom < otherEnd && other.ValidFrom < thisEnd;
    }

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();
}

/// <summary>
/// رویداد ایجاد قیمت نوشته‌شده.
/// </summary>
public sealed class PriceCreatedDomainEvent : IDomainEvent
{
    /// <summary>
    /// از ریشه می‌سازد.
    /// </summary>
    public PriceCreatedDomainEvent(AuthoredPrice price)
    {
        ArgumentNullException.ThrowIfNull(price);
        PriceId = price.PriceId;
        OfferId = price.OfferId;
        Metadata = EventMetadataFactory.ForDomain("pricing.price_created.domain");
    }

    /// <summary>
    /// قیمت ایجادشده.
    /// </summary>
    public Guid PriceId { get; }

    /// <summary>
    /// Offer هدف.
    /// </summary>
    public Guid OfferId { get; }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }
}

/// <summary>
/// رویداد فعال‌سازی قیمت پایه.
/// </summary>
public sealed class PriceActivatedDomainEvent : IDomainEvent
{
    /// <summary>
    /// از ریشه می‌سازد.
    /// </summary>
    public PriceActivatedDomainEvent(AuthoredPrice price)
    {
        ArgumentNullException.ThrowIfNull(price);
        PriceId = price.PriceId;
        Metadata = EventMetadataFactory.ForDomain("pricing.price_activated.domain");
    }

    /// <summary>
    /// قیمت فعال‌شده.
    /// </summary>
    public Guid PriceId { get; }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }
}

/// <summary>
/// رویداد تغییر مبلغ نوشته‌شده.
/// </summary>
public sealed class PriceChangedDomainEvent : IDomainEvent
{
    /// <summary>
    /// از ریشه می‌سازد.
    /// </summary>
    public PriceChangedDomainEvent(AuthoredPrice price)
    {
        ArgumentNullException.ThrowIfNull(price);
        PriceId = price.PriceId;
        Metadata = EventMetadataFactory.ForDomain("pricing.price_changed.domain");
    }

    /// <summary>
    /// قیمت تغییر یافته.
    /// </summary>
    public Guid PriceId { get; }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }
}

/// <summary>
/// رویداد خروج قیمت از انتخاب.
/// </summary>
public sealed class PriceExpiredDomainEvent : IDomainEvent
{
    /// <summary>
    /// از ریشه می‌سازد.
    /// </summary>
    public PriceExpiredDomainEvent(AuthoredPrice price)
    {
        ArgumentNullException.ThrowIfNull(price);
        PriceId = price.PriceId;
        Metadata = EventMetadataFactory.ForDomain("pricing.price_expired.domain");
    }

    /// <summary>
    /// قیمت منقضی.
    /// </summary>
    public Guid PriceId { get; }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }
}
