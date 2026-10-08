using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-PROMOTION-AMSC-001-W3 — durable ARCH-COMPLETE-002 certification lock for the Promotion module:
/// SoT/manifest certification truth, the single canonical stable-code owner with its reachability split,
/// the exhaustive endpoint/validator matrix, the Contracts-only boundary with own-schema persistence and
/// the preserved Host closure plus the wave evidence.
/// </summary>
public sealed class PromotionModuleAmsc001W3CertGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/Promotion";

    private static readonly string[] ProductionProjects =
    [
        "Tooba.Promotion.Contracts",
        "Tooba.Promotion.Domain",
        "Tooba.Promotion.Application",
        "Tooba.Promotion.Infrastructure",
    ];

    [Fact]
    public void Promotion_is_arch_complete_002_certified_in_sot_and_manifest()
    {
        var root = Repo();

        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-module-structure-manifests.json")));
        var entries = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Where(m => m.GetProperty("module").GetString() == "Promotion").ToArray();
        Assert.Single(entries);
        Assert.True(entries[0].GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", entries[0].GetProperty("lockVersion").GetString());
        Assert.Contains("TB-TMAR-PROMOTION-AMSC-001", entries[0].GetProperty("certificationNote").GetString()!, StringComparison.Ordinal);
        Assert.Equal(6, entries[0].GetProperty("projects").GetArrayLength());

        // Promotion is a move, not a copy: neither the pre-cert slot nor the uncertified list keeps it.
        Assert.DoesNotContain(
            manifest.RootElement.GetProperty("preCertModules").EnumerateArray(),
            m => m.GetProperty("module").GetString() == "Promotion");
        Assert.DoesNotContain(
            "Promotion",
            manifest.RootElement.GetProperty("uncertifiedHttpOwningModules").EnumerateArray()
                .Select(x => x.GetString()!).ToArray());

        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));
        var w3 = sot.RootElement.GetProperty("promotionAmsc001W3");
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", w3.GetProperty("verdict").GetString());
        Assert.Equal("CERTIFICATION_SUPERSEDED_STRUCTURE_VERIFIED", w3.GetProperty("state").GetString());
        Assert.Equal("ARCH-COMPLETE-002", w3.GetProperty("lockVersion").GetString());
        Assert.Equal("EXACT", w3.GetProperty("pathNamespaceState").GetString());
        Assert.Equal("ENFORCED", w3.GetProperty("rootAllowlistState").GetString());
        Assert.Equal("ZERO", w3.GetProperty("foreignAppInfraDomainCoupling").GetString());
        Assert.Equal("MODULE_OWNED_21_ROUTES_HOST_ZERO", w3.GetProperty("endpointOwnershipState").GetString());
        Assert.Equal(21, w3.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal("EXHAUSTIVE_19_VALIDATOR_REQUIRED_2_NO_VALIDATOR_REQUIRED", w3.GetProperty("validatorMatrixState").GetString());
        Assert.Equal("ZERO", w3.GetProperty("blockingResidualDebt").GetString());
        Assert.True(w3.GetProperty("microserviceExtractable").GetBoolean());
        Assert.Equal("NONE", w3.GetProperty("automaticNextImplementationTask").GetString());
        Assert.Equal("PRESERVED", w3.GetProperty("hostFinalClosureState").GetString());

        var certified = sot.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()!).ToArray();
        Assert.Equal(1, certified.Count(x => x == "Promotion"));

        // W3-R1 independently found the W3 validator classification false, so the W3 certification verdict
        // is explicitly BLOCKED / superseded pending a fresh Certify wave. Structure stays verified.
        Assert.Equal("BLOCKED_SUPERSEDED_BY_W3_R1_PENDING_FRESH_CERTIFY", w3.GetProperty("certificationState").GetString());
        Assert.Equal("BLOCKED_PENDING_FRESH_CERTIFY", w3.GetProperty("verdictState").GetString());
        Assert.Equal("TB-TMAR-PROMOTION-AMSC-001-W3-R1", w3.GetProperty("supersededByTask").GetString());
        Assert.Equal("TB-TMAR-PROMOTION-AMSC-001-W3-R2", w3.GetProperty("repairTask").GetString());
        Assert.Equal("EXHAUSTIVE_19_VALIDATOR_REQUIRED_2_NO_VALIDATOR_REQUIRED", w3.GetProperty("validatorMatrixState").GetString());
        Assert.Equal("PENDING_FRESH_CERTIFY", w3.GetProperty("freshCertifyState").GetString());
        Assert.True(entries[0].GetProperty("certificationState").GetString() == "BLOCKED_SUPERSEDED_BY_W3_R1_PENDING_FRESH_CERTIFY");

        // Wave lineage is recorded with its own commit SHAs (W0/W1 were recorded as result-only and are
        // reconciled here from the accepted wave commits).
        Assert.Equal("d00cf666", sot.RootElement.GetProperty("promotionAmsc001W0").GetProperty("commit").GetString());
        Assert.Equal("06858933", sot.RootElement.GetProperty("promotionAmsc001W1").GetProperty("commit").GetString());
        Assert.Equal("597147ea", sot.RootElement.GetProperty("promotionAmsc001W2").GetProperty("commit").GetString());
    }

    [Fact]
    public void Promotion_has_one_stable_code_owner_with_unique_descriptors_and_bilingual_keys()
    {
        var root = Repo();

        // Exactly one canonical PromotionErrorCodes declaration in production.
        var declarations = ProductionProjects
            .SelectMany(ProductionSources)
            .Count(file => File.ReadAllText(file).Contains("class PromotionErrorCodes", StringComparison.Ordinal));
        Assert.Equal(1, declarations);

        var codesFile = Path.Combine(root, ModuleRoot, "Tooba.Promotion.Contracts", "Errors", "PromotionErrorCodes.cs");
        Assert.Contains("namespace Tooba.Promotion.Contracts.Errors", File.ReadAllText(codesFile), StringComparison.Ordinal);

        // Declared-code identity is unique and every declared code is IsKnown.
        var declared = typeof(Tooba.Promotion.Contracts.Errors.PromotionErrorCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToArray();
        Assert.Equal(40, declared.Length);
        Assert.Equal(declared.Length, declared.Distinct(StringComparer.Ordinal).Count());
        Assert.All(declared, code => Assert.True(
            Tooba.Promotion.Contracts.Errors.PromotionErrorCodes.IsKnown(code), code));
        Assert.All(declared, code => Assert.DoesNotContain("authorization.denied", code, StringComparison.Ordinal));

        // Reachability split: 13 HTTP-reachable descriptors registered exactly once + 27 domain invariants.
        var contributor = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Promotion.Endpoints", "Errors", "PromotionErrorCatalogContributor.cs"));
        Assert.Equal(
            13,
            Regex.Matches(contributor, @"D\(PromotionErrorCodes\.").Count);
        Assert.Equal(40, declared.Length);

        // Every declared code resolves in both bilingual resource sets.
        foreach (var resx in new[] { "PromotionErrors.resx", "PromotionErrors.fa.resx" })
        {
            var content = File.ReadAllText(Path.Combine(
                root, ModuleRoot, "Tooba.Promotion.Contracts", "Resources", resx));
            foreach (var code in declared)
            {
                Assert.Contains($"name=\"{code}\"", content, StringComparison.Ordinal);
            }
        }

        // The resource set is registered exactly once by the module composition entry.
        var module = File.ReadAllText(Path.Combine(root, ModuleRoot, "Tooba.Promotion.Endpoints", "PromotionEndpointModule.cs"));
        Assert.Equal(1, Regex.Matches(module, "IErrorResourceSet, PromotionErrorResourceSet").Count);
    }

    [Fact]
    public void Promotion_endpoint_and_validator_matrix_is_exhaustive()
    {
        var root = Repo();
        var endpoints = Path.Combine(root, ModuleRoot, "Tooba.Promotion.Endpoints");

        // 21 module-owned routes across the three route groups.
        var seller = File.ReadAllText(Path.Combine(endpoints, "Seller", "PromotionSellerEndpoints.cs"));
        var admin = File.ReadAllText(Path.Combine(endpoints, "Admin", "PromotionAdminEndpoints.cs"));
        var merch = File.ReadAllText(Path.Combine(endpoints, "Admin", "MerchandisingCampaignAdminEndpoints.cs"));
        var routeMaps = Regex.Matches(seller, @"\bg\.Map(Get|Post|Put|Delete)\(").Count
            + Regex.Matches(admin, @"\bg\.Map(Get|Post|Put|Delete)\(").Count
            + Regex.Matches(merch, @"\bgroup\.Map(Get|Post|Put|Delete)\(").Count;
        Assert.Equal(21, routeMaps);

        // Every route is dispatched through ISender and never through a concrete handler.
        var joined = seller + admin + merch;
        Assert.Contains("ISender", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("IRequestHandler", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("SendAsync", joined, StringComparison.Ordinal);
        Assert.Equal(21, Regex.Matches(joined, @"await (sender|s)\.Send\(new ").Count);

        // The validator matrix is EXHAUSTIVE: 19 validators cover the 19 shape-bearing requests; the 2
        // remaining endpoint-reachable requests take no malformable transport shape. The third historical
        // exemption (ListMerchandisingCampaignTypesQuery) was a W3 classification defect repaired by
        // TB-TMAR-PROMOTION-AMSC-001-W3-R2 because that read binds a client-supplied locale.
        var validatorFile = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Promotion.Application", "Validation", "PromotionRequestValidators.cs"));
        var validatorTypes = Regex.Matches(validatorFile, @"public sealed class (?<n>\w+Validator) : AbstractValidator<")
            .Select(m => m.Groups["n"].Value).ToArray();
        Assert.Equal(19, validatorTypes.Length);
        Assert.Equal(validatorTypes.Length, validatorTypes.Distinct(StringComparer.Ordinal).Count());

        // Exactly the 2 unvalidated requests are the ones with no malformable transport shape.
        var validatedRequests = Regex.Matches(validatorFile, @"AbstractValidator<(?<t>\w+)>")
            .Select(m => m.Groups["t"].Value).ToHashSet(StringComparer.Ordinal);
        Assert.Equal(19, validatedRequests.Count);
        Assert.DoesNotContain("ListSellerPromotionsQuery", validatedRequests);
        Assert.DoesNotContain("ListAdminPromotionsQuery", validatedRequests);
        Assert.Contains("ListMerchandisingCampaignTypesQuery", validatedRequests);
        Assert.Equal(21, validatedRequests.Count + 2);

        // The repaired locale-bearing read is covered by the canonical locale code only.
        Assert.Contains("ListMerchandisingCampaignTypesQueryValidator", validatorFile, StringComparison.Ordinal);
        Assert.Contains("PromotionValidationCodes.LocaleInvalid", validatorFile, StringComparison.Ordinal);

        // Validators emit machine codes only and never duplicate a business rule.
        Assert.DoesNotContain("WithMessage(", validatorFile, StringComparison.Ordinal);
        Assert.Contains("WithErrorCode(", validatorFile, StringComparison.Ordinal);

        // Canonical API results: no raw Results.* payload mapping remains in the module.
        foreach (var file in Directory.EnumerateFiles(endpoints, "*.cs", SearchOption.AllDirectories)
                     .Where(f => !IsBuildOutput(f)))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Results.Json(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.BadRequest(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.Problem(", text, StringComparison.Ordinal);
        }

        Assert.Contains("api.Created(", seller, StringComparison.Ordinal);
    }

    [Fact]
    public void Promotion_boundary_persistence_and_schema_are_module_owned()
    {
        var root = Repo();
        var endpointsCsproj = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Promotion.Endpoints", "Tooba.Promotion.Endpoints.csproj"));
        Assert.DoesNotContain("Tooba.Promotion.Infrastructure", endpointsCsproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Promotion.Domain", endpointsCsproj, StringComparison.Ordinal);

        // Contracts carries no foreign module type and no foreign module reference.
        var contractsCsproj = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Promotion.Contracts", "Tooba.Promotion.Contracts.csproj"));
        foreach (var foreign in new[]
                 {
                     "Tooba.Offer.Contracts", "Tooba.Pricing.Contracts",
                     "Tooba.Inventory.Contracts", "Tooba.Catalog.Contracts", "Tooba.Party.Contracts",
                 })
        {
            Assert.DoesNotContain(foreign, contractsCsproj, StringComparison.Ordinal);
        }

        foreach (var file in ProductionSources("Tooba.Promotion.Contracts"))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("using Tooba.Offer", text, StringComparison.Ordinal);
            Assert.DoesNotContain("using Tooba.Pricing", text, StringComparison.Ordinal);
            Assert.DoesNotContain("using Tooba.Inventory", text, StringComparison.Ordinal);
        }

        // No foreign Application/Infrastructure/Domain edge anywhere in Promotion production.
        foreach (var project in ProductionProjects)
        {
            foreach (var file in ProductionSources(project))
            {
                var text = File.ReadAllText(file);
                foreach (var foreign in new[]
                         {
                             "Tooba.Offer.Application", "Tooba.Offer.Domain",
                             "Tooba.Pricing.Application", "Tooba.Pricing.Domain",
                             "Tooba.Inventory.Application", "Tooba.Inventory.Domain",
                             "Tooba.Catalog.Application", "Tooba.Catalog.Domain",
                             "Tooba.Party.Application", "Tooba.Party.Domain",
                         })
                {
                    Assert.DoesNotContain(foreign, text, StringComparison.Ordinal);
                }
            }
        }

        // The module owns exactly one DbContext with its own schema and its two unchanged migrations.
        var dbContexts = Directory.EnumerateFiles(Path.Combine(root, ModuleRoot), "*DbContext*.cs", SearchOption.AllDirectories)
            .Where(f => !IsBuildOutput(f))
            .Where(f => !IsEfMigrationOrSnapshot(f))
            .ToArray();
        Assert.Single(dbContexts);
        var dbContext = File.ReadAllText(dbContexts[0]);
        Assert.Contains("public const string Schema = \"promotion\"", dbContext, StringComparison.Ordinal);
        Assert.Contains("HasDefaultSchema(Schema)", dbContext, StringComparison.Ordinal);

        var migrations = Directory.EnumerateFiles(
                Path.Combine(root, ModuleRoot, "Tooba.Promotion.Infrastructure", "Persistence", "Migrations"), "*.cs")
            .Where(f => !f.EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase))
            .Where(f => !f.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .Select(Path.GetFileName).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(
            ["20260823210000_InitialPromotion.cs", "20260919134400_MerchandisingCampaignFoundation.cs"],
            migrations);
    }

    [Fact]
    public void Promotion_host_closure_and_evidence_are_preserved()
    {
        var root = Repo();

        // Host keeps only the composition root: DI registration, the module list and the route map call.
        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("AddPromotionEndpointPresentation()", program, StringComparison.Ordinal);
        Assert.Contains("MapPromotionEndpoints()", program, StringComparison.Ordinal);
        Assert.DoesNotContain("/v1/seller/promotions", program, StringComparison.Ordinal);
        Assert.DoesNotContain("/v1/admin/promotions", program, StringComparison.Ordinal);
        Assert.DoesNotContain("/v1/admin/merchandising-campaigns", program, StringComparison.Ordinal);

        // No Host/Promotion production folder.
        var hostPromotion = Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Promotion");
        Assert.True(!Directory.Exists(hostPromotion)
            || Directory.GetFiles(hostPromotion, "*.cs", SearchOption.AllDirectories)
                .Where(f => !IsBuildOutput(f)).Count() == 0);

        foreach (var wave in new[] { "W0", "W1", "W2", "W3" })
        {
            Assert.True(Directory.Exists(Path.Combine(
                root, "docs", "architecture", "evidence", $"TB-TMAR-PROMOTION-AMSC-001-{wave}")));
        }

        var w3Evidence = File.ReadAllText(Path.Combine(
            root, "docs", "architecture", "evidence", "TB-TMAR-PROMOTION-AMSC-001-W3", "certify.md"));
        Assert.Contains("ARCH-COMPLETE-002", w3Evidence, StringComparison.Ordinal);
        Assert.Contains("COMPLETE_REFERENCE_PATTERN", w3Evidence, StringComparison.Ordinal);
    }

    [Fact]
    public void Promotion_master_recovery_records_the_amsc_lineage()
    {
        var recovery = File.ReadAllText(Path.Combine(
            Repo(), "docs", "architecture", "TOOBA-TMAR-MASTER-RECOVERY.md"));
        Assert.Contains("TB-TMAR-PROMOTION-AMSC-001", recovery, StringComparison.Ordinal);
        Assert.Contains("promotion AMSC W3 certify", recovery, StringComparison.OrdinalIgnoreCase);
    }

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

    private static IEnumerable<string> ProductionSources(string projectRelativePath)
    {
        var root = Path.Combine(Repo(), "src", "backend", "Modules", "Promotion",
            projectRelativePath.Replace('/', Path.DirectorySeparatorChar));
        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !IsBuildOutput(f))
            .Where(f => !IsEfMigrationOrSnapshot(f));
    }

    private static bool IsEfMigrationOrSnapshot(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase);

    private static bool IsBuildOutput(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
}
