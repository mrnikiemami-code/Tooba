using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.CustomerProfile.Application.Composition;
using Tooba.CustomerProfile.Application.Account.Models;
using Tooba.CustomerProfile.Application.Profile.Queries;
using Tooba.CustomerProfile.Contracts.Dtos;
using Tooba.CustomerProfile.Contracts.Ports;

namespace Tooba.CustomerProfile.Application.Profile.Commands;

/// <summary>
/// Upsert descriptive profile fields for the trusted actor, then return the composed profile page.
/// Email/mobile credential authority remains Identity-owned.
/// </summary>
public sealed record UpsertCustomerProfileCommand(Guid ActorUserId, CustomerProfileWrite Input)
    : IRequest<Result<CustomerProfilePage>>;

/// <summary>Persists via CustomerProfile directory, then reuses the profile page query composition.</summary>
public sealed class UpsertCustomerProfileCommandHandler(
    ICustomerProfileDirectory profiles,
    ISender sender)
    : IRequestHandler<UpsertCustomerProfileCommand, Result<CustomerProfilePage>>
{
    /// <inheritdoc />
    public async Task<Result<CustomerProfilePage>> Handle(
        UpsertCustomerProfileCommand request,
        CancellationToken cancellationToken)
    {
        var persisted = await CustomerProfileOperation.ExecuteAsync(async () =>
        {
            await profiles.UpsertAsync(request.ActorUserId, request.Input, cancellationToken);
            return true;
        });
        if (persisted.IsFailure)
        {
            return Result.Failure<CustomerProfilePage>(persisted.Errors);
        }

        return await sender.Send(new GetCustomerProfilePageQuery(request.ActorUserId), cancellationToken);
    }
}
