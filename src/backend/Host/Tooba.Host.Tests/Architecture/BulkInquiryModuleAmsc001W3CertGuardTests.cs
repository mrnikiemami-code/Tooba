using System.Text.Json;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-BULKINQUIRY-AMSC-001-W3 — durable ARCH-COMPLETE-002 certification lock:
/// SoT records, manifest promotion, canonical mechanisms, Host ownership and evidence.
/// </summary>
public sealed class BulkInquiryModuleAmsc001W3CertGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/BulkInquiry";

    [Fact]
    public void BulkInquiry_is_arch_complete_002_certified_in_sot_and_manifest()
    {
        var root = Repo();

        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-module-structure-manifests.json")));
        var entry = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Single(m => m.GetProperty("module").GetString() == "BulkInquiry");
        Assert.True(entry.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", entry.GetProperty("lockVersion").GetString());
        Assert.Contains("TB-TMAR-BULKINQUIRY-AMSC-001 W0 7b89d81e", entry.GetProperty("certificationNote").GetString(), StringComparison.Ordinal);

        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));
        var w3 = sot.RootElement.GetProperty("bulkInquiryModuleAmsc001W3");
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", w3.GetProperty("verdict").GetString());
        Assert.Equal("CERTIFIED", w3.GetProperty("structureState").GetString());
        Assert.Equal("ARCH-COMPLETE-002", w3.GetProperty("lockVersion").GetString());
        Assert.Equal("EXACT", w3.GetProperty("pathNamespaceState").GetString());
        Assert.Equal("ENFORCED", w3.GetProperty("rootAllowlistState").GetString());
        Assert.Equal("ZERO", w3.GetProperty("foreignAppInfraDomainCoupling").GetString());
        Assert.Equal("MODULE_OWNED", w3.GetProperty("endpointOwnershipState").GetString());
        Assert.Equal(1, w3.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal("ZERO", w3.GetProperty("blockingResidualDebt").GetString());
        Assert.True(w3.GetProperty("microserviceExtractable").GetBoolean());
        Assert.Equal("NONE", w3.GetProperty("automaticNextImplementationTask").GetString());

        var amc = sot.RootElement.GetProperty("bulkInquiryAmc001");
        Assert.Equal("TB-TMAR-BULKINQUIRY-AMSC-001-W3", amc.GetProperty("supersededBy").GetString());

        var certified = sot.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Equal(1, certified.Count(x => x == "BulkInquiry"));

        // Wave lineage is recorded with its own commit SHAs.
        Assert.Equal("7b89d81e", sot.RootElement.GetProperty("bulkInquiryModuleAmsc001W0").GetProperty("commit").GetString());
        Assert.Equal("9e9e37df", sot.RootElement.GetProperty("bulkInquiryModuleAmsc001W1").GetProperty("commit").GetString());
        Assert.Equal("82c2fa8e", sot.RootElement.GetProperty("bulkInquiryModuleAmsc001W2").GetProperty("commit").GetString());
    }

    [Fact]
    public void BulkInquiry_uses_canonical_result_localization_and_typed_fault_mechanisms()
    {
        var root = Repo();
        var contracts = Path.Combine(root, ModuleRoot, "Tooba.BulkInquiry.Contracts");
        var app = Path.Combine(root, ModuleRoot, "Tooba.BulkInquiry.Application");
        var domain = Path.Combine(root, ModuleRoot, "Tooba.BulkInquiry.Domain");
        var infra = Path.Combine(root, ModuleRoot, "Tooba.BulkInquiry.Infrastructure");
        var endpoints = Path.Combine(root, ModuleRoot, "Tooba.BulkInquiry.Endpoints");

        // Catalog owns exactly one semantic descriptor; validation codes are Application-owned and uncatalogued.
        var catalog = File.ReadAllText(Path.Combine(contracts, "Errors", "BulkInquiryErrorCatalogContributor.cs"));
        Assert.Contains("BulkInquiryErrorCodes.Rejected", catalog, StringComparison.Ordinal);
        Assert.DoesNotContain("RequestRequired", catalog, StringComparison.Ordinal);
        Assert.DoesNotContain("AddressRequired", catalog, StringComparison.Ordinal);

        var codes = File.ReadAllText(Path.Combine(contracts, "Errors", "BulkInquiryErrorCodes.cs"));
        Assert.Contains("bulk_inquiry.rejected", codes, StringComparison.Ordinal);
        Assert.DoesNotContain("bulk_inquiry.validation.", codes, StringComparison.Ordinal);

        var validationCodes = File.ReadAllText(Path.Combine(app, "Validation", "BulkInquiryValidationCodes.cs"));
        Assert.Contains("bulk_inquiry.validation.request_required", validationCodes, StringComparison.Ordinal);
        Assert.Contains("bulk_inquiry.validation.address_required", validationCodes, StringComparison.Ordinal);

        // Both cultures still carry every localization key.
        foreach (var culture in new[] { "BulkInquiryErrors.resx", "BulkInquiryErrors.fa.resx" })
        {
            var resx = File.ReadAllText(Path.Combine(contracts, "Resources", culture));
            Assert.Contains("bulk_inquiry.rejected", resx, StringComparison.Ordinal);
            Assert.Contains("bulk_inquiry.validation.phone_required", resx, StringComparison.Ordinal);
        }

        // Typed faults: Domain/Directory use code-carrying ContractOperationException; the seam maps by code.
        var aggregate = File.ReadAllText(Path.Combine(domain, "Aggregates", "BulkPurchaseInquiry.cs"));
        Assert.Contains("ContractOperationException", aggregate, StringComparison.Ordinal);
        Assert.DoesNotContain("SemanticException", aggregate, StringComparison.Ordinal);

        var directory = File.ReadAllText(Path.Combine(infra, "Directories", "BulkInquiryDirectory.cs"));
        Assert.Contains("throw new ContractOperationException(BulkInquiryErrorCodes.Rejected)", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("SemanticException", directory, StringComparison.Ordinal);

        var seam = File.ReadAllText(Path.Combine(app, "Composition", "BulkInquiryOperation.cs"));
        Assert.Contains("catch (ContractOperationException ex) when (IsKnown(ex.Code))", seam, StringComparison.Ordinal);
        Assert.Contains("catch (SemanticException ex)", seam, StringComparison.Ordinal);

        // Endpoints stay thin and canonical.
        foreach (var file in Directory.EnumerateFiles(endpoints, "*.cs", SearchOption.AllDirectories)
                     .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                                 && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Results.Json", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.Problem", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Console.WriteLine", text, StringComparison.Ordinal);
            Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
        }

        var storefront = File.ReadAllText(Path.Combine(endpoints, "Storefront", "BulkInquiryStorefrontEndpoints.cs"));
        Assert.Contains("ISender", storefront, StringComparison.Ordinal);
        Assert.Contains("api.Created", storefront, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(infra, "BulkInquiryModule.cs"));
        Assert.Contains("BulkInquiryErrorCatalogContributor", module, StringComparison.Ordinal);
        Assert.Contains("BulkInquiryErrorResourceSet", module, StringComparison.Ordinal);
    }

    [Fact]
    public void BulkInquiry_host_residue_is_composition_only_and_closed_folder_is_preserved()
    {
        var root = Repo();
        Assert.False(Directory.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/BulkInquiry")));

        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("MapBulkInquiryModuleEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("AddBulkInquiryEndpointPresentation", program, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host.BulkInquiry", program, StringComparison.Ordinal);

        // The module's Infrastructure csproj keeps the Contracts-only Catalog seam and nothing foreign.
        var infraCsproj = File.ReadAllText(Path.Combine(root, ModuleRoot, "Tooba.BulkInquiry.Infrastructure", "Tooba.BulkInquiry.Infrastructure.csproj"));
        Assert.Contains("Tooba.Catalog.Contracts", infraCsproj, StringComparison.Ordinal);
        foreach (var forbidden in new[] { "Tooba.Catalog.Application", "Tooba.Catalog.Domain", "Tooba.Catalog.Infrastructure", "Tooba.Order.", "Tooba.Cart." })
        {
            Assert.DoesNotContain(forbidden, infraCsproj, StringComparison.Ordinal);
        }

        // Schema / migration safety: no new migration, unchanged schema name.
        var migrations = Path.Combine(root, ModuleRoot, "Tooba.BulkInquiry.Infrastructure", "Persistence", "Migrations");
        Assert.Equal(
            [
                "20260826120000_InitialBulkInquiry.Designer.cs",
                "20260826120000_InitialBulkInquiry.cs",
                "20260909130700_DecimalBulkInquiryQuantity.cs",
                "BulkInquiryDbContextModelSnapshot.cs",
            ],
            Directory.EnumerateFiles(migrations, "*.cs").Select(Path.GetFileName).OrderBy(x => x, StringComparer.Ordinal).ToArray());
        Assert.Contains("\"bulk_inquiry\"", File.ReadAllText(Path.Combine(root, ModuleRoot, "Tooba.BulkInquiry.Infrastructure", "Persistence", "BulkInquiryDbContext.cs")), StringComparison.Ordinal);
    }

    [Fact]
    public void BulkInquiry_amsc_evidence_and_recovery_checkpoint_exist()
    {
        var root = Repo();
        foreach (var wave in new[] { "W0", "W1", "W2", "W3" })
        {
            Assert.True(Directory.Exists(Path.Combine(
                root, $"docs/architecture/evidence/TB-TMAR-BULKINQUIRY-AMSC-001-{wave}")));
        }

        var recovery = File.ReadAllText(Path.Combine(root, "docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md"));
        Assert.Contains("BulkInquiry AMSC module recovery checkpoint", recovery, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-BULKINQUIRY-AMSC-001-W0", recovery, StringComparison.Ordinal);
        Assert.Contains("HISTORICAL / SUPERSEDED FOR CURRENT BULKINQUIRY MODULE RECOVERY", recovery, StringComparison.Ordinal);
    }

    [Fact]
    public void BulkInquiry_amsc_w3r1_recovery_reconciliation_truth_is_locked()
    {
        var root = Repo();

        // Domain keeps only BuildingBlocks + its own Contracts reference; no foreign module layering.
        var domainCsproj = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.BulkInquiry.Domain", "Tooba.BulkInquiry.Domain.csproj"));
        Assert.Contains("Tooba.BuildingBlocks.csproj", domainCsproj, StringComparison.Ordinal);
        Assert.Contains("Tooba.BulkInquiry.Contracts.csproj", domainCsproj, StringComparison.Ordinal);
        foreach (var foreign in new[]
                 {
                     "Tooba.Catalog.", "Tooba.Order.", "Tooba.Cart.", "Tooba.Fulfillment.",
                     "Tooba.Payment.", "Tooba.Settlement.", "Tooba.AccessControl.", "Tooba.AddressBook.",
                     "Tooba.Offer.", "Tooba.Inventory.", "Tooba.Party.", "Tooba.StoreContext.",
                 })
        {
            Assert.DoesNotContain(foreign, domainCsproj, StringComparison.Ordinal);
        }

        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));

        // W1 historical metadata must no longer carry the false REMOVED claim.
        var w1 = sot.RootElement.GetProperty("bulkInquiryModuleAmsc001W1");
        var w1Reference = w1.GetProperty("domainContractsReference").GetString();
        Assert.NotEqual("REMOVED", w1Reference);
        Assert.Contains("PRESERVED_OWN_MODULE_CONTRACTS_REFERENCE", w1Reference, StringComparison.Ordinal);

        // Additive R1 reconciliation record is present and honest.
        var w3r1 = sot.RootElement.GetProperty("bulkInquiryModuleAmsc001W3R1");
        Assert.Equal("BULKINQUIRY_AMSC_001_RECOVERY_RECONCILED", w3r1.GetProperty("state").GetString());
        Assert.False(w3r1.GetProperty("productionCodeChanged").GetBoolean());
        Assert.Equal("67b5b55a2fecec33f3109b90252152875680edc0", w3r1.GetProperty("certifiedCommit").GetString());
        Assert.Equal("PRESERVED_OWN_MODULE_CONTRACTS_REFERENCE", w3r1.GetProperty("w1DomainContractsTruth").GetString());
        Assert.Equal("ZERO", w3r1.GetProperty("foreignAppInfraDomainCoupling").GetString());
        Assert.Equal("RECORDED_67B5B55A", w3r1.GetProperty("masterRecoveryW3ShaState").GetString());
        Assert.Equal("NONE", w3r1.GetProperty("automaticNextImplementationTask").GetString());

        // W3 certification verdict itself is preserved unchanged.
        Assert.Equal("COMPLETE_REFERENCE_PATTERN",
            sot.RootElement.GetProperty("bulkInquiryModuleAmsc001W3").GetProperty("verdict").GetString());

        // Repository-global Host root checkpoint must remain untouched by this module-local task.
        Assert.Equal("TB-TMAR-HOST-ROOT-FINAL-CERT-001", sot.RootElement.GetProperty("lastAcceptedTask").GetString());
        Assert.Equal("HOST_ROOT_FINAL_CERTIFIED", sot.RootElement.GetProperty("currentHostCheckpoint").GetString());
        Assert.Equal("NONE", sot.RootElement.GetProperty("automaticNextImplementationTask").GetString());

        // Master Recovery records the W3 final commit SHA and the R1 reconciliation.
        var recovery = File.ReadAllText(Path.Combine(root, "docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md"));
        Assert.Contains("`TB-TMAR-BULKINQUIRY-AMSC-001-W3` Certify `67b5b55a`", recovery, StringComparison.Ordinal);
        Assert.Contains("BulkInquiry AMSC W3-R1 recovery reconciliation", recovery, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_BULKINQUIRY_AMSC_001_W3_R1", recovery, StringComparison.Ordinal);
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
