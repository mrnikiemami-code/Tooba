using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Tooba.Wallet.Application.Composition;
using Tooba.Wallet.Contracts.Errors;
using Tooba.Wallet.Endpoints.Contracts.Errors.Resources;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-WALLET-AMSC-001-W1 — migrate wave durable guard.
/// Locks the single canonical Contracts stable-code home with its declared reachability split, the
/// canonical typed-fault seam (no message-text classification, retired mapper), the bilingual resource
/// coverage with explicit embedded logical names, the transport validator matrix for the four
/// caller-controlled requests, the Foundation session code, the Contracts-only boundary, and the
/// unchanged Wallet schema migrations so the module stays extractable as an isolated microservice.
/// <para>
/// Assertions are location-independent (files are located by name, not by the technical-axis path) so
/// the W2 structure wave can folder the module without weakening this guard.
/// </para>
/// </summary>
public sealed class WalletModuleAmsc001W1MigrateGuardTests
{
    private const string ModuleRootRelative = "src/backend/Modules/Wallet";

    private static readonly string[] ProductionProjects =
    [
        "Tooba.Wallet.Contracts",
        "Tooba.Wallet.Domain",
        "Tooba.Wallet.Application",
        "Tooba.Wallet.Infrastructure",
        "Tooba.Wallet.Endpoints",
    ];

    [Fact]
    public void Stable_codes_live_in_the_single_canonical_contracts_errors_home()
    {
        var declaration = FindFile("Tooba.Wallet.Contracts", "WalletErrorCodes.cs");
        Assert.NotNull(declaration);
        Assert.Contains(
            "namespace Tooba.Wallet.Contracts.Errors",
            File.ReadAllText(declaration!),
            StringComparison.Ordinal);

        Assert.Equal("Tooba.Wallet.Contracts.Errors", typeof(WalletErrorCodes).Namespace);
        Assert.Equal("Tooba.Wallet.Contracts", typeof(WalletErrorCodes).Assembly.GetName().Name);

        var declarations = ProductionProjects
            .SelectMany(ProductionSources)
            .Count(file => File.ReadAllText(file).Contains("class WalletErrorCodes", StringComparison.Ordinal));
        Assert.Equal(1, declarations);
    }

