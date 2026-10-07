using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-PAGECOMPOSITION-AMSC-001-W3 — ARCH-COMPLETE-002 certification lock.
/// Pins the AMSC certification lineage (W0 analyze/W1 migrate/W2 structure SHAs + certified verdict),
/// the W1 canonical seam set (declared-code guard + dual-mechanism typed-fault seam), the exhaustive
/// 8/8 validator matrix, module-owned HTTP ownership, contracts-only/self-contained boundaries with
/// own-schema persistence, Host closure and the certified manifest/SoT state.
/// </summary>
public sealed class PageCompositionModuleAmsc001W3CertGuardTests
{
    [Fact]
    public void PageComposition_is_amsc001_certified_in_manifest_and_sot()
    {
        var root = Repo();

        using var manifest = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-module-structure-manifests.json"))
                .Replace("\uFEFF", string.Empty));
        var moduleEntry = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Single(m => m.GetProperty("module").GetString() == "PageComposition");
        Assert.True(moduleEntry.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", moduleEntry.GetProperty("lockVersion").GetString());
        Assert.Contains("TB-TMAR-PAGECOMPOSITION-AMSC-001", moduleEntry.GetProperty("certificationNote").GetString(), StringComparison.Ordinal);

        using var sot = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"))
                .Replace("\uFEFF", string.Empty));
        var block = sot.RootElement.GetProperty("pageCompositionAmsc001W3");
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", block.GetProperty("verdict").GetString());
        Assert.Equal("ARCH-COMPLETE-002", block.GetProperty("lockVersion").GetString());
        Assert.True(block.GetProperty("structureCertified").GetBoolean());
        Assert.True(block.GetProperty("microserviceExtractable").GetBoolean());

