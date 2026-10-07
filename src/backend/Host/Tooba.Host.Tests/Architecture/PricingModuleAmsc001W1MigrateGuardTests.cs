using System.Reflection;
using System.Text.RegularExpressions;
using Tooba.Pricing.Application.Composition;
using Tooba.Pricing.Contracts.Errors;
using Xunit;
namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-PRICING-AMSC-001-W1 — migrate wave durable guard.
/// Locks the single canonical Contracts stable-code home with its declared-code guard, the retired
/// duplicate Domain declaration, the canonical typed-fault seam, the Promotion Contracts-only
/// decoupling (the illegal <c>Promotion.Infrastructure -> Pricing.Application</c> edge) and the
/// zero-foreign-coupling boundary established by the migrate wave.
/// </summary>
public sealed class PricingModuleAmsc001W1MigrateGuardTests
{
    private static readonly string[] ProductionProjects =
    [
        "Tooba.Pricing.Contracts",
        "Tooba.Pricing.Domain",
        "Tooba.Pricing.Application",
        "Tooba.Pricing.Infrastructure",
    ];

    [Fact]
    public void Stable_codes_live_in_the_single_canonical_contracts_errors_home()
    {
        Assert.True(File.Exists(Path.Combine(
            Repo(), "src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Errors/PricingErrorCodes.cs")),
            "Contracts/Errors/PricingErrorCodes.cs must exist");
        Assert.Contains(
            "namespace Tooba.Pricing.Contracts.Errors",
            Read("src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Errors/PricingErrorCodes.cs"),
            StringComparison.Ordinal);

        // The duplicated Domain declaration and its folder must not resurrect.
        Assert.False(File.Exists(Path.Combine(
            Repo(), "src/backend/Modules/Pricing/Tooba.Pricing.Domain/Errors/PricingErrorCodes.cs")),
            "duplicate Domain/Errors/PricingErrorCodes.cs must not resurrect");
        Assert.False(Directory.Exists(Path.Combine(
            Repo(), "src/backend/Modules/Pricing/Tooba.Pricing.Domain/Errors")),
            "Domain/Errors folder must stay retired");

        // No second declaration of the same stable identity anywhere in Pricing production.
        var declarations = ProductionSources("Tooba.Pricing.Contracts")
            .Concat(ProductionSources("Tooba.Pricing.Domain"))
            .Concat(ProductionSources("Tooba.Pricing.Application"))
            .Concat(ProductionSources("Tooba.Pricing.Infrastructure"))
            .Count(file => File.ReadAllText(file).Contains(
                "class PricingErrorCodes", StringComparison.Ordinal));
        Assert.Equal(1, declarations);
    }

