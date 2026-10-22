using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.BuildingBlocks.Security;
using Tooba.Fulfillment.Application.ShippingTypes;
using Tooba.Fulfillment.Contracts.Errors;
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

/// <summary>بدنهٔ نوشتن ایجاد/ویرایش نوع ارسال در مرز HTTP.</summary>
/// <param name="Code">کد canonical؛ در ویرایش باید با کد ذخیره‌شده یکسان باشد (تغییر هویت ممنوع).</param>
/// <param name="SortOrder">ترتیب صریح.</param>
/// <param name="IsActive">وضعیت صریح؛ در ایجاد نادیده گرفته می‌شود چون تعریف تازه همیشه غیرفعال است.</param>
/// <param name="ExpectedRevision">بازبینی خوانده‌شده؛ فقط در ویرایش معنا دارد.</param>
/// <param name="Translations">ترجمه‌ها (patch در ویرایش).</param>
public sealed record ShippingTypeWriteRequest(
    string? Code,
    int? SortOrder,
    bool? IsActive,
    long? ExpectedRevision,
    IReadOnlyList<ShippingTypeTranslationWriteRequest>? Translations);

/// <summary>یک ترجمه در بدنه.</summary>
/// <param name="LanguageId">شناسهٔ زبان محتوا.</param>
/// <param name="Name">نام.</param>
/// <param name="Description">توضیح یا <c>null</c> برای پاک‌کردن.</param>
public sealed record ShippingTypeTranslationWriteRequest(Guid LanguageId, string? Name, string? Description);

/// <summary>
/// چهار مسیر افزودنی مدیریت کاتالوگ نوع ارسال زیر پیشوند واقعی <c>/v1/admin</c>.
/// </summary>
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
        Guid? languageId,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireViewAsync(context, cancellationToken);
        return api.From(await sender.Send(new ListShippingTypesQuery(languageId), cancellationToken));
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
        ShippingTypeWriteRequest body,
        ISender sender,
        IShippingTypeAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireManageAsync(context, cancellationToken);

        // شناسه/فعال‌بودن/بازبینی از سرور است؛ فیلدهای کلاینت در create خوانده نمی‌شوند.
        var command = new CreateShippingTypeCommand(new ShippingTypeCreateModel(
            body.Code ?? string.Empty,
            body.SortOrder ?? -1,
            ToInputs(body.Translations)));

        var result = await sender.Send(command, cancellationToken);
        return api.Created(
            $"/v1/admin/shipping-types/{(result.IsSuccess ? result.Value.ShippingTypeId : Guid.Empty)}",
            result);
    }

    private static async Task<IResult> UpdateAsync(
        Guid shippingTypeId,
        ShippingTypeWriteRequest body,
        ISender sender,
        IShippingTypeAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireManageAsync(context, cancellationToken);

        // حضور صریح فیلدهای اجباری: مقدار حذف‌شده یا null نباید بی‌صدا به false/0/فهرست خالی تبدیل شود.
        if (body.IsActive is null || body.SortOrder is null
            || body.ExpectedRevision is null || body.Translations is null)
        {
            throw new SemanticException(new SemanticError(FulfillmentErrorCodes.ShippingTypeTransportFieldMissing));
        }

        var result = await sender.Send(
            new UpdateShippingTypeCommand(
                shippingTypeId,
                new ShippingTypeUpdateModel(
                    body.ExpectedRevision.Value,
                    body.Code,
                    body.SortOrder.Value,
                    body.IsActive.Value,
                    ToInputs(body.Translations))),
            cancellationToken);
        return api.From(result);
    }

    private static IReadOnlyList<ShippingTypeTranslationInput> ToInputs(
        IReadOnlyList<ShippingTypeTranslationWriteRequest>? translations) =>
        translations is null
            ? []
            : translations
                .Where(x => x is not null)
                .Select(x => new ShippingTypeTranslationInput(x.LanguageId, x.Name ?? string.Empty, x.Description))
                .ToList();
}