    [Fact]
    public void Declared_code_catalog_splits_http_reachable_from_domain_invariants()
    {
        // 10 client-observable outcome codes + 40 domain/directory invariants = 50 declared.
        Assert.Equal(10, WalletErrorCodes.HttpReachable.Count);
        Assert.Equal(40, WalletErrorCodes.DomainInvariants.Count);

        Assert.True(WalletErrorCodes.IsHttpReachable(WalletErrorCodes.WalletRejected));
        Assert.True(WalletErrorCodes.IsHttpReachable(WalletErrorCodes.RedeemRejected));
        Assert.True(WalletErrorCodes.IsHttpReachable(WalletErrorCodes.GiftCardRejected));
        Assert.True(WalletErrorCodes.IsHttpReachable(WalletErrorCodes.GiftCardIssueRejected));
        Assert.True(WalletErrorCodes.IsHttpReachable(WalletErrorCodes.GiftCardRevokeRejected));
        Assert.True(WalletErrorCodes.IsHttpReachable(WalletErrorCodes.GiftCardMissing));
        Assert.True(WalletErrorCodes.IsHttpReachable(WalletErrorCodes.WalletMissing));
        Assert.True(WalletErrorCodes.IsHttpReachable(WalletErrorCodes.AdjustRejected));
        Assert.True(WalletErrorCodes.IsHttpReachable(WalletErrorCodes.AuthorizationUnavailable));
        Assert.True(WalletErrorCodes.IsHttpReachable(WalletErrorCodes.DemoNotReady));
        Assert.False(WalletErrorCodes.IsHttpReachable(WalletErrorCodes.GiftCardNotFound));
        Assert.False(WalletErrorCodes.IsHttpReachable(WalletErrorCodes.BalanceInsufficient));

        Assert.True(WalletErrorCodes.IsDomainInvariant(WalletErrorCodes.GiftCardNotFound));
        Assert.True(WalletErrorCodes.IsDomainInvariant(WalletErrorCodes.BalanceInsufficient));
        Assert.True(WalletErrorCodes.IsDomainInvariant(WalletErrorCodes.OutboxUnmappedEventType));
        Assert.False(WalletErrorCodes.IsDomainInvariant(WalletErrorCodes.WalletMissing));

        Assert.True(WalletErrorCodes.IsKnown(WalletErrorCodes.WalletMissing));
        Assert.True(WalletErrorCodes.IsKnown(WalletErrorCodes.BalanceInsufficient));
        Assert.False(WalletErrorCodes.IsKnown(null));
        Assert.False(WalletErrorCodes.IsKnown(string.Empty));
        Assert.False(WalletErrorCodes.IsKnown(" "));

        // Foundation-owned cross-cutting codes are deliberately NOT declared by Wallet.
        Assert.False(WalletErrorCodes.IsKnown("customer.session.required"));
        Assert.False(WalletErrorCodes.IsKnown("admin.authorization.denied"));

        var declared = DeclaredCodes();
        Assert.NotEmpty(declared);
        Assert.All(declared, code => Assert.True(WalletErrorCodes.IsKnown(code), code));
        Assert.Equal(declared.Length, declared.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(
            WalletErrorCodes.HttpReachable.Count + WalletErrorCodes.DomainInvariants.Count,
            declared.Length);
    }

    [Fact]
    public void Typed_fault_seam_maps_declared_codes_and_never_parses_message_text()
    {
        var seam = FindFile("Tooba.Wallet.Application", "WalletOperation.cs");
        Assert.NotNull(seam);
        var text = File.ReadAllText(seam!);

        Assert.Contains("ContractOperationException", text, StringComparison.Ordinal);
        Assert.Contains("WalletErrorCodes.IsKnown", text, StringComparison.Ordinal);
        Assert.Contains("WalletErrorCodes.IsHttpReachable", text, StringComparison.Ordinal);
        Assert.Contains("SemanticError", text, StringComparison.Ordinal);
        Assert.Contains("Task<Result<T>> ExecuteAsync<T>", text, StringComparison.Ordinal);
        Assert.Contains("Task<Result> ExecuteAsync", text, StringComparison.Ordinal);
        Assert.Contains("NotFoundIfNull", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
        Assert.DoesNotContain(".Message.Contains(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("StartsWith(\"wallet.", text, StringComparison.Ordinal);

        Assert.Equal("Tooba.Wallet.Application.Composition", typeof(WalletOperation).Namespace);

        // The retired message-heuristic mapper and its folder must stay retired.
        Assert.Null(FindFile("Tooba.Wallet.Application", "WalletExceptionMapper.cs"));
        Assert.False(Directory.Exists(Path.Combine(Repo(), ModuleRootRelative, "Tooba.Wallet.Application", "Errors")));

        // Enum parsing is a typed transport seam that carries stable codes, never prose.
        var enumParsing = FindFile("Tooba.Wallet.Application", "WalletEnumParsing.cs");
        Assert.NotNull(enumParsing);
        var enumText = File.ReadAllText(enumParsing!);
        Assert.Contains("ContractOperationException", enumText, StringComparison.Ordinal);
        Assert.Contains("WalletErrorCodes.", enumText, StringComparison.Ordinal);
    }

    [Fact]
    public void Error_localization_resource_set_is_wallet_owned_and_bilingual()
    {
        var set = FindFile("Tooba.Wallet.Endpoints", "WalletErrorResources.cs");
        Assert.NotNull(set);
        var setText = File.ReadAllText(set!);
        Assert.Contains("IErrorResourceSet", setText, StringComparison.Ordinal);
        Assert.Contains("\"wallet.\"", setText, StringComparison.Ordinal);
        Assert.Contains("\"giftcard.\"", setText, StringComparison.Ordinal);

        Assert.Equal("Tooba.Wallet.Endpoints.Contracts.Errors.Resources", typeof(WalletErrorResourceSet).Namespace);

        var en = FindFile("Tooba.Wallet.Endpoints", "WalletErrors.resx");
        var fa = FindFile("Tooba.Wallet.Endpoints", "WalletErrors.fa.resx");
        Assert.NotNull(en);
        Assert.NotNull(fa);

        var declared = DeclaredCodes();
        var enText = File.ReadAllText(en!);
        var faText = File.ReadAllText(fa!);
        foreach (var code in declared)
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
            Assert.Equal(declared.Length, keys.Length);
            Assert.All(keys, key => Assert.Contains(key, declared));
        }

        // The bilingual logical names are explicit and locked (not left to SDK convention).
        var endpointsCsproj = Read("Tooba.Wallet.Endpoints");
        Assert.Contains("Tooba.Wallet.Endpoints.Contracts.Errors.Resources.WalletErrors.resources", endpointsCsproj, StringComparison.Ordinal);
        Assert.Contains("Tooba.Wallet.Endpoints.Contracts.Errors.Resources.WalletErrors.fa.resources", endpointsCsproj, StringComparison.Ordinal);

        // The module contributor registers the client-observable codes exactly once and never
        // catalogues a domain-only invariant.
        var contributor = FindFile("Tooba.Wallet.Endpoints", "WalletErrorCatalogContributor.cs");
        Assert.NotNull(contributor);
        var contributorText = File.ReadAllText(contributor!);
        foreach (var httpReachable in new[]
                 {
                     WalletErrorCodes.WalletRejected,
                     WalletErrorCodes.RedeemRejected,
                     WalletErrorCodes.GiftCardRejected,
                     WalletErrorCodes.GiftCardIssueRejected,
                     WalletErrorCodes.GiftCardRevokeRejected,
                     WalletErrorCodes.GiftCardMissing,
                     WalletErrorCodes.WalletMissing,
                     WalletErrorCodes.AdjustRejected,
                     WalletErrorCodes.DemoNotReady,
                 })
        {
            var fieldName = typeof(WalletErrorCodes)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Single(f => f.IsLiteral && (string?)f.GetRawConstantValue() == httpReachable)
                .Name;
            Assert.Contains($"WalletErrorCodes.{fieldName}", contributorText, StringComparison.Ordinal);
        }

        Assert.Contains("WalletAdminAuthorizationCodes.AuthorizationUnavailable", contributorText, StringComparison.Ordinal);
        Assert.DoesNotContain("WalletErrorCodes.GiftCardNotFound", contributorText, StringComparison.Ordinal);
        Assert.DoesNotContain("WalletErrorCodes.BalanceInsufficient", contributorText, StringComparison.Ordinal);

        // The module-owned resource set resolves every declared code in both cultures.
        var resourceSet = new WalletErrorResourceSet();
        var enCulture = new System.Globalization.CultureInfo("en");
        var faCulture = new System.Globalization.CultureInfo("fa");
        Assert.True(resourceSet.Owns("wallet.rejected"));
        Assert.True(resourceSet.Owns("giftcard.missing"));
        Assert.False(resourceSet.Owns("customer.session.required"));
        Assert.False(resourceSet.Owns("order.not_found"));
        foreach (var code in declared)
        {
            Assert.False(string.IsNullOrWhiteSpace(resourceSet.GetString(code, enCulture)), $"{code} (en)");
            Assert.False(string.IsNullOrWhiteSpace(resourceSet.GetString(code, faCulture)), $"{code} (fa)");
        }
    }

    [Fact]
    public void Transport_validators_cover_the_four_caller_controlled_requests()
    {
        var validators = FindFile("Tooba.Wallet.Application", "WalletRequestValidators.cs");
        Assert.NotNull(validators);
        var text = File.ReadAllText(validators!);

        foreach (var type in new[]
                 {
                     "RedeemCustomerGiftCardCommandValidator",
                     "IssueAdminGiftCardCommandValidator",
                     "ListAdminGiftCardsQueryValidator",
                     "AdjustAdminWalletCommandValidator",
                 })
        {
            Assert.Contains(type, text, StringComparison.Ordinal);
        }

        // Exactly four transport validators — one per VALIDATOR_REQUIRED request of the 11-row matrix.
        Assert.Equal(4, Regex.Matches(text, @"class \w+Validator\s*:\s*AbstractValidator<").Count);

        // Validators emit stable machine codes, never user-facing prose.
        Assert.DoesNotContain("WithMessage(", text, StringComparison.Ordinal);
        Assert.Contains("WalletValidationCodes.", text, StringComparison.Ordinal);

        var codes = FindFile("Tooba.Wallet.Application", "WalletValidationCodes.cs");
        Assert.NotNull(codes);
        Assert.All(
            Regex.Matches(File.ReadAllText(codes!), @"""([^""]+)""").Select(m => m.Groups[1].Value),
            code => Assert.StartsWith("wallet.validation.", code, StringComparison.Ordinal));
    }

    [Fact]
    public void Customer_endpoints_use_the_foundation_session_code_and_host_has_no_wallet_authority()
    {
        var customer = FindFile("Tooba.Wallet.Endpoints", "WalletCustomerEndpoints.cs");
        Assert.NotNull(customer);
        var customerText = File.ReadAllText(customer!);
        Assert.Contains("FoundationErrorCodes.CustomerSessionRequired", customerText, StringComparison.Ordinal);
        Assert.DoesNotContain("\"customer.session.required\"", customerText, StringComparison.Ordinal);

        var hostRoot = Path.Combine(Repo(), "src/backend/Host/Tooba.Host");
        Assert.False(Directory.Exists(Path.Combine(hostRoot, "Wallet")));

        var program = File.ReadAllText(Path.Combine(hostRoot, "Program.cs"));
        Assert.Contains("MapWalletEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("AddWalletEndpointPresentation()", program, StringComparison.Ordinal);
        Assert.Contains("Tooba.Wallet.Application", program, StringComparison.Ordinal);
        Assert.Contains("RedeemCustomerGiftCardCommand", program, StringComparison.Ordinal);
        Assert.Contains("HostWalletAdminAuthorizer", program, StringComparison.Ordinal);
        Assert.DoesNotContain("HostWalletCustomerAuthorizer", program, StringComparison.Ordinal);

        var hostDbContextAllowlist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Program.cs",
            "ModuleMigrationRegistry.cs",
            "ProductWorkspaceDevelopmentBootstrap.cs",
        };
        var hostHits = Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                           && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(path => File.ReadAllText(path).Contains("WalletDbContext", StringComparison.Ordinal))
            .Where(path => !hostDbContextAllowlist.Contains(Path.GetFileName(path)))
            .ToList();
        Assert.True(hostHits.Count == 0, "Host WalletDbContext allowlist: " + string.Join("; ", hostHits));
    }

    [Fact]
    public void Wallet_never_references_foreign_application_infrastructure_or_domain()
    {
        // The single legal foreign edge remains Notification.Contracts.
        var infrastructureCsproj = Read("Tooba.Wallet.Infrastructure");
        Assert.Contains("Tooba.Notification.Contracts", infrastructureCsproj, StringComparison.Ordinal);

        foreach (var project in ProductionProjects)
        {
            foreach (var file in ProductionSources(project))
            {
                var text = File.ReadAllText(file);
                foreach (var foreign in new[]
                         {
                             "Notification.Application", "Notification.Infrastructure", "Notification.Domain",
                             "Order.Application", "Order.Infrastructure", "Order.Domain",
                             "Payment.Application", "Payment.Infrastructure", "Payment.Domain",
                             "Catalog.Application", "Catalog.Infrastructure", "Catalog.Domain",
                             "AccessControl.Application", "AccessControl.Infrastructure", "AccessControl.Domain",
                             "Support.Application", "Support.Infrastructure", "Support.Domain",
                             "Returns.Application", "Returns.Infrastructure", "Returns.Domain",
                             "Promotion.Application", "Promotion.Infrastructure", "Promotion.Domain",
                             "Settlement.Application", "Settlement.Infrastructure", "Settlement.Domain",
                         })
                {
                    Assert.DoesNotContain($"using Tooba.{foreign}", text, StringComparison.Ordinal);
                    Assert.DoesNotContain($"Tooba.{foreign}.", text, StringComparison.Ordinal);
                }

                Assert.DoesNotContain("OrderDbContext", text, StringComparison.Ordinal);
                Assert.DoesNotContain("NotificationDbContext", text, StringComparison.Ordinal);
            }
        }

        // Endpoints must never reach Infrastructure or Host.
        var endpointsRefs = ProjectRefs("Tooba.Wallet.Endpoints");
        Assert.Contains(endpointsRefs, r => r.Contains("Wallet.Application", StringComparison.Ordinal));
        Assert.DoesNotContain(endpointsRefs, r => r.Contains("Wallet.Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(endpointsRefs, r => r.Contains("Tooba.Host", StringComparison.OrdinalIgnoreCase));

        // Domain must depend only on BuildingBlocks + Wallet.Contracts.
        var domainRefs = ProjectRefs("Tooba.Wallet.Domain");
        Assert.Contains(domainRefs, r => r.Contains("Tooba.Wallet.Contracts", StringComparison.Ordinal));
        Assert.DoesNotContain(domainRefs, r => r.Contains("Wallet.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(domainRefs, r => r.Contains("Wallet.Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(domainRefs, r => r.Contains("Wallet.Endpoints", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Wallet_production_never_classifies_faults_by_message_text()
    {
        foreach (var project in ProductionProjects)
        {
            foreach (var file in ProductionSources(project))
            {
                var text = File.ReadAllText(file);
                Assert.DoesNotContain(".Message.Contains(", text, StringComparison.Ordinal);
                Assert.DoesNotContain(".Message.StartsWith(", text, StringComparison.Ordinal);
                Assert.DoesNotContain(".Message ==", text, StringComparison.Ordinal);
                Assert.DoesNotContain("TryMapExact", text, StringComparison.Ordinal);
                Assert.DoesNotContain("WalletExceptionMapper", text, StringComparison.Ordinal);
                Assert.DoesNotMatch(new Regex(@"InvalidOperationException\(""[^""]+""\)"), text);
                Assert.DoesNotContain("wallet.rejected.2", text, StringComparison.Ordinal);
                Assert.DoesNotContain("SWRlbXBv", text, StringComparison.Ordinal);
            }
        }

        // The stable-code literal identity lives ONLY in the Contracts declaration/resource files;
        // Application, Infrastructure and Endpoints must reference the constants, never inline them.
        var declaration = Path.GetFullPath(FindFile("Tooba.Wallet.Contracts", "WalletErrorCodes.cs")!);
        var resources = Path.GetFullPath(Path.Combine(Repo(), ModuleRootRelative, "Tooba.Wallet.Endpoints", "Resources"));

        foreach (var project in new[] { "Tooba.Wallet.Application", "Tooba.Wallet.Infrastructure", "Tooba.Wallet.Endpoints" })
        {
            foreach (var file in ProductionSources(project))
            {
                var full = Path.GetFullPath(file);
                if (string.Equals(full, declaration, StringComparison.OrdinalIgnoreCase)
                    || full.StartsWith(resources, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var text = File.ReadAllText(file);
                foreach (var code in new[] { "wallet.rejected", "wallet.missing", "giftcard.missing", "wallet.demo.not_ready" })
                {
                    Assert.DoesNotContain($"\"{code}\"", text, StringComparison.Ordinal);
                }
            }
        }
    }

    [Fact]
    public void Migration_did_not_change_the_wallet_schema_migrations()
    {
        var migrations = Directory
            .EnumerateFiles(Path.Combine(Repo(), ModuleRootRelative, "Tooba.Wallet.Infrastructure"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                           && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => Path.GetFileName(path).Contains("InitialWallet", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["20260827180000_InitialWallet.cs"], migrations);

        var schema = FindFile("Tooba.Wallet.Infrastructure", "WalletDbContext.cs");
        Assert.NotNull(schema);
        Assert.Contains("\"wallet\"", File.ReadAllText(schema!), StringComparison.Ordinal);
    }

    private static string[] DeclaredCodes() =>
        typeof(WalletErrorCodes)
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