        var certified = sot.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Equal(1, certified.Count(x => x == "PageComposition"));
    }

    [Fact]
    public void Amsc_lineage_pins_analyze_migrate_and_structure_shas()
    {
        using var sot = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(Repo(), "docs/architecture/tmar-current-state.json"))
                .Replace("\uFEFF", string.Empty));

        var w1 = sot.RootElement.GetProperty("pageCompositionAmsc001W1");
        Assert.Equal("MIGRATE_COMPLETE", w1.GetProperty("state").GetString());
        Assert.Equal("6a28c921", w1.GetProperty("commit").GetString());
        Assert.Equal("READY_FOR_STRUCTURE", w1.GetProperty("verdict").GetString());

        var w2 = sot.RootElement.GetProperty("pageCompositionAmsc001W2");
        Assert.Equal("STRUCTURE_READY_FOR_CERTIFY", w2.GetProperty("state").GetString());
        Assert.Equal("361a1837", w2.GetProperty("commit").GetString());
        Assert.Equal("READY_FOR_CERTIFY", w2.GetProperty("verdict").GetString());
        Assert.Equal("PROFESSIONAL_SHALLOW", w2.GetProperty("folderGranularityState").GetString());
        Assert.Equal("EXACT", w2.GetProperty("pathNamespaceState").GetString());
        Assert.Equal("ENFORCED", w2.GetProperty("rootAllowlistState").GetString());
    }

    [Fact]
    public void Canonical_seams_are_present_and_single_owned()
    {
        var root = Repo();

        var codes = File.ReadAllText(Src(root, "Tooba.PageComposition.Contracts/Errors/PageCompositionErrorCodes.cs"));
        Assert.Contains("public static bool IsKnown(string? code)", codes, StringComparison.Ordinal);
        Assert.Equal(8, Regex.Matches(codes, "public const string ").Count);

        var operation = File.ReadAllText(Src(root, "Tooba.PageComposition.Application/Composition/PageCompositionOperation.cs"));
        Assert.Contains("catch (ContractOperationException ex) when (PageCompositionErrorCodes.IsKnown(ex.Code))", operation, StringComparison.Ordinal);
        Assert.Contains("catch (SemanticException ex)", operation, StringComparison.Ordinal);
        Assert.Contains("public static async Task<Result> ExecuteAsync(Func<Task> action)", operation, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", operation, StringComparison.Ordinal);

        // Exactly one catalog contributor and one resource set, registered by the module composition.
        Assert.Equal(
            new[] { "PageCompositionErrorCatalogContributor.cs" },
            Directory.EnumerateFiles(Src(root, "Tooba.PageComposition.Contracts/Errors"), "*Contributor.cs").Select(Path.GetFileName).ToArray());
        var module = File.ReadAllText(Src(root, "Tooba.PageComposition.Infrastructure/PageCompositionModule.cs"));
        Assert.Contains("IErrorCatalogContributor, PageCompositionErrorCatalogContributor>", module, StringComparison.Ordinal);
        Assert.Contains("IErrorResourceSet, PageCompositionErrorResourceSet>", module, StringComparison.Ordinal);

        // Bilingual resource pair exists for the page-composition. keyspace.
        Assert.True(File.Exists(Src(root, "Tooba.PageComposition.Contracts/Resources/PageCompositionErrors.resx")));
        Assert.True(File.Exists(Src(root, "Tooba.PageComposition.Contracts/Resources/PageCompositionErrors.fa.resx")));
        var en = File.ReadAllText(Src(root, "Tooba.PageComposition.Contracts/Resources/PageCompositionErrors.resx"));
        var fa = File.ReadAllText(Src(root, "Tooba.PageComposition.Contracts/Resources/PageCompositionErrors.fa.resx"));
        foreach (var key in new[] { "page-composition.tenant.missing", "page-composition.section.missing", "page-composition.section-type.rejected", "page-composition.config.rejected", "page-composition.mutation.rejected", "page-composition.sectionType.required", "page-composition.sectionIds.required", "page-composition.sectionId.required" })
        {
            Assert.Contains($"\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"\"{key}\"", fa, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Validator_matrix_is_exhaustive_for_all_eight_endpoint_reachable_requests()
    {
        var root = Repo();

        // Six Admin validators + two Storefront validators, all stable-code emitting.
        var adminValidators = File.ReadAllText(Src(root, "Tooba.PageComposition.Application/Admin/Validators/PageCompositionValidators.cs"));
        foreach (var validator in new[]
        {
            "AbstractValidator<AdminAddHomeSectionCommand>",
            "AbstractValidator<AdminReorderHomeSectionsCommand>",
            "AbstractValidator<AdminUpdateHomeSectionCommand>",
            "AbstractValidator<AdminRemoveHomeSectionCommand>",
            "AbstractValidator<AdminRestoreDefaultHomeCompositionCommand>",
            "AbstractValidator<AdminGetHomeCompositionQuery>"
        })
        {
            Assert.Contains(validator, adminValidators, StringComparison.Ordinal);
        }

        var storefrontValidators = File.ReadAllText(Src(root, "Tooba.PageComposition.Application/Storefront/Validators/StorefrontCompositionValidators.cs"));
        Assert.Contains("AbstractValidator<GetHomeCompositionQuery>", storefrontValidators, StringComparison.Ordinal);
        Assert.Contains("AbstractValidator<GetSectionCatalogQuery>", storefrontValidators, StringComparison.Ordinal);

        // Every request type is endpoint-reachable via ISender and returns Result<T> through the seam.
        var adminEndpoints = File.ReadAllText(Src(root, "Tooba.PageComposition.Endpoints/Admin/PageCompositionAdminEndpoints.cs"));
        var storefrontEndpoints = File.ReadAllText(Src(root, "Tooba.PageComposition.Endpoints/Storefront/PageCompositionStorefrontEndpoints.cs"));
        Assert.Equal(7, Regex.Matches(adminEndpoints, "admin\\.Map").Count);
        Assert.Equal(1, Regex.Matches(storefrontEndpoints, "app\\.Map").Count);
        Assert.Contains("sender.Send(", adminEndpoints, StringComparison.Ordinal);
        Assert.Contains("sender.Send(", storefrontEndpoints, StringComparison.Ordinal);
        Assert.Contains("api.From(", adminEndpoints, StringComparison.Ordinal);
        Assert.Contains("api.From(", storefrontEndpoints, StringComparison.Ordinal);
    }

    [Fact]
    public void Boundaries_stay_self_contained_and_persistence_is_module_owned()
    {
        var root = Repo();
        var foreignPattern = "Tooba\\.(Order|Cart|Catalog|Identity|AccessControl|Content|CustomerProfile|Notification|Media|Party|Fulfillment|Inventory|BulkInquiry|Localization|Payment|Settlement|Story|Wishlist|UserPreference|ProductQnA|Reviews|OperatorProfile)\\.(Application|Infrastructure|Domain|Endpoints)";
        foreach (var project in new[] { "Contracts", "Domain", "Application", "Infrastructure", "Endpoints" })
        {
            var projectDir = Src(root, $"Tooba.PageComposition.{project}");
            foreach (var file in Directory.EnumerateFiles(projectDir, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                Assert.False(
                    Regex.IsMatch(File.ReadAllText(file), foreignPattern),
                    $"foreign module reference in {file}");
            }
        }

        // Own schema + own DbContext; the only DbSet triple is module entities + own outbox.
        var db = File.ReadAllText(Src(root, "Tooba.PageComposition.Infrastructure/Persistence/PageCompositionDbContext.cs"));
        Assert.Contains("\"page_composition\"", db, StringComparison.Ordinal);
        Assert.Contains("DbSet<PageDefinition> PageDefinitions", db, StringComparison.Ordinal);
        Assert.Contains("DbSet<PageSection> PageSections", db, StringComparison.Ordinal);
        Assert.Contains("DbSet<OutboxMessage> OutboxMessages", db, StringComparison.Ordinal);
        foreach (var foreignNs in new[] { "Tooba.Order.", "Tooba.Cart.", "Tooba.Catalog.", "Tooba.Identity.", "Tooba.AccessControl.", "Tooba.Content.", "Tooba.CustomerProfile.", "Tooba.Notification.", "Tooba.Media.", "Tooba.Party.", "Tooba.Fulfillment.", "Tooba.Inventory.", "Tooba.Localization.", "Tooba.Payment.", "Tooba.Settlement." })
        {
            Assert.DoesNotContain(foreignNs, db, StringComparison.Ordinal);
        }

        // Host closure: no Host PageComposition folder; module authorizer stays module-owned.
        Assert.False(Directory.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/PageComposition")));
        var authorizer = File.ReadAllText(Src(root, "Tooba.PageComposition.Endpoints/Admin/IPageCompositionAdminAuthorizer.cs"));
        Assert.Contains("interface IPageCompositionAdminAuthorizer", authorizer, StringComparison.Ordinal);
        Assert.Contains("class PageCompositionAdminAuthorizer", authorizer, StringComparison.Ordinal);
    }

    private static string Src(string root, string relative) => Path.Combine(
        root, "src", "backend", "Modules", "PageComposition", relative.Replace('/', Path.DirectorySeparatorChar));

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
