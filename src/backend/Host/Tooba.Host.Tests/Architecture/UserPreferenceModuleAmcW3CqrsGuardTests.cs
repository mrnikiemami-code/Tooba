using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-USERPREFERENCE-AMC-001-W3 — Result pipeline, Contracts catalog, thin endpoints.</summary>
public sealed class UserPreferenceModuleAmcW3CqrsGuardTests
{
    [Fact]
    public void UserPreference_w3_owns_catalog_in_contracts_and_result_pipeline()
    {
        var root = Repo();
        var contracts = Path.Combine(root, "src/backend/Modules/UserPreference/Tooba.UserPreference.Contracts");
        var app = Path.Combine(root, "src/backend/Modules/UserPreference/Tooba.UserPreference.Application");
        var endpoints = Path.Combine(root, "src/backend/Modules/UserPreference/Tooba.UserPreference.Endpoints");
        var infra = Path.Combine(root, "src/backend/Modules/UserPreference/Tooba.UserPreference.Infrastructure");

        Assert.True(File.Exists(Path.Combine(contracts, "Errors", "UserPreferenceErrorCatalogContributor.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Errors", "UserPreferenceErrorResourceSet.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Resources", "UserPreferenceErrors.resx")));
        Assert.False(Directory.Exists(Path.Combine(endpoints, "Errors")));
        Assert.False(Directory.Exists(Path.Combine(endpoints, "Resources")));
        Assert.True(File.Exists(Path.Combine(app, "Composition", "UserPreferenceOperation.cs")));

        var module = File.ReadAllText(Path.Combine(infra, "UserPreferenceModule.cs"));
        Assert.Contains("UserPreferenceErrorCatalogContributor", module, StringComparison.Ordinal);
        Assert.Contains("UserPreferenceErrorResourceSet", module, StringComparison.Ordinal);

        foreach (var file in Directory.EnumerateFiles(endpoints, "*.cs", SearchOption.AllDirectories)
                     .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                                 && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Results.Json", text, StringComparison.Ordinal);
            Assert.DoesNotContain("catch (SemanticException", text, StringComparison.Ordinal);
            Assert.DoesNotContain("catch (PlatformHttpException", text, StringComparison.Ordinal);
            Assert.DoesNotContain("FromPlatformException(", text, StringComparison.Ordinal);
        }

        var localeValidator = File.ReadAllText(Path.Combine(
            app, "LocalePreferences", "Validators", "UpsertUserPreferenceCommandValidator.cs"));
        Assert.Contains("UserPreferenceErrorCodes.", localeValidator, StringComparison.Ordinal);
        Assert.DoesNotContain("\"preference.validation.", localeValidator, StringComparison.Ordinal);

        var upsert = File.ReadAllText(Path.Combine(
            app, "LocalePreferences", "Commands", "UpsertUserPreferenceCommand.cs"));
        Assert.Contains("IRequest<Result<", upsert, StringComparison.Ordinal);
        Assert.Contains("UserPreferenceOperation.ExecuteAsync", upsert, StringComparison.Ordinal);
    }

    private static string Repo()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
