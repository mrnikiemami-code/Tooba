using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.Composition;

/// <summary>
/// Parses the wire refund-destination token into the typed <see cref="RefundDestination"/>.
/// Transport-shape parsing lives here (not in the exception seam); an unparseable token becomes the
/// stable <see cref="ReturnsErrorCodes.RefundDestinationInvalid"/> failure, and an absent token keeps the
/// shipped default (<see cref="RefundDestination.OriginalPayment"/>).
/// </summary>
public static class ReturnRefundDestinationParser
{
    /// <summary>Parse refund destination; empty → default, invalid → stable failure.</summary>
    public static Result<RefundDestination> Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Success(RefundDestination.OriginalPayment);
        }

        return Enum.TryParse<RefundDestination>(value, ignoreCase: true, out var parsed)
            ? Result.Success(parsed)
            : Result.Failure<RefundDestination>(new SemanticError(ReturnsErrorCodes.RefundDestinationInvalid));
    }
}
