namespace Tooba.Content.Application;

/// <summary>نمای Admin برچسب محتوا.</summary>
public sealed record ContentTagDto(
    Guid TagId,
    string LanguageCode,
    string Name,
    string NormalizedName,
    string? Slug,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>فرمان ایجاد برچسب.</summary>
public sealed record CreateContentTagCommand(string LanguageCode, string Name, string? Slug);

/// <summary>دایرکتوری برچسب‌های محتوا و انتساب به مقاله.</summary>
public interface IContentTagDirectory
{
    /// <summary>جستجوی محدود برچسب‌های یک زبان.</summary>
    Task<IReadOnlyList<ContentTagDto>> SearchAsync(
        string languageCode,
        string? search,
        int limit,
        bool activeOnly,
        CancellationToken cancellationToken);

    /// <summary>برچسب جدید می‌سازد.</summary>
    Task<ContentTagDto> CreateAsync(CreateContentTagCommand command, CancellationToken cancellationToken);

    /// <summary>برچسب‌های انتساب‌شده به مقاله را برمی‌گرداند.</summary>
    Task<IReadOnlyList<ContentTagDto>> ListArticleTagsAsync(Guid articleId, CancellationToken cancellationToken);

    /// <summary>برچسب را به مقاله منتسب می‌کند (idempotent).</summary>
    Task<IReadOnlyList<ContentTagDto>> AssignToArticleAsync(
        Guid articleId,
        Guid tagId,
        CancellationToken cancellationToken);

    /// <summary>انتساب برچسب را از مقاله حذف می‌کند.</summary>
    Task<IReadOnlyList<ContentTagDto>> RemoveFromArticleAsync(
        Guid articleId,
        Guid tagId,
        CancellationToken cancellationToken);

    /// <summary>نام برچسب‌های مقاله را برای projection عمومی برمی‌گرداند.</summary>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> GetArticleTagNamesAsync(
        IReadOnlyCollection<Guid> articleIds,
        CancellationToken cancellationToken);
}
