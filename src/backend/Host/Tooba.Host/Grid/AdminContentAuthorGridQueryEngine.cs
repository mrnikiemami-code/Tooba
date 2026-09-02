using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks.Grid;
using Tooba.Content.Application;
using Tooba.Content.Domain;
using Tooba.Content.Infrastructure.Persistence;

namespace Tooba.Host.Grid;

/// <summary>پرس‌وجوی DB-native گرید نویسندگان Admin.</summary>
internal sealed class AdminContentAuthorGridQueryEngine
{
    private readonly ContentDbContext _db;

    /// <summary>DbContext مالک Content را تزریق می‌کند.</summary>
    public AdminContentAuthorGridQueryEngine(ContentDbContext db) => _db = db;

    /// <summary>صفحهٔ گرید نویسندگان را برمی‌گرداند.</summary>
    public async Task<GridPageResponse<ContentAuthorGridRowDto>> QueryAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        IQueryable<ContentAuthor> q = _db.Authors.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            q = AdminEfGridQuery.ApplySearchAny(q, request.Search, x => x.DisplayName, x => x.Slug);
        }

        foreach (var filter in request.Filters)
        {
            q = ApplyFilter(q, filter);
        }

        var sort = request.Sort.FirstOrDefault() ?? new GridSortRequest("updated", "desc");
        return await AdminEfGridQuery.PageAsync(
            q,
            request,
            filtered => Order(filtered, sort),
            MapPageAsync,
            cancellationToken);
    }

    private static IQueryable<ContentAuthor> ApplyFilter(IQueryable<ContentAuthor> source, GridFilterRequest filter) =>
        filter.Field switch
        {
            "displayName" => AdminEfGridQuery.ApplyTextFilter(source, x => x.DisplayName, filter),
            "slug" => AdminEfGridQuery.ApplyTextFilter(source, x => x.Slug, filter),
            "isActive" => ApplyBoolFilter(source, filter),
            "updated" => AdminEfGridQuery.ApplyDateFilter(source, x => x.UpdatedAt, filter),
            _ => source,
        };

    private static IQueryable<ContentAuthor> ApplyBoolFilter(IQueryable<ContentAuthor> source, GridFilterRequest filter)
    {
        if (!bool.TryParse(filter.Value, out var value))
        {
            return source;
        }

        return filter.Operator == "notEqual"
            ? source.Where(x => x.IsActive != value)
            : source.Where(x => x.IsActive == value);
    }

    private static IOrderedQueryable<ContentAuthor> Order(IQueryable<ContentAuthor> source, GridSortRequest sort)
    {
        var asc = sort.Direction == "asc";
        return sort.Field switch
        {
            "displayName" => asc
                ? source.OrderBy(x => x.DisplayName).ThenByDescending(x => x.UpdatedAt)
                : source.OrderByDescending(x => x.DisplayName).ThenByDescending(x => x.UpdatedAt),
            "slug" => asc
                ? source.OrderBy(x => x.Slug).ThenBy(x => x.DisplayName)
                : source.OrderByDescending(x => x.Slug).ThenBy(x => x.DisplayName),
            "isActive" => asc
                ? source.OrderBy(x => x.IsActive).ThenBy(x => x.DisplayName)
                : source.OrderByDescending(x => x.IsActive).ThenBy(x => x.DisplayName),
            _ => asc
                ? source.OrderBy(x => x.UpdatedAt).ThenBy(x => x.DisplayName)
                : source.OrderByDescending(x => x.UpdatedAt).ThenBy(x => x.DisplayName),
        };
    }

    private async Task<IReadOnlyList<ContentAuthorGridRowDto>> MapPageAsync(
        List<ContentAuthor> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var ids = rows.Select(x => x.AuthorId).ToList();
        var articleCounts = await _db.Articles.AsNoTracking()
            .Where(x => x.AuthorId != null && ids.Contains(x.AuthorId.Value))
            .GroupBy(x => x.AuthorId!.Value)
            .Select(g => new { AuthorId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.AuthorId, x => x.Count, cancellationToken);

        return rows.Select(row => new ContentAuthorGridRowDto(
            row.AuthorId,
            row.DisplayName,
            row.Slug,
            row.IsActive,
            articleCounts.GetValueOrDefault(row.AuthorId),
            row.UpdatedAt)).ToList();
    }
}
