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
/// <para>
/// TB-274-R1: inspection is EXPLICIT. An omitted/null/blank token is NOT "the documented default" — receiving and
/// inspection are deliberate operator statements, so a missing token is refused before any case read or write exactly
/// like an unknown one. No path may turn silence into a usable unit.
/// </para>
/// </summary>
public static class ReturnItemConditionParser
{
    /// <summary>Stable wire token of <see cref="ReturnItemCondition.Usable"/>.</summary>
    public const string Usable = "usable";

    /// <summary>Stable wire token of <see cref="ReturnItemCondition.Damaged"/>.</summary>
    public const string Damaged = "damaged";

    /// <summary>
    /// Parses the wire token. A missing/null/blank token AND an unknown token are the same stable failure — never a
    /// fallback — so neither an omitted value nor a typo can turn a damaged unit into saleable stock.
    /// </summary>
    public static Result<ReturnItemCondition> Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<ReturnItemCondition>(new SemanticError(ReturnsErrorCodes.ReceiptLineInvalid));
        }

        return value.Trim().ToLowerInvariant() switch
        {
            Usable => Result.Success(ReturnItemCondition.Usable),
            Damaged => Result.Success(ReturnItemCondition.Damaged),
            _ => Result.Failure<ReturnItemCondition>(new SemanticError(ReturnsErrorCodes.ReceiptLineInvalid)),
        };
    }
}
