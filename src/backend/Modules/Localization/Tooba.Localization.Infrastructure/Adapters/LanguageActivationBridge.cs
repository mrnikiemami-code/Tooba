using Tooba.Localization.Application.Composition;
using Tooba.Localization.Application.Models;
using Tooba.Localization.Application.Ports;
using Tooba.Localization.Contracts.Ports;

namespace Tooba.Localization.Infrastructure.Adapters;

/// <summary>Contracts-facing active-language gate over Localization.Application directory.</summary>
public sealed class LanguageActivationBridge(ILanguageDirectory directory) : ILanguageActivationPort
{
    /// <inheritdoc />
    public Task EnsureActiveAsync(string languageCode, CancellationToken cancellationToken) =>
        directory.EnsureActiveLanguageCodeAsync(languageCode, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> IsActiveAsync(string languageCode, CancellationToken cancellationToken)
    {
        var row = await directory.GetByCodeAsync(languageCode, cancellationToken);
        return row is not null && row.IsActive;
    }
}
