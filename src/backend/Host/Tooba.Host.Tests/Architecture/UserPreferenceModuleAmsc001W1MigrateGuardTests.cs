using System.Reflection;
using System.Text.RegularExpressions;
using Tooba.UserPreference.Application.Composition;
using Tooba.UserPreference.Contracts.Errors;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-USERPREFERENCE-AMSC-001-W1 — migrate wave durable guard.
/// Locks the single canonical Contracts stable-code home with its declared-code guard, the canonical
/// dual-mechanism typed-fault seam, the localization surface (catalog + bilingual resources + explicit
/// embedded logical names) and the zero-foreign coupling boundary established by the migrate wave.
/// </summary>
public sealed class UserPreferenceModuleAmsc001W1MigrateGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/UserPreference";

    private static readonly string[] ProductionProjects =
    [
        "Tooba.UserPreference.Contracts",
        "Tooba.UserPreference.Domain",
        "Tooba.UserPreference.Application",
        "Tooba.UserPreference.Endpoints",
        "Tooba.UserPreference.Infrastructure",
    ];

    /// <summary>The ten UserPreference-owned declared codes (SessionRequired is Foundation-owned).</summary>
    private static readonly string[] DeclaredCodes =
    [
        "preference.rejected",
        "ui_preference.rejected",
        "ui_preference.invalid_json",
        "ui_preference.json_required",
        "preference.validation.actor_required",
        "preference.validation.locale_required",
        "ui_preference.validation.actor_required",
        "ui_preference.validation.key_required",
        "ui_preference.validation.json_required",
        "user_preference.outbox.unmapped_event_type",
    ];

    [Fact]
    public void Stable_codes_live_in_the_single_canonical_contracts_errors_home()
    {
        Assert.True(File.Exists(Path.Combine(
            Repo(), ModuleRoot, "Tooba.UserPreference.Contracts/Errors/UserPreferenceErrorCodes.cs")),
            "Contracts/Errors/UserPreferenceErrorCodes.cs must exist");
        Assert.Contains(
            "namespace Tooba.UserPreference.Contracts.Errors",
            Read($"{ModuleRoot}/Tooba.UserPreference.Contracts/Errors/UserPreferenceErrorCodes.cs"),
            StringComparison.Ordinal);

        // No second declaration of the same stable identity anywhere in UserPreference production.
        var declarations = ProductionProjects
            .SelectMany(ProductionSources)
            .Count(file => File.ReadAllText(file).Contains("class UserPreferenceErrorCodes", StringComparison.Ordinal));
        Assert.Equal(1, declarations);

        Assert.Equal("Tooba.UserPreference.Contracts.Errors", typeof(UserPreferenceErrorCodes).Namespace);
        Assert.Equal("Tooba.UserPreference.Contracts", typeof(UserPreferenceErrorCodes).Assembly.GetName().Name);
    }

    [Fact]
    public void Declared_code_catalog_is_the_single_known_code_source()
    {
        foreach (var code in DeclaredCodes)
        {
            Assert.True(UserPreferenceErrorCodes.IsKnown(code), code);
        }

        // Foundation-owned and unknown/empty input are never classified as UserPreference faults.
        Assert.False(UserPreferenceErrorCodes.IsKnown("customer.session.required"));
        Assert.False(UserPreferenceErrorCodes.IsKnown("order.not_found"));
        Assert.False(UserPreferenceErrorCodes.IsKnown("tax.rate.out_of_range"));
        Assert.False(UserPreferenceErrorCodes.IsKnown(null));
        Assert.False(UserPreferenceErrorCodes.IsKnown(string.Empty));
        Assert.False(UserPreferenceErrorCodes.IsKnown(" "));

        // The Foundation-owned session constant stays declared for consumption but never becomes a
        // module-owned use-case fault.
        Assert.Equal("customer.session.required", UserPreferenceErrorCodes.SessionRequired);

        var declared = typeof(UserPreferenceErrorCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(11, declared.Length);
        Assert.Equal(
            DeclaredCodes.Append("customer.session.required").OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            declared);
        Assert.Contains("customer.session.required", declared);

        var known = declared.Where(UserPreferenceErrorCodes.IsKnown)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(DeclaredCodes.OrderBy(x => x, StringComparer.Ordinal).ToArray(), known);
    }

    [Fact]
    public void Typed_fault_seam_maps_declared_codes_and_never_parses_message_text()
    {
        var seamPath = Path.Combine(
            Repo(), ModuleRoot, "Tooba.UserPreference.Application/Composition/UserPreferenceOperation.cs");
        Assert.True(File.Exists(seamPath), "Application/Composition/UserPreferenceOperation.cs must exist");
        var text = File.ReadAllText(seamPath);

        Assert.Contains("ContractOperationException", text, StringComparison.Ordinal);
        Assert.Contains("UserPreferenceErrorCodes.IsKnown", text, StringComparison.Ordinal);
        Assert.Contains("SemanticException", text, StringComparison.Ordinal);
        Assert.Contains("Task<Result<T>> ExecuteAsync<T>", text, StringComparison.Ordinal);
        Assert.Contains("Task<Result> ExecuteAsync", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
        Assert.DoesNotContain("StartsWith", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IResult", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGroup(", text, StringComparison.Ordinal);

        Assert.Equal("Tooba.UserPreference.Application.Composition", typeof(UserPreferenceOperation).Namespace);
    }

    [Fact]
    public void Error_catalog_and_bilingual_resources_are_registered_once_by_the_composition_root()
    {
        var module = Read($"{ModuleRoot}/Tooba.UserPreference.Infrastructure/UserPreferenceModule.cs");
        Assert.Equal(1, Regex.Matches(module,
            @"AddSingleton<\s*IErrorCatalogContributor,\s*UserPreferenceErrorCatalogContributor>").Count);
        Assert.Equal(1, Regex.Matches(module,
            @"AddSingleton<\s*IErrorResourceSet,\s*UserPreferenceErrorResourceSet>").Count);

        // Contracts-owned concrete types.
        Assert.Equal("Tooba.UserPreference.Contracts", typeof(UserPreferenceErrorCatalogContributor).Assembly.GetName().Name);
        Assert.Equal("Tooba.UserPreference.Contracts.Errors", typeof(UserPreferenceErrorCatalogContributor).Namespace);
        Assert.Equal("Tooba.UserPreference.Contracts.Errors", typeof(UserPreferenceErrorResourceSet).Namespace);

        var descriptors = new UserPreferenceErrorCatalogContributor().Contribute().ToArray();
        Assert.Equal(10, descriptors.Length);
        Assert.Equal(
            descriptors.Length,
            descriptors.Select(d => d.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(
            DeclaredCodes.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            descriptors.Select(d => d.Code).OrderBy(x => x, StringComparer.Ordinal).ToArray());
        foreach (var descriptor in descriptors)
        {
            Assert.Equal(descriptor.Code, descriptor.LocalizationKey);
            Assert.True(UserPreferenceErrorCodes.IsKnown(descriptor.Code), descriptor.Code);
        }

        // Foundation owns customer.session.required; UserPreference must never claim its descriptor.
        Assert.DoesNotContain(descriptors, d => d.Code == "customer.session.required");

        var en = Read($"{ModuleRoot}/Tooba.UserPreference.Contracts/Resources/UserPreferenceErrors.resx");
        var fa = Read($"{ModuleRoot}/Tooba.UserPreference.Contracts/Resources/UserPreferenceErrors.fa.resx");
        var enKeys = Regex.Matches(en, "<data name=\"([^\"]+)\"").Select(m => m.Groups[1].Value)
            .OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var faKeys = Regex.Matches(fa, "<data name=\"([^\"]+)\"").Select(m => m.Groups[1].Value)
            .OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(10, enKeys.Length);
        Assert.Equal(DeclaredCodes.OrderBy(x => x, StringComparer.Ordinal).ToArray(), enKeys);
        Assert.Equal(enKeys, faKeys);

        // The bilingual logical names are explicit and locked (not left to SDK convention).
        var contracts = Read($"{ModuleRoot}/Tooba.UserPreference.Contracts/Tooba.UserPreference.Contracts.csproj");
        Assert.Contains("Tooba.UserPreference.Contracts.Resources.UserPreferenceErrors.resources", contracts, StringComparison.Ordinal);
        Assert.Contains("Tooba.UserPreference.Contracts.Resources.UserPreferenceErrors.fa.resources", contracts, StringComparison.Ordinal);

        // Both cultures resolve through the module-owned resource set.
        var set = new UserPreferenceErrorResourceSet();
        var enCulture = new System.Globalization.CultureInfo("en");
        var faCulture = new System.Globalization.CultureInfo("fa");
        Assert.True(set.Owns("preference.rejected"));
        Assert.True(set.Owns("ui_preference.validation.key_required"));
        Assert.False(set.Owns("customer.session.required"));
        Assert.False(set.Owns("order.not_found"));
        foreach (var code in DeclaredCodes)
        {
            Assert.False(string.IsNullOrWhiteSpace(set.GetString(code, enCulture)), $"{code} (en)");
            Assert.False(string.IsNullOrWhiteSpace(set.GetString(code, faCulture)), $"{code} (fa)");
        }
    }

    [Fact]
    public void Stable_code_literals_are_declared_once_and_never_reinlined()
    {
        var declarationFile = Path.GetFullPath(Path.Combine(
            Repo(), ModuleRoot, "Tooba.UserPreference.Contracts/Errors/UserPreferenceErrorCodes.cs"));

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
    public void UserPreference_production_never_classifies_faults_by_message_text()
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
            @"Tooba\.(Tax|Order|Cart|Catalog|Identity|AccessControl|Content|CustomerProfile|Notification|Media|Fulfillment|Inventory|BulkInquiry|Localization|Settlement|Story|Wishlist|ProductQnA|Reviews|OperatorProfile|PageComposition|Promotion|Returns|Wallet|Support|AddressBook|Payment|StoreContext|Pricing|Offer|ProductWorkspace|Party|Host)\.(Application|Infrastructure|Domain|Endpoints)";

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
                    Regex.IsMatch(reference, @"Tooba\.(?!UserPreference\.)[A-Za-z]+\.(Application|Infrastructure|Domain|Endpoints)\.csproj"),
                    $"{project} -> {reference}");
            }
        }

        // The Domain may reference only its OWN module Contracts (the canonical Contracts/Errors home).
        var domainRefs = ProjectRefs("Tooba.UserPreference.Domain");
        Assert.All(
            domainRefs.Where(r => r.Contains("Contracts", StringComparison.OrdinalIgnoreCase)),
            r => Assert.Contains("Tooba.UserPreference.Contracts", r, StringComparison.Ordinal));

        // The only legal foreign edge is the Order.Contracts guest-actor vocabulary at the HTTP boundary.
        var endpointsRefs = ProjectRefs("Tooba.UserPreference.Endpoints");
        Assert.Contains(endpointsRefs, r => r.Contains("Tooba.Order.Contracts", StringComparison.Ordinal));
        Assert.DoesNotContain(endpointsRefs, r => r.Contains("Tooba.Order.Application", StringComparison.Ordinal));
        Assert.DoesNotContain(endpointsRefs, r => r.Contains("Tooba.Order.Domain", StringComparison.Ordinal));
        Assert.DoesNotContain(endpointsRefs, r => r.Contains("Tooba.Order.Infrastructure", StringComparison.Ordinal));
    }

    [Fact]
    public void No_userpreference_production_source_uses_a_raw_invalid_operation_fault()
    {
        // Every former raw string fault is now a declared stable code carried by a typed exception.
        foreach (var file in ProductionProjects.SelectMany(ProductionSources))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotMatch(new Regex(@"InvalidOperationException\(""[^""]+""\)"), text);
            Assert.DoesNotContain("UserPreference integration event is not registered.", text, StringComparison.Ordinal);
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
