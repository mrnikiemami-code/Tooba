namespace Tooba.Support.Application.Errors;

/// <summary>Stable Support semantic error codes for HTTP/use-case outcomes.</summary>
public static class SupportErrorCodes
{
    /// <summary>Authenticated customer session required.</summary>
    public const string CustomerSessionRequired = "customer.session.required";

    /// <summary>Ticket was not found for the audience scope.</summary>
    public const string Missing = "support.missing";

    /// <summary>Create/list/general Support rejection.</summary>
    public const string Rejected = "support.rejected";

    /// <summary>Reply rejected.</summary>
    public const string ReplyRejected = "support.reply.rejected";

    /// <summary>Close/reopen action rejected.</summary>
    public const string ActionRejected = "support.action.rejected";

    /// <summary>Admin patch rejected.</summary>
    public const string PatchRejected = "support.patch.rejected";

    /// <summary>Seller capability denied.</summary>
    public const string SellerAuthorizationDenied = "seller.authorization.denied";

    /// <summary>Admin capability denied.</summary>
    public const string AdminAuthorizationDenied = "admin.authorization.denied";

    /// <summary>Development demo seed not ready.</summary>
    public const string DemoNotReady = "support.demo.not_ready";
}
