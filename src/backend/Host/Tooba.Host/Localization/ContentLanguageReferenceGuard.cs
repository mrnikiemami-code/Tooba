using Microsoft.EntityFrameworkCore;
using Tooba.Content.Infrastructure.Persistence;
using Tooba.Localization.Application;

namespace Tooba.Host.Localization;

/// <summary>ارجاع ContentArticle.Locale به زبان پایدار.</summary>
public sealed class ContentLanguageReferenceGuard : ILanguageReferenceGuard
{
    private readonly ContentDbContext _content;

    /// <summary>Content DbContext را تزریق می‌کند.</summary>
    public ContentLanguageReferenceGuard(ContentDbContext content) => _content = content;

    /// <inheritdoc />
    public async Task<bool> IsReferencedAsync(string languageCode, CancellationToken cancellationToken)
    {
        var normalized = languageCode.Trim();
        return await _content.Articles.AsNoTracking()
            .AnyAsync(article => article.Locale == normalized, cancellationToken);
    }
}
