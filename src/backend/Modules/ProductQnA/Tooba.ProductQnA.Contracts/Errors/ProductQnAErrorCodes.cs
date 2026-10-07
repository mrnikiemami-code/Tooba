namespace Tooba.ProductQnA.Contracts.Errors;

/// <summary>
/// Stable ProductQnA semantic error codes for HTTP/use-case outcomes. Identity is the code itself —
/// never a message string and never localized prose. Values are the machine codes emitted by the
/// ProductQnA Domain/Infrastructure and mapped by the composed error catalog; they must never be
/// renamed or repurposed.
/// <para>
/// This is the single canonical home for ProductQnA stable-code identity. Transport/input-shape
/// validator codes do <b>not</b> live here — they are owned by
/// <c>Tooba.ProductQnA.Application.Validation.ProductQnAValidationCodes</c> and are mapped through the
/// Foundation <c>validation.failed</c> descriptor.
/// </para>
/// </summary>
public static class ProductQnAErrorCodes
{
    private static readonly HashSet<string> KnownCodes = new(StringComparer.Ordinal)
    {
        Rejected,
        NotFound,
    };

    /// <summary>
    /// True when <paramref name="code"/> is a stable code this module's contract boundary is allowed to
    /// surface as a use-case fault. Used by <c>ProductQnAOperation</c> so ProductQnA faults map to
    /// <c>Result</c> while codes owned by another module (or an unexpected fault) propagate untouched to
    /// the canonical global exception boundary. Classification is by typed code only — never by message text.
    /// </summary>
    public static bool IsKnown(string? code) =>
        !string.IsNullOrWhiteSpace(code) && KnownCodes.Contains(code);

    /// <summary>Question submission was rejected by domain/application rules.</summary>
    public const string Rejected = "product_qna.rejected";

    /// <summary>Published questions for the product slug were not found.</summary>
    public const string NotFound = "product_qna.not_found";

    /// <summary>Shared foundation customer session required (do not re-register descriptor).</summary>
    public const string SessionRequired = "customer.session.required";
}
