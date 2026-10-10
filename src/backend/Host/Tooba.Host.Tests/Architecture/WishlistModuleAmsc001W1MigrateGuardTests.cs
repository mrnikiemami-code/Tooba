using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Wishlist.Application.Composition;
using Tooba.Wishlist.Application.Customer.Commands;
using Tooba.Wishlist.Application.Customer.Queries;
using Tooba.Wishlist.Contracts.Errors;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-WISHLIST-AMSC-001-W1 — migrate wave durable guard.
/// Locks the single canonical Contracts stable-code home with its declared-code guard, the canonical
/// dual-mechanism typed-fault seam (no message-text classification), the retired raw outbox fault, the
/// bilingual resource coverage with explicit locked embedded logical names, the exhaustive transport
/// validator matrix (3 required + 1 proven exemption), the Foundation session code, and the
/// Contracts-only boundary so the module stays extractable as an isolated microservice.
/// </summary>
public sealed class WishlistModuleAmsc001W1MigrateGuardTests
{
    private const string ModuleRootRelative = "src/backend/Modules/Wishlist";

    private static readonly string[] ProductionProjects =
    [
        "Tooba.Wishlist.Contracts",
        "Tooba.Wishlist.Domain",
        "Tooba.Wishlist.Application",
        "Tooba.Wishlist.Infrastructure",
        "Tooba.Wishlist.Endpoints",
    ];

    /// <summary>The four Wishlist-owned stable machine codes.</summary>
    private static readonly string[] WishlistOwnedCodes =
    [
        "customer.wishlist.product_unavailable",
        "customer.wishlist.product_id_required",
        "customer.wishlist.product_ids_required",
        "wishlist.outbox.unmapped_event_type",
    ];

    [Fact]
    public void Stable_codes_live_in_the_single_canonical_contracts_errors_home()
    {
        var declaration = FindFile("Tooba.Wishlist.Contracts", "WishlistErrorCodes.cs");
        Assert.NotNull(declaration);
        Assert.Contains(
            "namespace Tooba.Wishlist.Contracts.Errors",
            File.ReadAllText(declaration!),
            StringComparison.Ordinal);

        Assert.Equal("Tooba.Wishlist.Contracts.Errors", typeof(WishlistErrorCodes).Namespace);
        Assert.Equal("Tooba.Wishlist.Contracts", typeof(WishlistErrorCodes).Assembly.GetName().Name);

        var declarations = ProductionProjects
            .SelectMany(ProductionSources)
            .Count(file => File.ReadAllText(file).Contains("class WishlistErrorCodes", StringComparison.Ordinal));
        Assert.Equal(1, declarations);
    }

