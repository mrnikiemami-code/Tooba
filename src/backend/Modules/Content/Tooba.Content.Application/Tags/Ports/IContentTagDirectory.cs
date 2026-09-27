using Tooba.Content.Application.Tags.Commands;
using Tooba.Content.Application.Tags.Models;

namespace Tooba.Content.Application.Tags.Ports;

/// <summary>دایرکتوری برچسب‌های محتوا و انتساب به مقاله.</summary>
public interface IContentTagDirectory
{
    Task<IReadOnlyList<ContentTagDto>> SearchAsync(
        string languageCode, string? search, int limit, bool activeOnly, CancellationToken cancellationToken);

    Task<ContentTagDto> CreateAsync(CreateTagCommand command, CancellationToken cancellationToken);

    Task<IReadOnlyList<ContentTagDto>> ListArticleTagsAsync(Guid articleId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ContentTagDto>> AssignToArticleAsync(
        Guid articleId, Guid tagId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ContentTagDto>> RemoveFromArticleAsync(
        Guid articleId, Guid tagId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> GetArticleTagNamesAsync(
        IReadOnlyCollection<Guid> articleIds, CancellationToken cancellationToken);
}
