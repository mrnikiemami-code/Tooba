using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.BuildingBlocks.Security;
using Tooba.Fulfillment.Application.Shipping.Models;
using Tooba.Fulfillment.Application.Shipping.Commands;
using Tooba.Fulfillment.Application.Shipping.Queries;
using Tooba.Fulfillment.Contracts.Shipping;

namespace Tooba.Fulfillment.Endpoints.Admin;

/// <summary>
/// گیت مجوز ماژول‌محور کاتالوگ نوع ارسال؛ دو مجوز موجود پلتفرم را <b>مستقل</b> می‌خواهد.
/// </summary>
/// <remarks>
/// درز موجود <see cref="IFulfillmentAdminAuthorizer"/> فقط گیت پنل است و پارامتر مجوز ندارد؛ پس ادعای
/// اجرای <c>fulfillment.view</c>/<c>fulfillment.manage</c> با آن مجاز نیست. این درز تازه همان گیت پنل را
/// با بررسی قابلیت روی <see cref="IAuthorizationService"/> ترکیب می‌کند و زمینهٔ Edition/Tenant را از
/// زمینهٔ trusted همین درخواست می‌گیرد؛ هیچ مجوز تازه‌ای ساخته یا به مسیرهای قدیمی دست زده نمی‌شود.
/// </remarks>
public interface IShippingTypeAdminAuthorizer
{
    /// <summary>مجوز خواندن کاتالوگ (<c>fulfillment.view</c>) پیش از هر dispatch.</summary>
    Task<Guid> RequireViewAsync(HttpContext context, CancellationToken cancellationToken);

    /// <summary>مجوز نوشتن کاتالوگ (<c>fulfillment.manage</c>) پیش از هر dispatch.</summary>
    Task<Guid> RequireManageAsync(HttpContext context, CancellationToken cancellationToken);
}

/// <summary>پیاده‌سازی روی درزهای عمومی پلتفرم، بدون وابستگی به Host.</summary>
public sealed class ShippingTypeAdminAuthorizer(
    IAdminPanelAccess adminAccess,
    IAuthorizationService authz,
    ICurrentEdition edition,
    ICurrentTenant tenant) : IShippingTypeAdminAuthorizer
{
    /// <summary>مجوز خواندن کاتالوگ.</summary>
    public const string View = "fulfillment.view";

    /// <summary>مجوز نوشتن کاتالوگ.</summary>
    public const string Manage = "fulfillment.manage";

    /// <inheritdoc />
    public async Task<Guid> RequireViewAsync(HttpContext context, CancellationToken cancellationToken) =>
        await RequireAsync(context, View, cancellationToken);

    /// <inheritdoc />
    public async Task<Guid> RequireManageAsync(HttpContext context, CancellationToken cancellationToken) =>
        await RequireAsync(context, Manage, cancellationToken);

    private async Task<Guid> RequireAsync(
        HttpContext context,
        string permissionId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        // همان گیت پنل مدیر؛ رد شدن آن پیش از هر بررسی قابلیت و پیش از ISender بالا می‌آید.
        var actor = await adminAccess.RequireAuthorizedAsync(context.Request, cancellationToken);
        var callContext = ShippingTypeInstallationContext.Resolve(edition, tenant);

        var decision = await authz.CanAsync(
            new AuthorizationCheck
            {
                Subject = AuthorizationSubject.ForUser(actor),
                Resource = new AuthorizationResource
                {
                    Type = AuthorizationObjectTypes.Permission,
                    Id = permissionId,
                },
                Permission = AuthorizationRelations.Check,
                CallContext = callContext,
            },
            cancellationToken);

        if (decision.Kind == AuthorizationDecisionKind.Allow)
        {
            return actor;
        }

        // شکست زیرساخت مجوز هرگز ALLOW نیست.
        if (decision.Kind == AuthorizationDecisionKind.Unavailable)
        {
            throw new SemanticException(new SemanticError(FoundationErrorCodes.AdminAuthorizationUnavailable));
        }

        throw new SemanticException(new SemanticError(FoundationErrorCodes.AdminAuthorizationDenied));
    }
}

/// <summary>زمینهٔ مجوز از نصب resolve‌شده؛ هیچ Edition/Tenant از کلاینت پذیرفته نمی‌شود.</summary>
internal static class ShippingTypeInstallationContext
{
    /// <summary>زمینهٔ فراخوانی مجوز را از زمینهٔ trusted همین درخواست می‌سازد.</summary>
    /// <param name="edition">حالت نصب resolve‌شده.</param>
    /// <param name="tenant">Tenant resolve‌شده (در Marketplace باید تهی باشد).</param>
    /// <exception cref="SemanticException">زمینهٔ ناقص/ناسازگار؛ همیشه fail-closed.</exception>
    public static AuthorizationCallContext Resolve(ICurrentEdition edition, ICurrentTenant tenant)
    {
        ArgumentNullException.ThrowIfNull(edition);
        ArgumentNullException.ThrowIfNull(tenant);

        return edition.Current?.Edition switch
        {
            ToobaEdition.SingleStore => SingleStore(tenant),
            ToobaEdition.Marketplace => Marketplace(tenant),
            _ => throw Unavailable(),
        };
    }

