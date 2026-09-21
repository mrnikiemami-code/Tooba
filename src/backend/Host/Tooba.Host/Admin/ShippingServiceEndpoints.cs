using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Ports;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Localization.Contracts;

namespace Tooba.Host.Admin;

#pragma warning disable CS1591

/// <summary>ترجمهٔ سرویس ارسال والد (wire-only request DTO).</summary>
public sealed record ShippingServiceTranslationWrite(Guid LanguageId, string Name, string? Description);

/// <summary>ترجمهٔ نوع سرویس فرزند.</summary>
public sealed record ShippingServiceOptionTranslationWrite(Guid LanguageId, string Name);

/// <summary>نوشتن گزینهٔ سطح ۲.</summary>
public sealed record ShippingServiceOptionWrite(
    Guid? ShippingServiceOptionId,
    string Code,
    bool IsActive,
    int SortOrder,
    IReadOnlyList<ShippingServiceOptionTranslationWrite> Translations);

/// <summary>ایجاد/ویرایش سرویس ارسال دو‌سطحی (wire-only).</summary>
public sealed record ShippingServiceWriteRequest(
    string Code,
    string ProviderKind,
    string IconKey,
    string ColorKey,
    bool IsActive,
    int SortOrder,
    IReadOnlyList<ShippingServiceTranslationWrite> Translations,
    IReadOnlyList<ShippingServiceOptionWrite> Options);

/// <summary>
/// CRUD سرویس ارسال — HOST_THIN_TRANSPORT: auth + ISender + ApiResponseFactory.
/// </summary>
public static class ShippingServiceEndpoints
{
    /// <summary>مسیرهای Admin سرویس ارسال را ثبت می‌کند.</summary>
    public static void MapShippingServiceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/admin/shipping-services");
        group.MapGet("/", ListAsync);
        group.MapGet("/{serviceId:guid}", GetAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{serviceId:guid}", UpdateAsync);
        group.MapPost("/{serviceId:guid}/deactivate", DeactivateAsync);
        group.MapPost("/ensure-seed", EnsureSeedHttpAsync);
    }

