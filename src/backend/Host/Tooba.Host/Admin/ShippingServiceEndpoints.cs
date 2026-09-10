using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application;
using Tooba.Fulfillment.Domain;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Localization.Application;

namespace Tooba.Host.Admin;

#pragma warning disable CS1591

/// <summary>ترجمهٔ سرویس ارسال والد.</summary>
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

/// <summary>ایجاد/ویرایش سرویس ارسال دو‌سطحی.</summary>
public sealed record ShippingServiceWriteRequest(
    string Code,
    string ProviderKind,
    string IconKey,
    string ColorKey,
    bool IsActive,
    int SortOrder,
    IReadOnlyList<ShippingServiceTranslationWrite> Translations,
    IReadOnlyList<ShippingServiceOptionWrite> Options);

/// <summary>ردیف لیست سرویس ارسال.</summary>
public sealed record ShippingServiceListItem(
    Guid ShippingServiceId,
    string Code,
    string ProviderKind,
    string IconKey,
    string ColorKey,
    string Name,
    bool IsActive,
    int SortOrder,
    int OptionCount,
    int ActiveOptionCount);

/// <summary>جزئیات گزینهٔ سطح ۲.</summary>
public sealed record ShippingServiceOptionDetail(
    Guid ShippingServiceOptionId,
    string Code,
    bool IsActive,
    int SortOrder,
    IReadOnlyList<ShippingServiceOptionTranslationWrite> Translations);

/// <summary>جزئیات سرویس ارسال.</summary>
public sealed record ShippingServiceDetail(
    Guid ShippingServiceId,
    string Code,
    string ProviderKind,
    string IconKey,
    string ColorKey,
    bool IsActive,
    int SortOrder,
    IReadOnlyList<ShippingServiceTranslationWrite> Translations,
    IReadOnlyList<ShippingServiceOptionDetail> Options);

