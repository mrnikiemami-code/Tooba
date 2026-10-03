using Tooba.Localization.Application.Models;

namespace Tooba.Localization.Application.Ports;

/// <summary>دایرکتوری زبان پایدار.</summary>
public interface ILanguageDirectory
{
    Task<IReadOnlyList<LanguageSnapshot>> ListAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<LanguageAdminSnapshot>> ListAdminAsync(CancellationToken cancellationToken);
    Task<LanguageSnapshot?> GetByCodeAsync(string code, CancellationToken cancellationToken);
    Task<LanguageAdminSnapshot?> GetAdminByCodeAsync(string code, CancellationToken cancellationToken);
    Task EnsureActiveLanguageCodeAsync(string code, CancellationToken cancellationToken);
    Task<LanguageSnapshot> CreateAsync(CreateLanguageSpec command, CancellationToken cancellationToken);
    Task<LanguageSnapshot> UpdateAsync(string code, UpdateLanguageSpec command, CancellationToken cancellationToken);
    Task<LanguageSnapshot> PatchAsync(string code, PatchLanguageSpec command, CancellationToken cancellationToken);
    Task BootstrapAsync(CancellationToken cancellationToken);
}
