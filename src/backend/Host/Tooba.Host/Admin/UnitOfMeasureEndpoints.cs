using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Localization.Application;

namespace Tooba.Host.Admin;

/// <summary>واحد اندازه‌گیری Admin — ترجمه‌ها با LanguageId رجیستری.</summary>
public sealed record UnitTranslationWrite(Guid LanguageId, string Name, string ShortName);

/// <summary>ایجاد/ویرایش واحد.</summary>
public sealed record UnitOfMeasureWriteRequest(
    string Code,
    string Dimension,
    bool IsActive,
    int SortOrder,
    IReadOnlyList<UnitTranslationWrite> Translations);

/// <summary>ردیف گرید واحد.</summary>
public sealed record UnitOfMeasureListItem(
    Guid UnitOfMeasureId,
    string Code,
    string Dimension,
    string Name,
    string ShortName,
    bool IsActive,
    int SortOrder,
    bool IsReferenced);

/// <summary>جزئیات واحد با همه ترجمه‌ها.</summary>
public sealed record UnitOfMeasureDetail(
    Guid UnitOfMeasureId,
    string Code,
    string Dimension,
    bool IsActive,
    int SortOrder,
    bool IsReferenced,
    IReadOnlyList<UnitTranslationWrite> Translations);

/// <summary>CRUD واحد اندازه‌گیری کاتالوگ.</summary>
public static class UnitOfMeasureEndpoints
{
    /// <summary>مسیرهای Admin واحد را ثبت می‌کند.</summary>
    public static void MapUnitOfMeasureEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/admin/catalog/units");
        group.MapGet("/", ListAsync);
        group.MapGet("/{unitId:guid}", GetAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{unitId:guid}", UpdateAsync);
        group.MapPost("/{unitId:guid}/deactivate", DeactivateAsync);
    }

