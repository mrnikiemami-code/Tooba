using Tooba.BuildingBlocks.Grid;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Infrastructure.Grid;
using Tooba.Content.Infrastructure.Persistence;

namespace Tooba.Content.Infrastructure.Adapters;

/// <summary>Application port over admin article grid engine.</summary>
public sealed class ContentArticleGridAdapter(ContentDbContext db) : IContentArticleGridPort
{
    /// <inheritdoc />
    public Task<GridPageResponse<AdminArticleSnapshot>> QueryAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        var q = ContentAdminGridPolicies.NormalizeArticles(request);
        return new AdminContentGridQueryEngine(db).QueryAsync(q, cancellationToken);
    }
}

/// <summary>Application port over admin author grid engine.</summary>
public sealed class ContentAuthorGridAdapter(ContentDbContext db) : IContentAuthorGridPort
{
    /// <inheritdoc />
    public Task<GridPageResponse<ContentAuthorGridRowDto>> QueryAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        var q = ContentAdminGridPolicies.NormalizeAuthors(request);
        return new AdminContentAuthorGridQueryEngine(db).QueryAsync(q, cancellationToken);
    }
}
