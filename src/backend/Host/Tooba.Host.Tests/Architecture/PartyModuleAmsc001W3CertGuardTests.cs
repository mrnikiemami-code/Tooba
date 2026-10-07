using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-PARTY-AMSC-001-W3 — ARCH-COMPLETE-002 certification lock.
/// Pins the AMSC certification lineage (W0 analyze/W1 migrate/W2 structure SHAs + certified verdict),
/// the W1 canonical seam set (declared-code guard + dual-mechanism typed-fault seam), the exhaustive
/// 4-request validator matrix, module-owned HTTP ownership, contracts-only/self-contained boundaries
/// with own-schema persistence, Host closure and the certified manifest/SoT state.
/// </summary>
public sealed class PartyModuleAmsc001W3CertGuardTests
{
    [Fact]
    public void Party_is_amsc001_certified_in_manifest_and_sot()
    {
        var root = Repo();

        using var manifest = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-module-structure-manifests.json"))
                .Replace("\uFEFF", string.Empty));
        var moduleEntry = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Single(m => m.GetProperty("module").GetString() == "Party");
        Assert.True(moduleEntry.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", moduleEntry.GetProperty("lockVersion").GetString());
        Assert.Contains("TB-TMAR-PARTY-AMSC-001", moduleEntry.GetProperty("certificationNote").GetString(), StringComparison.Ordinal);

        using var sot = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"))
                .Replace("\uFEFF", string.Empty));
        var block = sot.RootElement.GetProperty("partyAmsc001W3");
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", block.GetProperty("verdict").GetString());
        Assert.Equal("ARCH-COMPLETE-002", block.GetProperty("lockVersion").GetString());
        Assert.True(block.GetProperty("structureCertified").GetBoolean());
        Assert.True(block.GetProperty("microserviceExtractable").GetBoolean());

