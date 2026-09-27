using Tooba.BuildingBlocks.Grid;
using Tooba.Content.Infrastructure.Grid;
using Tooba.Content.Infrastructure.Persistence;
using Tooba.Content.Application.Articles.Models;
using Tooba.Content.Application.Articles.Ports;
using Tooba.Content.Application.Authors.Models;
using Tooba.Content.Application.Authors.Ports;

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
