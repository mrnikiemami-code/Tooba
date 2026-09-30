using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Settings.CheckoutIdentity.Ports;

namespace Tooba.Catalog.Application.Settings.CheckoutIdentity.Queries;

/// <summary>Storefront GET /v1/storefront/checkout-identity-policy payload.</summary>
public sealed record StorefrontCheckoutIdentityPolicyDto(
    string Policy,
    bool CartAnonymousAllowed,
    bool CheckoutAuthenticationRequired);

/// <summary>Loads public storefront checkout-identity policy flags.</summary>
public sealed record GetStorefrontCheckoutIdentityPolicyQuery
    : IRequest<Result<StorefrontCheckoutIdentityPolicyDto>>;

/// <summary>Handler for <see cref="GetStorefrontCheckoutIdentityPolicyQuery"/>.</summary>
public sealed class GetStorefrontCheckoutIdentityPolicyHandler(
    IStoreCheckoutIdentitySettingsDirectory directory)
    : IRequestHandler<GetStorefrontCheckoutIdentityPolicyQuery, Result<StorefrontCheckoutIdentityPolicyDto>>
{
    /// <inheritdoc />
    public async Task<Result<StorefrontCheckoutIdentityPolicyDto>> Handle(
        GetStorefrontCheckoutIdentityPolicyQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var view = await directory.GetAsync(cancellationToken);
        var authenticatedOnly = string.Equals(
            view.Policy,
            "AuthenticatedOnly",
            StringComparison.OrdinalIgnoreCase);
        return Result.Success(new StorefrontCheckoutIdentityPolicyDto(
            view.Policy,
            CartAnonymousAllowed: true,
            CheckoutAuthenticationRequired: authenticatedOnly));
    }
}