        var certified = sot.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Equal(1, certified.Count(x => x == "Party"));
    }

    [Fact]
    public void Amsc_lineage_pins_analyze_migrate_and_structure_shas()
    {
        using var sot = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(Repo(), "docs/architecture/tmar-current-state.json"))
                .Replace("\uFEFF", string.Empty));

        var w1 = sot.RootElement.GetProperty("partyAmsc001W1");
        Assert.Equal("MIGRATE_COMPLETE", w1.GetProperty("state").GetString());
        Assert.Equal("29012df0", w1.GetProperty("commit").GetString());
        Assert.Equal("READY_FOR_STRUCTURE", w1.GetProperty("verdict").GetString());

        var w2 = sot.RootElement.GetProperty("partyAmsc001W2");
        Assert.Equal("STRUCTURE_READY_FOR_CERTIFY", w2.GetProperty("state").GetString());
        Assert.Equal("ffff7100", w2.GetProperty("commit").GetString());
        Assert.Equal("READY_FOR_CERTIFY", w2.GetProperty("verdict").GetString());
        Assert.Equal("PROFESSIONAL_SHALLOW", w2.GetProperty("folderGranularityState").GetString());
        Assert.Equal("EXACT", w2.GetProperty("pathNamespaceState").GetString());
        Assert.Equal("ENFORCED", w2.GetProperty("rootAllowlistState").GetString());
    }

    [Fact]
    public void Canonical_seams_are_present_and_single_owned()
    {
        var root = Repo();

        var codes = File.ReadAllText(Src(root, "Tooba.Party.Contracts/Errors/PartyErrorCodes.cs"));
        Assert.Contains("public static bool IsKnown(string? code)", codes, StringComparison.Ordinal);
        Assert.Equal(11, Regex.Matches(codes, "public const string ").Count);

        var operation = File.ReadAllText(Src(root, "Tooba.Party.Application/Composition/PartyOperation.cs"));
        Assert.Contains("catch (ContractOperationException ex) when (PartyErrorCodes.IsKnown(ex.Code))", operation, StringComparison.Ordinal);
        Assert.Contains("catch (SemanticException ex)", operation, StringComparison.Ordinal);
        Assert.Contains("public static async Task<Result> ExecuteAsync(Func<Task> action)", operation, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", operation, StringComparison.Ordinal);

        // Exactly one catalog contributor and one resource set, registered by the module composition.
        Assert.Equal(
            new[] { "PartyErrorCatalogContributor.cs" },
            Directory.EnumerateFiles(Src(root, "Tooba.Party.Contracts/Errors"), "*Contributor.cs").Select(Path.GetFileName).ToArray());
        var module = File.ReadAllText(Src(root, "Tooba.Party.Infrastructure/PartyModule.cs"));
        Assert.Contains("IErrorCatalogContributor, PartyErrorCatalogContributor>", module, StringComparison.Ordinal);
        Assert.Contains("IErrorResourceSet, PartyErrorResourceSet>", module, StringComparison.Ordinal);

        // Bilingual resource pair exists for the seller.settings./party. keyspaces.
        Assert.True(File.Exists(Src(root, "Tooba.Party.Contracts/Resources/PartyErrors.resx")));
        Assert.True(File.Exists(Src(root, "Tooba.Party.Contracts/Resources/PartyErrors.fa.resx")));
        var en = File.ReadAllText(Src(root, "Tooba.Party.Contracts/Resources/PartyErrors.resx"));
        var fa = File.ReadAllText(Src(root, "Tooba.Party.Contracts/Resources/PartyErrors.fa.resx"));
        foreach (var key in new[] { "seller.settings.missing", "seller.settings.rejected", "party.operation.rejected", "seller.settings.validation.display_name_required", "seller.settings.validation.display_name_length", "seller.settings.validation.legal_name_shape", "seller.settings.validation.description_shape", "seller.settings.validation.support_phone_shape", "seller.settings.validation.support_email_shape", "seller.settings.validation.address_line_shape", "party.admin.sellers.validation.grid_request_required" })
        {
            Assert.Contains($"\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"\"{key}\"", fa, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Validator_matrix_is_exhaustive_for_all_four_endpoint_reachable_requests()
    {
        var root = Repo();

        // Two required validators, both stable-code emitting.
        var sellerValidator = File.ReadAllText(Src(root, "Tooba.Party.Application/Seller/Validators/UpdateSellerSettingsCommandValidator.cs"));
        Assert.Contains("AbstractValidator<UpdateSellerSettingsCommand>", sellerValidator, StringComparison.Ordinal);
        Assert.Contains("WithErrorCode(PartyErrorCodes.", sellerValidator, StringComparison.Ordinal);

        var adminValidator = File.ReadAllText(Src(root, "Tooba.Party.Application/Admin/Sellers/Validators/QueryAdminSellersGridQueryValidator.cs"));
        Assert.Contains("AbstractValidator<QueryAdminSellersGridQuery>", adminValidator, StringComparison.Ordinal);
        Assert.Contains("WithErrorCode(PartyErrorCodes.", adminValidator, StringComparison.Ordinal);

        // Every request type is endpoint-reachable via ISender and returns Result<T> through the seam.
        var sellerEndpoints = File.ReadAllText(Src(root, "Tooba.Party.Endpoints/Seller/PartySellerSettingsEndpoints.cs"));
        var adminEndpoints = File.ReadAllText(Src(root, "Tooba.Party.Endpoints/Admin/Sellers/PartyAdminSellersEndpoints.cs"));
        Assert.Equal(2, Regex.Matches(sellerEndpoints, "group\\.Map").Count);
        Assert.Equal(2, Regex.Matches(adminEndpoints, "app\\.Map").Count);
        Assert.Contains("sender.Send(", sellerEndpoints, StringComparison.Ordinal);
        Assert.Contains("sender.Send(", adminEndpoints, StringComparison.Ordinal);
        Assert.Contains("api.From(", sellerEndpoints, StringComparison.Ordinal);
        Assert.Contains("api.From(", adminEndpoints, StringComparison.Ordinal);
    }

    [Fact]
    public void Boundaries_stay_self_contained_and_persistence_is_module_owned()
    {
        var root = Repo();
        var foreignPattern = "Tooba\\.(Order|Cart|Catalog|Identity|AccessControl|Content|CustomerProfile|Notification|Media|Fulfillment|Inventory|BulkInquiry|Localization|Payment|Settlement|Story|Wishlist|UserPreference|ProductQnA|Reviews|OperatorProfile|PageComposition|Promotion|Pricing|Tax|Returns|Wallet|Support|AddressBook)\\.(Application|Infrastructure|Domain|Endpoints)";
        foreach (var project in new[] { "Contracts", "Domain", "Application", "Infrastructure", "Endpoints" })
        {
            var projectDir = Src(root, $"Tooba.Party.{project}");
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

        // Inbound boundary stays Contracts-only: Promotion must not consume Party.Application.
        var promotionInfraCsproj = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Promotion/Tooba.Promotion.Infrastructure/Tooba.Promotion.Infrastructure.csproj"));
        Assert.DoesNotContain("Tooba.Party.Application.csproj", promotionInfraCsproj, StringComparison.Ordinal);

        // Own schema + own DbContext; module entities + own outbox.
        var db = File.ReadAllText(Src(root, "Tooba.Party.Infrastructure/Persistence/PartyDbContext.cs"));
        Assert.Contains("\"party\"", db, StringComparison.Ordinal);
        Assert.Contains("DbSet<BusinessParty> Parties", db, StringComparison.Ordinal);
        Assert.Contains("DbSet<PartyCapability> Capabilities", db, StringComparison.Ordinal);
        Assert.Contains("DbSet<UserPartyLink> UserLinks", db, StringComparison.Ordinal);
        Assert.Contains("DbSet<PartyMembership> Memberships", db, StringComparison.Ordinal);
        Assert.Contains("DbSet<OrganizationRelationship> OrganizationRelationships", db, StringComparison.Ordinal);
        Assert.Contains("DbSet<OutboxMessage> OutboxMessages", db, StringComparison.Ordinal);

        // Host closure: no Host Party folder; the thin seller authorizer stays a Host security adapter.
        Assert.False(Directory.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Party")));
        var authorizer = File.ReadAllText(Src(root, "Tooba.Party.Endpoints/Seller/IPartySellerAuthorizer.cs"));
        Assert.Contains("interface IPartySellerAuthorizer", authorizer, StringComparison.Ordinal);
    }

    private static string Src(string root, string relative) => Path.Combine(
        root, "src", "backend", "Modules", "Party", relative.Replace('/', Path.DirectorySeparatorChar));

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
