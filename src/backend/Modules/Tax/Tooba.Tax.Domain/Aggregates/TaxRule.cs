using Tooba.BuildingBlocks;

namespace Tooba.Tax.Domain;

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