    private static AuthorizationCallContext SingleStore(ICurrentTenant tenant)
    {
        var current = tenant.Current;
        var tenantId = current?.TenantId.Value;

        if (current is null
            || current.Status != TenantStatus.Active
            || string.IsNullOrWhiteSpace(tenantId))
        {
            throw Unavailable();
        }

        return new AuthorizationCallContext
        {
            Edition = ToobaEdition.SingleStore,
            TenantId = tenantId.Trim(),
        };
    }

    private static AuthorizationCallContext Marketplace(ICurrentTenant tenant)
    {
        // در Marketplace هیچ Tenant مجاز نیست؛ Tenant باقی‌مانده یا ناسازگار، زمینهٔ نامعتبر است.
        if (tenant.Current is not null)
        {
            throw Unavailable();
        }

        return new AuthorizationCallContext
        {
            Edition = ToobaEdition.Marketplace,
            TenantId = null,
        };
    }

    private static SemanticException Unavailable() =>
        new(new SemanticError(FoundationErrorCodes.AdminAuthorizationUnavailable));
}

/// <summary>
/// چهار مسیر افزودنی مدیریت کاتالوگ نوع ارسال زیر پیشوند واقعی <c>/v1/admin</c>.
/// </summary>
/// <remarks>
/// ترتیب هر مسیر نوشتن ثابت است: مجوز <b>پیش از</b> خواندن بدنه، سپس خواندن/اعتبارسنجی بدنه، سپس ISender.
/// بنابراین یک بدنهٔ بدشکل از actor غیرمجاز هرگز به پردازش بدنه یا Application نمی‌رسد و هیچ پارامتر
/// complex از binding خودکار عبور نمی‌کند.
/// </remarks>
public static class ShippingTypeAdminEndpoints
{
    /// <summary>ثبت مسیرها روی گروه ادمین.</summary>
    /// <param name="admin">گروه <c>/v1/admin</c>.</param>
    public static void Map(RouteGroupBuilder admin)
    {
        ArgumentNullException.ThrowIfNull(admin);
        var group = admin.MapGroup("/shipping-types");
        group.MapGet("/", ListAsync);
        group.MapGet("/{shippingTypeId:guid}", GetAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{shippingTypeId:guid}", UpdateAsync);
    }

    private static async Task<IResult> ListAsync(
        ISender sender,
        IShippingTypeAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireViewAsync(context, cancellationToken);
        return api.From(await sender.Send(new ListShippingTypesQuery(), cancellationToken));
    }

    private static async Task<IResult> GetAsync(
        Guid shippingTypeId,
        ISender sender,
        IShippingTypeAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireViewAsync(context, cancellationToken);
        return api.From(await sender.Send(new GetShippingTypeQuery(shippingTypeId), cancellationToken));
    }

    private static async Task<IResult> CreateAsync(
        ISender sender,
        IShippingTypeAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        // مجوز نخست؛ بدنهٔ بدشکل از actor غیرمجاز هرگز خوانده نمی‌شود.
        await authorizer.RequireManageAsync(context, cancellationToken);

        ShippingTypeWriteModel body;
        try
        {
            body = await ShippingTypeBodyReader.ReadAsync(context.Request, cancellationToken);
        }
        catch (SemanticException exception)
        {
            return api.FromSemanticException(exception);
        }

        var result = await sender.Send(new CreateShippingTypeCommand(body), cancellationToken);
        return result.IsSuccess
            ? api.Created($"/v1/admin/shipping-types/{result.Value.ShippingTypeId}", result)
            : api.From(result);
    }

    private static async Task<IResult> UpdateAsync(
        Guid shippingTypeId,
        ISender sender,
        IShippingTypeAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireManageAsync(context, cancellationToken);

        ShippingTypeWriteModel body;
        try
        {
            body = await ShippingTypeBodyReader.ReadAsync(context.Request, cancellationToken);
        }
        catch (SemanticException exception)
        {
            return api.FromSemanticException(exception);
        }

        return api.From(await sender.Send(new UpdateShippingTypeCommand(shippingTypeId, body), cancellationToken));
    }
}
