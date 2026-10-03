namespace Tooba.Localization.Contracts.Ports;

/// <summary>Narrow active-language gate for foreign modules (Content locale assignment).</summary>
public interface ILanguageActivationPort
{
    /// <summary>Ensures the language code exists and is active; throws with localization.* stable codes when not.</summary>
    Task EnsureActiveAsync(string languageCode, CancellationToken cancellationToken);

    /// <summary>Returns whether the language code exists and is active (no throw).</summary>
    Task<bool> IsActiveAsync(string languageCode, CancellationToken cancellationToken);
}
