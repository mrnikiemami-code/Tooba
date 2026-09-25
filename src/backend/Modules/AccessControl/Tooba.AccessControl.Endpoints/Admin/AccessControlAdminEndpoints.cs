using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.AccessControl.Application.Commands.EnsureBootstrap;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Security;

namespace Tooba.AccessControl.Endpoints.Admin;

/// <summary>مسیرهای نازک پنل مدیر برای Access Control.</summary>
public static class AccessControlAdminEndpoints
{
    /// <summary>مسیرهای admin را ثبت می‌کند.</summary>
    /// <param name="group">گروه مسیر admin/access-control.</param>
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapPost("/bootstrap", BootstrapAsync);
    }

    private static async Task<IResult> BootstrapAsync(
        HttpRequest request,
        ISender sender,
        IAdminPanelAccess adminPanelAccess,
        ICurrentTenant tenant,
        CancellationToken cancellationToken)
    {
        var actor = await adminPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
        await sender.Send(
            new EnsureAccessControlBootstrapCommand(actor, tenant.Current?.TenantId.Value),
            cancellationToken);
        return Results.Json(new { ok = true });
    }
}
