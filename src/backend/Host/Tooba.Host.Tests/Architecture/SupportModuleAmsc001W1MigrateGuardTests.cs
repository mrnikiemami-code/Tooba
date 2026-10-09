using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Tooba.Support.Contracts.Errors;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-SUPPORT-AMSC-001-W1 — migrate wave durable guard.
/// Locks the single canonical Contracts stable-code home with its declared reachability split, the
/// canonical typed-fault seam (no message-text classification, retired mapper), the bilingual resource
/// coverage, the transport validator matrix for the caller-controlled requests, the Foundation session
/// code, the Contracts-only boundary, and the unchanged Support schema migrations so the module stays
/// extractable as an isolated microservice.
/// <para>
/// Assertions are location-independent (files are located by name, not by the technical-axis path) so
/// the W2 structure wave can folder the module without weakening this guard.
/// </para>
/// </summary>
public sealed class SupportModuleAmsc001W1MigrateGuardTests
{
    private const string ModuleRootRelative = "src/backend/Modules/Support";

    private static readonly string[] ProductionProjects =
    [
        "Tooba.Support.Contracts",
        "Tooba.Support.Domain",
        "Tooba.Support.Application",
        "Tooba.Support.Infrastructure",
        "Tooba.Support.Endpoints",
    ];

    [Fact]
    public void Stable_codes_live_in_the_single_canonical_contracts_errors_home()
    {
        var declaration = FindFile("Tooba.Support.Contracts", "SupportErrorCodes.cs");
        Assert.NotNull(declaration);
        Assert.Contains(
            "namespace Tooba.Support.Contracts.Errors",
            File.ReadAllText(declaration!),
            StringComparison.Ordinal);

        Assert.Equal("Tooba.Support.Contracts.Errors", typeof(SupportErrorCodes).Namespace);

        var declarations = ProductionProjects
            .SelectMany(ProductionSources)
            .Count(file => File.ReadAllText(file).Contains("class SupportErrorCodes", StringComparison.Ordinal));
        Assert.Equal(1, declarations);
    }