    [Fact]
    public void Declared_code_catalog_is_the_single_known_code_source()
    {
        Assert.True(PricingErrorCodes.IsKnown(PricingErrorCodes.AmountInvalid));
        Assert.True(PricingErrorCodes.IsKnown(PricingErrorCodes.Overlap));
        Assert.True(PricingErrorCodes.IsKnown(PricingErrorCodes.OfferMissing));
        Assert.True(PricingErrorCodes.IsKnown(PricingErrorCodes.CampaignRequired));
        Assert.True(PricingErrorCodes.IsKnown(PricingErrorCodes.ValidityInverted));
        Assert.True(PricingErrorCodes.IsKnown(PricingErrorCodes.RetiredReactivate));
        Assert.True(PricingErrorCodes.IsKnown(PricingErrorCodes.RetiredImmutable));
        Assert.True(PricingErrorCodes.IsKnown(PricingErrorCodes.CurrencyChangeForbidden));
        Assert.True(PricingErrorCodes.IsKnown(PricingErrorCodes.MarketInvalid));
        Assert.True(PricingErrorCodes.IsKnown(PricingErrorCodes.CurrencyInvalid));
        Assert.True(PricingErrorCodes.IsKnown(PricingErrorCodes.CurrencyDisplayUnit));

        // Foreign-owned codes and unknown/empty input are never classified as Pricing faults.
        Assert.False(PricingErrorCodes.IsKnown("offer.not_found"));
        Assert.False(PricingErrorCodes.IsKnown("cart.line.currency_missing"));
        Assert.False(PricingErrorCodes.IsKnown("pricing.price.id_required"));
        Assert.False(PricingErrorCodes.IsKnown(null));
        Assert.False(PricingErrorCodes.IsKnown(string.Empty));
        Assert.False(PricingErrorCodes.IsKnown(" "));

        var declared = typeof(PricingErrorCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToArray();
        Assert.Equal(11, declared.Length);
        Assert.All(declared, code => Assert.StartsWith("pricing.", code, StringComparison.Ordinal));
        Assert.All(declared, code => Assert.True(PricingErrorCodes.IsKnown(code), code));
    }

    [Fact]
    public void Typed_fault_seam_maps_declared_codes_and_never_parses_message_text()
    {
        var seamPath = Path.Combine(
            Repo(), "src/backend/Modules/Pricing/Tooba.Pricing.Application/Composition/PricingOperation.cs");
        Assert.True(File.Exists(seamPath), "Application/Composition/PricingOperation.cs must exist");
        var text = File.ReadAllText(seamPath);

        Assert.Contains("ContractOperationException", text, StringComparison.Ordinal);
        Assert.Contains("PricingErrorCodes.IsKnown", text, StringComparison.Ordinal);
        Assert.Contains("SemanticException", text, StringComparison.Ordinal);
        Assert.Contains("Task<Result<T>> ExecuteAsync<T>", text, StringComparison.Ordinal);
        Assert.Contains("Task<Result> ExecuteAsync", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
        Assert.DoesNotContain("StartsWith", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Contains(\"", text, StringComparison.Ordinal);

        Assert.Equal(
            "Tooba.Pricing.Application.Composition",
            typeof(PricingOperation).Namespace);
    }

    [Fact]
    public void Promotion_consumed_write_port_lives_in_contracts_not_application()
    {
        Assert.True(File.Exists(Path.Combine(
            Repo(), "src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Ports/IPriceDirectory.cs")),
            "Contracts/Ports/IPriceDirectory.cs must exist");
        Assert.False(File.Exists(Path.Combine(
            Repo(), "src/backend/Modules/Pricing/Tooba.Pricing.Application/Ports/IPriceDirectory.cs")),
            "Application/Ports/IPriceDirectory.cs must stay retired");

        var port = Read("src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Ports/IPriceDirectory.cs");
        Assert.Contains("public interface IPriceDirectory", port, StringComparison.Ordinal);
        Assert.Contains("CreatePriceAsync", port, StringComparison.Ordinal);
        Assert.Contains("CreateCampaignPriceAsync", port, StringComparison.Ordinal);
        Assert.Contains("ActivateAsync", port, StringComparison.Ordinal);
        Assert.Contains("ChangeAmountAsync", port, StringComparison.Ordinal);
        Assert.Contains("ExpireAsync", port, StringComparison.Ordinal);

        // The module-internal guard seam stays Application-owned.
        Assert.True(File.Exists(Path.Combine(
            Repo(), "src/backend/Modules/Pricing/Tooba.Pricing.Application/Ports/IPricingUseCaseGuard.cs")));
    }

    [Fact]
    public void Pricing_Contracts_surface_uses_path_derived_namespaces()
    {
        // W2 Structure relocated the root-level seller boundary and made every Contracts namespace
        // path-derived; W1 semantics (single canonical code home, Contracts-only boundary) are unchanged.
        Assert.True(File.Exists(Path.Combine(
            Repo(), "src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Seller/SellerOfferPricingContracts.cs")),
            "Contracts/Seller/SellerOfferPricingContracts.cs must exist after the W2 structure wave");
        Assert.False(File.Exists(Path.Combine(
            Repo(), "src/backend/Modules/Pricing/Tooba.Pricing.Contracts/SellerOfferPricingContracts.cs")),
            "the Contracts root dump must stay retired");

        Assert.Contains(
            "namespace Tooba.Pricing.Contracts.Ports",
            Read("src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Ports/IPriceDirectory.cs"),
            StringComparison.Ordinal);
        Assert.Contains(
            "namespace Tooba.Pricing.Contracts.Seller",
            Read("src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Seller/SellerOfferPricingContracts.cs"),
            StringComparison.Ordinal);

        // Promotion reaches the moved port through an explicit path-derived using — never a
        // project-level alias and never a fully-qualified name that hides folder debt.
        var promotionModule = Read(
            "src/backend/Modules/Promotion/Tooba.Promotion.Infrastructure/DependencyInjection/PromotionModule.cs");
        Assert.Contains("using Tooba.Pricing.Contracts.Ports;", promotionModule, StringComparison.Ordinal);
        foreach (var file in Directory.EnumerateFiles(
                     Path.Combine(Repo(), "src/backend/Modules/Promotion/Tooba.Promotion.Infrastructure"),
                     "*.cs",
                     SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("using Tooba.Pricing.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Pricing.Application.", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Pricing.Contracts.Ports.", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Pricing.Contracts.Seller.", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Promotion_infrastructure_consumes_pricing_contracts_only()
    {
        var csproj = Read("src/backend/Modules/Promotion/Tooba.Promotion.Infrastructure/Tooba.Promotion.Infrastructure.csproj");
        Assert.DoesNotContain("Tooba.Pricing.Application", csproj, StringComparison.Ordinal);
        Assert.Contains("Tooba.Pricing.Contracts", csproj, StringComparison.Ordinal);

        var promotionRoot = Path.Combine(
            Repo(), "src/backend/Modules/Promotion/Tooba.Promotion.Infrastructure");
        var sources = Directory.EnumerateFiles(promotionRoot, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToArray();
        Assert.NotEmpty(sources);
        foreach (var file in sources)
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("using Tooba.Pricing.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Pricing.Application.", text, StringComparison.Ordinal);
        }

        // The illegal inbound edge is gone repository-wide.
        foreach (var file in Directory.EnumerateFiles(
                     Path.Combine(Repo(), "src/backend/Modules/Promotion"),
                     "*.csproj",
                     SearchOption.AllDirectories))
        {
            Assert.DoesNotContain(
                "Tooba.Pricing.Application",
                File.ReadAllText(file),
                StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Cross_module_boundary_stays_contracts_only()
    {
        foreach (var project in ProductionProjects)
        {
            var refs = ProjectRefs(project);
            Assert.DoesNotContain(refs, r => r.Contains("Host", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(refs, r => Regex.IsMatch(
                r, @"Tooba\.(Order|Fulfillment|Returns|Support|Cart|Promotion|Payment|Wallet)\.(Application|Infrastructure|Domain)"));
            Assert.DoesNotContain(refs, r => Regex.IsMatch(
                r, @"Tooba\.(Order|Fulfillment|Returns|Support|Cart|Promotion|Payment|Wallet)\.Contracts"));
        }

        // The Domain may reference only its OWN module Contracts (the canonical Contracts/Errors home).
        var domainRefs = ProjectRefs("Tooba.Pricing.Domain");
        Assert.All(
            domainRefs.Where(r => r.Contains("Contracts", StringComparison.OrdinalIgnoreCase)),
            r => Assert.Contains("Tooba.Pricing.Contracts", r, StringComparison.Ordinal));
        Assert.DoesNotContain(domainRefs, r => r.Contains("Offer.Contracts", StringComparison.Ordinal));

        // The only foreign Contracts surface is Offer.Contracts (the Offer lookup seam).
        var offerContracts = ProductionProjects
            .SelectMany(ProjectRefs)
            .Where(r => r.Contains("Tooba.Offer.Contracts", StringComparison.Ordinal))
            .ToArray();
        Assert.NotEmpty(offerContracts);
        Assert.DoesNotContain(ProductionProjects.SelectMany(ProjectRefs),
            r => r.Contains("Tooba.Offer.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(ProductionProjects.SelectMany(ProjectRefs),
            r => r.Contains("OfferDbContext", StringComparison.Ordinal));
    }

    [Fact]
    public void Stable_code_literals_are_declared_once_and_never_reinlined()
    {
        var declarationFile = Path.GetFullPath(Path.Combine(
            Repo(), "src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Errors/PricingErrorCodes.cs"));

        foreach (var project in ProductionProjects)
        {
            foreach (var file in ProductionSources(project))
            {
                if (string.Equals(Path.GetFullPath(file), declarationFile, StringComparison.OrdinalIgnoreCase))
                {
                    continue; // the declaration file is the single literal home
                }

                var text = File.ReadAllText(file);
                foreach (var code in new[]
                         {
                             "pricing.amount.invalid",
                             "pricing.overlap",
                             "pricing.offer.missing",
                             "pricing.campaign.required",
                             "pricing.validity.inverted",
                             "pricing.retired.reactivate",
                             "pricing.retired.immutable",
                             "pricing.currency.change_forbidden",
                             "pricing.market.invalid",
                             "pricing.currency.invalid",
                             "pricing.currency.display_unit",
                         })
                {
                    Assert.DoesNotContain($"\"{code}\"", text, StringComparison.Ordinal);
                }
            }
        }
    }

    [Fact]
    public void Pricing_production_never_classifies_faults_by_message_text()
    {
        foreach (var project in ProductionProjects)
        {
            foreach (var file in ProductionSources(project))
            {
                var text = File.ReadAllText(file);
                Assert.DoesNotContain(".Message.Contains(", text, StringComparison.Ordinal);
                Assert.DoesNotContain(".Message.StartsWith(", text, StringComparison.Ordinal);
                Assert.DoesNotContain(".Message ==", text, StringComparison.Ordinal);
                Assert.DoesNotContain("PlatformHttpException", text, StringComparison.Ordinal);
                Assert.DoesNotContain("TypeForwardedTo", text, StringComparison.Ordinal);
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
        var root = Path.Combine(Repo(), "src", "backend", "Modules", "Pricing", project);
        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
    }

    private static IReadOnlyList<string> ProjectRefs(string project)
    {
        var doc = System.Xml.Linq.XDocument.Load(Path.Combine(
            Repo(), "src", "backend", "Modules", "Pricing", project, $"{project}.csproj"));
        return doc.Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Where(x => x.Length > 0)
            .ToArray();
    }
}
