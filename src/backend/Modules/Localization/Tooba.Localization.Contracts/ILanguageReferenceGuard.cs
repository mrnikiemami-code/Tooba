namespace Tooba.Localization.Contracts;

/// <summary>
/// Cross-module language reference check (Content articles, etc.).
/// Owned by Localization Contracts; implemented by Content.Infrastructure.
/// </summary>
public interface ILanguageReferenceGuard
{
    /// <summary>آیا کد زبان در ماژول‌های دیگر ارجاع شده است؟</summary>
    Task<bool> IsReferencedAsync(string languageCode, CancellationToken cancellationToken);
}
