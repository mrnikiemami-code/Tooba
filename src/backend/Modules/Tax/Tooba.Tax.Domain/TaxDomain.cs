using Tooba.BuildingBlocks;

namespace Tooba.Tax.Domain;

/// <summary>
/// نتیجهٔ محاسبهٔ مالیات. معافیت، نرخ صفر، نبودن قاعده و خطای محاسبه یکی نیستند.
/// </summary>
public enum TaxOutcome
{
    /// <summary>
    /// قاعدهٔ درصدی اعمال شد و مبلغ مالیات جدا از قیمت پایه است.
    /// </summary>
    Taxable = 0,

    /// <summary>
    /// معافیت صریح؛ صفر شدن مبلغ به‌معنای نرخ صفر یا نبودن قاعده نیست.
    /// </summary>
    Exempt = 1,

    /// <summary>
    /// قاعدهٔ قابل‌اعمال با نرخ صفر؛ معافیت یا خطای پیکربندی نیست.
    /// </summary>
    ZeroRated = 2,

    /// <summary>
    /// هیچ قاعدهٔ قابل‌اعمالی برای حوزه/بازار/طبقه در زمان محاسبات پیدا نشد.
    /// </summary>
    NoApplicableRule = 3,

    /// <summary>
    /// محاسبه به‌خاطر ابهام قاعده، ارز ناسازگار یا دادهٔ نامعتبر شکست خورد.
    /// </summary>
    CalculationError = 4,
}

/// <summary>
/// گونهٔ قاعده. نرخ درصد در کد حوزهٔ مالیاتی قفل نمی‌شود.
/// </summary>
public enum TaxRuleKind
{
    /// <summary>
    /// درصد پیکربندی‌شده روی مبلغ بدون مالیات.
    /// </summary>
    Percentage = 0,

    /// <summary>
    /// معافیت صریح با دلیل؛ نرخ صفر ساختگی نیست.
    /// </summary>
    Exempt = 1,

    /// <summary>
    /// نرخ صفرِ قابل‌اعمال.
    /// </summary>
    ZeroRated = 2,
}

/// <summary>
/// وضعیت انتشار قاعده.
/// </summary>
public enum TaxRuleStatus
{
    /// <summary>
    /// پیش‌نویس؛ در محاسبه شرکت نمی‌کند.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// فعال در پنجرهٔ اعتبار.
    /// </summary>
    Active = 1,

    /// <summary>
    /// بازنشسته.
    /// </summary>
    Retired = 2,
}

/// <summary>
/// سیاست بازنویسی نرخ. مشتری/درخواست HTTP نرخ را تزریق نمی‌کند.
/// </summary>
public enum TaxOverridePolicy
{
    /// <summary>
    /// بازنویسی ممنوع است.
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// فقط مسیر داخلی معتمد با پرچم صریح سرور.
    /// </summary>
    TrustedInternal = 1,
}

/// <summary>
/// طبقهٔ مالیاتی مات برای ارجاع Catalog/Offer؛ نرخ روی کالا ذخیره نمی‌شود.
/// </summary>
public sealed class TaxCategory
{
    /// <summary>
    /// سازندهٔ EF.
    /// </summary>
    private TaxCategory()
    {
    }

    /// <summary>
    /// شناسهٔ طبقه.
    /// </summary>
    public Guid CategoryId { get; init; }

    /// <summary>
    /// کد پایدار پیکربندی.
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// توضیح عملیاتی؛ متن قانون نیست.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// زمان ایجاد UTC.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// طبقه می‌سازد. نرخ مالیات اینجا نیست.
    /// </summary>
    public static TaxCategory Create(string code, string displayName, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("کد طبقهٔ مالیاتی خالی نیست.");
        }

        return new TaxCategory
        {
            CategoryId = UuidV7.New(),
            Code = code.Trim(),
            DisplayName = displayName.Trim(),
            CreatedAt = now,
        };
    }
}

/// <summary>
/// انتساب طبقه به Offer. نرخ و مبلغ مالیات اینجا ذخیره نمی‌شود.
/// </summary>
public sealed class TaxOfferClassification
{
    /// <summary>
    /// سازندهٔ EF.
    /// </summary>
    private TaxOfferClassification()
    {
    }

