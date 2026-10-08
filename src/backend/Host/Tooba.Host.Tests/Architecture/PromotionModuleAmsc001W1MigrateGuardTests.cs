using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Tooba.Promotion.Contracts.Errors;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-PROMOTION-AMSC-001-W1 — migrate wave durable guard.
/// Locks the single canonical Contracts stable-code home with its declared-code guard and reachability
/// split, the canonical typed-fault seam, the zero-foreign-coupling boundary (the illegal
/// <c>Promotion.Infrastructure -> Inventory.Application/Domain</c> edge), the Promotion-owned
/// Contracts signature that stopped carrying <c>Offer.Contracts</c>, the transport validator coverage,
/// the canonical seller-create result mapping and the cohesion splits of the migrate wave.
/// </summary>
public sealed class PromotionModuleAmsc001W1MigrateGuardTests
{
    private static readonly string[] ProductionProjects =
    [
        "Tooba.Promotion.Contracts",
        "Tooba.Promotion.Domain",
        "Tooba.Promotion.Application",
        "Tooba.Promotion.Infrastructure",
    ];

    [Fact]
    public void Stable_codes_live_in_the_single_canonical_contracts_errors_home()
    {
        Assert.True(File.Exists(Path.Combine(
            Repo(), "src/backend/Modules/Promotion/Tooba.Promotion.Contracts/Errors/PromotionErrorCodes.cs")),
            "Contracts/Errors/PromotionErrorCodes.cs must exist");
        Assert.Contains(
            "namespace Tooba.Promotion.Contracts.Errors",
            Read("src/backend/Modules/Promotion/Tooba.Promotion.Contracts/Errors/PromotionErrorCodes.cs"),
            StringComparison.Ordinal);

        // The retired Application-owned stable-code/mapper home must not resurrect.
        Assert.False(File.Exists(Path.Combine(
            Repo(), "src/backend/Modules/Promotion/Tooba.Promotion.Application/Errors/PromotionErrors.cs")),
            "Application/Errors/PromotionErrors.cs must stay retired");
        Assert.False(Directory.Exists(Path.Combine(
            Repo(), "src/backend/Modules/Promotion/Tooba.Promotion.Application/Errors")),
            "Application/Errors folder must stay retired");

        var declarations = ProductionSources("Tooba.Promotion.Contracts")
            .Concat(ProductionSources("Tooba.Promotion.Domain"))
            .Concat(ProductionSources("Tooba.Promotion.Application"))
            .Concat(ProductionSources("Tooba.Promotion.Infrastructure"))
            .Count(file => File.ReadAllText(file).Contains("class PromotionErrorCodes", StringComparison.Ordinal));
        Assert.Equal(1, declarations);
    }

