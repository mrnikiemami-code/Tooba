using MediatR;
using Tooba.CustomerProfile.Application.Models;
using Tooba.CustomerProfile.Application.Ports;
using Tooba.CustomerProfile.Application.Queries.GetCustomerProfilePage;
using Tooba.CustomerProfile.Contracts;

namespace Tooba.CustomerProfile.Application.Commands.UpsertCustomerProfile;

/// <summary>
/// Upsert descriptive profile fields for the trusted actor, then return the composed profile page.
/// Email/mobile credential authority remains Identity-owned.
/// </summary>
public sealed record UpsertCustomerProfileCommand(Guid ActorUserId, CustomerProfileWrite Input)
    : IRequest<CustomerProfilePage>;

/// <summary>Persists via CustomerProfile directory, then reuses the profile page query composition.</summary>
public sealed class UpsertCustomerProfileCommandHandler(
    ICustomerProfileDirectory profiles,
    ISender sender)
    : IRequestHandler<UpsertCustomerProfileCommand, CustomerProfilePage>
{
    /// <inheritdoc />
    public async Task<CustomerProfilePage> Handle(
        UpsertCustomerProfileCommand request,
        CancellationToken cancellationToken)
    {
        await profiles.UpsertAsync(request.ActorUserId, request.Input, cancellationToken);
        return await sender.Send(new GetCustomerProfilePageQuery(request.ActorUserId), cancellationToken);
    }
}
