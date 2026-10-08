using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-PROMOTION-AMSC-001-W3-R3 — durable fresh-certification lock for the Promotion module.
/// It re-derives the shipped route/request universe and the validator set from disk and proves exact set
/// equality (not a validator count), then locks the current certification authorities, the manifest
/// certification truth, the own-schema persistence, the preserved Host checkpoint and the canonical
/// outcome. It is additive: it never replaces or weakens the W3/W3-R2 assertions.
/// </summary>
public sealed class PromotionModuleAmsc001W3R3CertGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/Promotion";

    private static readonly string[] EndpointFiles =
    [
        "Tooba.Promotion.Endpoints/Seller/PromotionSellerEndpoints.cs",
        "Tooba.Promotion.Endpoints/Admin/PromotionAdminEndpoints.cs",
        "Tooba.Promotion.Endpoints/Admin/MerchandisingCampaignAdminEndpoints.cs",
    ];

    [Fact]
    public void Promotion_route_request_universe_equals_the_classified_validator_matrix()
    {
        var root = Repo();

        // (1) Shipped route-reachable request universe, derived from the actual endpoint Send sites.
        var endpointText = string.Concat(EndpointFiles.Select(f =>
            File.ReadAllText(Path.Combine(root, ModuleRoot, f.Replace('/', Path.DirectorySeparatorChar)))));

        var routeRequests = Regex.Matches(endpointText, @"Send\(new (?<t>\w+)\(")
            .Select(m => m.Groups["t"].Value)
            .ToHashSet(StringComparer.Ordinal);

        // Exactly 21 distinct endpoint-reachable requests, one-to-one with the 21 shipped routes.
        Assert.Equal(21, routeRequests.Count);

        var routeLiterals = Regex.Matches(endpointText, @"\b(g|group)\.Map(Get|Post|Put|Delete)\(").Count;
        Assert.Equal(21, routeLiterals);

        // (2) The validator universe, derived from the single canonical validator file.
        var validatorFile = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Promotion.Application", "Validation", "PromotionRequestValidators.cs"));
        var validatorTargets = Regex.Matches(validatorFile, @"AbstractValidator<(?<t>\w+)>")
            .Select(m => m.Groups["t"].Value)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Equal(19, validatorTargets.Count);

        // (3) Exact set relation: every validated target is an endpoint-reachable request, and the only
        // endpoint-reachable requests without a validator are the two independently-proven exemptions.
        Assert.True(validatorTargets.IsSubsetOf(routeRequests),
            "a validator targets a request that is not endpoint-reachable");

        var unvalidated = routeRequests.Except(validatorTargets, StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(
            ["ListAdminPromotionsQuery", "ListSellerPromotionsQuery"],
            unvalidated);

        // Set equality: 19 validated + 2 exemptions == the 21 shipped requests, each exactly once.
        Assert.Equal(routeRequests.Count, validatorTargets.Count + unvalidated.Length);

        // The repaired locale-bearing read is a validated request using the canonical locale code.
        Assert.Contains("ListMerchandisingCampaignTypesQuery", validatorTargets);
        Assert.Contains("ListMerchandisingCampaignTypesQueryValidator", validatorFile, StringComparison.Ordinal);
        Assert.Contains("PromotionValidationCodes.LocaleInvalid", validatorFile, StringComparison.Ordinal);

        // Validators emit machine codes only; no business rule is duplicated.
        Assert.DoesNotContain("WithMessage(", validatorFile, StringComparison.Ordinal);
    }

    [Fact]
    public void Promotion_w3r3_certification_truth_and_authority_ancestry_are_recorded()
    {
        var root = Repo();

        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));
        var r3 = sot.RootElement.GetProperty("promotionAmsc001W3R3");
        Assert.Equal("TB-TMAR-PROMOTION-AMSC-001-W3-R3", r3.GetProperty("task").GetString());
        Assert.Equal("TB-TMAR-PROMOTION-AMSC-001-W3-R2", r3.GetProperty("parentTask").GetString());
        Assert.Equal("PROMOTION_AMSC_001_RECERTIFIED", r3.GetProperty("state").GetString());
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", r3.GetProperty("verdict").GetString());
        Assert.Equal("ARCH-COMPLETE-002", r3.GetProperty("lockVersion").GetString());
        Assert.True(r3.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("HTTP_OWNING", r3.GetProperty("httpApplicability").GetString());
        Assert.Equal(21, r3.GetProperty("moduleOwnedRouteCount").GetInt32());
        Assert.Equal(0, r3.GetProperty("hostOwnedRouteCount").GetInt32());
        Assert.Equal(21, r3.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal("EXHAUSTIVE_19_VALIDATOR_REQUIRED_2_NO_VALIDATOR_REQUIRED", r3.GetProperty("validatorMatrixState").GetString());
        Assert.Equal("VERIFIED_CANONICAL", r3.GetProperty("localeValidatorState").GetString());
        Assert.Equal("ZERO", r3.GetProperty("foreignAppInfraDomainCoupling").GetString());
        Assert.Equal("UNCHANGED", r3.GetProperty("schemaMigrationState").GetString());
        Assert.Equal("PRESERVED", r3.GetProperty("hostCheckpointState").GetString());
        Assert.Equal("NONE", r3.GetProperty("guardsWeakened").GetString());
        Assert.Equal("NONE", r3.GetProperty("baselinesWidened").GetString());
        Assert.False(r3.GetProperty("productionCodeChanged").GetBoolean());
        Assert.Equal("USER_REVIEW_PROMOTION_AMSC_001_W3_R3", r3.GetProperty("workflowStop").GetString());
        Assert.Equal("NONE", r3.GetProperty("automaticNextImplementationTask").GetString());
        Assert.Equal("PENDING_ARCHITECT_FINAL_SHA_RECONCILIATION", r3.GetProperty("architectReconciliationState").GetString());

        // Exact authority ancestry: W2 structure, W3-R1 review, W3-R2 repair are pinned in the block.
        Assert.Equal("597147ea8cc3fa4a0f9d6e301133a7237fc68e43", r3.GetProperty("currentStructureCommit").GetString());
        Assert.Equal("85d9818d79fa0b1b906c52af16b1a33f1c2149ce", r3.GetProperty("independentReviewCommit").GetString());
        Assert.Equal("b002d4596dbec4851bc77c6810c7c4b3e3a1b08c", r3.GetProperty("repairCommit").GetString());

        // Historical W3 block now points to its reinstated current authority without losing its own state.
        var w3 = sot.RootElement.GetProperty("promotionAmsc001W3");
        Assert.Equal("promotionAmsc001W3R3", w3.GetProperty("currentCertificationAuthority").GetString());
        Assert.Equal("PROMOTION_AMSC_001_RECERTIFIED", w3.GetProperty("reinstatedByState").GetString());
    }

    [Fact]
    public void Promotion_manifest_certification_truth_and_schema_host_lock_are_current()
    {
        var root = Repo();

        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-module-structure-manifests.json")));
        var entries = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Where(m => m.GetProperty("module").GetString() == "Promotion").ToArray();
        Assert.Single(entries);
        Assert.True(entries[0].GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", entries[0].GetProperty("lockVersion").GetString());
        Assert.Equal("PROMOTION_AMSC_001_RECERTIFIED", entries[0].GetProperty("currentCertificationState").GetString());
        Assert.Equal("TB-TMAR-PROMOTION-AMSC-001-W3-R3", entries[0].GetProperty("currentCertificationTask").GetString());
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", entries[0].GetProperty("currentVerdict").GetString());
        // The historical blocked state is preserved as immutable evidence.
        Assert.Equal("BLOCKED_SUPERSEDED_BY_W3_R1_PENDING_FRESH_CERTIFY", entries[0].GetProperty("certificationState").GetString());
        Assert.Equal(6, entries[0].GetProperty("projects").GetArrayLength());
        Assert.DoesNotContain(
            manifest.RootElement.GetProperty("preCertModules").EnumerateArray(),
            m => m.GetProperty("module").GetString() == "Promotion");

        // Own schema and the two unchanged migrations.
        var dbContext = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Promotion.Infrastructure", "Persistence", "PromotionDbContext.cs"));
        Assert.Contains("public const string Schema = \"promotion\"", dbContext, StringComparison.Ordinal);
        var migrations = Directory.EnumerateFiles(
                Path.Combine(root, ModuleRoot, "Tooba.Promotion.Infrastructure", "Persistence", "Migrations"), "*.cs")
            .Where(f => !f.EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase))
            .Where(f => !f.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .Select(Path.GetFileName).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(
            ["20260823210000_InitialPromotion.cs", "20260919134400_MerchandisingCampaignFoundation.cs"],
            migrations);

        // Preserved global Host checkpoint and Host route-authority ZERO.
        var sotText = File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"currentHostCheckpoint\": \"HOST_ROOT_FINAL_CERTIFIED\"", sotText, StringComparison.Ordinal);
        Assert.Contains("\"lastAcceptedTask\": \"TB-TMAR-HOST-ROOT-FINAL-CERT-001\"", sotText, StringComparison.Ordinal);
        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.DoesNotContain("/v1/seller/promotions", program, StringComparison.Ordinal);
        Assert.DoesNotContain("/v1/admin/promotions", program, StringComparison.Ordinal);
        Assert.DoesNotContain("/v1/admin/merchandising-campaigns", program, StringComparison.Ordinal);

        // Canonical outcome: no raw Results.* payload mapping remains; the 201 path uses api.Created.
        foreach (var file in Directory.EnumerateFiles(
                     Path.Combine(root, ModuleRoot, "Tooba.Promotion.Endpoints"), "*.cs", SearchOption.AllDirectories)
                     .Where(f => !IsBuildOutput(f)))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Results.Json(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.BadRequest(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.Problem(", text, StringComparison.Ordinal);
        }
        var seller = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Promotion.Endpoints", "Seller", "PromotionSellerEndpoints.cs"));
        Assert.Contains("api.Created(", seller, StringComparison.Ordinal);

        // This wave's evidence and durable guard exist.
        foreach (var evidence in new[] { "certification.md", "validation.md" })
        {
            Assert.True(File.Exists(Path.Combine(
                root, "docs", "architecture", "evidence", "TB-TMAR-PROMOTION-AMSC-001-W3-R3", evidence)));
        }
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

    private static bool IsBuildOutput(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
}
