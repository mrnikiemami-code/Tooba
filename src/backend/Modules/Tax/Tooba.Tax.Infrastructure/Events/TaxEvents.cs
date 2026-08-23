using Tooba.BuildingBlocks;
using Tooba.Tax.Domain;

namespace Tooba.Tax.Infrastructure.Events;

/// <summary>
/// ایجاد قاعدهٔ مالیاتی.
/// </summary>
public sealed class TaxRuleCreatedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار.
    /// </summary>
    public const string EventTypeName = "tax.rule_created.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// قاعده.
    /// </summary>
    public Guid RuleId { get; set; }
}

/// <summary>
/// فعال‌سازی قاعده.
/// </summary>
public sealed class TaxRuleActivatedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار.
    /// </summary>
    public const string EventTypeName = "tax.rule_activated.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// قاعده.
    /// </summary>
    public Guid RuleId { get; set; }
}

/// <summary>
/// تغییر قاعده.
/// </summary>
public sealed class TaxRuleChangedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار.
    /// </summary>
    public const string EventTypeName = "tax.rule_changed.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// قاعده.
    /// </summary>
    public Guid RuleId { get; set; }
}

/// <summary>
/// شکست محاسبه.
/// </summary>
public sealed class TaxCalculationFailedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار.
    /// </summary>
    public const string EventTypeName = "tax.calculation_failed.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// نتیجه.
    /// </summary>
    public string Outcome { get; set; } = string.Empty;
}