    [Fact]
    public void Declared_code_catalog_splits_http_reachable_from_domain_invariants()
    {
        // HTTP-reachable outcome codes: exactly one catalogued descriptor each.
        Assert.True(PromotionErrorCodes.IsHttpReachable(PromotionErrorCodes.Missing));
        Assert.True(PromotionErrorCodes.IsHttpReachable(PromotionErrorCodes.MerchandisingCampaignMissing));
        Assert.True(PromotionErrorCodes.IsHttpReachable(PromotionErrorCodes.CampaignValidation));
        Assert.True(PromotionErrorCodes.IsHttpReachable(PromotionErrorCodes.CampaignPrice));
        Assert.True(PromotionErrorCodes.IsHttpReachable(PromotionErrorCodes.OutboxUnmappedEventType));

        // Domain invariants are Result-carried identities, never catalogued with their own HTTP descriptor.
        Assert.True(PromotionErrorCodes.IsDomainInvariant(PromotionErrorCodes.DefinitionNameRequired));
        Assert.True(PromotionErrorCodes.IsDomainInvariant(PromotionErrorCodes.CampaignWindowInvalid));
        Assert.False(PromotionErrorCodes.IsHttpReachable(PromotionErrorCodes.DefinitionNameRequired));
        Assert.False(PromotionErrorCodes.IsDomainInvariant(PromotionErrorCodes.Missing));

        // Exact reachability split: 13 HTTP-reachable + 27 domain invariants = 40 declared.
        Assert.Equal(13, PromotionErrorCodes.HttpReachable.Count);
        Assert.Equal(27, PromotionErrorCodes.DomainInvariants.Count);
        Assert.Equal(40, PromotionErrorCodes.HttpReachable.Count + PromotionErrorCodes.DomainInvariants.Count);

        // Every HTTP-reachable code is registered exactly once by the module contributor.
        var contributor = Read("src/backend/Modules/Promotion/Tooba.Promotion.Endpoints/Errors/PromotionErrorCatalogContributor.cs");
        Assert.Equal(
            PromotionErrorCodes.HttpReachable.Count,
            Regex.Matches(contributor, @"D\(PromotionErrorCodes\.").Count);

        // Foundation-owned cross-cutting codes are deliberately NOT declared by Promotion.
        Assert.False(PromotionErrorCodes.IsKnown("seller.authorization.denied"));
        Assert.False(PromotionErrorCodes.IsKnown("admin.authorization.denied"));
        Assert.False(PromotionErrorCodes.IsKnown("offer.not_found"));
        Assert.False(PromotionErrorCodes.IsKnown(null));
        Assert.False(PromotionErrorCodes.IsKnown(string.Empty));
        Assert.False(PromotionErrorCodes.IsKnown(" "));

        var declared = typeof(PromotionErrorCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToArray();
        Assert.NotEmpty(declared);
        Assert.All(declared, code => Assert.True(PromotionErrorCodes.IsKnown(code), code));
        Assert.All(declared, code => Assert.DoesNotContain("authorization.denied", code, StringComparison.Ordinal));
        Assert.Equal(
            declared.Length,
            declared.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(
            PromotionErrorCodes.HttpReachable.Count + PromotionErrorCodes.DomainInvariants.Count,
            declared.Length);
    }

    [Fact]
    public void Typed_fault_seam_maps_declared_codes_and_never_parses_message_text()
    {
        var seamPath = Path.Combine(
            Repo(), "src/backend/Modules/Promotion/Tooba.Promotion.Application/Composition/PromotionOperation.cs");
        Assert.True(File.Exists(seamPath), "Application/Composition/PromotionOperation.cs must exist");
        var text = File.ReadAllText(seamPath);

        Assert.Contains("ContractOperationException", text, StringComparison.Ordinal);
        Assert.Contains("PromotionErrorCodes.IsKnown", text, StringComparison.Ordinal);
        Assert.Contains("SemanticException", text, StringComparison.Ordinal);
        Assert.Contains("Task<Result<T>> ExecuteAsync<T>", text, StringComparison.Ordinal);
        Assert.Contains("Task<Result> ExecuteAsync", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", text, StringComparison.Ordinal);

        Assert.Equal("Tooba.Promotion.Application.Composition", typeof(Tooba.Promotion.Application.Composition.PromotionOperation).Namespace);
    }

    [Fact]
    public void Error_localization_resource_set_is_promotion_owned_and_registered()
    {
        Assert.True(File.Exists(Path.Combine(
            Repo(), "src/backend/Modules/Promotion/Tooba.Promotion.Contracts/Errors/PromotionErrorResourceSet.cs")));
        Assert.True(File.Exists(Path.Combine(
            Repo(), "src/backend/Modules/Promotion/Tooba.Promotion.Contracts/Resources/PromotionErrors.resx")));
        Assert.True(File.Exists(Path.Combine(
            Repo(), "src/backend/Modules/Promotion/Tooba.Promotion.Contracts/Resources/PromotionErrors.fa.resx")));

        var module = Read("src/backend/Modules/Promotion/Tooba.Promotion.Endpoints/PromotionEndpointModule.cs");
        Assert.Contains("PromotionErrorResourceSet", module, StringComparison.Ordinal);
    }

    [Fact]
    public void Promotion_infrastructure_never_references_foreign_application_or_domain()
    {
        var csproj = Read("src/backend/Modules/Promotion/Tooba.Promotion.Infrastructure/Tooba.Promotion.Infrastructure.csproj");
        Assert.DoesNotContain("Tooba.Inventory.Application", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Inventory.Domain", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Pricing.Application", csproj, StringComparison.Ordinal);
        Assert.Contains("Tooba.Inventory.Contracts", csproj, StringComparison.Ordinal);

        foreach (var project in ProductionProjects)
        {
            foreach (var file in ProductionSources(project))
            {
                var text = File.ReadAllText(file);
                Assert.DoesNotContain("using Tooba.Inventory.Application", text, StringComparison.Ordinal);
                Assert.DoesNotContain("Tooba.Inventory.Application.", text, StringComparison.Ordinal);
                Assert.DoesNotContain("using Tooba.Inventory.Domain", text, StringComparison.Ordinal);
                Assert.DoesNotContain("Tooba.Inventory.Domain.", text, StringComparison.Ordinal);
                Assert.DoesNotContain("using Tooba.Pricing.Application", text, StringComparison.Ordinal);
                Assert.DoesNotContain("Tooba.Pricing.Application.", text, StringComparison.Ordinal);
            }
        }

        // The stale/illegal App→App baseline entries are retired repository-wide.
        var baseline = Read("src/backend/Host/Tooba.Host.Tests/Baselines/tmar-app-to-app-edges.json");
        Assert.DoesNotContain("Tooba.Promotion.Application -> Tooba.Inventory.Application", baseline, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Promotion.Application -> Tooba.Pricing.Application", baseline, StringComparison.Ordinal);
    }

    [Fact]
    public void Promotion_contracts_signature_does_not_carry_foreign_types()
    {
        var contractsCsproj = Read("src/backend/Modules/Promotion/Tooba.Promotion.Contracts/Tooba.Promotion.Contracts.csproj");
        Assert.DoesNotContain("Tooba.Offer.Contracts", contractsCsproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Pricing.Contracts", contractsCsproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Inventory.Contracts", contractsCsproj, StringComparison.Ordinal);

        foreach (var file in ProductionSources("Tooba.Promotion.Contracts"))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Tooba.Offer.Contracts", text, StringComparison.Ordinal);
            Assert.DoesNotContain("using Tooba.Offer", text, StringComparison.Ordinal);
        }

        // The Application layer no longer needs the foreign Offer.Contracts edge either.
        var appCsproj = Read("src/backend/Modules/Promotion/Tooba.Promotion.Application/Tooba.Promotion.Application.csproj");
        Assert.DoesNotContain("Tooba.Offer.Contracts", appCsproj, StringComparison.Ordinal);
    }

    [Fact]
    public void Transport_validators_cover_the_endpoint_reachable_requests()
    {
        var validators = Read("src/backend/Modules/Promotion/Tooba.Promotion.Application/Validation/PromotionRequestValidators.cs");
        foreach (var type in new[]
                 {
                     "CreateSellerPromotionCommandValidator",
                     "UpdateSellerPromotionCommandValidator",
                     "ActivateSellerPromotionCommandValidator",
                     "DeactivateSellerPromotionCommandValidator",
                     "GetSellerPromotionQueryValidator",
                     "GetAdminPromotionQueryValidator",
                     "DeactivateAdminPromotionCommandValidator",
                     "ListMerchandisingCampaignsQueryValidator",
                     "ListMerchandisingCampaignTypesQueryValidator",
                     "ListMerchandisingOfferCandidatesQueryValidator",
                     "GetMerchandisingCampaignQueryValidator",
                     "CreateMerchandisingCampaignCommandValidator",
                     "UpdateMerchandisingCampaignCommandValidator",
                     "PublishMerchandisingCampaignCommandValidator",
                     "ArchiveMerchandisingCampaignCommandValidator",
                     "AddMerchandisingCampaignMemberCommandValidator",
                     "RemoveMerchandisingCampaignMemberCommandValidator",
                     "ReorderMerchandisingCampaignMembersCommandValidator",
                     "SetMerchandisingCampaignMemberPriceCommandValidator",
                 })
        {
            Assert.Contains(type, validators, StringComparison.Ordinal);
        }

        // Validators emit stable machine codes, never user-facing prose.
        Assert.DoesNotContain("WithMessage(", validators, StringComparison.Ordinal);
        Assert.DoesNotContain("FluentValidation", Read("src/backend/Modules/Promotion/Tooba.Promotion.Domain/Aggregates/PromotionDefinition.cs"), StringComparison.Ordinal);
    }

    [Fact]
    public void Seller_create_endpoint_uses_the_canonical_created_result()
    {
        var endpoints = Read("src/backend/Modules/Promotion/Tooba.Promotion.Endpoints/Seller/PromotionSellerEndpoints.cs");
        Assert.Contains("api.Created(", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Json(", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.BadRequest(", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Problem(", endpoints, StringComparison.Ordinal);
    }

    [Fact]
    public void Cohesion_splits_are_real_and_no_duplicate_declaration_remains()
    {
        // PromotionDirectory split into three cohesive files.
        Assert.True(File.Exists(Path.Combine(
            Repo(), "src/backend/Modules/Promotion/Tooba.Promotion.Infrastructure/Directories/OpenPromotionUseCaseGuard.cs")));
        Assert.True(File.Exists(Path.Combine(
            Repo(), "src/backend/Modules/Promotion/Tooba.Promotion.Infrastructure/Directories/DeferredPromotionRedemptionLedger.cs")));
        Assert.False(File.Exists(Path.Combine(
            Repo(), "src/backend/Modules/Promotion/Tooba.Promotion.Application/Merchandising/Admin/MerchandisingCampaignAdminCqrs.cs")),
            "the technical-axis CQRS bundle must stay retired");
        Assert.False(File.Exists(Path.Combine(
            Repo(), "src/backend/Modules/Promotion/Tooba.Promotion.Application/Ports/PromotionDirectoryPorts.cs")),
            "the generic ports bundle must stay retired");
        Assert.False(File.Exists(Path.Combine(
            Repo(), "src/backend/Modules/Promotion/Tooba.Promotion.Application/Merchandising/MerchandisingCampaignPorts.cs")),
            "the generic merchandising ports bundle must stay retired");

        var directory = Read("src/backend/Modules/Promotion/Tooba.Promotion.Infrastructure/Directories/PromotionDirectory.cs");
        Assert.DoesNotContain("OpenPromotionUseCaseGuard", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("DeferredPromotionRedemptionLedger", directory, StringComparison.Ordinal);
    }

    [Fact]
    public void Promotion_production_never_classifies_faults_by_message_text_or_inlines_code_literals()
    {
        var declarationFile = Path.GetFullPath(Path.Combine(
            Repo(), "src/backend/Modules/Promotion/Tooba.Promotion.Contracts/Errors/PromotionErrorCodes.cs"));
        var resourceFolder = Path.GetFullPath(Path.Combine(
            Repo(), "src/backend/Modules/Promotion/Tooba.Promotion.Contracts/Resources"));

        foreach (var project in ProductionProjects)
        {
            foreach (var file in ProductionSources(project))
            {
                var text = File.ReadAllText(file);
                Assert.DoesNotContain(".Message.Contains(", text, StringComparison.Ordinal);
                Assert.DoesNotContain(".Message.StartsWith(", text, StringComparison.Ordinal);
                Assert.DoesNotContain(".Message ==", text, StringComparison.Ordinal);
                Assert.DoesNotContain("PromotionExceptionMapper", text, StringComparison.Ordinal);
                Assert.DoesNotContain("throw new InvalidOperationException(\"promotion.", text, StringComparison.Ordinal);

                if (string.Equals(Path.GetFullPath(file), declarationFile, StringComparison.OrdinalIgnoreCase)
                    || Path.GetFullPath(file).StartsWith(resourceFolder, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (var code in new[] { "promotion.missing", "promotion.mutation.rejected", "campaign.validation", "merchandising.campaign.missing" })
                {
                    Assert.DoesNotContain($"\"{code}\"", text, StringComparison.Ordinal);
                }
            }
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

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(Repo(), relativePath));

    private static IEnumerable<string> ProductionSources(string project)
    {
        var root = Path.Combine(Repo(), "src", "backend", "Modules", "Promotion", project);
        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !p.EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<string> ProjectRefs(string project)
    {
        var doc = XDocument.Load(Path.Combine(
            Repo(), "src", "backend", "Modules", "Promotion", project, $"{project}.csproj"));
        return doc.Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Where(x => x.Length > 0)
            .ToArray();
    }
}
