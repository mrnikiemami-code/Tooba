using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.ReturnRequests.Models;

/// <summary>
/// TB-279-R3 (B2): the reviewed per-condition split of ONE requested line, as the resolution authority reads it. Every
/// portion is required: a row whose accepted condition was never reviewed is not a provable split.
/// </summary>
public sealed record ReturnHeldGoodsPlanPortion(
    decimal AcceptedUsable,
    decimal AcceptedDamaged,
    decimal RejectedReceivedUsable,
    decimal RejectedReceivedDamaged);

/// <summary>
/// TB-279-R3 (B2): the stored physical evidence of ONE requested line. <see cref="Received"/> is the coherent inspected
/// quantity (usable/damaged) the receiving authority answered, <see cref="HandedBack"/> is what the custody authority
/// already returned to the owning customer, and <see cref="Plan"/> is the reviewed split when one exists.
/// </summary>
public sealed record ReturnHeldGoodsLineEvidence(
    Guid ReturnItemId,
    decimal Requested,
    IReadOnlyList<(decimal Quantity, ReturnItemCondition Condition)> Received,
    bool ReceivedEvidenceCoherent,
    ReturnHeldGoodsPlanPortion? Plan,
    IReadOnlyList<(decimal Quantity, ReturnItemCondition Condition)> HandedBack);

/// <summary>
/// TB-279-R3 (B2): the outstanding held quantity of one line and whether it is PROVABLE.
/// <para>
/// <see cref="IsUnprovable"/> is true when the stored receiving rows contradict the request (or cannot be read as an
/// inspected quantity). Unknown/incoherent physical evidence is never proof that nothing arrived: the caller must keep
/// a conservative claim and surface the typed refusal instead of releasing capacity.
/// </para>
/// </summary>
public sealed record ReturnHeldGoodsResult(decimal OutstandingHeld, bool IsUnprovable);

/// <summary>
/// TB-279-R3 (B2): the ONE authority over "what does this case still physically hold?".
/// <para>
/// A rejected/cancelled case owns its goods until the custody authority records their actual handback. The quantity is
/// derived ONLY from the stored receiving/resolution/custody facts of that case — never from its status, and never from
/// its requested quantity read as "received".
/// </para>
/// <list type="bullet">
/// <item><description>a reviewed plan: the ACCEPTED portion stays consumed and the REJECTED-RECEIVED portion stays held
/// only while its own condition's handback is outstanding;</description></item>
/// <item><description>no reviewed plan: every coherently received unit is held until handed back;</description></item>
/// <item><description>incoherent receipt rows: the held quantity cannot be proven, so the conservative upper bound of the
/// claim is kept and the result is marked unprovable — a caller must refuse rather than treat it as zero.</description></item>
/// </list>
/// </summary>
public static class ReturnHeldGoodsPolicy
{
    /// <summary>The outstanding held quantity of ONE line, from its stored physical facts.</summary>
    public static ReturnHeldGoodsResult Describe(ReturnHeldGoodsLineEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);

        if (!evidence.ReceivedEvidenceCoherent)
        {
            // A contradictory stored receipt is not "nothing was received": the maximum plausible hold is kept, and the
            // caller answers its typed evidence/capacity refusal instead of granting the units to a sibling claim.
            return new ReturnHeldGoodsResult(Math.Max(0m, evidence.Requested), IsUnprovable: true);
        }

        var handedBackUsable = SumOf(evidence.HandedBack, ReturnItemCondition.Usable);
        var handedBackDamaged = SumOf(evidence.HandedBack, ReturnItemCondition.Damaged);

        if (evidence.Plan is { } plan)
        {
            // The accepted portion is consumed by the return (money/stock), and the declined-but-received portion is
            // still the customer's goods until its own condition's handback.
            var accepted = plan.AcceptedUsable + plan.AcceptedDamaged;
            var owedUsable = Math.Max(0m, plan.RejectedReceivedUsable - handedBackUsable);
            var owedDamaged = Math.Max(0m, plan.RejectedReceivedDamaged - handedBackDamaged);
            return new ReturnHeldGoodsResult(accepted + owedUsable + owedDamaged, IsUnprovable: false);
        }

        var receivedUsable = SumOf(evidence.Received, ReturnItemCondition.Usable);
        var receivedDamaged = SumOf(evidence.Received, ReturnItemCondition.Damaged);
        var held = Math.Max(0m, receivedUsable - handedBackUsable)
            + Math.Max(0m, receivedDamaged - handedBackDamaged);
        return new ReturnHeldGoodsResult(held, IsUnprovable: false);
    }

    private static decimal SumOf(
        IReadOnlyList<(decimal Quantity, ReturnItemCondition Condition)> portions,
        ReturnItemCondition condition) =>
        portions.Where(x => x.Condition == condition && x.Quantity > 0m).Sum(x => x.Quantity);
}
