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
        // Same TB-191 group rate row use cases behind the seller owner gate.
        group.MapGet(
            "/shipping-services/authoring-drafts/{draftId:guid}/region-groups/{groupId:guid}/rate-rows",
            GetGroupRateRowsAsync);
        group.MapPut(
            "/shipping-services/authoring-drafts/{draftId:guid}/region-groups/{groupId:guid}/rate-rows",
            ReplaceGroupRateRowsAsync);
        // Same TB-192 presentation use cases behind the seller owner gate.
        group.MapGet(
            "/shipping-services/authoring-drafts/{draftId:guid}/presentation",
            GetPresentationAsync);
        group.MapPatch("/shipping-services/authoring-drafts/{draftId:guid}/presentation", PatchPresentationAsync);
        // Same TB-193 weekly schedule use cases behind the seller owner gate.
        group.MapGet(
            "/shipping-services/authoring-drafts/{draftId:guid}/weekly-schedule",
            GetWeeklyScheduleAsync);
        group.MapPut(
            "/shipping-services/authoring-drafts/{draftId:guid}/weekly-schedule",
            ReplaceWeeklyScheduleAsync);
        // Same TB-194 method-root settings use cases behind the seller owner gate.
        group.MapGet(
            "/shipping-services/authoring-drafts/{draftId:guid}/method-settings",
            GetMethodSettingsAsync);
        group.MapPut(
            "/shipping-services/authoring-drafts/{draftId:guid}/method-settings",
            ReplaceMethodSettingsAsync);
        // Same TB-197 type selection use cases behind the seller owner gate. The catalog is still
        // Fulfillment-owned and read-only here; a seller can never administer it.
        group.MapGet("/shipping-services/authoring-types", ListTypeChoicesAsync);
        group.MapGet(
            "/shipping-services/authoring-drafts/{draftId:guid}/method-type",
            GetMethodTypeAsync);
        group.MapPut(
            "/shipping-services/authoring-drafts/{draftId:guid}/method-type",
            ReplaceMethodTypeAsync);
        // Same TB-199 atomic editor document use cases behind the seller owner gate.
        group.MapGet(
            "/shipping-services/authoring-drafts/{draftId:guid}/editor-document",
            GetEditorDocumentAsync);
        group.MapPut(
            "/shipping-services/authoring-drafts/{draftId:guid}/editor-document",
            ReplaceEditorDocumentAsync);
        // Same TB-220 publication use cases behind the seller owner gate. The owner kind never comes from
        // the payload, so a legitimate Marketplace seller keeps its own owner identity.
        group.MapGet(
            "/shipping-services/authoring-drafts/{draftId:guid}/publication",
            GetPublicationAsync);
        group.MapPost(
            "/shipping-services/authoring-drafts/{draftId:guid}/publication",
            PublishAsync);
        group.MapPost(
            "/shipping-services/authoring-drafts/{draftId:guid}/publication/withdraw",
            WithdrawPublicationAsync);
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

    private static async Task<IResult> GetGroupRateRowsAsync(
        Guid draftId,
        Guid groupId,
        MediatR.ISender sender,
        IShippingAuthoringOwnerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var scope = await authorizer.RequireSellerOwnerAsync(context, ShippingAdminPermissions.View, cancellationToken);
        return api.From(await sender.Send(
            new GetShippingAuthoringGroupRateRowsQuery(scope, draftId, groupId),
            cancellationToken));
    }

    private static async Task<IResult> ReplaceGroupRateRowsAsync(
        Guid draftId,
        Guid groupId,
        MediatR.ISender sender,
        IShippingAuthoringOwnerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var scope = await authorizer.RequireSellerOwnerAsync(context, ShippingAdminPermissions.Manage, cancellationToken);
        var (body, failure) = await ShippingEndpointBody.ReadAsync<ShippingAuthoringRateRowsReplaceRequest>(
            context,
            api,
            cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        return api.From(await sender.Send(
            new ReplaceShippingAuthoringGroupRateRowsCommand(scope, draftId, groupId, body!),
            cancellationToken));
    }

    private static async Task<IResult> GetPresentationAsync(
        Guid draftId,
        MediatR.ISender sender,
        IShippingAuthoringOwnerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var scope = await authorizer.RequireSellerOwnerAsync(context, ShippingAdminPermissions.View, cancellationToken);
        return api.From(await sender.Send(
            new GetShippingAuthoringPresentationQuery(scope, draftId),
            cancellationToken));
    }

    private static async Task<IResult> PatchPresentationAsync(
        Guid draftId,
        MediatR.ISender sender,
        IShippingAuthoringOwnerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var scope = await authorizer.RequireSellerOwnerAsync(context, ShippingAdminPermissions.Manage, cancellationToken);
        var (body, failure) = await ShippingEndpointBody.ReadAsync<ShippingAuthoringPresentationPatchRequest>(
            context,
            api,
            cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        return api.From(await sender.Send(
            new ReplaceShippingAuthoringPresentationCommand(scope, draftId, body!),
            cancellationToken));
    }

    private static async Task<IResult> GetWeeklyScheduleAsync(
        Guid draftId,
        MediatR.ISender sender,
        IShippingAuthoringOwnerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var scope = await authorizer.RequireSellerOwnerAsync(context, ShippingAdminPermissions.View, cancellationToken);
        return api.From(await sender.Send(
            new GetShippingAuthoringWeeklyScheduleQuery(scope, draftId),
            cancellationToken));
    }

    private static async Task<IResult> ReplaceWeeklyScheduleAsync(
        Guid draftId,
        MediatR.ISender sender,
        IShippingAuthoringOwnerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var scope = await authorizer.RequireSellerOwnerAsync(context, ShippingAdminPermissions.Manage, cancellationToken);
        var (body, failure) = await ShippingEndpointBody.ReadAsync<ShippingAuthoringWeeklyScheduleReplaceRequest>(
            context,
            api,
            cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        return api.From(await sender.Send(
            new ReplaceShippingAuthoringWeeklyScheduleCommand(scope, draftId, body!),
            cancellationToken));
    }
    private static async Task<IResult> GetMethodSettingsAsync(
        Guid draftId,
        MediatR.ISender sender,
        IShippingAuthoringOwnerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var scope = await authorizer.RequireSellerOwnerAsync(context, ShippingAdminPermissions.View, cancellationToken);
        return api.From(await sender.Send(
            new GetShippingAuthoringMethodSettingsQuery(scope, draftId),
            cancellationToken));
    }

    private static async Task<IResult> ReplaceMethodSettingsAsync(
        Guid draftId,
        MediatR.ISender sender,
        IShippingAuthoringOwnerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var scope = await authorizer.RequireSellerOwnerAsync(context, ShippingAdminPermissions.Manage, cancellationToken);
        var (body, failure) = await ShippingEndpointBody.ReadAsync<ShippingAuthoringMethodSettingsReplaceRequest>(
            context,
            api,
            cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        return api.From(await sender.Send(
            new ReplaceShippingAuthoringMethodSettingsCommand(scope, draftId, body!),
            cancellationToken));
    }

    private static async Task<IResult> ListTypeChoicesAsync(
        MediatR.ISender sender,
        IShippingAuthoringOwnerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var scope = await authorizer.RequireSellerOwnerAsync(context, ShippingAdminPermissions.View, cancellationToken);
        return api.From(await sender.Send(new GetShippingAuthoringTypeChoicesQuery(scope), cancellationToken));
    }

    private static async Task<IResult> GetMethodTypeAsync(
        Guid draftId,
        MediatR.ISender sender,
        IShippingAuthoringOwnerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var scope = await authorizer.RequireSellerOwnerAsync(context, ShippingAdminPermissions.View, cancellationToken);
        return api.From(await sender.Send(
            new GetShippingAuthoringDraftTypeAssignmentQuery(scope, draftId),
            cancellationToken));
    }

    private static async Task<IResult> ReplaceMethodTypeAsync(
        Guid draftId,
        MediatR.ISender sender,
        IShippingAuthoringOwnerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var scope = await authorizer.RequireSellerOwnerAsync(context, ShippingAdminPermissions.Manage, cancellationToken);
        var (body, failure) = await ShippingEndpointBody.ReadAsync<ShippingAuthoringDraftTypeAssignmentReplaceRequest>(
            context,
            api,
            cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        return api.From(await sender.Send(
            new ReplaceShippingAuthoringDraftTypeAssignmentCommand(scope, draftId, body!),
            cancellationToken));
    }

    private static async Task<IResult> GetEditorDocumentAsync(
        Guid draftId,
        MediatR.ISender sender,
        IShippingAuthoringOwnerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var scope = await authorizer.RequireSellerOwnerAsync(context, ShippingAdminPermissions.View, cancellationToken);
        return api.From(await sender.Send(
            new GetShippingAuthoringEditorDocumentQuery(scope, draftId),
            cancellationToken));
    }

    private static async Task<IResult> ReplaceEditorDocumentAsync(
        Guid draftId,
        MediatR.ISender sender,
        IShippingAuthoringOwnerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var scope = await authorizer.RequireSellerOwnerAsync(context, ShippingAdminPermissions.Manage, cancellationToken);
        var (body, failure) = await ShippingEndpointBody.ReadAsync<ShippingAuthoringEditorDocumentReplaceRequest>(
            context,
            api,
            cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        return api.From(await sender.Send(
            new ReplaceShippingAuthoringEditorDocumentCommand(scope, draftId, body!),
            cancellationToken));
    }

    private static async Task<IResult> GetPublicationAsync(
        Guid draftId,
        MediatR.ISender sender,
        IShippingAuthoringOwnerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var scope = await authorizer.RequireSellerOwnerAsync(context, ShippingAdminPermissions.View, cancellationToken);
        return api.From(await sender.Send(
            new GetShippingAuthoringPublicationQuery(scope, draftId),
            cancellationToken));
    }

    private static async Task<IResult> PublishAsync(
        Guid draftId,
        MediatR.ISender sender,
        IShippingAuthoringOwnerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var scope = await authorizer.RequireSellerOwnerAsync(context, ShippingAdminPermissions.Manage, cancellationToken);
        var (body, failure) = await ShippingEndpointBody.ReadAsync<ShippingAuthoringPublicationPublishRequest>(
            context,
            api,
            cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        return api.From(await sender.Send(
            new PublishShippingAuthoringDraftCommand(scope, draftId, body!),
            cancellationToken));
    }

    private static async Task<IResult> WithdrawPublicationAsync(
        Guid draftId,
        MediatR.ISender sender,
        IShippingAuthoringOwnerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var scope = await authorizer.RequireSellerOwnerAsync(context, ShippingAdminPermissions.Manage, cancellationToken);
        var (body, failure) = await ShippingEndpointBody.ReadAsync<ShippingAuthoringPublicationWithdrawRequest>(
            context,
            api,
            cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        return api.From(await sender.Send(
            new WithdrawShippingAuthoringPublicationCommand(scope, draftId, body!),
            cancellationToken));
    }
}