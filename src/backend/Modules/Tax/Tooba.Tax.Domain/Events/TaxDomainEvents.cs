using Tooba.BuildingBlocks;

namespace Tooba.Tax.Domain;

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
