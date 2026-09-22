namespace Tooba.Notification.Application.Errors;

/// <summary>Stable Notification semantic error codes for HTTP/use-case outcomes.</summary>
public static class NotificationErrorCodes
{
    /// <summary>Authenticated customer session required (production unauthenticated).</summary>
    public const string CustomerSessionRequired = "customer.session.required";

    /// <summary>Notification was not found for the recipient scope.</summary>
    public const string Missing = "notification.missing";
}
