using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.BuildingBlocks.Security;
using Tooba.Story.Endpoints.Seller;

namespace Tooba.Host.Security.Seller;

/// <summary>
/// Host transport adapter for Story seller Endpoints auth seam over ISellerPanelAccess,
/// plus the owning Story capability check from the Access Control catalog.
/// DENY and Unavailable are fail-closed; hiding a menu is not server authorization.
/// </summary>
public sealed class HostStorySellerAuthorizer(
    ISellerPanelAccess sellerAccess,
    IAuthorizationService authorization,
    ICurrentEdition edition) : IStorySellerAuthorizer
{
    /// <inheritdoc />
    public async Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
        HttpContext httpContext,
        string permissionId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionId);

        var (actorUserId, sellerPartyId) =
            await sellerAccess.RequireAuthorizedAsync(httpContext.Request, cancellationToken);

        var effectiveEdition = edition.Current?.Edition
            ?? throw new SemanticException(new SemanticError(SellerSecurityErrorCodes.AuthorizationUnavailable));

        var decision = await authorization.CanAsync(
            new AuthorizationCheck
            {
                Subject = AuthorizationSubject.ForUser(actorUserId),
                Resource = new AuthorizationResource
                {
                    Type = AuthorizationObjectTypes.Permission,
                    Id = permissionId,
                },
                Permission = AuthorizationRelations.Check,
                CallContext = new AuthorizationCallContext
                {
                    Edition = effectiveEdition,
                },
            },
            cancellationToken);

        if (decision.Kind == AuthorizationDecisionKind.Allow)
        {
            return (actorUserId, sellerPartyId);
        }

        if (decision.Kind == AuthorizationDecisionKind.Unavailable)
        {
            throw new SemanticException(new SemanticError(SellerSecurityErrorCodes.AuthorizationUnavailable));
        }

        throw new SemanticException(new SemanticError(FoundationErrorCodes.SellerAuthorizationDenied));
    }
}
