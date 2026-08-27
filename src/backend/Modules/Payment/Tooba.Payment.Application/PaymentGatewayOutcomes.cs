namespace Tooba.Payment.Application;

/// <summary>
/// کدهای نتیجهٔ درگاه که هنوز قطعی نیستند؛ Payment نباید به Failed برود.
/// </summary>
public static class PaymentGatewayOutcomes
{
    /// <summary>
    /// آیا FailureCode به معنای نامعلوم/موقت است (Pending بماند).
    /// </summary>
    public static bool IsIndeterminate(string? failureCode)
    {
        if (string.IsNullOrWhiteSpace(failureCode))
        {
            return false;
        }

        return failureCode is
            "GATEWAY_TIMEOUT"
            or "GATEWAY_UNAVAILABLE"
            or "GATEWAY_RATE_LIMITED"
            or "GATEWAY_PENDING"
            or "GATEWAY_UNKNOWN";
    }
}
