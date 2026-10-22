using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Shipping.Application.Authoring.Commands;
using Tooba.Shipping.Application.Authoring.Queries;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Endpoints.Admin;

namespace Tooba.Shipping.Endpoints.Seller;

/// <summary>
/// مسیرهای نویسندگی پیش‌نویس منطقه‌ای Shipping برای فروشندهٔ مجاز. همان use case های ادمین با
/// همان مجوزدهی مالک فروشنده؛ در نصب SingleStore این مسیرها با ناتوانی قابلیت بسته می‌شوند.
/// </summary>
/// <remarks>
/// هیچ هدر فروشنده در این لایه parse یا به پیام/پاسخ کپی نمی‌شود؛ SellerPartyId فقط از درز
/// canonical پلتفرم در مجوزدهی می‌آید.
/// </remarks>
public static class ShippingSellerAuthoringDraftEndpoints
{
    /// <summary>مسیرهای پیش‌نویس نویسندگی را زیر گروه فروشنده می‌نگارد.</summary>
    /// <param name="group">گروه مسیر فروشنده.</param>
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/shipping-services/authoring-drafts", ListAsync);
        group.MapPost("/shipping-services/authoring-drafts", CreateAsync);
        group.MapGet("/shipping-services/authoring-drafts/{draftId:guid}", GetAsync);
        group.MapPut("/shipping-services/authoring-drafts/{draftId:guid}", ReplaceRegionsAsync);
    }

    private static async Task<IResult> ListAsync(
        MediatR.ISender sender,
        IShippingAuthoringOwnerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var scope = await authorizer.RequireSellerOwnerAsync(context, ShippingAdminPermissions.View, cancellationToken);
        return api.From(await sender.Send(new GetShippingAuthoringDraftsQuery(scope), cancellationToken));
    }

    private static async Task<IResult> CreateAsync(
        MediatR.ISender sender,
        IShippingAuthoringOwnerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var scope = await authorizer.RequireSellerOwnerAsync(context, ShippingAdminPermissions.Manage, cancellationToken);
        var (body, failure) = await ShippingEndpointBody.ReadAsync<ShippingAuthoringDraftCreateRequest>(
            context,
            api,
            cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var result = await sender.Send(
            new CreateShippingAuthoringDraftCommand(scope, body!),
            cancellationToken);
        return result.IsFailure
            ? api.From(result)
            : api.Created(
                $"/v1/seller/shipping-services/authoring-drafts/{result.Value.DraftId}",
                result);
    }

    private static async Task<IResult> GetAsync(
        Guid draftId,
        MediatR.ISender sender,
        IShippingAuthoringOwnerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var scope = await authorizer.RequireSellerOwnerAsync(context, ShippingAdminPermissions.View, cancellationToken);
        return api.From(await sender.Send(new GetShippingAuthoringDraftQuery(scope, draftId), cancellationToken));
    }

    private static async Task<IResult> ReplaceRegionsAsync(
        Guid draftId,
        MediatR.ISender sender,
        IShippingAuthoringOwnerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var scope = await authorizer.RequireSellerOwnerAsync(context, ShippingAdminPermissions.Manage, cancellationToken);
        var (body, failure) = await ShippingEndpointBody.ReadAsync<ShippingAuthoringDraftReplaceRegionsRequest>(
            context,
            api,
            cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        return api.From(await sender.Send(
            new ReplaceShippingAuthoringDraftRegionsCommand(scope, draftId, body!),
            cancellationToken));
    }
}