    [Fact]
    public void Declared_code_catalog_has_the_declared_code_guard()
    {
        var codesFile = File.ReadAllText(FindFile("Tooba.Wishlist.Contracts", "WishlistErrorCodes.cs")!);
        Assert.Contains("private static readonly HashSet<string> KnownCodes", codesFile, StringComparison.Ordinal);
        Assert.Contains("public static bool IsKnown(string? code)", codesFile, StringComparison.Ordinal);

        foreach (var code in WishlistOwnedCodes)
        {
            Assert.True(WishlistErrorCodes.IsKnown(code), code);
        }

        Assert.False(WishlistErrorCodes.IsKnown(null));
        Assert.False(WishlistErrorCodes.IsKnown(string.Empty));
        Assert.False(WishlistErrorCodes.IsKnown(" "));

        // Foundation-owned cross-cutting code is consumable but never a Wishlist use-case fault.
        Assert.False(WishlistErrorCodes.IsKnown(WishlistErrorCodes.SessionRequired));
        Assert.False(WishlistErrorCodes.IsKnown("order.not_found"));

        var declared = DeclaredCodes();
        Assert.Equal(5, declared.Length);
        Assert.Equal(
            WishlistOwnedCodes.Append(WishlistErrorCodes.SessionRequired)
                .OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            declared.OrderBy(x => x, StringComparer.Ordinal).ToArray());
        Assert.Equal(declared.Length, declared.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Typed_fault_seam_maps_declared_codes_and_never_parses_message_text()
    {
        var seam = FindFile("Tooba.Wishlist.Application", "WishlistOperation.cs");
        Assert.NotNull(seam);
        var text = File.ReadAllText(seam!);

        Assert.Contains("ContractOperationException", text, StringComparison.Ordinal);
        Assert.Contains("WishlistErrorCodes.IsKnown", text, StringComparison.Ordinal);
        Assert.Contains("SemanticException", text, StringComparison.Ordinal);
        Assert.Contains("Task<Result<T>> ExecuteAsync<T>", text, StringComparison.Ordinal);
        Assert.Contains("Task<Result> ExecuteAsync", text, StringComparison.Ordinal);
        Assert.Equal(
            2,
            Regex.Matches(text, @"catch \(ContractOperationException ex\) when \(WishlistErrorCodes\.IsKnown\(ex\.Code\)\)").Count);
        Assert.Equal(2, Regex.Matches(text, @"catch \(SemanticException ex\)").Count);
        Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
        Assert.DoesNotContain(".Message.Contains(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("StartsWith(\"wishlist.", text, StringComparison.Ordinal);

        Assert.Equal("Tooba.Wishlist.Application.Composition", typeof(WishlistOperation).Namespace);
    }

    [Fact]
    public void Raw_outbox_fault_is_retired_for_a_typed_contract_code()
    {
        var outbox = FindFile("Tooba.Wishlist.Infrastructure", "WishlistOutboxRegistration.cs");
        Assert.NotNull(outbox);
        var text = File.ReadAllText(outbox!);

        Assert.Contains(
            "ContractOperationException(WishlistErrorCodes.OutboxUnmappedEventType)",
            text,
            StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Wishlist integration event is not registered.", text, StringComparison.Ordinal);
        Assert.Contains("Translate(IDomainEvent domainEvent, EventMetadata metadata) => null", text, StringComparison.Ordinal);
        Assert.Contains("ResolveEventClrType(string eventTypeName) => null", text, StringComparison.Ordinal);

        // The typed fault is exercised through the canonical seam and maps to the declared code.
        var registration = new Tooba.Wishlist.Infrastructure.Persistence.WishlistOutboxRegistration();
        var exception = Assert.Throws<ContractOperationException>(
            () => registration.GetEventTypeName(typeof(object)));
        Assert.Equal(WishlistErrorCodes.OutboxUnmappedEventType, exception.Code);
        Assert.True(WishlistErrorCodes.IsKnown(exception.Code));
    }

    [Fact]
    public void Error_localization_resource_set_is_wishlist_owned_and_bilingual()
    {
        var setText = File.ReadAllText(FindFile("Tooba.Wishlist.Contracts", "WishlistErrorResourceSet.cs")!);
        Assert.Contains("IErrorResourceSet", setText, StringComparison.Ordinal);
        Assert.Contains("\"customer.wishlist.\"", setText, StringComparison.Ordinal);
        Assert.Contains("\"wishlist.\"", setText, StringComparison.Ordinal);

        var en = FindFile("Tooba.Wishlist.Contracts", "WishlistErrors.resx");
        var fa = FindFile("Tooba.Wishlist.Contracts", "WishlistErrors.fa.resx");
        Assert.NotNull(en);
        Assert.NotNull(fa);

        var enText = File.ReadAllText(en!);
        var faText = File.ReadAllText(fa!);
        foreach (var code in WishlistOwnedCodes)
        {
            Assert.Contains($"name=\"{code}\"", enText, StringComparison.Ordinal);
            Assert.Contains($"name=\"{code}\"", faText, StringComparison.Ordinal);
        }

        // Every resx entry is a real declared code (no orphan localization key).
        foreach (var resx in new[] { enText, faText })
        {
            var keys = Regex.Matches(resx, @"data name=""([^""]+)""")
                .Select(m => m.Groups[1].Value)
                .ToArray();
            Assert.Equal(WishlistOwnedCodes.Length, keys.Length);
            Assert.All(keys, key => Assert.Contains(key, WishlistOwnedCodes));
        }

        // The bilingual logical names are explicit and locked (not left to SDK convention).
        var contractsCsproj = Read("Tooba.Wishlist.Contracts");
        Assert.Contains(
            "Tooba.Wishlist.Contracts.Resources.WishlistErrors.resources",
            contractsCsproj,
            StringComparison.Ordinal);
        Assert.Contains(
            "Tooba.Wishlist.Contracts.Resources.WishlistErrors.fa.resources",
            contractsCsproj,
            StringComparison.Ordinal);

        // The contributor registers the four Wishlist-owned codes exactly once.
        var contributor = File.ReadAllText(FindFile("Tooba.Wishlist.Contracts", "WishlistErrorCatalogContributor.cs")!);
        foreach (var field in new[]
                 {
                     "ProductUnavailable", "ProductIdRequired", "ProductIdsRequired", "OutboxUnmappedEventType",
                 })
        {
            Assert.Contains($"WishlistErrorCodes.{field}", contributor, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("SessionRequired", contributor, StringComparison.Ordinal);

        var descriptors = new WishlistErrorCatalogContributor().Contribute().ToArray();
        Assert.Equal(WishlistOwnedCodes.Length, descriptors.Length);
        Assert.DoesNotContain(descriptors, d => d.Code == WishlistErrorCodes.SessionRequired);

        // The module-owned resource set resolves every declared code in both cultures.
        var resourceSet = new WishlistErrorResourceSet();
        var enCulture = CultureInfo.GetCultureInfo("en");
        var faCulture = CultureInfo.GetCultureInfo("fa");
        Assert.True(resourceSet.Owns(WishlistErrorCodes.ProductUnavailable));
        Assert.True(resourceSet.Owns(WishlistErrorCodes.OutboxUnmappedEventType));
        Assert.False(resourceSet.Owns(WishlistErrorCodes.SessionRequired));
        Assert.False(resourceSet.Owns("order.not_found"));
        foreach (var code in WishlistOwnedCodes)
        {
            Assert.False(string.IsNullOrWhiteSpace(resourceSet.GetString(code, enCulture)), $"{code} (en)");
            var faValue = resourceSet.GetString(code, faCulture);
            Assert.False(string.IsNullOrWhiteSpace(faValue), $"{code} (fa)");
            Assert.True(faValue!.Any(ch => ch is >= '\u0600' and <= '\u06ff'), $"expected Persian for {code}");
        }
    }

    [Fact]
    public void Transport_validators_cover_the_three_caller_controlled_requests()
    {
        var validators = FindFile("Tooba.Wishlist.Application", "WishlistValidators.cs");
        Assert.NotNull(validators);
        var text = File.ReadAllText(validators!);

        foreach (var type in new[]
                 {
                     "AddWishlistItemCommandValidator",
                     "RemoveWishlistItemCommandValidator",
                     "GetWishlistMembershipQueryValidator",
                 })
        {
            Assert.Contains(type, text, StringComparison.Ordinal);
        }

        Assert.Equal(3, Regex.Matches(text, @"class \w+Validator\s*:\s*AbstractValidator<").Count);
        Assert.DoesNotContain("AbstractValidator<ListWishlistPageQuery>", text, StringComparison.Ordinal);

        // Validators emit stable machine codes, never user-facing prose.
        Assert.DoesNotContain("WithMessage(", text, StringComparison.Ordinal);
        Assert.Contains("WishlistErrorCodes.", text, StringComparison.Ordinal);
        Assert.DoesNotContain("\"customer.wishlist.", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Validators_are_discoverable_through_the_real_cqrs_registration()
    {
        // Non-circular proof: the three transport validators resolve from a real container built by the
        // canonical foundation registration for the Wishlist Application assembly.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(AddWishlistItemCommand).Assembly);
        using var provider = services.BuildServiceProvider();

        foreach (var required in new[]
                 {
                     typeof(AddWishlistItemCommand),
                     typeof(RemoveWishlistItemCommand),
                     typeof(GetWishlistMembershipQuery),
                 })
        {
            var validatorType = typeof(IValidator<>).MakeGenericType(required);
            Assert.NotNull(provider.GetService(validatorType));
        }

        var exemptType = typeof(IValidator<>).MakeGenericType(typeof(ListWishlistPageQuery));
        Assert.Null(provider.GetService(exemptType));
    }

    [Fact]
    public void Customer_endpoints_use_the_foundation_session_code_and_host_has_no_wishlist_authority()
    {
        var customer = FindFile("Tooba.Wishlist.Endpoints", "WishlistCustomerEndpoints.cs");
        Assert.NotNull(customer);
        var customerText = File.ReadAllText(customer!);
        Assert.Contains("WishlistErrorCodes.SessionRequired", customerText, StringComparison.Ordinal);
        Assert.DoesNotContain("\"customer.session.required\"", customerText, StringComparison.Ordinal);

        var hostRoot = Path.Combine(Repo(), "src/backend/Host/Tooba.Host");
        Assert.False(Directory.Exists(Path.Combine(hostRoot, "Wishlist")));

        var program = File.ReadAllText(Path.Combine(hostRoot, "Program.cs"));
        Assert.Contains("MapWishlistModuleEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("AddWishlistEndpointPresentation", program, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host.Wishlist", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Wishlist_never_references_foreign_application_infrastructure_or_domain()
    {
        var foreignPattern =
            @"Tooba\.(Order|Cart|Catalog|Identity|AccessControl|Content|CustomerProfile|Notification|Media|Fulfillment|Inventory|BulkInquiry|Localization|Settlement|Story|ProductQnA|Reviews|OperatorProfile|PageComposition|Promotion|Returns|Wallet|Support|AddressBook|Payment|StoreContext|Pricing|Offer|Party|Tax|ProductWorkspace|Host)\.(Application|Infrastructure|Domain|Endpoints)";

        foreach (var project in ProductionProjects)
        {
            foreach (var file in ProductionSources(project))
            {
                var text = File.ReadAllText(file);
                Assert.False(
                    Regex.IsMatch(text, foreignPattern),
                    $"foreign module coupling in {Path.GetFileName(file)}");

                foreach (var foreignContext in new[]
                         {
                             "CatalogDbContext", "OrderDbContext", "NotificationDbContext",
                             "IdentityDbContext", "AccessControlDbContext", "PaymentDbContext",
                             "WalletDbContext", "CartDbContext", "InventoryDbContext",
                         })
                {
                    Assert.DoesNotContain(foreignContext, text, StringComparison.Ordinal);
                }
            }
        }

        // Endpoints must never reach Infrastructure or Host.
        var endpointsRefs = ProjectRefs("Tooba.Wishlist.Endpoints");
        Assert.Contains(endpointsRefs, r => r.Contains("Wishlist.Application", StringComparison.Ordinal));
        Assert.DoesNotContain(endpointsRefs, r => r.Contains("Wishlist.Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(endpointsRefs, r => r.Contains("Tooba.Host", StringComparison.OrdinalIgnoreCase));

        // Domain must depend only on BuildingBlocks + Wishlist.Contracts.
        var domainRefs = ProjectRefs("Tooba.Wishlist.Domain");
        Assert.Contains(domainRefs, r => r.Contains("Tooba.Wishlist.Contracts", StringComparison.Ordinal));
        Assert.DoesNotContain(domainRefs, r => r.Contains("Wishlist.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(domainRefs, r => r.Contains("Wishlist.Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(domainRefs, r => r.Contains("Wishlist.Endpoints", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Wishlist_production_never_classifies_faults_by_message_text_or_raw_literal()
    {
        foreach (var project in ProductionProjects)
        {
            foreach (var file in ProductionSources(project))
            {
                var text = File.ReadAllText(file);
                Assert.DoesNotContain(".Message.Contains(", text, StringComparison.Ordinal);
                Assert.DoesNotContain(".Message.StartsWith(", text, StringComparison.Ordinal);
                Assert.DoesNotContain(".Message ==", text, StringComparison.Ordinal);
                Assert.DoesNotMatch(new Regex(@"InvalidOperationException\(""[^""]+""\)"), text);
            }
        }
    }

    [Fact]
    public void Migration_did_not_change_the_wishlist_schema_migrations()
    {
        var migrations = Directory
            .EnumerateFiles(Path.Combine(Repo(), ModuleRootRelative, "Tooba.Wishlist.Infrastructure"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                           && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => Path.GetFileName(path).Contains("InitialWishlist", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            ["20260825162434_InitialWishlist.Designer.cs", "20260825162434_InitialWishlist.cs"],
            migrations);

        var schema = FindFile("Tooba.Wishlist.Infrastructure", "WishlistDbContext.cs");
        Assert.NotNull(schema);
        Assert.Contains("\"wishlist\"", File.ReadAllText(schema!), StringComparison.Ordinal);
    }

    private static string[] DeclaredCodes() =>
        typeof(WishlistErrorCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToArray();

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

    private static string Read(string project) =>
        File.ReadAllText(Path.Combine(Repo(), ModuleRootRelative, project, $"{project}.csproj"));

    private static string? FindFile(string project, string fileName)
    {
        var root = Path.Combine(Repo(), ModuleRootRelative, project);
        if (!Directory.Exists(root))
        {
            return null;
        }

        return Directory.EnumerateFiles(root, fileName, SearchOption.AllDirectories)
            .FirstOrDefault(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                                    && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
    }

    private static IEnumerable<string> ProductionSources(string project)
    {
        var root = Path.Combine(Repo(), ModuleRootRelative, project);
        if (!Directory.Exists(root))
        {
            return [];
        }

        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !p.EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<string> ProjectRefs(string project)
    {
        var doc = XDocument.Load(Path.Combine(Repo(), ModuleRootRelative, project, $"{project}.csproj"));
        return doc.Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Where(x => x.Length > 0)
            .ToArray();
    }
}
