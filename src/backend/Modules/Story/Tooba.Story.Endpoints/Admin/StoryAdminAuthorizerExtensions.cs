using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation.Errors;

namespace Tooba.Story.Endpoints.Admin;

public static class StoryAdminAuthorizerExtensions
{
    public static async Task<Guid> RequireAllAuthorizedAsync(
        this IStoryAdminAuthorizer authorizer,
        HttpContext httpContext,
        IReadOnlyList<string> permissionIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorizer);
        if (permissionIds.Count == 0)
            throw new ArgumentException("At least one Story permission is required.", nameof(permissionIds));

        Guid actorUserId = default;
        foreach (var permissionId in permissionIds)
        {
            var currentActor = await authorizer.RequireAuthorizedAsync(httpContext, permissionId, cancellationToken);
            if (actorUserId == default)
            {
                actorUserId = currentActor;
                continue;
            }

            if (currentActor != actorUserId)
                throw new SemanticException(new SemanticError(FoundationErrorCodes.AdminAuthorizationDenied));
        }

        return actorUserId;
    }
}
