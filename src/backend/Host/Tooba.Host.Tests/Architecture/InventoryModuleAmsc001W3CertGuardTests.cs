using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-INVENTORY-AMSC-001-W3 — durable ARCH-COMPLETE-002 certification lock for Inventory:
/// SoT/manifest certification records with the AMSC wave lineage, the single stable-code owner and
/// bilingual resource sets, the typed-fault composition seam, the INTERNAL_ONLY boundary with zero
/// foreign Application/Infrastructure/Domain coupling, and the AMSC evidence tree.
/// </summary>
public sealed class InventoryModuleAmsc001W3CertGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/Inventory";

    private static readonly string[] ProductionProjects =
    [
        "Tooba.Inventory.Application",
        "Tooba.Inventory.Contracts",
        "Tooba.Inventory.Domain",
        "Tooba.Inventory.Infrastructure",
    ];

    [Fact]
    public void Inventory_is_arch_complete_002_certified_in_sot_and_manifest()
    {
        var root = Repo();

        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-module-structure-manifests.json")));
        var entries = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Where(m => string.Equals(m.GetProperty("module").GetString(), "Inventory", StringComparison.Ordinal))
            .ToArray();
        Assert.Single(entries);
        Assert.True(entries[0].GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", entries[0].GetProperty("lockVersion").GetString());
        Assert.Contains("TB-TMAR-INVENTORY-AMSC-001-W3", entries[0].GetProperty("certificationNote").GetString()!, StringComparison.Ordinal);
        Assert.Equal(5, entries[0].GetProperty("projects").GetArrayLength());
        Assert.DoesNotContain(
            manifest.RootElement.GetProperty("preCertModules").EnumerateArray(),
            m => string.Equals(m.GetProperty("module").GetString(), "Inventory", StringComparison.Ordinal));

        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));
        var w3 = sot.RootElement.GetProperty("inventoryModuleAmsc001W3");
        Assert.Equal("INVENTORY_AMSC_001_CERTIFIED", w3.GetProperty("state").GetString());
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", w3.GetProperty("verdict").GetString());
        Assert.Equal("ARCH-COMPLETE-002", w3.GetProperty("lockVersion").GetString());
        Assert.Equal("CERTIFIED", w3.GetProperty("structureState").GetString());
        Assert.True(w3.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("EXACT", w3.GetProperty("pathNamespaceState").GetString());
        Assert.Equal("ENFORCED", w3.GetProperty("rootAllowlistState").GetString());
        Assert.Equal("ZERO", w3.GetProperty("foreignAppInfraDomainCoupling").GetString());
        Assert.Equal("NOT_APPLICABLE_INTERNAL_ONLY", w3.GetProperty("endpointOwnershipState").GetString());
        Assert.Equal(0, w3.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal("ZERO", w3.GetProperty("blockingResidualDebt").GetString());
        Assert.True(w3.GetProperty("microserviceExtractable").GetBoolean());
        Assert.Equal("NONE", w3.GetProperty("automaticNextImplementationTask").GetString());
        Assert.Equal("PRESERVED", w3.GetProperty("hostFinalClosureState").GetString());
        Assert.Equal("PRESERVED_OFFER_OWNED", w3.GetProperty("offerHttpSurfaceState").GetString());

        var certified = sot.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Equal(1, certified.Count(x => x == "Inventory"));

        // Wave lineage is recorded with its own commit SHAs (each wave's starting head is the parent
        // wave's commit, so the chain is verifiable end to end).
        Assert.Equal("c6917553", sot.RootElement.GetProperty("inventoryModuleAmsc001W0").GetProperty("commit").GetString());
        Assert.Equal("c6917553", sot.RootElement.GetProperty("inventoryModuleAmsc001W1").GetProperty("startingHead").GetString());
        Assert.Equal("133d413d", sot.RootElement.GetProperty("inventoryModuleAmsc001W2").GetProperty("startingHead").GetString());
        Assert.Equal("87101cb4", sot.RootElement.GetProperty("inventoryModuleAmsc001W3").GetProperty("startingHead").GetString());
        Assert.Equal("TB-TMAR-INVENTORY-AMSC-001-W2", sot.RootElement.GetProperty("inventoryModuleAmsc001W3").GetProperty("parentTask").GetString());
    }

    [Fact]
    public void Inventory_has_zero_hardcoded_fault_text_and_one_stable_code_owner()
    {
        var root = Repo();

        var production = ProductionSources(root).ToArray();
        Assert.NotEmpty(production);

        foreach (var file in production)
        {
            var text = File.ReadAllText(file).TrimStart('\uFEFF');
            // No raw string-literal fault payload; every fault carries a stable code. The single legal
            // exception is a cross-module code owned by Order that Inventory merely surfaces unchanged.
            Assert.DoesNotMatch(new Regex("InvalidOperationException\\(\"[^\"]+\"\\)"), text);
            var literals = Regex.Matches(text, "ContractOperationException\\(\"(?<code>[^\"]+)\"\\)")
                .Select(m => m.Groups["code"].Value)
                .Where(code => !code.StartsWith("order.", StringComparison.Ordinal))
                .ToArray();
            Assert.Empty(literals);
        }

        // The Domain consumes Contracts-owned codes but declares no code constant of its own.
        var domain = string.Join("\n", production
            .Where(p => p.Contains($"{Path.DirectorySeparatorChar}Tooba.Inventory.Domain{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(File.ReadAllText));
        Assert.DoesNotContain("public const string", domain, StringComparison.Ordinal);

        // Exactly one declared stable-code owner, and every declared code is registered + localized.
        var codesFile = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Inventory.Contracts", "Errors", "InventoryErrorCodes.cs"));
        var declared = Regex.Matches(codesFile, "public const string \\w+ = \"(?<c>[^\"]+)\"")
            .Select(m => m.Groups["c"].Value).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(29, declared.Length);
        Assert.DoesNotContain(declared, c => c.Contains("retry_limit_reached", StringComparison.Ordinal));
        Assert.DoesNotContain(declared, c => c.StartsWith("inventory.recovery.", StringComparison.Ordinal));

        var contributor = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Inventory.Contracts", "Errors", "InventoryErrorCatalogContributor.cs"));
        var names = Regex.Matches(codesFile, "public const string (?<n>\\w+)")
            .Select(m => m.Groups["n"].Value).ToArray();
        var descriptors = Regex.Matches(contributor, "D\\(InventoryErrorCodes\\.(?<n>\\w+)")
            .Select(m => m.Groups["n"].Value).ToArray();
        Assert.Equal(29, descriptors.Length);
        Assert.Equal(names.Length, descriptors.Distinct(StringComparer.Ordinal).Count());
        foreach (var name in names)
        {
            Assert.Contains(name, descriptors);
        }

        foreach (var culture in new[] { "InventoryErrors.resx", "InventoryErrors.fa.resx" })
        {
            var resx = File.ReadAllText(Path.Combine(
                root, ModuleRoot, "Tooba.Inventory.Contracts", "Resources", culture));
            foreach (var code in declared)
            {
                Assert.Contains($"name=\"{code}\"", resx, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Inventory_typed_fault_seam_and_catalog_registration_are_canonical()
    {
        var root = Repo();

        var seam = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Inventory.Application", "Composition", "InventoryOperation.cs"));
        Assert.Contains("public static async Task<Result<T>> ExecuteAsync<T>", seam, StringComparison.Ordinal);
        Assert.Contains("public static async Task<Result> ExecuteAsync", seam, StringComparison.Ordinal);
        Assert.Contains("catch (ContractOperationException ex) when (InventoryErrorCodes.IsKnown(ex.Code))", seam, StringComparison.Ordinal);
        Assert.Contains("catch (SemanticException ex)", seam, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Inventory.Infrastructure", "DependencyInjection", "InventoryModule.cs"));
        Assert.Contains("IErrorCatalogContributor, InventoryErrorCatalogContributor", module, StringComparison.Ordinal);
        Assert.Contains("IErrorResourceSet, InventoryErrorResourceSet", module, StringComparison.Ordinal);

        var resourceSet = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Inventory.Contracts", "Errors", "InventoryErrorResourceSet.cs"));
        Assert.Contains("\"inventory.\"", resourceSet, StringComparison.Ordinal);
        Assert.Contains("InventoryErrorResources.Manager", resourceSet, StringComparison.Ordinal);
    }

    [Fact]
    public void Inventory_is_internal_only_and_offer_owned_with_zero_foreign_coupling()
    {
        var root = Repo();

        // INTERNAL_ONLY: no Endpoints project, no Host Inventory folder, no module HTTP mapping.
        Assert.False(Directory.Exists(Path.Combine(root, ModuleRoot, "Tooba.Inventory.Endpoints")));
        Assert.False(Directory.Exists(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Inventory")));

        // Offer owns the seller HTTP surface and consumes the Inventory Contracts port only.
        var offerEndpoint = File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Offer",
            "Tooba.Offer.Endpoints", "Seller", "OfferSellerEndpoints.cs"));
        Assert.Contains("/offers/{offerId:guid}/inventory", offerEndpoint, StringComparison.Ordinal);
        Assert.Contains("SetOfferInventoryCommand", offerEndpoint, StringComparison.Ordinal);
        var offerHandler = File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Offer",
            "Tooba.Offer.Application", "Offers", "Commands", "SetOfferInventory", "SetOfferInventoryCommand.cs"));
        Assert.Contains("using Tooba.Inventory.Contracts.Seller;", offerHandler, StringComparison.Ordinal);
        Assert.Contains("ISellerOfferInventoryGateway", offerHandler, StringComparison.Ordinal);
        Assert.DoesNotContain("InventoryDbContext", offerHandler, StringComparison.Ordinal);

        // Microservice extractability: no foreign Application/Infrastructure/Domain project edge in any
        // Inventory project; the only foreign references are the legal Contracts seams.
        foreach (var project in ProductionProjects)
        {
            var csproj = XDocument.Load(Path.Combine(root, ModuleRoot, project, project + ".csproj"));
            var refs = csproj.Descendants("ProjectReference")
                .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
                .ToArray();
            foreach (var reference in refs)
            {
                var foreign = !reference.Contains($"Tooba.Inventory.", StringComparison.Ordinal);
                if (!foreign)
                {
                    continue;
                }

                Assert.DoesNotContain(".Application/", reference, StringComparison.Ordinal);
                Assert.DoesNotContain(".Infrastructure/", reference, StringComparison.Ordinal);
                Assert.DoesNotContain(".Domain/", reference, StringComparison.Ordinal);
                Assert.DoesNotContain(".Endpoints/", reference, StringComparison.Ordinal);
            }
        }

        // No cross-module EF/SQL join and no foreign DbContext reach-through.
        var joined = string.Join("\n", ProductionSources(root).Select(File.ReadAllText));
        Assert.DoesNotContain("OfferDbContext", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("TypeForwardedTo", joined, StringComparison.Ordinal);
    }

    [Fact]
    public void Inventory_host_closure_and_evidence_are_preserved()
    {
        var root = Repo();

        foreach (var wave in new[] { "W0", "W1", "W2", "W3" })
        {
            Assert.True(Directory.Exists(Path.Combine(
                root, "docs", "architecture", "evidence", $"TB-TMAR-INVENTORY-AMSC-001-{wave}")));
        }

        var w3Evidence = File.ReadAllText(Path.Combine(
            root, "docs", "architecture", "evidence", "TB-TMAR-INVENTORY-AMSC-001-W3", "certification.md"));
        Assert.Contains("ARCH-COMPLETE-002", w3Evidence, StringComparison.Ordinal);
        Assert.Contains("COMPLETE_REFERENCE_PATTERN", w3Evidence, StringComparison.Ordinal);

        // Repository-global Host root checkpoint must remain untouched by this module-local task.
        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));
        Assert.Equal("TB-TMAR-HOST-ROOT-FINAL-CERT-001", sot.RootElement.GetProperty("lastAcceptedTask").GetString());
        Assert.Equal("NONE", sot.RootElement.GetProperty("automaticNextImplementationTask").GetString());
        Assert.Equal("NONE", sot.RootElement.GetProperty("inventoryModuleAmsc001W3").GetProperty("automaticNextImplementationTask").GetString());

        // Master Recovery records the Inventory certification checkpoint.
        var recovery = File.ReadAllText(Path.Combine(root, "docs/architecture", "TOOBA-TMAR-MASTER-RECOVERY.md"));
        Assert.Contains("Inventory AMSC module recovery checkpoint", recovery, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-INVENTORY-AMSC-001-W3", recovery, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_INVENTORY_AMSC_001_W3", recovery, StringComparison.Ordinal);
    }

    private static IEnumerable<string> ProductionSources(string root) =>
        ProductionProjects
            .SelectMany(p => Directory.EnumerateFiles(
                Path.Combine(root, ModuleRoot, p), "*.cs", SearchOption.AllDirectories))
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !Path.GetFileName(p).EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase));

    private static string Repo()
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
