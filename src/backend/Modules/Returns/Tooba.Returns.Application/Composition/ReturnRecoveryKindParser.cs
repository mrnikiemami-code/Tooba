using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Contracts.Errors;

namespace Tooba.Returns.Application.Composition;

/// <summary>
/// TB-273: parses the wire recovery-kind token into the typed <see cref="ReturnRecoveryKind"/>.
/// Transport-shape parsing lives here (not in the exception seam) exactly like the refund-destination parser, so
/// an unparseable token is the stable <c>return.recovery.kind_invalid</c> failure instead of a message heuristic.
/// </summary>
public static class ReturnRecoveryKindParser
{
    /// <summary>Stable wire token of <see cref="ReturnRecoveryKind.ResumePendingRefund"/>.</summary>
    public const string ResumePendingRefund = "resume_pending_refund";

    /// <summary>Stable wire token of <see cref="ReturnRecoveryKind.SettlePhysicalDisposition"/>.</summary>
    public const string SettlePhysicalDisposition = "settle_physical_disposition";

    /// <summary>Parses the wire token; absent or unknown is a stable failure (a recovery kind is never defaulted).</summary>
    public static Result<ReturnRecoveryKind> Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<ReturnRecoveryKind>(new SemanticError(ReturnsErrorCodes.RecoveryKindInvalid));
        }

        return value.Trim().ToLowerInvariant() switch
        {
            ResumePendingRefund => Result.Success(ReturnRecoveryKind.ResumePendingRefund),
            SettlePhysicalDisposition => Result.Success(ReturnRecoveryKind.SettlePhysicalDisposition),
            _ => Result.Failure<ReturnRecoveryKind>(new SemanticError(ReturnsErrorCodes.RecoveryKindInvalid)),
        };
    }
}
