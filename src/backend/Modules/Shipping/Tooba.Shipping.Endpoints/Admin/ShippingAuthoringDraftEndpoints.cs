using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Shipping.Application.Authoring.Commands;
using Tooba.Shipping.Application.Authoring.Queries;
using Tooba.Shipping.Contracts.Authoring;

namespace Tooba.Shipping.Endpoints.Admin;

/// <summary>
/// مسیرهای نویسندگی پیش‌نویس منطقه‌ای Shipping برای ادمین. مالک همیشه پس از مجوزدهی سرورساخته
/// می‌شود و هیچ route/query/body آن را bind نمی‌کند.
/// </summary>
/// <remarks>
/// خواندن <c>fulfillment.view</c> و نوشتن <c>fulfillment.manage</c> را <b>مستقل</b> می‌خواهد؛
/// مجوز خواندن هرگز مجوز نوشتن نیست. ذخیرهٔ یک پیش‌نویس هیچ روش ارسالی را فعال نمی‌کند.
/// </remarks>
public static class ShippingAuthoringDraftEndpoints
{
    /// <summary>مسیرهای پیش‌نویس نویسندگی را زیر گروه ادمین می‌نگارد.</summary>
    /// <param name="group">گروه مسیر ادمین.</param>
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/shipping-services/authoring-drafts", ListAsync);
        group.MapPost("/shipping-services/authoring-drafts", CreateAsync);
        group.MapGet("/shipping-services/authoring-drafts/{draftId:guid}", GetAsync);
        group.MapPut("/shipping-services/authoring-drafts/{draftId:guid}", ReplaceRegionsAsync);
        // TB-191 additive group rate rows: a row references the STABLE group id and never repeats
        // geography. Same root draft revision as the region replacement, so neither can overwrite the
        // other's work.
        group.MapGet(
            "/shipping-services/authoring-drafts/{draftId:guid}/region-groups/{groupId:guid}/rate-rows",
            GetGroupRateRowsAsync);
        group.MapPut(
            "/shipping-services/authoring-drafts/{draftId:guid}/region-groups/{groupId:guid}/rate-rows",
            ReplaceGroupRateRowsAsync);
        // TB-192 owner-local multilingual presentation: raw language-keyed text plus the ACTUAL active
        // registry on read, and a selected-language-only patch on write. Interface locale never selects
        // content language and the body never carries owner/mode/tenant/currency.
        group.MapGet(
            "/shipping-services/authoring-drafts/{draftId:guid}/presentation",
            GetPresentationAsync);
        group.MapPatch("/shipping-services/authoring-drafts/{draftId:guid}/presentation", PatchPresentationAsync);
        // TB-193 owner-scoped per-weekday operating schedule: ONE schedule attached to the draft/method,
        // shared by all its region groups. Absent is UNCONFIGURED, never seven implicit working days, and
        // the explicit timezone is validated server-side rather than inferred from locale or country.
        group.MapGet(
            "/shipping-services/authoring-drafts/{draftId:guid}/weekly-schedule",
            GetWeeklyScheduleAsync);
        group.MapPut(
            "/shipping-services/authoring-drafts/{draftId:guid}/weekly-schedule",
            ReplaceWeeklyScheduleAsync);
        // TB-194 method-root draft settings: freight payment timing and calculation mode are TWO
        // independent selections owned once by the draft/method. Absence is UNCONFIGURED, never an
        // implicit Manual/Prepaid, and neither choice evaluates a rate or activates the method.
        group.MapGet(
            "/shipping-services/authoring-drafts/{draftId:guid}/method-settings",
            GetMethodSettingsAsync);
        group.MapPut(
            "/shipping-services/authoring-drafts/{draftId:guid}/method-settings",
            ReplaceMethodSettingsAsync);
        // TB-197 optional draft type selection: the shared catalog is Fulfillment-owned and read ONLY
        // through its Contracts seam. The type is chosen ONCE on the draft/method root, never per group,
        // rate row, language or schedule, and choosing it activates nothing.
        group.MapGet("/shipping-services/authoring-types", ListTypeChoicesAsync);
        group.MapGet(
            "/shipping-services/authoring-drafts/{draftId:guid}/method-type",
            GetMethodTypeAsync);
        group.MapPut(
            "/shipping-services/authoring-drafts/{draftId:guid}/method-type",
            ReplaceMethodTypeAsync);
        // TB-199 atomic editor document for an EXISTING draft: ONE consistent read snapshot and ONE
        // composite atomic save with a single root revision advance. Full-method creation, UI and any
        // runtime activation remain out of scope.
        group.MapGet(
            "/shipping-services/authoring-drafts/{draftId:guid}/editor-document",
            GetEditorDocumentAsync);
        group.MapPut(
            "/shipping-services/authoring-drafts/{draftId:guid}/editor-document",
            ReplaceEditorDocumentAsync);
    }

    private static async Task<IResult> ListAsync(
        MediatR.ISender sender,
        IShippingAuthoringOwnerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var scope = await authorizer.RequireAdminOwnerAsync(context, ShippingAdminPermissions.View, cancellationToken);
        return api.From(await sender.Send(new GetShippingAuthoringDraftsQuery(scope), cancellationToken));
    }

    private static async Task<IResult> CreateAsync(
        MediatR.ISender sender,
        IShippingAuthoringOwnerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var scope = await authorizer.RequireAdminOwnerAsync(context, ShippingAdminPermissions.Manage, cancellationToken);
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
                $"/v1/admin/shipping-services/authoring-drafts/{result.Value.DraftId}",
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
        var scope = await authorizer.RequireAdminOwnerAsync(context, ShippingAdminPermissions.View, cancellationToken);
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
        var scope = await authorizer.RequireAdminOwnerAsync(context, ShippingAdminPermissions.Manage, cancellationToken);
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
        var scope = await authorizer.RequireAdminOwnerAsync(context, ShippingAdminPermissions.View, cancellationToken);
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
        var scope = await authorizer.RequireAdminOwnerAsync(context, ShippingAdminPermissions.Manage, cancellationToken);
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
        var scope = await authorizer.RequireAdminOwnerAsync(context, ShippingAdminPermissions.View, cancellationToken);
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
        var scope = await authorizer.RequireAdminOwnerAsync(context, ShippingAdminPermissions.Manage, cancellationToken);
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
        var scope = await authorizer.RequireAdminOwnerAsync(context, ShippingAdminPermissions.View, cancellationToken);
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
        var scope = await authorizer.RequireAdminOwnerAsync(context, ShippingAdminPermissions.Manage, cancellationToken);
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
        var scope = await authorizer.RequireAdminOwnerAsync(context, ShippingAdminPermissions.View, cancellationToken);
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
        var scope = await authorizer.RequireAdminOwnerAsync(context, ShippingAdminPermissions.Manage, cancellationToken);
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
        var scope = await authorizer.RequireAdminOwnerAsync(context, ShippingAdminPermissions.View, cancellationToken);
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
        var scope = await authorizer.RequireAdminOwnerAsync(context, ShippingAdminPermissions.View, cancellationToken);
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
        var scope = await authorizer.RequireAdminOwnerAsync(context, ShippingAdminPermissions.Manage, cancellationToken);
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
        var scope = await authorizer.RequireAdminOwnerAsync(context, ShippingAdminPermissions.View, cancellationToken);
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
        var scope = await authorizer.RequireAdminOwnerAsync(context, ShippingAdminPermissions.Manage, cancellationToken);
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
}
