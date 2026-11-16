using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.Composition;

/// <summary>
/// TB-274: parses the wire inspection-condition token into the typed <see cref="ReturnItemCondition"/>.
/// <para>
/// Transport-shape parsing lives here (not in the exception seam) exactly like the refund-destination and
/// recovery-kind parsers, so an undefined condition is the stable <c>return.receipt.line_invalid</c> failure instead of
/// a model-binding fault, and a value is never silently coerced into "usable".
/// </para>
/// </summary>
public static class ReturnItemConditionParser
{
    /// <summary>Stable wire token of <see cref="ReturnItemCondition.Usable"/> (also the default when omitted).</summary>
    public const string Usable = "usable";

    /// <summary>Stable wire token of <see cref="ReturnItemCondition.Damaged"/>.</summary>
    public const string Damaged = "damaged";

    /// <summary>
    /// Parses the wire token. An omitted token means the documented default (<c>usable</c>); an unknown token is a
    /// stable failure, never a fallback, so a typo cannot turn a damaged unit into saleable stock.
    /// </summary>
    public static Result<ReturnItemCondition> Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Success(ReturnItemCondition.Usable);
        }

        return value.Trim().ToLowerInvariant() switch
        {
            Usable => Result.Success(ReturnItemCondition.Usable),
            Damaged => Result.Success(ReturnItemCondition.Damaged),
            _ => Result.Failure<ReturnItemCondition>(new SemanticError(ReturnsErrorCodes.ReceiptLineInvalid)),
        };
    }
}
