using Tooba.Returns.Contracts.Operations;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.ReturnRequests.Models;

/// <summary>
/// Application-internal aliases of the module's stable eligibility reason vocabulary. The single
/// canonical vocabulary and the reason→HTTP-code mapping live in the boundary contract
/// <see cref="ReturnEligibilityReasons"/> (consumed by the Order admin orchestration); this alias keeps
/// the internal evaluator/directory reading a stable Application-local name without re-declaring either
/// the vocabulary or the mapping, and without hard-coded user-facing prose.
/// </summary>
public static class ReturnEligibilityReasonCodes
{
    /// <summary>Order is not paid.</summary>
    public const string NotPaid = ReturnEligibilityReasons.NotPaid;

    /// <summary>Nothing delivered yet.</summary>
    public const string NotDelivered = ReturnEligibilityReasons.NotDelivered;

    /// <summary>Return window expired.</summary>
    public const string WindowExpired = ReturnEligibilityReasons.WindowExpired;

    /// <summary>Nothing returnable left.</summary>
    public const string NothingReturnable = ReturnEligibilityReasons.NothingReturnable;

    /// <summary>Line policy snapshot forbids returns.</summary>
    public const string NonReturnable = ReturnEligibilityReasons.NonReturnable;

    /// <summary>Eligible.</summary>
    public const string Eligible = ReturnEligibilityReasons.Eligible;

    /// <summary>Order not found.</summary>
    public const string OrderMissing = ReturnEligibilityReasons.OrderMissing;

    /// <summary>Fulfillment not found.</summary>
    public const string FulfillmentMissing = ReturnEligibilityReasons.FulfillmentMissing;

    /// <summary>Stable HTTP error code of a reason code — delegates to the boundary contract.</summary>
    public static string ToErrorCode(string reasonCode) => ReturnEligibilityReasons.ToErrorCode(reasonCode);
}