    /// <summary>
    /// Offer طرف قرارداد؛ FK به schema offer نیست.
    /// </summary>
    public Guid OfferId { get; init; }

    /// <summary>
    /// طبقهٔ مالیاتی مات.
    /// </summary>
    public Guid CategoryId { get; init; }

    /// <summary>
    /// انتساب می‌سازد.
    /// </summary>
    public static TaxOfferClassification Assign(Guid offerId, Guid categoryId) =>
        new()
        {
            OfferId = offerId,
            CategoryId = categoryId,
        };
}

/// <summary>
/// قاعدهٔ مؤثر به تاریخ با حوزهٔ مالیاتی صریح. نرخ ایران یا تاریخ قانون در کد قفل نیست.
/// </summary>
public sealed class TaxRule : IHasDomainEvents
{
    private readonly DomainEventCollector _domainEvents = new();

    /// <summary>
    /// سازندهٔ EF.
    /// </summary>
    private TaxRule()
    {
    }

    /// <summary>
    /// شناسهٔ قاعده.
    /// </summary>
    public Guid RuleId { get; init; }

    /// <summary>
    /// حوزهٔ مالیاتی صریح؛ از Locale یا Market استنباط نمی‌شود.
    /// </summary>
    public string Jurisdiction { get; init; } = string.Empty;

    /// <summary>
    /// بازار تجاری در صورت نیاز به تفکیک پیکربندی.
    /// </summary>
    public string Market { get; init; } = string.Empty;

    /// <summary>
    /// طبقهٔ مشمول.
    /// </summary>
    public Guid CategoryId { get; init; }

    /// <summary>
    /// گونهٔ قاعده.
    /// </summary>
    public TaxRuleKind Kind { get; init; }

    /// <summary>
    /// نرخ کسری (مثلاً ۰٫۰۹). برای معافیت و نرخ صفر صفر است.
    /// </summary>
    public decimal Rate { get; private set; }

    /// <summary>
    /// شروع اعتبار UTC. تاریخ جلالی کلید دامنه نیست.
    /// </summary>
    public DateTimeOffset EffectiveFrom { get; init; }

    /// <summary>
    /// پایان اعتبار اختیاری UTC.
    /// </summary>
    public DateTimeOffset? EffectiveTo { get; private set; }

    /// <summary>
    /// وضعیت انتشار.
    /// </summary>
    public TaxRuleStatus Status { get; private set; }

    /// <summary>
    /// اولویت؛ عدد بزرگ‌تر خاص‌تر است. تساوی اولویت در همپوشانی خطا است.
    /// </summary>
    public int Specificity { get; init; }

    /// <summary>
    /// سیاست بازنویسی داخلی.
    /// </summary>
    public TaxOverridePolicy OverridePolicy { get; init; }

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
    /// قاعده می‌سازد. درصد حوزه در کد سخت نیست.
    /// </summary>
    public static TaxRule Create(
        string jurisdiction,
        string market,
        Guid categoryId,
        TaxRuleKind kind,
        decimal rate,
        DateTimeOffset effectiveFrom,
        DateTimeOffset? effectiveTo,
        int specificity,
        TaxOverridePolicy overridePolicy,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(jurisdiction))
        {
            throw new InvalidOperationException("حوزهٔ مالیاتی باید صریح باشد؛ از Locale استنباط نمی‌شود.");
        }

        if (string.IsNullOrWhiteSpace(market))
        {
            throw new InvalidOperationException("بازار قاعده خالی نیست؛ با حوزه یکی گرفته نمی‌شود.");
        }

        if (effectiveTo is not null && effectiveTo <= effectiveFrom)
        {
            throw new InvalidOperationException("پنجرهٔ اعتبار قاعده نامعتبر است.");
        }

        if (kind == TaxRuleKind.Percentage)
        {
            if (rate < 0 || rate > 1)
            {
                throw new InvalidOperationException("نرخ درصدی باید کسری بین صفر و یک باشد.");
            }
        }
        else if (rate != 0)
        {
            throw new InvalidOperationException("قاعدهٔ معاف یا نرخ صفر نباید نرخ درصدی غیرصفر داشته باشد.");
        }

