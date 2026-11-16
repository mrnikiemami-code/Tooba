using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.ReturnRequests.Models;

/// <summary>
/// TB-273: the ONE authority that answers "what can still be recovered for this case?" from STORED facts only.
/// <para>
/// Money and physical work are separate owners' phases: a successful refund does not imply the received units
/// were settled, and a settled stock phase does not imply the money was released. The description therefore
/// reports financial and physical completion as two independent facts and never guesses an action from a status
/// alone.
/// </para>
/// </summary>
public enum ReturnRecoveryPhase
{
    /// <summary>No approved financial attempt; there is nothing to recover (requested/rejected/cancelled).</summary>
    None = 0,

    /// <summary>An approval exists and exactly ONE bound attempt is still pending: the money outcome is unknown.</summary>
    PendingMoney = 1,

    /// <summary>A definite financial failure. The existing deliberate new-attempt retry is a DIFFERENT intent.</summary>
    FailedMoney = 2,

    /// <summary>The money is confirmed and the physical disposition of the received units is unfinished.</summary>
    PendingDisposition = 3,

    /// <summary>Money confirmed and every received line recorded its disposition: the recorded completion.</summary>
    Settled = 4,

    /// <summary>Inconsistent/ambiguous stored binding or missing owning physical evidence: a typed blocker, no guess.</summary>
    Blocked = 5,
}

/// <summary>
/// TB-273: the exact recovery intent a client may ask for. This is deliberately NOT the existing
/// <c>retry_refund</c> intent (which mints a NEW attempt for a definite failure).
/// </summary>
public enum ReturnRecoveryKind
{
    /// <summary>Repeat the EXISTING still-pending attempt with its recorded key, payment, amount and currency.</summary>
    ResumePendingRefund = 0,

    /// <summary>Settle the physical disposition only: zero refund/Wallet calls, zero new attempts.</summary>
    SettlePhysicalDisposition = 1,
}

/// <summary>
/// TB-273: the additive read description of one case's recovery state. Every field is derived from stored
/// case/decision/attempt/item facts; computing it never contacts a provider, retries money, restocks or writes.
/// </summary>
public sealed record ReturnRecoveryDescription(
    ReturnRecoveryPhase Phase,
    ReturnRecoveryKind? RecoveryKind,
    Guid? RefundAttemptId,
    string? BlockerCode,
    bool FinancialComplete,
    bool PhysicalComplete);

/// <summary>
/// TB-273: pure recovery projection over stored facts. The same policy is reused by the recovery command (to
/// decide what is executable) and by the admin read projection (to describe what is stored), so a read and an
/// action can never disagree about the state of one case.
/// </summary>
public static class ReturnRecoveryPolicy
{
    /// <summary>Describes the recovery state of the given stored case snapshot.</summary>
    public static ReturnRecoveryDescription Describe(ReturnSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var attempts = snapshot.RefundAttempts;
        var pending = attempts.Where(x => x.Status == RefundAttemptStatus.Pending).ToArray();
        var moneySucceeded = attempts.Any(x => x.Status == RefundAttemptStatus.Succeeded);
        var physicalComplete = snapshot.Items.Count > 0
            && snapshot.Items.All(x => x.Disposition != ReturnItemDisposition.None);

        switch (snapshot.Status)
        {
            case ReturnRequestStatus.Requested:
            case ReturnRequestStatus.Rejected:
            case ReturnRequestStatus.Cancelled:
                // No approved financial attempt exists, so there is no financial or physical recovery to run.
                return new ReturnRecoveryDescription(
                    ReturnRecoveryPhase.None, null, null, null, FinancialComplete: false, physicalComplete);

            case ReturnRequestStatus.Approved:
            case ReturnRequestStatus.RefundProcessing:
            {
                // An approval is recorded; the money answer is UNKNOWN only when exactly one attempt is bound and
                // still pending. Zero or several pending attempts is an ambiguous binding, never a guessed resume.
                if (pending.Length == 1 && !moneySucceeded)
                {
                    return new ReturnRecoveryDescription(
                        ReturnRecoveryPhase.PendingMoney,
                        ReturnRecoveryKind.ResumePendingRefund,
                        pending[0].RefundAttemptId,
                        null,
                        FinancialComplete: false,
                        physicalComplete);
                }

                return new ReturnRecoveryDescription(
                    ReturnRecoveryPhase.Blocked,
                    null,
                    null,
                    ReturnsErrorCodes.RecoveryAttemptUnbound,
                    FinancialComplete: false,
                    physicalComplete);
            }

            case ReturnRequestStatus.RefundFailed:
                // Definite financial failure: the existing retry intent owns a NEW attempt. It is never reported as
                // unknown money, and this recovery command never mints the retry.
                return new ReturnRecoveryDescription(
                    ReturnRecoveryPhase.FailedMoney, null, null, null, FinancialComplete: false, physicalComplete);

            case ReturnRequestStatus.Completed:
            {
                if (!moneySucceeded || pending.Length > 0)
                {
                    // A completed case whose money row cannot be bound is inconsistent, not settled.
                    return new ReturnRecoveryDescription(
                        ReturnRecoveryPhase.Blocked,
                        null,
                        null,
                        ReturnsErrorCodes.RecoveryAttemptUnbound,
                        FinancialComplete: false,
                        physicalComplete);
                }

                if (physicalComplete)
                {
                    return new ReturnRecoveryDescription(
                        ReturnRecoveryPhase.Settled, null, null, null, FinancialComplete: true, PhysicalComplete: true);
                }

                // Money confirmed, physical work unfinished. A delivered-customer RMA has no recorded receipt or
                // inspection fact, so no disposition can be honestly executed from the default condition.
                if (snapshot.Origin == ReturnOrigin.CustomerDelivered)
                {
                    return new ReturnRecoveryDescription(
                        ReturnRecoveryPhase.Blocked,
                        null,
                        null,
                        ReturnsErrorCodes.RecoveryReceivingRequired,
                        FinancialComplete: true,
                        PhysicalComplete: false);
                }

                return new ReturnRecoveryDescription(
                    ReturnRecoveryPhase.PendingDisposition,
                    ReturnRecoveryKind.SettlePhysicalDisposition,
                    null,
                    null,
                    FinancialComplete: true,
                    PhysicalComplete: false);
            }

            default:
                return new ReturnRecoveryDescription(
                    ReturnRecoveryPhase.Blocked,
                    null,
                    null,
                    ReturnsErrorCodes.RecoveryAttemptUnbound,
                    FinancialComplete: false,
                    physicalComplete);
        }
    }
}