    private static async Task<IResult> ListAsync(
        ISender sender,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        string? language,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            return api.From(await sender.Send(new ListShippingServicesQuery(language), cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return api.FromFailure(new SemanticError(ex.ErrorCode ?? "platform.http"));
        }
    }

    private static async Task<IResult> GetAsync(
        Guid serviceId,
        ISender sender,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            return api.From(await sender.Send(new GetShippingServiceQuery(serviceId), cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return api.FromFailure(new SemanticError(ex.ErrorCode ?? "platform.http"));
        }
    }

    private static async Task<IResult> CreateAsync(
        ShippingServiceWriteRequest body,
        ISender sender,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            return api.From(await sender.Send(new CreateShippingServiceCommand(ToModel(body)), cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return api.FromFailure(new SemanticError(ex.ErrorCode ?? "platform.http"));
        }
    }

    private static async Task<IResult> UpdateAsync(
        Guid serviceId,
        ShippingServiceWriteRequest body,
        ISender sender,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            return api.From(await sender.Send(new UpdateShippingServiceCommand(serviceId, ToModel(body)), cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return api.FromFailure(new SemanticError(ex.ErrorCode ?? "platform.http"));
        }
    }

    private static async Task<IResult> DeactivateAsync(
        Guid serviceId,
        ISender sender,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            var result = await sender.Send(new DeactivateShippingServiceCommand(serviceId), cancellationToken);
            return result.IsFailure ? api.From(result) : Results.Json(new { ok = true });
        }
        catch (PlatformHttpException ex)
        {
            return api.FromFailure(new SemanticError(ex.ErrorCode ?? "platform.http"));
        }
    }

    private static async Task<IResult> EnsureSeedHttpAsync(
        ISender sender,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            var result = await sender.Send(new EnsureShippingCatalogSeedCommand(), cancellationToken);
            return result.IsFailure ? api.From(result) : Results.Json(new { ok = true });
        }
        catch (PlatformHttpException ex)
        {
            return api.FromFailure(new SemanticError(ex.ErrorCode ?? "platform.http"));
        }
    }

    /// <summary>پر کردن درخت روش ارسال برای مودال ایجاد مرسوله.</summary>
    public static async Task<IReadOnlyList<object>> ListEnabledMethodsTreeAsync(
        IShippingCatalogReader catalog,
        ILanguageLookup languages,
        ShippingMethodsOptions options,
        ISender sender,
        string? language,
        CancellationToken cancellationToken)
    {
        await sender.Send(new EnsureShippingCatalogSeedCommand(), cancellationToken);
        var langId = await ShippingServiceSemantic.ResolveLanguageIdAsync(languages, language, cancellationToken);
        var enabledCodes = ShippingMethodRegistry.Enabled(options)
            .Select(x => x.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var catalogRows = await catalog.ListAsync(cancellationToken);
        var services = catalogRows
            .Where(x => x.IsActive && enabledCodes.Contains(x.Code))
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Code)
            .ToList();
        if (services.Count == 0)
        {
            return ShippingMethodRegistry.Enabled(options)
                .Select(x => (object)new
                {
                    code = x.Code,
                    labelFa = x.LabelFa,
                    name = x.LabelFa,
                    providerKind = x.ProviderKind,
                    iconKey = x.Code,
                    colorKey = DefaultColor(x.Code),
                    options = DefaultOptions(x.Code),
                })
                .ToList();
        }

        return services.Select(s =>
        {
            var name = s.Translations.FirstOrDefault(r => r.LanguageId == langId)?.Name
                ?? s.Translations.FirstOrDefault()?.Name
                ?? s.Code;
            var mappedOptions = s.Options.Where(o => o.IsActive).Select(o =>
            {
                var optionName = o.Translations.FirstOrDefault(r => r.LanguageId == langId)?.Name
                    ?? o.Translations.FirstOrDefault()?.Name
                    ?? o.Code;
                return new { code = o.Code, labelFa = optionName, name = optionName };
            }).ToList();
            return (object)new
            {
                code = s.Code,
                labelFa = name,
                name,
                providerKind = s.ProviderKind,
                iconKey = s.IconKey,
                colorKey = s.ColorKey,
                options = mappedOptions,
            };
        }).ToList();
    }

    private static ShippingServiceWriteModel ToModel(ShippingServiceWriteRequest body) =>
        new(
            body.Code,
            body.ProviderKind,
            body.IconKey,
            body.ColorKey,
            body.IsActive,
            body.SortOrder,
            body.Translations.Select(t => new ShippingServiceTranslationWriteModel(t.LanguageId, t.Name, t.Description)).ToList(),
            body.Options.Select(o => new ShippingServiceOptionWriteModel(
                o.ShippingServiceOptionId,
                o.Code,
                o.IsActive,
                o.SortOrder,
                o.Translations.Select(t => new ShippingServiceOptionTranslationWriteModel(t.LanguageId, t.Name)).ToList())).ToList());

    private static string DefaultColor(string code) => code switch
    {
        "post" => "blue",
        "tipax" => "amber",
        "snapp_courier" => "emerald",
        "store_courier" => "violet",
        "in_person" => "rose",
        _ => "blue",
    };

    private static object[] DefaultOptions(string code) =>
        code is "post" or "tipax"
            ?
            [
                new { code = "express", labelFa = "پیشتاز", name = "پیشتاز" },
                new { code = "standard", labelFa = "معمولی", name = "معمولی" },
            ]
            : [];
}

/// <summary>Adapter Host برای اعتبار LanguageId سرویس ارسال و seed — Localization.Contracts only.</summary>
public sealed class HostShippingServiceLanguageGate : IShippingServiceLanguageGate
{
    private readonly ILanguageLookup _languages;

    /// <summary>Gate را می‌سازد.</summary>
    public HostShippingServiceLanguageGate(ILanguageLookup languages) => _languages = languages;

    /// <inheritdoc />
    public async Task EnsureKnownAsync(IReadOnlyList<Guid> languageIds, CancellationToken cancellationToken)
    {
        var known = (await _languages.ListAsync(cancellationToken)).Select(x => x.LanguageId).ToHashSet();
        if (languageIds.Any(id => !known.Contains(id)))
        {
            throw new InvalidOperationException("shipping_service.language_invalid");
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShippingServiceSeedLanguage>> ListForSeedAsync(CancellationToken cancellationToken)
    {
        var langs = await _languages.ListAsync(cancellationToken);
        return langs.Select(x => new ShippingServiceSeedLanguage(x.LanguageId, x.Code, x.Culture, x.IsDefault)).ToList();
    }
}
