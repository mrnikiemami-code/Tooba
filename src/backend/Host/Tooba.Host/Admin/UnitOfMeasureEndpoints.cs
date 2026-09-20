using MediatR;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
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

/// <summary>خواندن واحد از Catalog؛ نوشتن از طریق CQRS.</summary>
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
        ISender sender,
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
            var unitId = await sender.Send(new CreateUnitOfMeasureCommand(ToModel(body)), cancellationToken);
            return Results.Json(new { UnitOfMeasureId = unitId });
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
        ISender sender,
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
            var id = await sender.Send(new UpdateUnitOfMeasureCommand(unitId, ToModel(body)), cancellationToken);
            return Results.Json(new { UnitOfMeasureId = id });
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
        ISender sender,
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
            var result = await sender.Send(new DeactivateUnitOfMeasureCommand(unitId), cancellationToken);
            return Results.Json(new { UnitOfMeasureId = result.UnitId, IsActive = result.IsActive });
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static UnitOfMeasureWriteModel ToModel(UnitOfMeasureWriteRequest body) =>
        new(
            body.Code,
            body.Dimension,
            body.IsActive,
            body.SortOrder,
            body.Translations.Select(t => new UnitOfMeasureTranslationWriteModel(t.LanguageId, t.Name, t.ShortName)).ToList());

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

/// <summary>Adapter Host برای اعتبار LanguageId واحدها.</summary>
public sealed class HostUnitOfMeasureLanguageGate : IUnitOfMeasureLanguageGate
{
    private readonly ILanguageDirectory _languages;

    /// <summary>Gate را می‌سازد.</summary>
    public HostUnitOfMeasureLanguageGate(ILanguageDirectory languages) => _languages = languages;

    /// <inheritdoc />
    public async Task EnsureKnownAsync(IReadOnlyList<Guid> languageIds, CancellationToken cancellationToken)
    {
        var known = (await _languages.ListAsync(cancellationToken)).Select(x => x.LanguageId).ToHashSet();
        if (languageIds.Any(id => !known.Contains(id)))
        {
            throw new InvalidOperationException("unit.language.unknown");
        }
    }
}
