using System.Reflection;
using System.Text.RegularExpressions;
using Tooba.Tax.Application.Composition;
using Tooba.Tax.Contracts.Errors;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-TAX-AMSC-001-W1 — migrate wave durable guard.
/// Locks the single canonical Contracts stable-code home with its declared-code guard, the canonical
/// typed-fault seam, the localization surface (catalog + bilingual resources) and the zero-foreign
/// coupling boundary established by the migrate wave.
///
/// TB-TMAR-TAX-AMSC-001-W2 made every production namespace path-derived, so the physical paths pinned
/// here follow the post-W2 tree while keeping every W1 assertion intact (never weakened).
/// </summary>
public sealed class TaxModuleAmsc001W1MigrateGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/Tax";

    private static readonly string[] ProductionProjects =
    [
        "Tooba.Tax.Contracts",
        "Tooba.Tax.Domain",
        "Tooba.Tax.Application",
        "Tooba.Tax.Infrastructure",
    ];

    private static readonly string[] DeclaredCodes =
    [
        "tax.rule.id_required",
        "tax.jurisdiction.required",
        "tax.market.required",
        "tax.validity.inverted",
        "tax.rate.out_of_range",
        "tax.rate.not_applicable",
        "tax.rate.kind_mismatch",
        "tax.category.id_required",
        "tax.category.code_required",
        "tax.category.missing",
        "tax.outbox.unmapped_event_type",
    ];

    [Fact]
    public void Stable_codes_live_in_the_single_canonical_contracts_errors_home()
    {
        Assert.True(File.Exists(Path.Combine(
            Repo(), ModuleRoot, "Tooba.Tax.Contracts/Errors/TaxErrorCodes.cs")),
            "Contracts/Errors/TaxErrorCodes.cs must exist");
        Assert.Contains(
            "namespace Tooba.Tax.Contracts.Errors",
            Read($"{ModuleRoot}/Tooba.Tax.Contracts/Errors/TaxErrorCodes.cs"),
            StringComparison.Ordinal);

        // No second declaration of the same stable identity anywhere in Tax production.
        var declarations = ProductionProjects
            .SelectMany(ProductionSources)
            .Count(file => File.ReadAllText(file).Contains("class TaxErrorCodes", StringComparison.Ordinal));
        Assert.Equal(1, declarations);

        Assert.Equal("Tooba.Tax.Contracts.Errors", typeof(TaxErrorCodes).Namespace);
        Assert.Equal("Tooba.Tax.Contracts", typeof(TaxErrorCodes).Assembly.GetName().Name);
    }

    [Fact]
    public void Declared_code_catalog_is_the_single_known_code_source()
    {
        foreach (var code in DeclaredCodes)
        {
            Assert.True(TaxErrorCodes.IsKnown(code), code);
        }

        // Foreign-owned codes and unknown/empty input are never classified as Tax faults.
        Assert.False(TaxErrorCodes.IsKnown("checkout.tax.unavailable"));
        Assert.False(TaxErrorCodes.IsKnown("order.not_found"));
        Assert.False(TaxErrorCodes.IsKnown("pricing.amount.invalid"));
        Assert.False(TaxErrorCodes.IsKnown(null));
        Assert.False(TaxErrorCodes.IsKnown(string.Empty));
        Assert.False(TaxErrorCodes.IsKnown(" "));

        var declared = typeof(TaxErrorCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(11, declared.Length);
        Assert.Equal(DeclaredCodes.OrderBy(x => x, StringComparer.Ordinal).ToArray(), declared);
        Assert.All(declared, code => Assert.StartsWith("tax.", code, StringComparison.Ordinal));
        Assert.All(declared, code => Assert.True(TaxErrorCodes.IsKnown(code), code));
    }

    [Fact]
    public void Typed_fault_seam_maps_declared_codes_and_never_parses_message_text()
    {
        var seamPath = Path.Combine(
            Repo(), ModuleRoot, "Tooba.Tax.Application/Composition/TaxOperation.cs");
        Assert.True(File.Exists(seamPath), "Application/Composition/TaxOperation.cs must exist");
        var text = File.ReadAllText(seamPath);

        Assert.Contains("ContractOperationException", text, StringComparison.Ordinal);
        Assert.Contains("TaxErrorCodes.IsKnown", text, StringComparison.Ordinal);
        Assert.Contains("SemanticException", text, StringComparison.Ordinal);
        Assert.Contains("Task<Result<T>> ExecuteAsync<T>", text, StringComparison.Ordinal);
        Assert.Contains("Task<Result> ExecuteAsync", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
        Assert.DoesNotContain("StartsWith", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IResult", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGroup(", text, StringComparison.Ordinal);

        Assert.Equal("Tooba.Tax.Application.Composition", typeof(TaxOperation).Namespace);
    }

    [Fact]
    public void Error_catalog_and_bilingual_resources_are_registered_once_by_the_composition_root()
    {
        var module = Read($"{ModuleRoot}/Tooba.Tax.Infrastructure/DependencyInjection/TaxModule.cs");
        Assert.Equal(1, Regex.Matches(module,
            @"AddSingleton<\s*IErrorCatalogContributor,\s*TaxErrorCatalogContributor>").Count);
        Assert.Equal(1, Regex.Matches(module,
            @"AddSingleton<\s*IErrorResourceSet,\s*TaxErrorResourceSet>").Count);

        // Contracts-owned concrete types.
        Assert.Equal("Tooba.Tax.Contracts", typeof(TaxErrorCatalogContributor).Assembly.GetName().Name);
        Assert.Equal("Tooba.Tax.Contracts.Errors", typeof(TaxErrorCatalogContributor).Namespace);
        Assert.Equal("Tooba.Tax.Contracts.Errors", typeof(TaxErrorResourceSet).Namespace);

        var descriptors = new TaxErrorCatalogContributor().Contribute().ToArray();
        Assert.Equal(11, descriptors.Length);
        Assert.Equal(
            descriptors.Length,
            descriptors.Select(d => d.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(
            DeclaredCodes.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            descriptors.Select(d => d.Code).OrderBy(x => x, StringComparer.Ordinal).ToArray());
        foreach (var descriptor in descriptors)
        {
            Assert.StartsWith("tax.", descriptor.Code, StringComparison.Ordinal);
            Assert.Equal(descriptor.Code, descriptor.LocalizationKey);
            Assert.True(TaxErrorCodes.IsKnown(descriptor.Code), descriptor.Code);
        }

        // Order owns checkout.tax.unavailable; Tax must never claim its descriptor.
        Assert.DoesNotContain(descriptors, d => d.Code == "checkout.tax.unavailable");

        var en = Read($"{ModuleRoot}/Tooba.Tax.Contracts/Resources/TaxErrors.resx");
        var fa = Read($"{ModuleRoot}/Tooba.Tax.Contracts/Resources/TaxErrors.fa.resx");
        var enKeys = Regex.Matches(en, "<data name=\"(tax\\.[^\"]+)\"").Select(m => m.Groups[1].Value)
            .OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var faKeys = Regex.Matches(fa, "<data name=\"(tax\\.[^\"]+)\"").Select(m => m.Groups[1].Value)
            .OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(11, enKeys.Length);
        Assert.Equal(DeclaredCodes.OrderBy(x => x, StringComparer.Ordinal).ToArray(), enKeys);
        Assert.Equal(enKeys, faKeys);

        var contracts = Read($"{ModuleRoot}/Tooba.Tax.Contracts/Tooba.Tax.Contracts.csproj");
        Assert.Contains("Tooba.Tax.Contracts.Resources.TaxErrors.resources", contracts, StringComparison.Ordinal);
        Assert.Contains("Tooba.Tax.Contracts.Resources.TaxErrors.fa.resources", contracts, StringComparison.Ordinal);

        Assert.True(new TaxErrorResourceSet().Owns("tax.rate.out_of_range"));
        Assert.False(new TaxErrorResourceSet().Owns("checkout.tax.unavailable"));
        Assert.False(new TaxErrorResourceSet().Owns("order.not_found"));
    }

    [Fact]
    public void Stable_code_literals_are_declared_once_and_never_reinlined()
    {
        var declarationFile = Path.GetFullPath(Path.Combine(
            Repo(), ModuleRoot, "Tooba.Tax.Contracts/Errors/TaxErrorCodes.cs"));

        foreach (var file in ProductionProjects.SelectMany(ProductionSources))
        {
            if (string.Equals(Path.GetFullPath(file), declarationFile, StringComparison.OrdinalIgnoreCase))
            {
                continue; // the declaration file is the single literal home
            }

            var text = File.ReadAllText(file);
            foreach (var code in DeclaredCodes)
            {
                Assert.DoesNotContain($"\"{code}\"", text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Tax_production_never_classifies_faults_by_message_text()
    {
        foreach (var file in ProductionProjects.SelectMany(ProductionSources))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain(".Message.Contains(", text, StringComparison.Ordinal);
            Assert.DoesNotContain(".Message.StartsWith(", text, StringComparison.Ordinal);
            Assert.DoesNotContain(".Message ==", text, StringComparison.Ordinal);
            Assert.DoesNotContain("PlatformHttpException", text, StringComparison.Ordinal);
            Assert.DoesNotContain("TypeForwardedTo", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Cross_module_boundary_stays_contracts_only()
    {
        var foreignPattern =
            @"Tooba\.(Order|Cart|Catalog|Identity|AccessControl|Content|CustomerProfile|Notification|Media|Fulfillment|Inventory|BulkInquiry|Localization|Settlement|Story|Wishlist|UserPreference|ProductQnA|Reviews|OperatorProfile|PageComposition|Promotion|Returns|Wallet|Support|AddressBook|Payment|StoreContext|Pricing|Offer)\.(Application|Infrastructure|Domain|Endpoints)";

        foreach (var project in ProductionProjects)
        {
            foreach (var file in ProductionSources(project))
            {
                Assert.False(
                    Regex.IsMatch(File.ReadAllText(file), foreignPattern),
                    $"foreign module Application/Infrastructure/Domain/Endpoints reference in {file}");
            }

            foreach (var reference in ProjectRefs(project))
            {
                Assert.DoesNotContain("Host", reference, StringComparison.OrdinalIgnoreCase);
                Assert.False(
                    Regex.IsMatch(reference, @"Tooba\.(?!Tax\.)[A-Za-z]+\.(Application|Infrastructure|Domain|Endpoints)\.csproj"),
                    $"{project} -> {reference}");
            }
        }

        // The Domain may reference only its OWN module Contracts (the canonical Contracts/Errors home).
        var domainRefs = ProjectRefs("Tooba.Tax.Domain");
        Assert.All(
            domainRefs.Where(r => r.Contains("Contracts", StringComparison.OrdinalIgnoreCase)),
            r => Assert.Contains("Tooba.Tax.Contracts", r, StringComparison.Ordinal));
        Assert.DoesNotContain(domainRefs, r => r.Contains("Offer.Contracts", StringComparison.Ordinal));
        Assert.DoesNotContain(domainRefs, r => r.Contains("Pricing.Contracts", StringComparison.Ordinal));
    }

    [Fact]
    public void No_tax_production_source_uses_a_raw_invalid_operation_fault()
    {
        // Every former raw string fault is now a declared stable code carried by a typed exception.
        foreach (var file in ProductionProjects.SelectMany(ProductionSources))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotMatch(new Regex(@"InvalidOperationException\(""[^""]+""\)"), text);
            Assert.DoesNotContain("Unmapped Tax integration event type.", text, StringComparison.Ordinal);
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
        File.ReadAllText(Path.Combine(Repo(), relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static IEnumerable<string> ProductionSources(string project)
    {
        var root = Path.Combine(Repo(), ModuleRoot.Replace('/', Path.DirectorySeparatorChar), project);
        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
    }

    private static IReadOnlyList<string> ProjectRefs(string project)
    {
        var doc = System.Xml.Linq.XDocument.Load(Path.Combine(
            Repo(), ModuleRoot.Replace('/', Path.DirectorySeparatorChar), project, $"{project}.csproj"));
        return doc.Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Where(x => x.Length > 0)
            .ToArray();
    }
}
