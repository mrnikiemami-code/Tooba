using Tooba.Localization.Application.Composition;
using Tooba.Localization.Application.Models;
using Tooba.Localization.Application.Ports;
using Tooba.Localization.Contracts.Ports;

namespace Tooba.Localization.Infrastructure.Adapters;

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
            .Select(x => new LanguageLookupSnapshot(x.LanguageId, x.Code, x.Culture, x.UrlPrefix, x.IsDefault)
            {
                // Every value is projected from the authoritative Localization row. Activation is the
                // real row flag, never default-true inference.
                IsActive = x.IsActive,
                Direction = x.Direction,
                DisplayName = x.DisplayName,
                NativeName = x.NativeName,
                SortOrder = x.SortOrder,
            })
            .ToList();
    }
}
