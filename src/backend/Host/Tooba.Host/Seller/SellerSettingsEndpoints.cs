using Tooba.AccessControl.Application;
using Tooba.AccessControl.Domain;
using Tooba.BuildingBlocks;
using Tooba.Party.Application;

namespace Tooba.Host.Seller;

/// <summary>
/// مرز HTTP تنظیمات کسب‌وکار فروشنده روی مالک Party Organization.
/// </summary>
public static class SellerSettingsEndpoints
{
    /// <summary>
    /// مسیرهای تنظیمات فروشنده را ثبت می‌کند.
    /// </summary>
    public static void MapSellerSettingsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/seller/settings");
        group.MapGet("/", GetSettingsAsync);
        group.MapPut("/", UpdateSettingsAsync);
    }

    private static async Task<IResult> GetSettingsAsync(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        IAccessControlDirectory access,
        IPartyDirectory parties,
        CancellationToken cancellationToken)
    {
        try
        {
            var (actorUserId, sellerPartyId) = await SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
            await EnsureSellerCapabilityAsync(actorUserId, sellerPartyId, "seller.settings.view", access, cancellationToken);
            var profile = await parties.GetOrganizationProfileAsync(sellerPartyId, cancellationToken);
            if (profile is null)
            {
                return Results.Json(
                    new { title = "Not Found", errorCode = "seller.settings.missing" },
                    statusCode: StatusCodes.Status404NotFound);
            }

            var canManage = await HasSellerCapabilityAsync(
                actorUserId, sellerPartyId, "seller.settings.manage", access, cancellationToken);
            return Results.Json(new
            {
                partyId = profile.PartyId,
                displayName = profile.DisplayName,
                legalName = profile.LegalName,
                description = profile.Description,
                supportPhone = profile.SupportPhone,
                supportEmail = profile.SupportEmail,
                addressLine = profile.AddressLine,
                updatedAt = profile.UpdatedAt,
                canManage,
            });
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Message, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
        catch (InvalidOperationException)
        {
            return Results.Json(
                new { title = "Rejected", errorCode = "seller.settings.rejected" },
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> UpdateSettingsAsync(
        OrganizationProfileWriteRequest body,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        IAccessControlDirectory access,
        IPartyDirectory parties,
        CancellationToken cancellationToken)
    {
        try
        {
            var (actorUserId, sellerPartyId) = await SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
            await EnsureSellerCapabilityAsync(actorUserId, sellerPartyId, "seller.settings.manage", access, cancellationToken);
            var updated = await parties.UpdateOrganizationProfileAsync(
                sellerPartyId,
                new OrganizationProfileWrite(
                    body.DisplayName,
                    body.LegalName,
                    body.Description,
                    body.SupportPhone,
                    body.SupportEmail,
                    body.AddressLine),
                cancellationToken);
            return Results.Json(new
            {
                partyId = updated.PartyId,
                displayName = updated.DisplayName,
                legalName = updated.LegalName,
                description = updated.Description,
                supportPhone = updated.SupportPhone,
                supportEmail = updated.SupportEmail,
                addressLine = updated.AddressLine,
                updatedAt = updated.UpdatedAt,
                canManage = true,
            });
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Message, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
        catch (InvalidOperationException)
        {
            return Results.Json(
                new { title = "Rejected", errorCode = "seller.settings.rejected" },
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>بررسی قابلیت فروشنده از Access Control مؤثر.</summary>
    internal static async Task EnsureSellerCapabilityAsync(
        Guid actorUserId,
        Guid sellerPartyId,
        string permissionId,
        IAccessControlDirectory access,
        CancellationToken cancellationToken)
    {
        if (!await HasSellerCapabilityAsync(actorUserId, sellerPartyId, permissionId, access, cancellationToken))
        {
            throw new PlatformHttpException(403, "مجوز تنظیمات فروشنده وجود ندارد.", "seller.authorization.denied");
        }
    }

    private static async Task<bool> HasSellerCapabilityAsync(
        Guid actorUserId,
        Guid sellerPartyId,
        string permissionId,
        IAccessControlDirectory access,
        CancellationToken cancellationToken)
    {
        var effective = await access.GetEffectiveAccessAsync(
            actorUserId,
            new AccessOwnerScope(AccessOwnerScopeKind.Seller, sellerPartyId),
            cancellationToken);
        return effective.Permissions.Any(p =>
            p.PermissionId == permissionId
            && !p.DeniedByCeiling
            && p.ScopeKind == AccessScopeKind.GlobalWithinOwner);
    }
}

/// <summary>بدنهٔ ویرایش تنظیمات سازمانی فروشنده.</summary>
public sealed record OrganizationProfileWriteRequest(
    string DisplayName,
    string? LegalName,
    string? Description,
    string? SupportPhone,
    string? SupportEmail,
    string? AddressLine);