    [Fact]
    public void Declared_code_catalog_splits_http_reachable_from_domain_invariants()
    {
        // 7 client-observable outcome codes + 21 domain/directory invariants = 28 declared.
        Assert.Equal(7, SupportErrorCodes.HttpReachable.Count);
        Assert.Equal(21, SupportErrorCodes.DomainInvariants.Count);

        Assert.True(SupportErrorCodes.IsHttpReachable(SupportErrorCodes.Missing));
        Assert.True(SupportErrorCodes.IsHttpReachable(SupportErrorCodes.Rejected));
        Assert.True(SupportErrorCodes.IsHttpReachable(SupportErrorCodes.ReplyRejected));
        Assert.True(SupportErrorCodes.IsHttpReachable(SupportErrorCodes.ActionRejected));
        Assert.True(SupportErrorCodes.IsHttpReachable(SupportErrorCodes.PatchRejected));
        Assert.True(SupportErrorCodes.IsHttpReachable(SupportErrorCodes.DemoNotReady));
        Assert.False(SupportErrorCodes.IsHttpReachable(SupportErrorCodes.TicketNotFound));

        Assert.True(SupportErrorCodes.IsDomainInvariant(SupportErrorCodes.TicketNotFound));
        Assert.True(SupportErrorCodes.IsDomainInvariant(SupportErrorCodes.OutboxEmitNotSupported));
        Assert.False(SupportErrorCodes.IsDomainInvariant(SupportErrorCodes.Missing));

        Assert.True(SupportErrorCodes.IsKnown(SupportErrorCodes.Missing));
        Assert.True(SupportErrorCodes.IsKnown(SupportErrorCodes.TicketNotFound));
        Assert.False(SupportErrorCodes.IsKnown(null));
        Assert.False(SupportErrorCodes.IsKnown(string.Empty));
        Assert.False(SupportErrorCodes.IsKnown(" "));

        // Foundation-owned cross-cutting codes are deliberately NOT declared by Support.
        Assert.False(SupportErrorCodes.IsKnown("customer.session.required"));
        Assert.False(SupportErrorCodes.IsKnown("seller.authorization.denied"));
        Assert.False(SupportErrorCodes.IsKnown("admin.authorization.denied"));

        var declared = DeclaredCodes();
        Assert.NotEmpty(declared);
        Assert.All(declared, code => Assert.True(SupportErrorCodes.IsKnown(code), code));
        Assert.Equal(declared.Length, declared.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(
            SupportErrorCodes.HttpReachable.Count + SupportErrorCodes.DomainInvariants.Count,
            declared.Length);
    }

    [Fact]
    public void Typed_fault_seam_maps_declared_codes_and_never_parses_message_text()
    {
        var seam = FindFile("Tooba.Support.Application", "SupportOperation.cs");
        Assert.NotNull(seam);
        var text = File.ReadAllText(seam!);

        Assert.Contains("ContractOperationException", text, StringComparison.Ordinal);
        Assert.Contains("SupportErrorCodes.IsKnown", text, StringComparison.Ordinal);
        Assert.Contains("SupportErrorCodes.IsHttpReachable", text, StringComparison.Ordinal);
        Assert.Contains("SemanticError", text, StringComparison.Ordinal);
        Assert.Contains("Task<Result<T>> ExecuteAsync<T>", text, StringComparison.Ordinal);
        Assert.Contains("Task<Result> ExecuteAsync", text, StringComparison.Ordinal);
        Assert.Contains("NotFoundIfNull", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);

        // The retired message-heuristic mapper and its folder must stay retired.
        Assert.Null(FindFile("Tooba.Support.Application", "SupportExceptionMapper.cs"));
        Assert.False(Directory.Exists(Path.Combine(Repo(), ModuleRootRelative, "Tooba.Support.Application", "Errors")));

        // Enum parsing is a typed transport seam that carries stable codes, never prose.
        var enumParsing = FindFile("Tooba.Support.Application", "SupportEnumParsing.cs");
        Assert.NotNull(enumParsing);
        var enumText = File.ReadAllText(enumParsing!);
        Assert.Contains("ContractOperationException", enumText, StringComparison.Ordinal);
        Assert.Contains("SupportErrorCodes.", enumText, StringComparison.Ordinal);
    }

    [Fact]
    public void Error_localization_resource_set_is_support_owned_and_bilingual()
    {
        var set = FindFile("Tooba.Support.Endpoints", "SupportErrorResources.cs");
        Assert.NotNull(set);
        var setText = File.ReadAllText(set!);
        Assert.Contains("IErrorResourceSet", setText, StringComparison.Ordinal);
        Assert.Contains("\"support.\"", setText, StringComparison.Ordinal);

        var en = FindFile("Tooba.Support.Endpoints", "SupportErrors.resx");
        var fa = FindFile("Tooba.Support.Endpoints", "SupportErrors.fa.resx");
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

        // The module contributor registers the client-observable codes exactly once and never
        // catalogues a domain-only invariant.
        var contributor = FindFile("Tooba.Support.Endpoints", "SupportErrorCatalogContributor.cs");
        Assert.NotNull(contributor);
        var contributorText = File.ReadAllText(contributor!);
        foreach (var httpReachable in new[]
                 {
                     SupportErrorCodes.Missing,
                     SupportErrorCodes.Rejected,
                     SupportErrorCodes.ReplyRejected,
                     SupportErrorCodes.ActionRejected,
                     SupportErrorCodes.PatchRejected,
                     SupportErrorCodes.DemoNotReady,
                 })
        {
            var fieldName = typeof(SupportErrorCodes)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Single(f => f.IsLiteral && (string?)f.GetRawConstantValue() == httpReachable)
                .Name;
            Assert.Contains($"SupportErrorCodes.{fieldName}", contributorText, StringComparison.Ordinal);
        }

        Assert.Contains("SupportAdminAuthorizationCodes.AuthorizationUnavailable", contributorText, StringComparison.Ordinal);
        Assert.DoesNotContain("SupportErrorCodes.TicketNotFound", contributorText, StringComparison.Ordinal);
        Assert.DoesNotContain("SupportErrorCodes.OutboxEmitNotSupported", contributorText, StringComparison.Ordinal);
    }

    [Fact]
    public void Transport_validators_cover_the_nine_caller_controlled_requests()
    {
        var validators = FindFile("Tooba.Support.Application", "SupportRequestValidators.cs");
        Assert.NotNull(validators);
        var text = File.ReadAllText(validators!);

        foreach (var type in new[]
                 {
                     "CreateCustomerTicketCommandValidator",
                     "CreateSellerTicketCommandValidator",
                     "ReplyCustomerTicketCommandValidator",
                     "ReplySellerTicketCommandValidator",
                     "ReplyAdminTicketCommandValidator",
                     "PatchAdminTicketCommandValidator",
                     "ListCustomerTicketsQueryValidator",
                     "ListSellerTicketsQueryValidator",
                     "ListAdminTicketsQueryValidator",
                 })
        {
            Assert.Contains(type, text, StringComparison.Ordinal);
        }

        // Exactly nine transport validators — one per VALIDATOR_REQUIRED request of the 17-row matrix.
        Assert.Equal(9, Regex.Matches(text, @"class \w+Validator\s*:\s*AbstractValidator<").Count);

        // Validators emit stable machine codes, never user-facing prose.
        Assert.DoesNotContain("WithMessage(", text, StringComparison.Ordinal);
        Assert.Contains("SupportValidationCodes.", text, StringComparison.Ordinal);

        var codes = FindFile("Tooba.Support.Application", "SupportValidationCodes.cs");
        Assert.NotNull(codes);
        Assert.All(
            Regex.Matches(File.ReadAllText(codes!), @"""([^""]+)""").Select(m => m.Groups[1].Value),
            code => Assert.StartsWith("support.validation.", code, StringComparison.Ordinal));
    }

    [Fact]
    public void Customer_endpoints_use_the_foundation_session_code_and_host_has_no_support_authority()
    {
        var customer = FindFile("Tooba.Support.Endpoints", "SupportCustomerEndpoints.cs");
        Assert.NotNull(customer);
        var customerText = File.ReadAllText(customer!);
        Assert.Contains("FoundationErrorCodes.CustomerSessionRequired", customerText, StringComparison.Ordinal);
        Assert.DoesNotContain("\"customer.session.required\"", customerText, StringComparison.Ordinal);

        var hostRoot = Path.Combine(Repo(), "src/backend/Host/Tooba.Host");
        Assert.False(Directory.Exists(Path.Combine(hostRoot, "Support")));

        var program = File.ReadAllText(Path.Combine(hostRoot, "Program.cs"));
        Assert.Contains("MapSupportEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("AddSupportEndpointPresentation()", program, StringComparison.Ordinal);
        Assert.Contains("Tooba.Support.Application", program, StringComparison.Ordinal);
        Assert.Contains("CreateCustomerTicketCommand", program, StringComparison.Ordinal);
        Assert.Contains("HostSupportSellerAuthorizer", program, StringComparison.Ordinal);
        Assert.Contains("HostSupportAdminAuthorizer", program, StringComparison.Ordinal);

        var hostDbContextAllowlist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Program.cs",
            "ModuleMigrationRegistry.cs",
            "ProductWorkspaceDevelopmentBootstrap.cs",
        };
        var hostHits = Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                           && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(path => File.ReadAllText(path).Contains("SupportDbContext", StringComparison.Ordinal))
            .Where(path => !hostDbContextAllowlist.Contains(Path.GetFileName(path)))
            .ToList();
        Assert.True(hostHits.Count == 0, "Host SupportDbContext allowlist: " + string.Join("; ", hostHits));
    }

    [Fact]
    public void Support_never_references_foreign_application_infrastructure_or_domain()
    {
        // The single legal foreign edge remains Notification.Contracts.
        var infrastructureCsproj = Read("Tooba.Support.Infrastructure");
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
        var endpointsRefs = ProjectRefs("Tooba.Support.Endpoints");
        Assert.Contains(endpointsRefs, r => r.Contains("Support.Application", StringComparison.Ordinal));
        Assert.DoesNotContain(endpointsRefs, r => r.Contains("Support.Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(endpointsRefs, r => r.Contains("Tooba.Host", StringComparison.OrdinalIgnoreCase));

        // Domain must depend only on BuildingBlocks + Support.Contracts.
        var domainRefs = ProjectRefs("Tooba.Support.Domain");
        Assert.Contains(domainRefs, r => r.Contains("Tooba.Support.Contracts", StringComparison.Ordinal));
        Assert.DoesNotContain(domainRefs, r => r.Contains("Support.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(domainRefs, r => r.Contains("Support.Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(domainRefs, r => r.Contains("Support.Endpoints", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Support_production_never_classifies_faults_by_message_text()
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
                Assert.DoesNotContain("SupportExceptionMapper", text, StringComparison.Ordinal);
                Assert.DoesNotContain("InvalidOperationException(\"support.", text, StringComparison.Ordinal);
            }
        }

        // The stable-code literal identity lives ONLY in the Contracts declaration/resource files;
        // Application, Infrastructure and Endpoints must reference the constants, never inline them.
        var declaration = Path.GetFullPath(FindFile("Tooba.Support.Contracts", "SupportErrorCodes.cs")!);
        var resources = Path.GetFullPath(Path.Combine(Repo(), ModuleRootRelative, "Tooba.Support.Endpoints", "Resources"));

        foreach (var project in new[] { "Tooba.Support.Application", "Tooba.Support.Infrastructure", "Tooba.Support.Endpoints" })
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
                foreach (var code in new[] { "support.missing", "support.rejected", "support.ticket_not_found", "support.demo.not_ready" })
                {
                    Assert.DoesNotContain($"\"{code}\"", text, StringComparison.Ordinal);
                }
            }
        }
    }

    [Fact]
    public void Migration_did_not_change_the_support_schema_migrations()
    {
        var migrations = Directory
            .EnumerateFiles(Path.Combine(Repo(), ModuleRootRelative, "Tooba.Support.Infrastructure"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                           && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => Path.GetFileName(path).Contains("InitialSupport", StringComparison.Ordinal)
                           || Path.GetFileName(path).EndsWith("ModelSnapshot.cs", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "20260827120000_InitialSupport.Designer.cs",
                "20260827120000_InitialSupport.cs",
                "SupportDbContextModelSnapshot.cs",
            ],
            migrations);

        var schema = FindFile("Tooba.Support.Infrastructure", "SupportDbContext.cs");
        Assert.NotNull(schema);
        Assert.Contains("support", File.ReadAllText(schema!), StringComparison.Ordinal);
    }

    private static string[] DeclaredCodes() =>
        typeof(SupportErrorCodes)
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
