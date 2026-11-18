using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Returns.Contracts.Errors;

namespace Tooba.Returns.Application.Composition;

/// <summary>
/// TB-279-R1 (F3): parses and normalizes the wire proof-kind token of a custody handback.
/// <para>
/// Custody is the durable claim that rejected received goods really went back to the owning customer, so the evidence
/// behind it is explicit. Two reviewed semantics exist: an <c>operator_attestation</c> (the operator physically handed
/// the goods over and attests to it) and a <c>carrier_delivered_back</c> fact (the owning carrier delivered them back).
/// A tracking number alone is not proof of a handback, so a missing/blank/unknown token is refused before any case
/// read — never defaulted to the operator kind.
/// </para>
/// </summary>
public static class ReturnCustodyProofKindParser
{
    /// <summary>Stable token: the operator handed the goods over in person and attests to it.</summary>
    public const string OperatorAttestation = "operator_attestation";

    /// <summary>Stable token: the owning carrier delivered the goods back to the customer.</summary>
    public const string CarrierDeliveredBack = "carrier_delivered_back";

    /// <summary>The stable stored/wire tokens, in their published order.</summary>
    public static IReadOnlyList<string> All { get; } = [OperatorAttestation, CarrierDeliveredBack];

    /// <summary>Parses the wire token; a missing or unknown token is the same typed refusal, never a default kind.</summary>
    public static Result<string> Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<string>(new SemanticError(ReturnsErrorCodes.CustodyProofKindRequired));
        }

        var token = value.Trim().ToLowerInvariant();
        return All.Contains(token, StringComparer.Ordinal)
            ? Result.Success(token)
            : Result.Failure<string>(new SemanticError(ReturnsErrorCodes.CustodyProofKindUnavailable));
    }
}
