namespace Tooba.Returns.Contracts.Operations;

// ---------------------------------------------------------------------------------------------------------------
// TB-273: additive recovery facts. Money completion and physical completion are separate owners' phases, so a
// consumer can never read "refunded" as "all received units settled". These fields are ADDITIVE: the existing
// numeric enum values and every shipped wire member keep their meaning, and no new executable menu verb is
// published to a frontend that cannot handle it.
// ---------------------------------------------------------------------------------------------------------------

/// <summary>TB-273: stored recovery phase of one return case.</summary>
public enum ReturnRecoveryPhaseOption
{
    /// <summary>No approved financial attempt.</summary>
    None = 0,

    /// <summary>An approval exists and exactly one bound attempt is still pending (unknown outcome).</summary>
    PendingMoney = 1,

    /// <summary>A definite financial failure; the existing new-attempt retry is a different intent.</summary>
    FailedMoney = 2,

    /// <summary>Money confirmed, physical disposition of the received units unfinished.</summary>
    PendingDisposition = 3,

    /// <summary>Money confirmed and every received line recorded its disposition.</summary>
    Settled = 4,

    /// <summary>Inconsistent/ambiguous binding or missing owning physical evidence: explicit blocker, no guess.</summary>
    Blocked = 5,
}

/// <summary>TB-273: the explicit recovery intent an operator may ask for.</summary>
public enum ReturnRecoveryKindOption
{
    /// <summary>Repeat the EXISTING pending attempt with its recorded key/payment/amount/currency.</summary>
    ResumePendingRefund = 0,

    /// <summary>Physical disposition only: zero refund/Wallet calls, zero new attempts.</summary>
    SettlePhysicalDisposition = 1,
}

/// <summary>TB-273: additive recovery description of one case, derived from stored facts only.</summary>
public sealed record ReturnRecoverySnapshot(
    ReturnRecoveryPhaseOption Phase,
    ReturnRecoveryKindOption? RecoveryKind,
    Guid? RefundAttemptId,
    string? BlockerCode,
    bool FinancialComplete,
    bool PhysicalComplete);
