using Tooba.Localization.Application;
using Tooba.Localization.Contracts;

namespace Tooba.Localization.Infrastructure;

/// <summary>Contracts-facing language lookup over Localization.Application directory.</summary>
public sealed class LanguageLookupBridge : ILanguageLookup
{
    private readonly ILanguageDirectory _directory;

    /// <summary>Bridge را می‌سازد.</summary>
    public LanguageLookupBridge(ILanguageDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public async Task<IReadOnlyList<LanguageLookupSnapshot>> ListAsync(CancellationToken cancellationToken)
    {
        var rows = await _directory.ListAsync(cancellationToken);
        return rows
            .Select(x => new LanguageLookupSnapshot(x.LanguageId, x.Code, x.Culture, x.UrlPrefix, x.IsDefault))
            .ToList();
    }
}
