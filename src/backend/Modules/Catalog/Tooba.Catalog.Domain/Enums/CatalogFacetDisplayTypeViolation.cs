using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.Enums;

/// <summary>
/// Typed outcome for facet display-type vs ValueKind validation (no message-as-code).
/// </summary>
public enum CatalogFacetDisplayTypeViolation
{
    /// <summary>Valid combination.</summary>
    None = 0,

    /// <summary>Color swatch requires option color metadata (not yet supported).</summary>
    ColorSwatchUnsupported = 1,

    /// <summary>Boolean attributes require BooleanToggle.</summary>
    BooleanRequiresToggle = 2,

    /// <summary>Number attributes require Range.</summary>
    NumberRequiresRange = 3,

    /// <summary>Display type incompatible with Text ValueKind.</summary>
    TextDisplayIncompatible = 4,

    /// <summary>Display type incompatible with Enumeration ValueKind.</summary>
    EnumerationDisplayIncompatible = 5,

    /// <summary>Instant ValueKind facets are not supported.</summary>
    InstantUnsupported = 6,

    /// <summary>BooleanToggle used with non-boolean ValueKind.</summary>
    BooleanToggleRequiresBoolean = 7,
}