/// <summary>CRUD سرویس ارسال دو‌سطحی + seed اولیه.</summary>
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
        FulfillmentDbContext db,
        ILanguageDirectory languages,
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
            await EnsureSeedAsync(db, languages, cancellationToken);
            var langId = await ResolveLanguageIdAsync(languages, language, cancellationToken);
            var services = await db.ShippingServices.AsNoTracking()
                .OrderBy(x => x.SortOrder).ThenBy(x => x.Code)
                .ToListAsync(cancellationToken);
            var serviceIds = services.Select(x => x.ShippingServiceId).ToArray();
            var translations = await db.ShippingServiceTranslations.AsNoTracking()
                .Where(x => serviceIds.Contains(x.ShippingServiceId))
                .ToListAsync(cancellationToken);
            var options = await db.ShippingServiceOptions.AsNoTracking()
                .Where(x => serviceIds.Contains(x.ShippingServiceId))
                .ToListAsync(cancellationToken);
            var byService = translations.GroupBy(x => x.ShippingServiceId)
                .ToDictionary(g => g.Key, g => g.ToList());
            var optionsByService = options.GroupBy(x => x.ShippingServiceId)
                .ToDictionary(g => g.Key, g => g.ToList());
            var items = services.Select(s =>
            {
                byService.TryGetValue(s.ShippingServiceId, out var rows);
                var picked = rows?.FirstOrDefault(r => r.LanguageId == langId) ?? rows?.FirstOrDefault();
                optionsByService.TryGetValue(s.ShippingServiceId, out var opts);
                opts ??= [];
                return new ShippingServiceListItem(
                    s.ShippingServiceId,
                    s.Code,
                    s.ProviderKind,
                    s.IconKey,
                    s.ColorKey,
                    picked?.Name ?? s.Code,
                    s.IsActive,
                    s.SortOrder,
                    opts.Count,
                    opts.Count(o => o.IsActive));
            }).ToList();
            return Results.Json(items);
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> GetAsync(
        Guid serviceId,
        FulfillmentDbContext db,
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
            var detail = await LoadDetailAsync(db, serviceId, cancellationToken);
            return detail is null
                ? Results.Json(new { title = "not found", errorCode = "shipping_service.not_found" }, statusCode: 404)
                : Results.Json(detail);
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> CreateAsync(
        ShippingServiceWriteRequest body,
        FulfillmentDbContext db,
        ILanguageDirectory languages,
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
            await ValidateLanguagesAsync(languages, body, cancellationToken);
            var code = body.Code.Trim().ToLowerInvariant();
            if (await db.ShippingServices.AnyAsync(x => x.Code == code, cancellationToken))
            {
                return Results.Json(new { title = "duplicate", errorCode = "shipping_service.code_duplicate" }, statusCode: 409);
            }

            var now = DateTimeOffset.UtcNow;
            var id = Guid.NewGuid();
            var entity = ShippingService.Create(
                id,
                body.Code,
                body.ProviderKind,
                body.IconKey,
                body.ColorKey,
                body.SortOrder,
                body.IsActive,
                now);
            db.ShippingServices.Add(entity);
            AddTranslations(db, id, body.Translations);
            AddOptions(db, id, body.Options, now);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Json(await LoadDetailAsync(db, id, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(new { title = ex.Message, errorCode = ex.Message }, statusCode: 400);
        }
    }

    private static async Task<IResult> UpdateAsync(
        Guid serviceId,
        ShippingServiceWriteRequest body,
        FulfillmentDbContext db,
        ILanguageDirectory languages,
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
            await ValidateLanguagesAsync(languages, body, cancellationToken);
            var entity = await db.ShippingServices.FirstOrDefaultAsync(x => x.ShippingServiceId == serviceId, cancellationToken);
            if (entity is null)
            {
                return Results.Json(new { title = "not found", errorCode = "shipping_service.not_found" }, statusCode: 404);
            }

            var code = body.Code.Trim().ToLowerInvariant();
            if (await db.ShippingServices.AnyAsync(x => x.Code == code && x.ShippingServiceId != serviceId, cancellationToken))
            {
                return Results.Json(new { title = "duplicate", errorCode = "shipping_service.code_duplicate" }, statusCode: 409);
            }

            var now = DateTimeOffset.UtcNow;
            entity.Update(body.Code, body.ProviderKind, body.IconKey, body.ColorKey, body.SortOrder, now);
            entity.SetActive(body.IsActive, now);

            var existingTranslations = await db.ShippingServiceTranslations
                .Where(x => x.ShippingServiceId == serviceId).ToListAsync(cancellationToken);
            db.ShippingServiceTranslations.RemoveRange(existingTranslations);
            AddTranslations(db, serviceId, body.Translations);

            var existingOptions = await db.ShippingServiceOptions
                .Where(x => x.ShippingServiceId == serviceId).ToListAsync(cancellationToken);
            var optionIds = existingOptions.Select(x => x.ShippingServiceOptionId).ToArray();
            var existingOptionTranslations = await db.ShippingServiceOptionTranslations
                .Where(x => optionIds.Contains(x.ShippingServiceOptionId)).ToListAsync(cancellationToken);
            db.ShippingServiceOptionTranslations.RemoveRange(existingOptionTranslations);
            db.ShippingServiceOptions.RemoveRange(existingOptions);
            AddOptions(db, serviceId, body.Options, now);

            await db.SaveChangesAsync(cancellationToken);
            return Results.Json(await LoadDetailAsync(db, serviceId, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(new { title = ex.Message, errorCode = ex.Message }, statusCode: 400);
        }
    }

    private static async Task<IResult> DeactivateAsync(
        Guid serviceId,
        FulfillmentDbContext db,
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
            var entity = await db.ShippingServices.FirstOrDefaultAsync(x => x.ShippingServiceId == serviceId, cancellationToken);
            if (entity is null)
            {
                return Results.Json(new { title = "not found", errorCode = "shipping_service.not_found" }, statusCode: 404);
            }

            entity.SetActive(false, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Json(new { ok = true });
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> EnsureSeedHttpAsync(
        FulfillmentDbContext db,
        ILanguageDirectory languages,
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
            await EnsureSeedAsync(db, languages, cancellationToken);
            return Results.Json(new { ok = true });
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    /// <summary>پر کردن درخت روش ارسال برای مودال ایجاد مرسوله.</summary>
    public static async Task<IReadOnlyList<object>> ListEnabledMethodsTreeAsync(
        FulfillmentDbContext db,
        ILanguageDirectory languages,
        ShippingMethodsOptions options,
        string? language,
        CancellationToken cancellationToken)
    {
        await EnsureSeedAsync(db, languages, cancellationToken);
        var langId = await ResolveLanguageIdAsync(languages, language, cancellationToken);
        var enabledCodes = ShippingMethodRegistry.Enabled(options)
            .Select(x => x.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var services = await db.ShippingServices.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);
        services = services.Where(x => enabledCodes.Contains(x.Code)).ToList();
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

        var serviceIds = services.Select(x => x.ShippingServiceId).ToArray();
        var translations = await db.ShippingServiceTranslations.AsNoTracking()
            .Where(x => serviceIds.Contains(x.ShippingServiceId)).ToListAsync(cancellationToken);
        var opts = await db.ShippingServiceOptions.AsNoTracking()
            .Where(x => serviceIds.Contains(x.ShippingServiceId) && x.IsActive)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);
        var optionIds = opts.Select(x => x.ShippingServiceOptionId).ToArray();
        var optionTranslations = await db.ShippingServiceOptionTranslations.AsNoTracking()
            .Where(x => optionIds.Contains(x.ShippingServiceOptionId)).ToListAsync(cancellationToken);

        var tByService = translations.GroupBy(x => x.ShippingServiceId).ToDictionary(g => g.Key, g => g.ToList());
        var oByService = opts.GroupBy(x => x.ShippingServiceId).ToDictionary(g => g.Key, g => g.ToList());
        var otByOption = optionTranslations.GroupBy(x => x.ShippingServiceOptionId).ToDictionary(g => g.Key, g => g.ToList());

        return services.Select(s =>
        {
            tByService.TryGetValue(s.ShippingServiceId, out var rows);
            var name = rows?.FirstOrDefault(r => r.LanguageId == langId)?.Name
                ?? rows?.FirstOrDefault()?.Name
                ?? s.Code;
            oByService.TryGetValue(s.ShippingServiceId, out var children);
            children ??= [];
            var mappedOptions = children.Select(o =>
            {
                otByOption.TryGetValue(o.ShippingServiceOptionId, out var otRows);
                var optionName = otRows?.FirstOrDefault(r => r.LanguageId == langId)?.Name
                    ?? otRows?.FirstOrDefault()?.Name
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

    internal static async Task EnsureSeedAsync(
        FulfillmentDbContext db,
        ILanguageDirectory languages,
        CancellationToken cancellationToken)
    {
        if (await db.ShippingServices.AnyAsync(cancellationToken))
        {
            return;
        }

        var langs = await languages.ListAsync(cancellationToken);
        if (langs.Count == 0)
        {
            return;
        }

        var fa = langs.FirstOrDefault(x => x.Culture.StartsWith("fa", StringComparison.OrdinalIgnoreCase)
                || x.Code.StartsWith("fa", StringComparison.OrdinalIgnoreCase))
            ?? langs.First(x => x.IsDefault)
            ?? langs[0];
        var en = langs.FirstOrDefault(x => x.Culture.StartsWith("en", StringComparison.OrdinalIgnoreCase)
                || x.Code.StartsWith("en", StringComparison.OrdinalIgnoreCase));

        var now = DateTimeOffset.UtcNow;
        void AddService(
            string code,
            string providerKind,
            string iconKey,
            string colorKey,
            int sort,
            string faName,
            string enName,
            (string code, string fa, string en)[] options)
        {
            var id = Guid.NewGuid();
            db.ShippingServices.Add(ShippingService.Create(id, code, providerKind, iconKey, colorKey, sort, true, now));
            db.ShippingServiceTranslations.Add(
                ShippingServiceTranslation.Create(Guid.NewGuid(), id, fa.LanguageId, faName, null));
            if (en is not null)
            {
                db.ShippingServiceTranslations.Add(
                    ShippingServiceTranslation.Create(Guid.NewGuid(), id, en.LanguageId, enName, null));
            }

            var order = 10;
            foreach (var option in options)
            {
                var oid = Guid.NewGuid();
                db.ShippingServiceOptions.Add(ShippingServiceOption.Create(oid, id, option.code, order, true, now));
                db.ShippingServiceOptionTranslations.Add(
                    ShippingServiceOptionTranslation.Create(Guid.NewGuid(), oid, fa.LanguageId, option.fa));
                if (en is not null)
                {
                    db.ShippingServiceOptionTranslations.Add(
                        ShippingServiceOptionTranslation.Create(Guid.NewGuid(), oid, en.LanguageId, option.en));
                }

                order += 10;
            }
        }

        AddService("post", "post", "post", "blue", 10, "پست", "Post",
        [
            ("express", "پیشتاز", "Express"),
            ("standard", "معمولی", "Standard"),
        ]);
        AddService("tipax", "tipax", "tipax", "amber", 20, "تیپاکس", "Tipax",
        [
            ("express", "پیشتاز", "Express"),
            ("standard", "معمولی", "Standard"),
        ]);
        AddService("snapp_courier", "courier", "courier", "emerald", 30, "اسنپ / پیک آنلاین", "Snapp / Online courier", []);
        AddService("store_courier", "store_courier", "bike", "violet", 40, "پیک فروشگاه", "Store courier", []);
        AddService("in_person", "in_person", "store", "rose", 50, "تحویل حضوری", "In person", []);

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<ShippingServiceDetail?> LoadDetailAsync(
        FulfillmentDbContext db,
        Guid serviceId,
        CancellationToken cancellationToken)
    {
        var entity = await db.ShippingServices.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ShippingServiceId == serviceId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var translations = await db.ShippingServiceTranslations.AsNoTracking()
            .Where(x => x.ShippingServiceId == serviceId).ToListAsync(cancellationToken);
        var options = await db.ShippingServiceOptions.AsNoTracking()
            .Where(x => x.ShippingServiceId == serviceId)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);
        var optionIds = options.Select(x => x.ShippingServiceOptionId).ToArray();
        var optionTranslations = await db.ShippingServiceOptionTranslations.AsNoTracking()
            .Where(x => optionIds.Contains(x.ShippingServiceOptionId)).ToListAsync(cancellationToken);
        var otByOption = optionTranslations.GroupBy(x => x.ShippingServiceOptionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return new ShippingServiceDetail(
            entity.ShippingServiceId,
            entity.Code,
            entity.ProviderKind,
            entity.IconKey,
            entity.ColorKey,
            entity.IsActive,
            entity.SortOrder,
            translations.Select(t => new ShippingServiceTranslationWrite(t.LanguageId, t.Name, t.Description)).ToList(),
            options.Select(o =>
            {
                otByOption.TryGetValue(o.ShippingServiceOptionId, out var rows);
                rows ??= [];
                return new ShippingServiceOptionDetail(
                    o.ShippingServiceOptionId,
                    o.Code,
                    o.IsActive,
                    o.SortOrder,
                    rows.Select(r => new ShippingServiceOptionTranslationWrite(r.LanguageId, r.Name)).ToList());
            }).ToList());
    }

    private static void AddTranslations(
        FulfillmentDbContext db,
        Guid serviceId,
        IReadOnlyList<ShippingServiceTranslationWrite> translations)
    {
        foreach (var row in translations.Where(x => x.LanguageId != Guid.Empty && !string.IsNullOrWhiteSpace(x.Name)))
        {
            db.ShippingServiceTranslations.Add(
                ShippingServiceTranslation.Create(Guid.NewGuid(), serviceId, row.LanguageId, row.Name, row.Description));
        }
    }

    private static void AddOptions(
        FulfillmentDbContext db,
        Guid serviceId,
        IReadOnlyList<ShippingServiceOptionWrite> options,
        DateTimeOffset now)
    {
        foreach (var option in options)
        {
            if (string.IsNullOrWhiteSpace(option.Code))
            {
                continue;
            }

            var oid = option.ShippingServiceOptionId is { } existing && existing != Guid.Empty
                ? existing
                : Guid.NewGuid();
            db.ShippingServiceOptions.Add(
                ShippingServiceOption.Create(oid, serviceId, option.Code, option.SortOrder, option.IsActive, now));
            foreach (var tr in option.Translations.Where(x => x.LanguageId != Guid.Empty && !string.IsNullOrWhiteSpace(x.Name)))
            {
                db.ShippingServiceOptionTranslations.Add(
                    ShippingServiceOptionTranslation.Create(Guid.NewGuid(), oid, tr.LanguageId, tr.Name));
            }
        }
    }

    private static async Task ValidateLanguagesAsync(
        ILanguageDirectory languages,
        ShippingServiceWriteRequest body,
        CancellationToken cancellationToken)
    {
        var known = (await languages.ListAsync(cancellationToken)).Select(x => x.LanguageId).ToHashSet();
        foreach (var t in body.Translations)
        {
            if (!known.Contains(t.LanguageId))
            {
                throw new InvalidOperationException("shipping_service.language_invalid");
            }
        }

        foreach (var option in body.Options)
        {
            foreach (var t in option.Translations)
            {
                if (!known.Contains(t.LanguageId))
                {
                    throw new InvalidOperationException("shipping_service.language_invalid");
                }
            }
        }
    }

    private static async Task<Guid> ResolveLanguageIdAsync(
        ILanguageDirectory languages,
        string? language,
        CancellationToken cancellationToken)
    {
        var langs = await languages.ListAsync(cancellationToken);
        if (langs.Count == 0)
        {
            return Guid.Empty;
        }

        if (!string.IsNullOrWhiteSpace(language))
        {
            var needle = language.Trim();
            var hit = langs.FirstOrDefault(x =>
                x.Code.Equals(needle, StringComparison.OrdinalIgnoreCase)
                || x.Culture.Equals(needle, StringComparison.OrdinalIgnoreCase)
                || x.UrlPrefix.Equals(needle, StringComparison.OrdinalIgnoreCase)
                || x.Culture.StartsWith(needle, StringComparison.OrdinalIgnoreCase));
            if (hit is not null)
            {
                return hit.LanguageId;
            }
        }

        return (langs.FirstOrDefault(x => x.IsDefault) ?? langs[0]).LanguageId;
    }

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
