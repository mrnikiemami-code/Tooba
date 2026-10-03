using Microsoft.EntityFrameworkCore;
using Tooba.Content.Infrastructure.Persistence;
using Tooba.Localization.Contracts.Errors;
using Tooba.Localization.Contracts.Ports;

namespace Tooba.Content.Infrastructure.Adapters;

/// <summary>ارجاع ContentArticle.Locale به زبان پایدار — Content owns ContentDbContext.</summary>
public sealed class ContentLanguageReferenceGuard(ContentDbContext content) : ILanguageReferenceGuard
{
    /// <inheritdoc />
    public async Task<bool> IsReferencedAsync(string languageCode, CancellationToken cancellationToken)
    {
        var normalized = languageCode.Trim();
        return await content.Articles.AsNoTracking()
            .AnyAsync(article => article.Locale == normalized, cancellationToken);
    }
}
