using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.CustomerProfile.Application.Composition;
using Tooba.CustomerProfile.Application.Account.Models;
using Tooba.CustomerProfile.Application.Ports;
using Tooba.CustomerProfile.Contracts.Ports;
using Tooba.CustomerProfile.Contracts.Errors;
using Tooba.Identity.Contracts;
using Tooba.Identity.Contracts.Auth;
using Tooba.Order.Contracts.Customer;

namespace Tooba.CustomerProfile.Application.Profile.Queries;

/// <summary>
/// Read customer-account profile page. Actor comes from the trusted server seam (not transport payload).
/// Order/Identity enrichment is Contracts-only; no foreign Application/Infrastructure.
/// </summary>
public sealed record GetCustomerProfilePageQuery(Guid ActorUserId) : IRequest<Result<CustomerProfilePage>>;

/// <summary>Composes profile presentation from CustomerProfile + Identity + optional Order hints.</summary>
public sealed class GetCustomerProfilePageQueryHandler(
    ICustomerProfileDirectory profiles,
    IIdentityContactLookup identityContacts,
    ICustomerOrderDashboardSummaryPort orderSummary,
    ICustomerAccountDisplayTexts displayTexts)
    : IRequestHandler<GetCustomerProfilePageQuery, Result<CustomerProfilePage>>
{
    /// <inheritdoc />
    public Task<Result<CustomerProfilePage>> Handle(
        GetCustomerProfilePageQuery request,
        CancellationToken cancellationToken) =>
        CustomerProfileOperation.ExecuteAsync(async () =>
        {
            if (request.ActorUserId == Guid.Empty)
            {
                throw new ContractOperationException(CustomerProfileErrorCodes.ActorRequired);
            }

            var stored = await profiles.GetAsync(request.ActorUserId, cancellationToken);
            var contact = await identityContacts.GetContactAsync(request.ActorUserId, cancellationToken);
            var summary = await orderSummary.GetAsync(request.ActorUserId, cancellationToken);
            var address = summary?.LatestShippingAddress;
            var displayName = stored?.DisplayName
                ?? (string.IsNullOrWhiteSpace(summary?.LatestOrderRecipientDisplayName)
                    ? null
                    : summary!.LatestOrderRecipientDisplayName)
                ?? displayTexts.DefaultDisplayName;
            var mobile = contact.Mobile
                ?? (string.IsNullOrWhiteSpace(summary?.LatestContactMobile)
                    ? null
                    : summary!.LatestContactMobile);
            return new CustomerProfilePage(
                request.ActorUserId,
                displayName,
                stored?.FirstName,
                stored?.LastName,
                contact.Email,
                mobile,
                stored?.BirthDate,
                stored?.Bio,
                address,
                EmailEditable: false,
                MobileEditable: false,
                AvatarUploadAvailable: false,
                NationalCodeEditable: false,
                AddressEditable: false,
                Editable: true);
        });
}
