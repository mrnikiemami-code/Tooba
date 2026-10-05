using Tooba.BuildingBlocks;
using Tooba.Identity.Application.Models;
using Tooba.Identity.Application.Options;
using Tooba.Identity.Application.Ports;
using Tooba.Identity.Contracts;
using Tooba.Identity.Contracts.Auth;
using Tooba.Identity.Contracts.Errors;
using Tooba.Identity.Domain.Aggregates;
using Tooba.Identity.Domain.Enums;
using Tooba.Identity.Domain.Events;
using Tooba.Identity.Domain.Rules;

namespace Tooba.Identity.Infrastructure.Otp;

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

        throw new ContractOperationException(outcome.Kind switch
        {
            OtpDeliveryOutcomeKind.RateLimited => IdentityErrorCodes.OtpDeliveryRateLimited,
            OtpDeliveryOutcomeKind.InvalidDestination => IdentityErrorCodes.OtpDeliveryInvalidDestination,
            OtpDeliveryOutcomeKind.Unavailable => IdentityErrorCodes.OtpDeliveryUnavailable,
            _ => IdentityErrorCodes.OtpDeliveryUnconfigured,
        });
    }
}
