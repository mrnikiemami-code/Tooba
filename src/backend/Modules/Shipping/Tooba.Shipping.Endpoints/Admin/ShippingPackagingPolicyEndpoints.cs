using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Text.Json;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Shipping.Application.Packaging.Commands;
using Tooba.Shipping.Application.Validation;
using Tooba.Shipping.Contracts.Packaging;

namespace Tooba.Shipping.Endpoints.Admin;

/// <summary>
/// مارس‌کنندهٔ سیاست هزینهٔ بسته‌بندی Shipping. همان مرز مجوز و همان شکل بدنهٔ نرخ/پوشش را
/// بازاستفاده می‌کند؛ هیچ مسیر بی‌مجوز یا مجوز تازه‌ای ساخته نمی‌شود.
/// </summary>
public static class ShippingPackagingPolicyEndpoints
{
    /// <summary>
    /// کلید فیلد بدنه در پوشش اعتبارسنجی. یک بدنهٔ خوانانشدنی به هیچ خصیصهٔ خاصی نسبت داده
    /// نمی‌شود، پس با همان کلید عمومی <c>request</c> گزارش می‌شود.
    /// </summary>
    private const string BodyField = "request";

    /// <summary>مسیرهای سیاست بسته‌بندی را زیر گروه ادمین می‌نگارد.</summary>
    /// <param name="group">گروه مسیر ادمین.</param>
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/shipping-services/{serviceCode}/packaging-policies", ListAsync);
        group.MapPost("/shipping-services/{serviceCode}/packaging-policies", CreateAsync);
        group.MapPut(
            "/shipping-services/{serviceCode}/packaging-policies/{packagingPolicyId:guid}",
            UpdateAsync);
        group.MapPatch(
            "/shipping-services/{serviceCode}/packaging-policies/{packagingPolicyId:guid}/active",
            SetActiveAsync);
    }

    private static async Task<IResult> ListAsync(
        string serviceCode,
        MediatR.ISender sender,
        IShippingAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, ShippingAdminPermissions.View, cancellationToken);
        return api.From(await sender.Send(new GetShippingPackagingPoliciesQuery(serviceCode), cancellationToken));
    }

    private static async Task<IResult> CreateAsync(
        string serviceCode,
        MediatR.ISender sender,
        IShippingAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, ShippingAdminPermissions.Manage, cancellationToken);
        var (body, failure) = await ReadBodyAsync<ShippingPackagingPolicyWriteRequest>(
            context,
            api,
            cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        return api.From(await sender.Send(
            new CreateShippingPackagingPolicyCommand(serviceCode, body),
            cancellationToken));
    }

    private static async Task<IResult> UpdateAsync(
        string serviceCode,
        Guid packagingPolicyId,
        MediatR.ISender sender,
        IShippingAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, ShippingAdminPermissions.Manage, cancellationToken);
        var (body, failure) = await ReadBodyAsync<ShippingPackagingPolicyWriteRequest>(
            context,
            api,
            cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        return api.From(await sender.Send(
            new UpdateShippingPackagingPolicyCommand(packagingPolicyId, serviceCode, body),
            cancellationToken));
    }

    private static async Task<IResult> SetActiveAsync(
        string serviceCode,
        Guid packagingPolicyId,
        MediatR.ISender sender,
        IShippingAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, ShippingAdminPermissions.Manage, cancellationToken);
        var (body, failure) = await ReadBodyAsync<ShippingPackagingPolicyActiveRequest>(
            context,
            api,
            cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        return api.From(await sender.Send(
            new SetShippingPackagingPolicyActiveCommand(packagingPolicyId, serviceCode, body.IsActive),
            cancellationToken));
    }

    /// <summary>
    /// بدنه را می‌خواند و اگر بدنهٔ ارسالی خوانا نباشد (JSON نامعتبر، contentType غیر-JSON،
    /// شناسهٔ نامعتبر/تهی یا بدنهٔ کاملاً غایب) شکست اعتبارسنجی اعلام‌شده می‌سازد.
    /// </summary>
    private static async Task<(T Body, IResult? Failure)> ReadBodyAsync<T>(
        HttpContext context,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
        where T : class
    {
        var body = await TryReadBodyAsync<T>(context, cancellationToken);
        return body is null
            ? (default!, api.FromException(InvalidBodyException()))
            : (body, null);
    }

    private static async Task<T?> TryReadBodyAsync<T>(HttpContext context, CancellationToken cancellationToken)
        where T : class
    {
        if (!context.Request.HasJsonContentType())
        {
            return null;
        }

        try
        {
            return await context.Request.ReadFromJsonAsync<T>(cancellationToken);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (BadHttpRequestException)
        {
            return null;
        }
    }

    private static ValidationException InvalidBodyException() =>
        new([new ValidationFailure(BodyField, ShippingValidationCodes.BodyInvalid)
        {
            ErrorCode = ShippingValidationCodes.BodyInvalid,
        }]);
}
