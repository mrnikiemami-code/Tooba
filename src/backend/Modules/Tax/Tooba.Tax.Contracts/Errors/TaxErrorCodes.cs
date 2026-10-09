namespace Tooba.Tax.Contracts.Errors;

/// <summary>
/// Stable Tax semantic error codes. Identity is the code itself — never a message string and never
/// localized prose. Values are the machine codes emitted by the Tax Domain/Infrastructure and mapped
/// by the composed error catalog; they must never be renamed or repurposed.
/// <para>
/// This is the single canonical home for Tax stable-code identity. Ownership is unique: every code
/// below is Tax-owned, registered exactly once by <c>TaxErrorCatalogContributor</c> and localized by
/// <c>TaxErrorResourceSet</c>. Tax never re-registers a foreign-owned descriptor and no other module
/// registers a Tax code. In particular <c>checkout.tax.unavailable</c> is Order-owned (it is the Order
/// checkout use case's own outcome) and is deliberately absent here.
/// </para>
/// </summary>
public static class TaxErrorCodes
{
    private static readonly HashSet<string> KnownCodes = new(StringComparer.Ordinal)
    {
        RuleIdRequired,
        JurisdictionRequired,
        MarketRequired,
        ValidityInverted,
        RateOutOfRange,
        RateNotApplicable,
        RateKindMismatch,
        CategoryIdRequired,
        CategoryCodeRequired,
        CategoryMissing,
        OutboxUnmappedEventType,
    };

    /// <summary>
    /// True when <paramref name="code"/> is a stable code this module's contract boundary is allowed to
    /// surface as a use-case fault. Used by <c>TaxOperation</c> so Tax faults map to <c>Result</c> while
    /// codes owned by another module (or an unexpected fault) propagate untouched to the canonical
    /// global exception boundary. Classification is by typed code only — never by message text.
    /// </summary>
    public static bool IsKnown(string? code) =>
        !string.IsNullOrWhiteSpace(code) && KnownCodes.Contains(code);

    /// <summary>A tax rule id is required.</summary>
    public const string RuleIdRequired = "tax.rule.id_required";

    /// <summary>An explicit tax jurisdiction is required; it is never inferred from locale or market.</summary>
    public const string JurisdictionRequired = "tax.jurisdiction.required";

    /// <summary>A commercial market is required for the rule configuration.</summary>
    public const string MarketRequired = "tax.market.required";

    /// <summary>The rule validity window ends at or before it starts.</summary>
    public const string ValidityInverted = "tax.validity.inverted";

    /// <summary>A percentage rate must be within the closed unit interval.</summary>
    public const string RateOutOfRange = "tax.rate.out_of_range";

    /// <summary>A non-percentage rule kind must not carry a non-zero rate.</summary>
    public const string RateNotApplicable = "tax.rate.not_applicable";

    /// <summary>The rate can only be changed on a percentage rule.</summary>
    public const string RateKindMismatch = "tax.rate.kind_mismatch";

    /// <summary>A tax category id is required.</summary>
    public const string CategoryIdRequired = "tax.category.id_required";

    /// <summary>A stable tax category code is required.</summary>
    public const string CategoryCodeRequired = "tax.category.code_required";

    /// <summary>The referenced tax category does not exist.</summary>
    public const string CategoryMissing = "tax.category.missing";

    /// <summary>The outbox registration has no mapping for the given integration event type.</summary>
    public const string OutboxUnmappedEventType = "tax.outbox.unmapped_event_type";
}
