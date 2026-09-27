using MediatR;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Catalog.Application.Settings.CheckoutAbuse.Commands;
using Tooba.Catalog.Application.Settings.CheckoutAbuse.Queries;
using Tooba.Catalog.Endpoints.Admin;

namespace Tooba.Catalog.Endpoints.Admin.Settings;

/// <summary>Admin checkout-abuse settings — Catalog-owned HTTP via MediatR.</summary>
public static class CheckoutAbuseSettingsEndpoints
{
    /// <summary>Maps GET/PUT /v1/admin/settings/checkout-abuse.</summary>
    public static void MapCheckoutAbuseSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/admin/settings/checkout-abuse");
        group.MapGet("/", GetAsync);
        group.MapPut("/", PutAsync);
    }

    private static async Task<IResult> GetAsync(
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new GetCheckoutAbuseSettingsQuery(), cancellationToken));
    }

    private static async Task<IResult> PutAsync(
        CheckoutAbuseSettingsWriteRequest body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var actor = await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new SaveCheckoutAbuseSettingsCommand(
                body.MaxOpenUnpaidOrdersPerCustomer,
                body.ReservationCommitWindowMinutes,
                body.MaxCheckoutCommitsPerCustomerInWindow,
                actor),
            cancellationToken));
    }
}

/// <summary>PUT body for checkout-abuse settings.</summary>
public sealed record CheckoutAbuseSettingsWriteRequest(
    int? MaxOpenUnpaidOrdersPerCustomer,
    int? ReservationCommitWindowMinutes,
    int? MaxCheckoutCommitsPerCustomerInWindow);
