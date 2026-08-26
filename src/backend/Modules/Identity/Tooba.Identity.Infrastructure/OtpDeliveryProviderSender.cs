using Tooba.Identity.Application;
using Tooba.Identity.Domain;

namespace Tooba.Identity.Infrastructure;

/// <summary>
/// IOtpSender adapter over IOtpDeliveryProvider with stable Identity error codes.
/// </summary>
public sealed class OtpDeliveryProviderSender : IOtpSender
{
    private readonly IOtpDeliveryProvider _provider;

    /// <summary>Wraps provider as Identity IOtpSender.</summary>
    public OtpDeliveryProviderSender(IOtpDeliveryProvider provider) => _provider = provider;

    /// <inheritdoc />
    public async Task SendAsync(OtpPurpose purpose, string destination, string oneTimeCode, CancellationToken cancellationToken)
    {
        var outcome = await _provider.DeliverAsync(new OtpDeliveryMessage(purpose, destination, oneTimeCode), cancellationToken);
        if (outcome.Kind == OtpDeliveryOutcomeKind.Succeeded)
        {
            return;
        }

        throw outcome.Kind switch
        {
            OtpDeliveryOutcomeKind.RateLimited => new InvalidOperationException("identity.otp.delivery.rate_limited"),
            OtpDeliveryOutcomeKind.InvalidDestination => new InvalidOperationException("identity.otp.delivery.invalid_destination"),
            OtpDeliveryOutcomeKind.Unavailable => new InvalidOperationException("identity.otp.delivery.unavailable"),
            _ => new InvalidOperationException("identity.otp.delivery.unconfigured"),
        };
    }
}
