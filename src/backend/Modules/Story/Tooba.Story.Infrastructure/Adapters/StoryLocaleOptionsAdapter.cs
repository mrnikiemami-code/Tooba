using Tooba.Localization.Contracts.Ports;
using Tooba.Story.Application.Stories.Ports;

namespace Tooba.Story.Infrastructure.Adapters;

/// <summary>گزینه‌های زبان استوری از رجیستری Localization (فقط Contracts).</summary>
public sealed class StoryLocaleOptionsAdapter(ILanguageLookup languages) : IStoryLocaleOptionsPort
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<StoryLocaleOption>> ListEnabledAsync(CancellationToken cancellationToken)
    {
        var rows = await languages.ListAsync(cancellationToken);
        return rows
            .Where(row => row.IsActive == true)
            .OrderBy(row => row.SortOrder ?? int.MaxValue)
            .ThenBy(row => row.Code, StringComparer.OrdinalIgnoreCase)
            .Select(row => new StoryLocaleOption(
                row.Code,
                string.IsNullOrWhiteSpace(row.NativeName)
                    ? (string.IsNullOrWhiteSpace(row.DisplayName) ? row.Code : row.DisplayName!)
                    : row.NativeName!,
                row.IsDefault))
            .ToList();
    }
}
