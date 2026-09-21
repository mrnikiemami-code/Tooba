using System.Xml.Linq;
using Xunit;

namespace Tooba.Inventory.Tests.Architecture;

public sealed class InventoryArchitectureGuardTests
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

    [Fact]
    public void Domain_csproj_has_no_application_infrastructure_or_endpoints_reference()
    {
        var refs = ProjectRefs("Tooba.Inventory.Domain");
        Assert.DoesNotContain(refs, r => r.Contains("Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Endpoints", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Contracts", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Seller_inventory_write_returns_result_without_direct_semantic_exception()
    {
        var contracts = File.ReadAllText(Path.Combine(
            InventoryRoot(), "Tooba.Inventory.Contracts", "SellerOfferInventoryContracts.cs"));
        Assert.Contains("Task<Result> SetInventoryAsync", contracts, StringComparison.Ordinal);

        var directory = File.ReadAllText(Path.Combine(
            InventoryRoot(), "Tooba.Inventory.Infrastructure", "InventoryDirectory.cs"));
        var start = directory.IndexOf("public async Task<Result> SetInventoryAsync", StringComparison.Ordinal);
        Assert.True(start >= 0);
        var end = directory.IndexOf("public async Task<IReadOnlyDictionary<Guid, InventoryAvailability>> GetAvailabilityBatchAsync", start, StringComparison.Ordinal);
        Assert.True(end > start);
        var body = directory[start..end];
        Assert.Contains("Result.Failure", body, StringComparison.Ordinal);
        Assert.Contains("Result.Success()", body, StringComparison.Ordinal);
        Assert.DoesNotContain("throw new SemanticException", body, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (", body, StringComparison.Ordinal);
    }

    [Fact]
    public void Inventory_golden_boundaries_remain_clean()
    {
        Assert.DoesNotContain(ProjectRefs("Tooba.Inventory.Domain"), x => x.Contains("Tooba.Inventory.Contracts", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("TypeForwardedTo", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("OfferDbContext", StringComparison.Ordinal));
        Assert.DoesNotContain(Sources("Tooba.Inventory.Contracts"), x => x.Text.Contains("namespace Tooba.Inventory.Domain", StringComparison.Ordinal));
        var directory = File.ReadAllText(Path.Combine(InventoryRoot(), "Tooba.Inventory.Infrastructure", "InventoryDirectory.cs"));
        Assert.Contains("IModuleCallTracer", directory, StringComparison.Ordinal);
        Assert.Contains("IOfferLookupGateway", directory, StringComparison.Ordinal);
        Assert.Contains("IClock", directory, StringComparison.Ordinal);
        Assert.Contains("IIdGenerator", directory, StringComparison.Ordinal);
        var hostRoot = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host");
        var hostHits = Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(path => File.ReadAllText(path).Contains("InventoryDbContext", StringComparison.Ordinal))
            .ToList();
        Assert.Empty(hostHits);
        var bypass = AllProductionSources()
            .Where(x => x.Text.Contains("DateTimeOffset.UtcNow", StringComparison.Ordinal)
                        || x.Text.Contains("DateTime.UtcNow", StringComparison.Ordinal)
                        || x.Text.Contains("Guid.NewGuid()", StringComparison.Ordinal)
                        || x.Text.Contains("UuidV7.New()", StringComparison.Ordinal)
                        || x.Text.Contains("StartActivity(", StringComparison.Ordinal)
                        || x.Text.Contains("PlatformHttpException", StringComparison.Ordinal))
            .Select(x => x.Path)
            .ToList();
        Assert.True(bypass.Count == 0, string.Join("; ", bypass));
    }

    private static IEnumerable<(string Path, string Text)> AllProductionSources() =>
        Sources("Tooba.Inventory.Domain")
            .Concat(Sources("Tooba.Inventory.Application"))
            .Concat(Sources("Tooba.Inventory.Contracts"))
            .Concat(Sources("Tooba.Inventory.Infrastructure"));

    private static IEnumerable<(string Path, string Text)> Sources(string projectFolder)
    {
        var root = Path.Combine(InventoryRoot(), projectFolder);
        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var n = file.Replace('\\', '/');
            if (n.Contains("/bin/", StringComparison.Ordinal) || n.Contains("/obj/", StringComparison.Ordinal))
                continue;
            if (n.Contains("/Migrations/", StringComparison.OrdinalIgnoreCase) || n.EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase))
                continue;
            yield return (Path.GetRelativePath(RepoRoot(), file), File.ReadAllText(file));
        }
    }

    private static IReadOnlyList<string> ProjectRefs(string projectFolder)
    {
        var csproj = Path.Combine(InventoryRoot(), projectFolder, projectFolder + ".csproj");
        var doc = XDocument.Load(csproj);
        return doc.Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Where(x => x.Length > 0)
            .ToArray();
    }
}
