using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Inventory.Tests.Architecture;

/// <summary>
/// TB-TMAR-INVENTORY-AMSC-001 Wave 1 (Migrate) durable guards: one typed fault mechanism, one
/// registered error-catalog/resource owner per Inventory code, and a single typed-fault-to-Result
/// composition seam. These lock the migration so no raw <c>InvalidOperationException</c> fault,
/// unregistered code, or unlocalized key can reappear inside Inventory.
/// </summary>
public sealed class InventoryAmcW1MigrateGuardTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs", "ai", "TOOBA-PIPELINE-PROTOCOL.md"))
                || Directory.Exists(Path.Combine(dir.FullName, ".git")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }

    private static string InventoryRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Inventory");

    private static IEnumerable<(string Path, string Text)> ProductionSources()
    {
        var root = InventoryRoot();
        foreach (var project in new[]
                 {
                     "Tooba.Inventory.Domain", "Tooba.Inventory.Application",
                     "Tooba.Inventory.Contracts", "Tooba.Inventory.Infrastructure",
                 })
        {
            foreach (var file in Directory.EnumerateFiles(Path.Combine(root, project), "*.cs", SearchOption.AllDirectories))
            {
                var n = file.Replace('\\', '/');
                if (n.Contains("/bin/", StringComparison.Ordinal) || n.Contains("/obj/", StringComparison.Ordinal))
                {
                    continue;
                }

                if (n.Contains("/Migrations/", StringComparison.OrdinalIgnoreCase)
                    || n.EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                yield return (Path.GetRelativePath(RepoRoot(), file), File.ReadAllText(file));
            }
        }
    }

    [Fact]
    public void No_raw_InvalidOperationException_fault_remains_in_production()
    {
        var offenders = ProductionSources()
            .Where(x => x.Text.Contains("new InvalidOperationException(", StringComparison.Ordinal))
            .Select(x => x.Path)
            .ToList();
        Assert.True(offenders.Count == 0, "raw InvalidOperationException fault: " + string.Join("; ", offenders));
    }

    [Fact]
    public void No_raw_inventory_machine_code_literal_outside_the_contract_owner()
    {
        var offenders = ProductionSources()
            .Where(x => !x.Path.EndsWith("InventoryErrorCodes.cs", StringComparison.Ordinal))
            .SelectMany(x => Regex.Matches(x.Text, @"""inventory\.(?!reserved\.|released\.|adjusted\.|reservation_consumed\.|availability_changed\.)[a-z0-9_.]+""")
                .Select(m => $"{x.Path}:{m.Value}"))
            .ToList();
        Assert.True(offenders.Count == 0, "raw inventory.* code literal: " + string.Join("; ", offenders));
    }

    [Fact]
    public void Every_declared_inventory_code_is_catalogued_and_localized()
    {
        var codesFile = File.ReadAllText(Path.Combine(
            InventoryRoot(), "Tooba.Inventory.Contracts", "Errors", "InventoryErrorCodes.cs"));
        var codes = Regex.Matches(codesFile, @"public const string \w+ = ""(inventory\.[a-z0-9_.]+)""")
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();
        Assert.True(codes.Count >= 29, "unexpectedly few Inventory codes: " + codes.Count);

        var contributor = File.ReadAllText(Path.Combine(
            InventoryRoot(), "Tooba.Inventory.Contracts", "Errors", "InventoryErrorCatalogContributor.cs"));
        var english = File.ReadAllText(Path.Combine(
            InventoryRoot(), "Tooba.Inventory.Contracts", "Resources", "InventoryErrors.resx"));
        var persian = File.ReadAllText(Path.Combine(
            InventoryRoot(), "Tooba.Inventory.Contracts", "Resources", "InventoryErrors.fa.resx"));

        foreach (var code in codes)
        {
            Assert.Contains($"InventoryErrorCodes.{ConstantFor(codesFile, code)}", contributor, StringComparison.Ordinal);
            Assert.Contains($"name=\"{code}\"", english, StringComparison.Ordinal);
            Assert.Contains($"name=\"{code}\"", persian, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Resource_set_owns_the_inventory_keyspace_and_is_registered()
    {
        var resourceSet = File.ReadAllText(Path.Combine(
            InventoryRoot(), "Tooba.Inventory.Contracts", "Errors", "InventoryErrorResourceSet.cs"));
        Assert.Contains("\"inventory.\"", resourceSet, StringComparison.Ordinal);
        Assert.Contains("InventoryErrorResources.Manager", resourceSet, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            InventoryRoot(), "Tooba.Inventory.Infrastructure", "DependencyInjection", "InventoryModule.cs"));
        Assert.Contains("IErrorCatalogContributor, InventoryErrorCatalogContributor", module, StringComparison.Ordinal);
        Assert.Contains("IErrorResourceSet, InventoryErrorResourceSet", module, StringComparison.Ordinal);
    }

    [Fact]
    public void Single_typed_fault_to_result_composition_seam_exists()
    {
        var seam = File.ReadAllText(Path.Combine(
            InventoryRoot(), "Tooba.Inventory.Application", "Composition", "InventoryOperation.cs"));
        Assert.Contains("public static async Task<Result<T>> ExecuteAsync<T>", seam, StringComparison.Ordinal);
        Assert.Contains("public static async Task<Result> ExecuteAsync", seam, StringComparison.Ordinal);
        Assert.Contains("catch (ContractOperationException ex) when (InventoryErrorCodes.IsKnown(ex.Code))", seam, StringComparison.Ordinal);
        Assert.Contains("catch (SemanticException ex)", seam, StringComparison.Ordinal);
    }

    [Fact]
    public void Known_code_ownership_does_not_claim_foreign_reservation_retry_code()
    {
        var codesFile = File.ReadAllText(Path.Combine(
            InventoryRoot(), "Tooba.Inventory.Contracts", "Errors", "InventoryErrorCodes.cs"));
        Assert.DoesNotContain("retry_limit_reached", codesFile, StringComparison.Ordinal);
        Assert.DoesNotContain("inventory.recovery.", codesFile, StringComparison.Ordinal);
    }

    private static string ConstantFor(string codesFile, string code)
    {
        var match = Regex.Match(
            codesFile,
            $@"public const string (\w+) = ""{Regex.Escape(code)}""");
        Assert.True(match.Success, $"no constant for {code}");
        return match.Groups[1].Value;
    }
}
