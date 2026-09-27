using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ADMIN-AMC-001-W2-R1 — Catalog Application Quantity Settings semantic structure repair.
/// </summary>
public sealed class HostAdminAmcW2R1GuardTests
{
    [Fact]
    public void Quantity_surface_has_no_Application_Contracts_bundle()
    {
        var quantityRoot = QuantityRoot();
        Assert.True(Directory.Exists(quantityRoot), quantityRoot);
        var contractsBundles = Directory.GetFiles(quantityRoot, "*Contracts.cs", SearchOption.AllDirectories);
        Assert.Empty(contractsBundles);

        var settingsRoot = Path.Combine(AppRoot(), "Settings");
        var flatLegacy = new[]
        {
            Path.Combine(settingsRoot, "StoreQuantitySettingsContracts.cs"),
            Path.Combine(settingsRoot, "StoreQuantitySettingsHandlers.cs"),
            Path.Combine(settingsRoot, "SaveStoreQuantitySettingsCommandValidator.cs"),
        };
        foreach (var path in flatLegacy)
        {
            Assert.False(File.Exists(path), path);
        }
    }

    [Fact]
    public void Quantity_capability_uses_shallow_responsibility_folders()
    {
        var quantityRoot = QuantityRoot();
        foreach (var folder in new[] { "Commands", "Queries", "Models", "Ports", "Validators", "Mapping" })
        {
            Assert.True(Directory.Exists(Path.Combine(quantityRoot, folder)), folder);
        }

        // No one-file-per-request nested leaf under Commands/Queries.
        Assert.Empty(Directory.GetDirectories(Path.Combine(quantityRoot, "Commands")));
        Assert.Empty(Directory.GetDirectories(Path.Combine(quantityRoot, "Queries")));

        Assert.True(File.Exists(Path.Combine(quantityRoot, "Commands", "SaveStoreQuantitySettingsCommand.cs")));
        Assert.True(File.Exists(Path.Combine(quantityRoot, "Commands", "SaveStoreQuantitySettingsHandler.cs")));
        Assert.True(File.Exists(Path.Combine(quantityRoot, "Queries", "GetStoreQuantitySettingsQuery.cs")));
        Assert.True(File.Exists(Path.Combine(quantityRoot, "Queries", "GetStoreQuantitySettingsHandler.cs")));
        Assert.True(File.Exists(Path.Combine(quantityRoot, "Models", "StoreQuantitySettingsView.cs")));
        Assert.True(File.Exists(Path.Combine(quantityRoot, "Ports", "IStoreQuantitySettingsDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(quantityRoot, "Validators", "SaveStoreQuantitySettingsCommandValidator.cs")));
    }

    [Fact]
    public void Quantity_command_and_query_are_authoritative_and_unique()
    {
        var appRoot = AppRoot();
        var commandMatches = Directory.GetFiles(appRoot, "SaveStoreQuantitySettingsCommand.cs", SearchOption.AllDirectories);
        var queryMatches = Directory.GetFiles(appRoot, "GetStoreQuantitySettingsQuery.cs", SearchOption.AllDirectories);
        Assert.Single(commandMatches);
        Assert.Single(queryMatches);

        var commandText = File.ReadAllText(commandMatches[0]);
        var queryText = File.ReadAllText(queryMatches[0]);
        Assert.Contains("sealed record SaveStoreQuantitySettingsCommand", commandText, StringComparison.Ordinal);
        Assert.Contains("sealed record GetStoreQuantitySettingsQuery", queryText, StringComparison.Ordinal);
        Assert.DoesNotContain("interface IStoreQuantitySettingsDirectory", commandText, StringComparison.Ordinal);
        Assert.DoesNotContain("interface IStoreQuantitySettingsDirectory", queryText, StringComparison.Ordinal);
        Assert.DoesNotContain("IRequestHandler", commandText, StringComparison.Ordinal);
        Assert.DoesNotContain("IRequestHandler", queryText, StringComparison.Ordinal);
    }

    [Fact]
    public void Quantity_path_namespace_is_exact()
    {
        var quantityRoot = QuantityRoot();
        foreach (var file in Directory.GetFiles(quantityRoot, "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(AppRoot(), file).Replace('\\', '/');
            var expectedNs = "Tooba.Catalog.Application." + Path.GetDirectoryName(relative)!
                .Replace('/', '.')
                .Replace('\\', '.');
            var text = File.ReadAllText(file);
            Assert.True(
                Regex.IsMatch(text, $@"namespace\s+{Regex.Escape(expectedNs)}\s*;", RegexOptions.CultureInvariant),
                $"{relative} expected namespace {expectedNs}");
        }
    }

    [Fact]
    public void Validator_targets_authoritative_Save_command()
    {
        var validator = Path.Combine(QuantityRoot(), "Validators", "SaveStoreQuantitySettingsCommandValidator.cs");
        var text = File.ReadAllText(validator);
        Assert.Contains("AbstractValidator<SaveStoreQuantitySettingsCommand>", text, StringComparison.Ordinal);
        Assert.Contains("Tooba.Catalog.Application.Settings.Quantity.Commands", text, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(QuantityRoot(), "Validators", "GetStoreQuantitySettingsQueryValidator.cs")));
    }

    [Fact]
    public void Host_Admin_appearance_deferred_and_quantity_still_gone()
    {
        // Historical W2-R1: Admin at 58. W3 UoM (−1 → 57). W4 Tags (−1 → 56). W5 MegaMenu (−1 → 55).
        var admin = Path.Combine(FindRepoRoot(), "src/backend/Host/Tooba.Host/Admin");
        Assert.Equal(55, Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories).Length);
        Assert.True(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsComposer.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "QuantitySettingsEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "UnitOfMeasureEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "CatalogTagEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "CatalogMegaMenuEndpoints.cs")));
    }

    private static string AppRoot() =>
        Path.Combine(FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application");

    private static string QuantityRoot() =>
        Path.Combine(AppRoot(), "Settings", "Quantity");

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
