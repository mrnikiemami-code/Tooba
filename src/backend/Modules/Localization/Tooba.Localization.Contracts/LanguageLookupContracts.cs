namespace Tooba.Localization.Contracts;

/// <summary>Stable language row for cross-module lookup (no Localization.Application dependency).</summary>
public sealed record LanguageLookupSnapshot(
    Guid LanguageId,
    string Code,
    string Culture,
    string UrlPrefix,
    bool IsDefault);

/// <summary>
/// Minimal public language lookup for modules that need language resolution/fallback
/// without depending on Localization.Application.
/// </summary>
public interface ILanguageLookup
{
    /// <summary>List known languages for resolution and seed.</summary>
    Task<IReadOnlyList<LanguageLookupSnapshot>> ListAsync(CancellationToken cancellationToken);
}
