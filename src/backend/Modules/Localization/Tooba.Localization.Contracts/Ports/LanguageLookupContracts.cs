namespace Tooba.Localization.Contracts.Ports;

/// <summary>Stable language row for cross-module lookup (no Localization.Application dependency).</summary>
/// <remarks>
/// <para>
/// TB-SHIPPING-ADMIN-CONFIGURATION-CONTEXT-188: the authoritative Localization row already carries
/// activation, direction, names and order. Consumers that must project the <b>configured</b> language
/// set (not merely the known set) need that metadata, so it is exposed here as additive
/// non-positional properties.
/// </para>
/// <para>
/// The five positional members, the deconstruction and the known-language set/order produced by
/// <see cref="ILanguageLookup.ListAsync"/> are unchanged. The added members are optional so a
/// manually constructed legacy snapshot keeps compiling; <c>null</c> means "the source never
/// supplied this fact" and must never be read as a genuine configured value.
/// </para>
/// </remarks>
public sealed record LanguageLookupSnapshot(
    Guid LanguageId,
    string Code,
    string Culture,
    string UrlPrefix,
    bool IsDefault)
{
    /// <summary>
    /// Owner-provided activation flag of the real registry row. <c>null</c> for a legacy snapshot
    /// that never carried it — absence is not "active" and must not be inferred as <c>true</c>.
    /// </summary>
    public bool? IsActive { get; init; }

    /// <summary>Owner-provided writing direction (<c>rtl</c>/<c>ltr</c>), or <c>null</c> when absent.</summary>
    public string? Direction { get; init; }

    /// <summary>Owner-provided display name, or <c>null</c> when absent.</summary>
    public string? DisplayName { get; init; }

    /// <summary>Owner-provided native name, or <c>null</c> when absent.</summary>
    public string? NativeName { get; init; }

    /// <summary>Owner-provided ordering rank, or <c>null</c> when absent.</summary>
    public int? SortOrder { get; init; }
}

/// <summary>
/// Minimal public language lookup for modules that need language resolution/fallback
/// without depending on Localization.Application.
/// </summary>
public interface ILanguageLookup
{
    /// <summary>
    /// List <b>known</b> languages for resolution and seed. This is deliberately not filtered to
    /// active rows: inactive languages are still known identities. Consumers that need the configured
    /// set select on <see cref="LanguageLookupSnapshot.IsActive"/> themselves.
    /// </summary>
    Task<IReadOnlyList<LanguageLookupSnapshot>> ListAsync(CancellationToken cancellationToken);
}