        var rule = new TaxRule
        {
            RuleId = UuidV7.New(),
            Jurisdiction = jurisdiction.Trim(),
            Market = market.Trim(),
            CategoryId = categoryId,
            Kind = kind,
            Rate = rate,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            Status = TaxRuleStatus.Draft,
            Specificity = specificity,
            OverridePolicy = overridePolicy,
            CreatedAt = now,
            UpdatedAt = now,
        };
        rule._domainEvents.Add(new TaxRuleCreatedDomainEvent(rule.RuleId));
        return rule;
    }

    /// <summary>
    /// قاعده را برای انتخاب فعال می‌کند.
    /// </summary>
    public void Activate(DateTimeOffset now)
    {
        Status = TaxRuleStatus.Active;
        UpdatedAt = now;
        _domainEvents.Add(new TaxRuleActivatedDomainEvent(RuleId));
    }

    /// <summary>
    /// نرخ پیش‌نویس را عوض می‌کند. تصویر سفارش تاریخی را بازنویسی نمی‌کند.
    /// </summary>
    public void ChangeRate(decimal rate, DateTimeOffset now)
    {
        if (Kind != TaxRuleKind.Percentage)
        {
            throw new InvalidOperationException("فقط قاعدهٔ درصدی نرخ قابل‌تغییر دارد.");
        }

        if (rate < 0 || rate > 1)
        {
            throw new InvalidOperationException("نرخ درصدی باید کسری بین صفر و یک باشد.");
        }

        Rate = rate;
        UpdatedAt = now;
        _domainEvents.Add(new TaxRuleChangedDomainEvent(RuleId));
    }

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// آیا لحظه داخل پنجره است.
    /// </summary>
    public bool IsEffectiveAt(DateTimeOffset at) =>
        at >= EffectiveFrom && (EffectiveTo is null || at < EffectiveTo);
}

/// <summary>
/// گرد کردن قطعی مبلغ مالیات طبق مقیاس ارز. ممیز شناور نیست.
/// </summary>
public static class TaxRounding
{
    /// <summary>
    /// IRR بدون اعشار؛ بقیه دو رقم. Midpoint AwayFromZero.
    /// </summary>
    public static decimal Round(decimal amount, string currency)
    {
        var scale = string.Equals(currency, "IRR", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(currency, "JPY", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(currency, "KRW", StringComparison.OrdinalIgnoreCase)
            ? 0
            : 2;
        return decimal.Round(amount, scale, MidpointRounding.AwayFromZero);
    }
}

/// <summary>
/// رویداد ایجاد قاعده.
/// </summary>
public sealed class TaxRuleCreatedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد را می‌سازد.
    /// </summary>
    public TaxRuleCreatedDomainEvent(Guid ruleId)
    {
        RuleId = ruleId;
        Metadata = EventMetadataFactory.ForDomain("tax.rule_created.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// قاعده.
    /// </summary>
    public Guid RuleId { get; }
}

/// <summary>
/// رویداد فعال‌سازی قاعده.
/// </summary>
public sealed class TaxRuleActivatedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد را می‌سازد.
    /// </summary>
    public TaxRuleActivatedDomainEvent(Guid ruleId)
    {
        RuleId = ruleId;
        Metadata = EventMetadataFactory.ForDomain("tax.rule_activated.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// قاعده.
    /// </summary>
    public Guid RuleId { get; }
}

/// <summary>
/// رویداد تغییر قاعده.
/// </summary>
public sealed class TaxRuleChangedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد را می‌سازد.
    /// </summary>
    public TaxRuleChangedDomainEvent(Guid ruleId)
    {
        RuleId = ruleId;
        Metadata = EventMetadataFactory.ForDomain("tax.rule_changed.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// قاعده.
    /// </summary>
    public Guid RuleId { get; }
}

/// <summary>
/// رویداد شکست محاسبه برای مشاهده‌پذیری.
/// </summary>
public sealed class TaxCalculationFailedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد را می‌سازد.
    /// </summary>
    public TaxCalculationFailedDomainEvent(TaxOutcome outcome)
    {
        Outcome = outcome;
        Metadata = EventMetadataFactory.ForDomain("tax.calculation_failed.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// نتیجهٔ شکست.
    /// </summary>
    public TaxOutcome Outcome { get; }
}