    private static async Task<IResult> ListAsync(
        CatalogDbContext catalog,
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
            var langId = await ResolveLanguageIdAsync(languages, language, cancellationToken);
            var units = await catalog.UnitsOfMeasure.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.Code).ToListAsync(cancellationToken);
            var ids = units.Select(x => x.UnitOfMeasureId).ToArray();
            var translations = await catalog.UnitOfMeasureTranslations.AsNoTracking()
                .Where(x => ids.Contains(x.UnitOfMeasureId)).ToListAsync(cancellationToken);
            var referenced = await catalog.Products.AsNoTracking()
                .Where(p => ids.Contains(p.UnitOfMeasureId))
                .Select(p => p.UnitOfMeasureId)
                .Distinct()
                .ToListAsync(cancellationToken);
            var refSet = referenced.ToHashSet();
            var byUnit = translations.GroupBy(x => x.UnitOfMeasureId).ToDictionary(g => g.Key, g => g.ToList());
            var items = units.Select(u =>
            {
                byUnit.TryGetValue(u.UnitOfMeasureId, out var rows);
                var picked = rows?.FirstOrDefault(r => r.LanguageId == langId) ?? rows?.FirstOrDefault();
                return new UnitOfMeasureListItem(
                    u.UnitOfMeasureId,
                    u.Code,
                    u.Dimension.ToString(),
                    picked?.Name ?? u.Code,
                    picked?.ShortName ?? u.Code,
                    u.IsActive,
                    u.SortOrder,
                    refSet.Contains(u.UnitOfMeasureId));
            }).ToList();
            return Results.Json(items);
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> GetAsync(
        Guid unitId,
        CatalogDbContext catalog,
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
            var unit = await catalog.UnitsOfMeasure.AsNoTracking()
                .SingleOrDefaultAsync(x => x.UnitOfMeasureId == unitId, cancellationToken)
                ?? throw new PlatformHttpException(404, "Not Found", "unit.missing");
            var translations = await catalog.UnitOfMeasureTranslations.AsNoTracking()
                .Where(x => x.UnitOfMeasureId == unitId).ToListAsync(cancellationToken);
            var referenced = await catalog.Products.AsNoTracking().AnyAsync(p => p.UnitOfMeasureId == unitId, cancellationToken);
            return Results.Json(new UnitOfMeasureDetail(
                unit.UnitOfMeasureId,
                unit.Code,
                unit.Dimension.ToString(),
                unit.IsActive,
                unit.SortOrder,
                referenced,
                translations.Select(t => new UnitTranslationWrite(t.LanguageId, t.Name, t.ShortName)).ToList()));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> CreateAsync(
        UnitOfMeasureWriteRequest body,
        CatalogDbContext catalog,
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
            var now = DateTimeOffset.UtcNow;
            var dimension = ParseDimension(body.Dimension);
            var unit = UnitOfMeasure.Create(UuidV7.New(), body.Code, dimension, body.IsActive, body.SortOrder, now);
            await EnsureUniqueCodeAsync(catalog, unit.Code, null, cancellationToken);
            await ValidateTranslationsAsync(languages, body.Translations, cancellationToken);
            catalog.UnitsOfMeasure.Add(unit);
            foreach (var t in body.Translations)
            {
                catalog.UnitOfMeasureTranslations.Add(UnitOfMeasureTranslation.Create(unit.UnitOfMeasureId, t.LanguageId, t.Name, t.ShortName));
            }

            await catalog.SaveChangesAsync(cancellationToken);
            return Results.Json(new { unit.UnitOfMeasureId });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(new { title = ex.Message, errorCode = ex.Message }, statusCode: 400);
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> UpdateAsync(
        Guid unitId,
        UnitOfMeasureWriteRequest body,
        CatalogDbContext catalog,
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
            var unit = await catalog.UnitsOfMeasure.SingleOrDefaultAsync(x => x.UnitOfMeasureId == unitId, cancellationToken)
                ?? throw new PlatformHttpException(404, "Not Found", "unit.missing");
            var dimension = ParseDimension(body.Dimension);
            var code = body.Code.Trim().ToLowerInvariant();
            await EnsureUniqueCodeAsync(catalog, code, unitId, cancellationToken);
            await ValidateTranslationsAsync(languages, body.Translations, cancellationToken);
            unit.Update(code, dimension, body.SortOrder, DateTimeOffset.UtcNow);
            unit.SetActive(body.IsActive, DateTimeOffset.UtcNow);
            var existing = await catalog.UnitOfMeasureTranslations.Where(x => x.UnitOfMeasureId == unitId).ToListAsync(cancellationToken);
            foreach (var t in body.Translations)
            {
                var row = existing.FirstOrDefault(x => x.LanguageId == t.LanguageId);
                if (row is null)
                {
                    catalog.UnitOfMeasureTranslations.Add(UnitOfMeasureTranslation.Create(unitId, t.LanguageId, t.Name, t.ShortName));
                }
                else
                {
                    row.SetText(t.Name, t.ShortName);
                }
            }

            await catalog.SaveChangesAsync(cancellationToken);
            return Results.Json(new { unit.UnitOfMeasureId });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(new { title = ex.Message, errorCode = ex.Message }, statusCode: 400);
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> DeactivateAsync(
        Guid unitId,
        CatalogDbContext catalog,
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
            var unit = await catalog.UnitsOfMeasure.SingleOrDefaultAsync(x => x.UnitOfMeasureId == unitId, cancellationToken)
                ?? throw new PlatformHttpException(404, "Not Found", "unit.missing");
            unit.SetActive(false, DateTimeOffset.UtcNow);
            await catalog.SaveChangesAsync(cancellationToken);
            return Results.Json(new { unit.UnitOfMeasureId, unit.IsActive });
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static UnitOfMeasureDimension ParseDimension(string raw) =>
        Enum.TryParse<UnitOfMeasureDimension>(raw, true, out var d) ? d : throw new InvalidOperationException("unit.dimension.invalid");

    private static async Task EnsureUniqueCodeAsync(
        CatalogDbContext catalog,
        string code,
        Guid? exceptId,
        CancellationToken cancellationToken)
    {
        var clash = await catalog.UnitsOfMeasure.AsNoTracking()
            .AnyAsync(x => x.Code == code && (!exceptId.HasValue || x.UnitOfMeasureId != exceptId), cancellationToken);
        if (clash)
        {
            throw new InvalidOperationException("unit.code.duplicate");
        }
    }

    private static async Task ValidateTranslationsAsync(
        ILanguageDirectory languages,
        IReadOnlyList<UnitTranslationWrite> translations,
        CancellationToken cancellationToken)
    {
        var known = (await languages.ListAsync(cancellationToken)).Select(x => x.LanguageId).ToHashSet();
        if (translations.Any(t => !known.Contains(t.LanguageId)))
        {
            throw new InvalidOperationException("unit.language.unknown");
        }
    }

    private static async Task<Guid> ResolveLanguageIdAsync(
        ILanguageDirectory languages,
        string? language,
        CancellationToken cancellationToken)
    {
        var list = await languages.ListAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(language))
        {
            var match = list.FirstOrDefault(x =>
                string.Equals(x.Code, language, StringComparison.OrdinalIgnoreCase)
                || string.Equals(x.UrlPrefix, language, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                return match.LanguageId;
            }
        }

        return (list.FirstOrDefault(x => x.IsDefault) ?? list.FirstOrDefault())?.LanguageId ?? Guid.Empty;
    }
}
