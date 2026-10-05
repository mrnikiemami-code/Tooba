using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Contracts.Errors;

namespace Tooba.Fulfillment.Application.Errors;

/// <summary>
/// Typed expected-failure helper for the Fulfillment boundary.
/// Validates a stable code against the declared <see cref="FulfillmentErrorCodes"/> set once, so an
/// uncatalogued code fails fast at the throw site instead of silently degrading to platform.unexpected.
/// <see cref="RequireKnown"/> is the single seam that turns a stable code into a typed fault: the
/// production call sites (Domain/Directory/Semantic) only ever pass codes from the declared set, so the
/// residual guard failure can never surface a bare message and is never classified from prose.
/// </summary>
public static class FulfillmentErrors
{
    private static readonly HashSet<string> Known = typeof(FulfillmentErrorCodes)
        .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
        .Where(f => f.IsLiteral && f.FieldType == typeof(string))
        .Select(f => (string)f.GetRawConstantValue()!)
        .ToHashSet(StringComparer.Ordinal);

    /// <summary>Returns the code when it is a declared stable Fulfillment code; otherwise throws.</summary>
    public static string RequireKnown(string code) =>
        Known.Contains(code) ? code : throw new InvalidOperationException(code);

    /// <summary>Creates a SemanticError for a declared stable Fulfillment code; unknown codes throw.</summary>
    public static SemanticError Semantic(string code) => new(RequireKnown(code));

    /// <summary>Creates a typed contract-boundary failure for a declared stable Fulfillment code; unknown codes throw.</summary>
    public static ContractOperationException Contract(string code) => new(RequireKnown(code));

    /// <summary>Creates a typed semantic fault for a declared stable Fulfillment code; unknown codes throw.</summary>
    public static SemanticException SemanticFault(string code) => new(new SemanticError(RequireKnown(code)));

    /// <summary>True when the code is a declared stable Fulfillment code.</summary>
    public static bool IsKnown(string code) => Known.Contains(code);
}
