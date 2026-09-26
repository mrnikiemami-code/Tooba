namespace Tooba.Identity.Contracts.Problems;

/// <summary>
/// Typed, contract-safe fault raised by the Identity module when a normalized login identifier
/// already exists in the current identity schema. Host callers classify the expected duplicate
/// path through this Contracts fault instead of referencing Identity implementation types.
/// The normalized value is diagnostic only and must never be logged or returned to clients.
/// </summary>
public sealed class IdentityDuplicateIdentifierFault : Exception
{
    /// <summary>Creates the typed duplicate-identifier fault.</summary>
    /// <param name="normalizedValue">Normalized duplicate value; never logged or exposed.</param>
    public IdentityDuplicateIdentifierFault(string normalizedValue)
        : base(IdentityErrorCodes.IdentifierConflict)
    {
        NormalizedValue = normalizedValue;
    }

    /// <summary>Normalized duplicate value. Diagnostic only; never logged or exposed.</summary>
    public string NormalizedValue { get; }
}
