using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Tags.Models;

namespace Tooba.Catalog.Application.Tags.Ports;

/// <summary>Catalog-owned port for Admin tag read/write and assignments.</summary>
public interface ITagDirectory
{
    /// <summary>Lists tags ordered by Code (max 200), optional Name/Code search.</summary>
    Task<IReadOnlyList<TagView>> ListAsync(string locale, string? search, CancellationToken cancellationToken);

    /// <summary>Gets one tag; missing → catalog.tag.missing.</summary>
    Task<Result<TagView>> GetAsync(Guid tagId, string? locale, CancellationToken cancellationToken);

    /// <summary>Creates a tag with localized names and optional code/slug.</summary>
    Task<Result<TagView>> CreateAsync(CreateTagWriteModel model, CancellationToken cancellationToken);

    /// <summary>Lists tags assigned to a product.</summary>
    Task<IReadOnlyList<TagView>> ListProductTagsAsync(Guid productId, string? locale, CancellationToken cancellationToken);

    /// <summary>Assigns a tag to a product; duplicate/missing → Result failure.</summary>
    Task<Result<IReadOnlyList<TagView>>> AssignProductTagAsync(
        Guid productId,
        Guid tagId,
        CancellationToken cancellationToken);

    /// <summary>Removes product-tag assignment (idempotent).</summary>
    Task<Result<IReadOnlyList<TagView>>> RemoveProductTagAsync(
        Guid productId,
        Guid tagId,
        CancellationToken cancellationToken);

    /// <summary>Lists tags assigned to a category.</summary>
    Task<IReadOnlyList<TagView>> ListCategoryTagsAsync(Guid categoryId, string? locale, CancellationToken cancellationToken);

    /// <summary>Assigns a tag to a category; duplicate/missing → Result failure.</summary>
    Task<Result<IReadOnlyList<TagView>>> AssignCategoryTagAsync(
        Guid categoryId,
        Guid tagId,
        CancellationToken cancellationToken);

    /// <summary>Removes category-tag assignment (idempotent).</summary>
    Task<Result<IReadOnlyList<TagView>>> RemoveCategoryTagAsync(
        Guid categoryId,
        Guid tagId,
        CancellationToken cancellationToken);
}
