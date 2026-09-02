using Tooba.BuildingBlocks.Grid;
using Tooba.Content.Application;
using Tooba.Content.Infrastructure.Persistence;
using Tooba.Host.Grid;

namespace Tooba.Host.Content;

/// <summary>ترکیب HTTP برای مسیرهای مدیریتی نویسندهٔ Content.</summary>
public sealed class ContentAuthorPanelComposer
{
    private readonly IContentAuthorDirectory _authors;
    private readonly AdminContentAuthorGridQueryEngine _grid;

    /// <summary>دایرکتوری نویسنده و DbContext را تزریق می‌کند.</summary>
    public ContentAuthorPanelComposer(IContentAuthorDirectory authors, ContentDbContext db)
    {
        _authors = authors;
        _grid = new AdminContentAuthorGridQueryEngine(db);
    }

    /// <summary>صفحه‌بندی server-side گرید نویسندگان Admin.</summary>
    public Task<GridPageResponse<ContentAuthorGridRowDto>> QueryGridAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        var q = AdminListGridPolicies.ContentAuthors.Normalize(request);
        return _grid.QueryAsync(q, cancellationToken);
    }

    /// <summary>workspace یک نویسنده را برمی‌گرداند.</summary>
    public Task<ContentAuthorWorkspaceDto?> GetWorkspaceAsync(Guid authorId, CancellationToken cancellationToken) =>
        _authors.GetWorkspaceAsync(authorId, cancellationToken);

    /// <summary>نویسندهٔ جدید می‌سازد.</summary>
    public Task<ContentAuthorWorkspaceDto> CreateAsync(
        CreateContentAuthorCommand command,
        CancellationToken cancellationToken) =>
        _authors.CreateAsync(command, cancellationToken);

    /// <summary>نویسنده را به‌روزرسانی می‌کند.</summary>
    public Task<ContentAuthorWorkspaceDto> UpdateAsync(
        Guid authorId,
        UpdateContentAuthorCommand command,
        CancellationToken cancellationToken) =>
        _authors.UpdateAsync(authorId, command, cancellationToken);

    /// <summary>نویسنده را غیرفعال می‌کند.</summary>
    public Task DeactivateAsync(Guid authorId, CancellationToken cancellationToken) =>
        _authors.DeactivateAsync(authorId, cancellationToken);

    /// <summary>فهرست picker نویسنده‌ها را برمی‌گرداند.</summary>
    public Task<IReadOnlyList<ContentAuthorPickerItemDto>> GetPickerListAsync(
        string? search,
        bool activeOnly,
        CancellationToken cancellationToken) =>
        _authors.GetPickerListAsync(search, activeOnly, cancellationToken);
}
