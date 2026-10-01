using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Security;
using Tooba.Party.Application.Admin.Sellers.Queries;

namespace Tooba.Party.Endpoints.Admin.Sellers;

/// <summary>
/// GET /v1/admin/sellers — مالکیت Party via MediatR و <see cref="ApiResponseFactory"/>.
/// </summary>
public static class PartyAdminSellersEndpoints
{
    /// <summary>مسیر فهرست فروشندگان Admin را ثبت می‌کند.</summary>
    public static void Map(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.MapGet("/v1/admin/sellers", ListAsync);
    }

    private static async Task<IResult> ListAsync(
        HttpRequest request,
        IAdminPanelAccess adminAccess,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        await adminAccess.RequireAuthorizedAsync(request, cancellationToken).ConfigureAwait(false);
        return api.From(await sender.Send(new ListAdminSellersQuery(), cancellationToken).ConfigureAwait(false));
    }
}
