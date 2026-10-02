using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.CustomerProfile.Contracts;
using Tooba.Identity.Application.Auth.Models;
using Tooba.Identity.Contracts;
using Tooba.Identity.Contracts.Problems;

namespace Tooba.Identity.Application.Auth.Queries;

/// <summary>Projects the authenticated principal for storefront account chrome.</summary>
public sealed record GetAuthMeQuery(
    Guid UserId,
    Guid SessionId,
    string Edition,
    string? TenantId,
    bool IsAuthenticated) : IRequest<Result<AuthMeDto>>;

/// <summary>Handler for <see cref="GetAuthMeQuery"/>.</summary>
public sealed class GetAuthMeQueryHandler(
    ICustomerProfileDirectory profiles,
    IIdentityContactLookup contacts)
    : IRequestHandler<GetAuthMeQuery, Result<AuthMeDto>>
{
    /// <inheritdoc />
    public async Task<Result<AuthMeDto>> Handle(GetAuthMeQuery request, CancellationToken cancellationToken)
    {
        if (!request.IsAuthenticated)
        {
            return Result.Failure<AuthMeDto>(new SemanticError(IdentityErrorCodes.SessionInvalid));
        }

        var profile = await profiles.GetAsync(request.UserId, cancellationToken);
        var contact = await contacts.GetContactAsync(request.UserId, cancellationToken);
        var displayName = CanonicalName(profile?.DisplayName, profile?.FirstName, profile?.LastName);
        return Result.Success(new AuthMeDto(
            request.UserId,
            request.SessionId,
            request.Edition,
            request.TenantId,
            displayName,
            BlankToNull(profile?.FirstName),
            BlankToNull(profile?.LastName),
            BlankToNull(contact.Mobile)));
    }

    private static string? CanonicalName(string? displayName, string? firstName, string? lastName)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName.Trim();
        }

        var first = firstName?.Trim() ?? string.Empty;
        var last = lastName?.Trim() ?? string.Empty;
        return first.Length > 0 && last.Length > 0 ? first + " " + last : null;
    }

    private static string? BlankToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
