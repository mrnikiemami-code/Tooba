using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Fulfillment.Application.Shipping.Commands;
using Tooba.Fulfillment.Application.Shipping.Queries;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Fulfillment.Endpoints.Admin;

namespace Tooba.Fulfillment.Endpoints.Shipping;

/// <summary>Wire-only translation DTO.</summary>
public sealed record ShippingServiceTranslationWrite(Guid LanguageId, string Name, string? Description);
public sealed record ShippingServiceOptionTranslationWrite(Guid LanguageId, string Name);

/// <summary>TB-094: تنها بدنهٔ نوشتن نام یک روش یک‌سطحی — فقط نام؛ هیچ هویت یا سطح دوم.</summary>
public sealed record OneLevelShippingMethodTranslationWriteRequest(string? Name);
public sealed record ShippingServiceOptionWrite(
    Guid? ShippingServiceOptionId, string Code, bool IsActive, int SortOrder,
    IReadOnlyList<ShippingServiceOptionTranslationWrite> Translations);
public sealed record ShippingServiceWriteRequest(
    string Code, string ProviderKind, string IconKey, string ColorKey, bool IsActive, int SortOrder,
    IReadOnlyList<ShippingServiceTranslationWrite> Translations,
    IReadOnlyList<ShippingServiceOptionWrite> Options);

/// <summary>Admin shipping-services CRUD — module-owned.</summary>
public static class ShippingServiceEndpoints
{
    public static void Map(RouteGroupBuilder admin)
    {
        ArgumentNullException.ThrowIfNull(admin);
        var group = admin.MapGroup("/shipping-services");
        group.MapGet("/", ListAsync);
        group.MapGet("/{serviceId:guid}", GetAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{serviceId:guid}", UpdateAsync);
        group.MapPost("/{serviceId:guid}/deactivate", DeactivateAsync);
        group.MapPost("/ensure-seed", EnsureSeedHttpAsync);

        // TB-SHIPPING-ONE-LEVEL-TABS-085: مسیر فقط-خواندنی ادمین برای دروازه/کاتالوگ یک‌سطحی.
        // مسیر مستقل و افزودنی است تا رفتار مسیرهای قدیمی سرویس/گزینه بی‌تغییر بماند.
        group.MapGet("/shipping-methods/one-level", ListOneLevelMethodsAsync);
        // TB-SHIPPING-ONE-LEVEL-TABS-086: حل پیوند قدیمی برای همان روش انتخاب‌شده. ورودی فقط
        // کد روش است؛ هیچ serviceCode/optionCode خام از کلاینت پذیرفته نمی‌شود.
        group.MapGet("/shipping-methods/one-level/{methodCode}/binding", ResolveOneLevelBindingAsync);
        // TB-SHIPPING-ONE-LEVEL-TABS-094: تنها نوشتن مجاز این مسیر — نام نمایشی مستقل هر روش در یک
        // زبان. کد روش تغییرناپذیر است و هیچ سطح دوم/گروه‌بندی ارائه‌دهنده ساخته نمی‌شود.
        group.MapPut(
            "/shipping-methods/one-level/{methodCode}/translations/{languageId:guid}",
            UpdateOneLevelMethodTranslationAsync);
    }

    private static async Task<IResult> ListAsync(
        ISender sender, IFulfillmentAdminAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, string? language, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(new ListShippingServicesQuery(language), cancellationToken));
    }

    private static async Task<IResult> GetAsync(
        Guid serviceId, ISender sender, IFulfillmentAdminAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(new GetShippingServiceQuery(serviceId), cancellationToken));
    }

    private static async Task<IResult> CreateAsync(
        ShippingServiceWriteRequest body, ISender sender, IFulfillmentAdminAuthorizer authorizer,
        ApiResponseFactory api, HttpContext context, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(new CreateShippingServiceCommand(ToModel(body)), cancellationToken));
    }

    private static async Task<IResult> UpdateAsync(
        Guid serviceId, ShippingServiceWriteRequest body, ISender sender, IFulfillmentAdminAuthorizer authorizer,
        ApiResponseFactory api, HttpContext context, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(new UpdateShippingServiceCommand(serviceId, ToModel(body)), cancellationToken));
    }

    private static async Task<IResult> DeactivateAsync(
        Guid serviceId, ISender sender, IFulfillmentAdminAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        var result = await sender.Send(new DeactivateShippingServiceCommand(serviceId), cancellationToken);
        return api.From(result);
    }

    private static async Task<IResult> EnsureSeedHttpAsync(
        ISender sender, IFulfillmentAdminAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        var result = await sender.Send(new EnsureShippingCatalogSeedCommand(), cancellationToken);
        return api.From(result);
    }

    /// <summary>
    /// وضعیت مؤثر دروازهٔ کاتالوگ یک‌سطحی و فهرست فقط-خواندنی روش‌ها برای پنل ادمین.
    /// مجوز ادمین لازم است؛ دروازه خاموش ⇒ <c>Disabled/ready=false</c> بدون خواندن دیتابیس؛
    /// جدول مهاجرت‌نشده ⇒ <c>NOT_READY</c> کنترل‌شده، نه خطای ۵۰۰.
    /// </summary>
    private static async Task<IResult> ListOneLevelMethodsAsync(
        ISender sender, IFulfillmentAdminAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, string? language, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(new ListOneLevelShippingMethodsQuery(language), cancellationToken));
    }

    /// <summary>
    /// حل پیوند قدیمی یک روش یک‌سطحی از هویت canonical خودش. مجوز ادمین لازم است؛ دروازه خاموش،
    /// کاتالوگ مهاجرت‌نشده یا کد ناشناخته ⇒ <c>resolved=false</c> کنترل‌شده، نه خطای ۵۰۰.
    /// </summary>
    private static async Task<IResult> ResolveOneLevelBindingAsync(
        string methodCode, ISender sender, IFulfillmentAdminAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(
            new ResolveOneLevelShippingMethodBindingQuery(methodCode), cancellationToken));
    }

    /// <summary>
    /// TB-SHIPPING-ONE-LEVEL-TABS-094: نام نمایشی مستقل یک روش یک‌سطحی را در یک زبان می‌نویسد.
    /// مجوز ادمین لازم است؛ دروازهٔ خاموش، کاتالوگ مهاجرت‌نشده، روش ناشناخته، کد نامعتبر و نام
    /// خالی/بلندتر از حد همه fail-closed رد می‌شوند و هیچ نوشتنی رخ نمی‌دهد. کد روش در URL تنها
    /// هویت است و بدنه هرگز serviceCode/optionCode نمی‌پذیرد.
    /// </summary>
    private static async Task<IResult> UpdateOneLevelMethodTranslationAsync(
        string methodCode, Guid languageId, OneLevelShippingMethodTranslationWriteRequest body,
        ISender sender, IFulfillmentAdminAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(
            new UpdateOneLevelShippingMethodTranslationCommand(methodCode, languageId, body?.Name),
            cancellationToken));
    }

    private static ShippingServiceWriteModel ToModel(ShippingServiceWriteRequest body) =>
        new(
            body.Code, body.ProviderKind, body.IconKey, body.ColorKey, body.IsActive, body.SortOrder,
            body.Translations.Select(t => new ShippingServiceTranslationWriteModel(t.LanguageId, t.Name, t.Description)).ToList(),
            body.Options.Select(o => new ShippingServiceOptionWriteModel(
                o.ShippingServiceOptionId, o.Code, o.IsActive, o.SortOrder,
                o.Translations.Select(t => new ShippingServiceOptionTranslationWriteModel(t.LanguageId, t.Name)).ToList())).ToList());
}
