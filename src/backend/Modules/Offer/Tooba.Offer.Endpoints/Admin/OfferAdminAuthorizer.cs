using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.BuildingBlocks.Security;

#pragma warning disable CS1591
namespace Tooba.Offer.Endpoints.Admin;

/// <summary>Admin Offer permissions use the existing platform capabilities, not seller headers.</summary>
public interface IOfferAdminAuthorizer
{
    Task<Guid> RequireViewAsync(HttpContext context, CancellationToken cancellationToken);
    Task<Guid> RequireCreateAsync(HttpContext context, CancellationToken cancellationToken);
    Task<Guid> RequireEditAsync(HttpContext context, CancellationToken cancellationToken);
}

/// <summary>Host-neutral admin Offer authorizer over the existing panel and capability seams.</summary>
public sealed class OfferAdminAuthorizer(
    IAdminPanelAccess adminAccess,
    IAuthorizationService authorization,
    ICurrentEdition edition,
    ICurrentTenant tenant) : IOfferAdminAuthorizer
{
    public Task<Guid> RequireViewAsync(HttpContext context, CancellationToken cancellationToken)
        => RequireAsync(context, "product.view", cancellationToken);

    public Task<Guid> RequireCreateAsync(HttpContext context, CancellationToken cancellationToken)
        => RequireAsync(context, "product.create", cancellationToken);

    public Task<Guid> RequireEditAsync(HttpContext context, CancellationToken cancellationToken)
        => RequireAsync(context, "product.edit", cancellationToken);

    private async Task<Guid> RequireAsync(HttpContext context, string permission, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var actor = await adminAccess.RequireAuthorizedAsync(context.Request, cancellationToken);
        var decision = await authorization.CanAsync(
            new AuthorizationCheck
            {
                Subject = AuthorizationSubject.ForUser(actor),
                Resource = new AuthorizationResource
                {
                    Type = AuthorizationObjectTypes.Permission,
                    Id = permission,
                },
                Permission = AuthorizationRelations.Check,
                CallContext = CallContext(edition, tenant),
            },
            cancellationToken);
        if (decision.Kind == AuthorizationDecisionKind.Allow)
        {
            return actor;
        }
        if (decision.Kind == AuthorizationDecisionKind.Unavailable)
        {
            throw new SemanticException(new SemanticError(FoundationErrorCodes.AdminAuthorizationUnavailable));
        }
        throw new SemanticException(new SemanticError(FoundationErrorCodes.AdminAuthorizationDenied));
    }

    private static AuthorizationCallContext CallContext(ICurrentEdition edition, ICurrentTenant tenant)
        => edition.Current?.Edition switch
        {
            ToobaEdition.SingleStore => new AuthorizationCallContext
            {
                Edition = ToobaEdition.SingleStore,
                TenantId = tenant.Current?.TenantId.Value
                    ?? throw new SemanticException(new SemanticError(FoundationErrorCodes.AdminTenantMissing)),
            },
            ToobaEdition.Marketplace => new AuthorizationCallContext
            {
                Edition = ToobaEdition.Marketplace,
                TenantId = null,
            },
            _ => throw new SemanticException(new SemanticError(FoundationErrorCodes.AdminAuthorizationUnavailable)),
        };
}


