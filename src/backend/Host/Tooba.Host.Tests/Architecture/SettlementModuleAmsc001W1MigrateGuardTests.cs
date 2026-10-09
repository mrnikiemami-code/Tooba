using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Tooba.Settlement.Contracts.Errors;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-SETTLEMENT-AMSC-001-W1 — migrate wave durable guard.
/// Locks the single canonical Contracts stable-code home with its declared-code/reachability split, the
/// canonical typed-fault seam, the localization resource set with bilingual resources, the Contracts
/// home for the three published integration events, the transport validator coverage, the capability-first
/// Application layout, the Domain's single self-module Contracts edge, the Contracts-only boundary that
/// lets Settlement be extracted as an isolated microservice, and the unchanged schema.
/// </summary>
public sealed class SettlementModuleAmsc001W1MigrateGuardTests
{
    private const string ModuleRootRelative = "src/backend/Modules/Settlement";

    private static readonly string[] ProductionProjects =
    [
        "Tooba.Settlement.Contracts",
        "Tooba.Settlement.Domain",
        "Tooba.Settlement.Application",
        "Tooba.Settlement.Infrastructure",
        "Tooba.Settlement.Endpoints",
    ];

    [Fact]
    public void Stable_codes_live_in_the_single_canonical_contracts_errors_home()
    {
        Assert.True(File.Exists(Path.Combine(
            Repo(), ModuleRootRelative, "Tooba.Settlement.Contracts/Errors/SettlementErrorCodes.cs")),
            "Contracts/Errors/SettlementErrorCodes.cs must exist");
        Assert.Contains(
            "namespace Tooba.Settlement.Contracts.Errors",
            Read($"{ModuleRootRelative}/Tooba.Settlement.Contracts/Errors/SettlementErrorCodes.cs"),
            StringComparison.Ordinal);

        // The retired Application/Errors home (codes + message mapper) must stay retired.
        Assert.False(Directory.Exists(Path.Combine(
            Repo(), ModuleRootRelative, "Tooba.Settlement.Application/Errors")));

        var declarations = ProductionSources("Tooba.Settlement.Contracts")
            .Concat(ProductionSources("Tooba.Settlement.Domain"))
            .Concat(ProductionSources("Tooba.Settlement.Application"))
            .Concat(ProductionSources("Tooba.Settlement.Infrastructure"))
            .Concat(ProductionSources("Tooba.Settlement.Endpoints"))
            .Count(file => File.ReadAllText(file).Contains("class SettlementErrorCodes", StringComparison.Ordinal));
        Assert.Equal(1, declarations);
    }

    [Fact]
    public void Declared_code_catalog_splits_http_reachable_from_platform_faults()
    {
        // 17 HTTP-reachable outcome codes + 1 platform-side outbox fault = 18 declared.
        Assert.Equal(17, SettlementErrorCodes.HttpReachable.Count);
        Assert.Equal(1, SettlementErrorCodes.PlatformFaults.Count);

        Assert.True(SettlementErrorCodes.IsHttpReachable(SettlementErrorCodes.AccountMissing));
        Assert.True(SettlementErrorCodes.IsHttpReachable(SettlementErrorCodes.PayoutInvalidAmount));
        Assert.True(SettlementErrorCodes.IsHttpReachable(SettlementErrorCodes.GatewayUnconfigured));
        Assert.True(SettlementErrorCodes.IsHttpReachable(SettlementErrorCodes.UnconfirmPayoutCompleted));
        Assert.True(SettlementErrorCodes.IsHttpReachable(SettlementErrorCodes.CancelPayoutCompleted));
        Assert.True(SettlementErrorCodes.IsHttpReachable(SettlementErrorCodes.RestorePayoutCompleted));
        Assert.False(SettlementErrorCodes.IsHttpReachable(SettlementErrorCodes.OutboxUnmapped));

        Assert.True(SettlementErrorCodes.IsPlatformFault(SettlementErrorCodes.OutboxUnmapped));
        Assert.False(SettlementErrorCodes.IsPlatformFault(SettlementErrorCodes.AccountMissing));

        Assert.True(SettlementErrorCodes.IsKnown(SettlementErrorCodes.OutboxUnmapped));
        Assert.False(SettlementErrorCodes.IsKnown(null));
        Assert.False(SettlementErrorCodes.IsKnown(string.Empty));
        Assert.False(SettlementErrorCodes.IsKnown(" "));

        // Foundation-owned cross-cutting codes are deliberately NOT declared by Settlement.
        Assert.False(SettlementErrorCodes.IsKnown("customer.session.required"));
        Assert.False(SettlementErrorCodes.IsKnown("seller.authorization.denied"));
        Assert.False(SettlementErrorCodes.IsKnown("admin.authorization.denied"));

        var declared = typeof(SettlementErrorCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToArray();

        Assert.Equal(18, declared.Length);
        Assert.Equal(declared.Length, declared.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(
            declared.OrderBy(x => x, StringComparer.Ordinal),
            SettlementErrorCodes.HttpReachable
                .Concat(SettlementErrorCodes.PlatformFaults)
                .OrderBy(x => x, StringComparer.Ordinal));

        // Every declared code resolves through IsKnown — the typed seam's filter.
        Assert.All(declared, code => Assert.True(SettlementErrorCodes.IsKnown(code)));
    }

    [Fact]
    public void Bilingual_resource_set_covers_every_declared_code_and_is_registered_once()
    {
        Assert.True(File.Exists(Path.Combine(
            Repo(), ModuleRootRelative, "Tooba.Settlement.Contracts/Errors/SettlementErrorResourceSet.cs")));
        Assert.True(File.Exists(Path.Combine(
            Repo(), ModuleRootRelative, "Tooba.Settlement.Contracts/Resources/SettlementErrors.resx")));
        Assert.True(File.Exists(Path.Combine(
            Repo(), ModuleRootRelative, "Tooba.Settlement.Contracts/Resources/SettlementErrors.fa.resx")));

        var en = Read($"{ModuleRootRelative}/Tooba.Settlement.Contracts/Resources/SettlementErrors.resx");
        var fa = Read($"{ModuleRootRelative}/Tooba.Settlement.Contracts/Resources/SettlementErrors.fa.resx");
        foreach (var code in SettlementErrorCodes.HttpReachable.Concat(SettlementErrorCodes.PlatformFaults))
        {
            Assert.Contains($"name=\"{code}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{code}\"", fa, StringComparison.Ordinal);
        }

        var resourceSet = Read($"{ModuleRootRelative}/Tooba.Settlement.Contracts/Errors/SettlementErrorResourceSet.cs");
        Assert.Contains("IErrorResourceSet", resourceSet, StringComparison.Ordinal);
        Assert.Contains("Settlement", resourceSet, StringComparison.Ordinal);

        var module = Read($"{ModuleRootRelative}/Tooba.Settlement.Endpoints/SettlementEndpointModule.cs");
        Assert.Equal(1, Regex.Matches(module, @"AddSingleton<IErrorResourceSet, SettlementErrorResourceSet>").Count);

        // The resources are real embedded resources of the Contracts assembly.
        var csproj = Read($"{ModuleRootRelative}/Tooba.Settlement.Contracts/Tooba.Settlement.Contracts.csproj");
        Assert.Contains("EmbeddedResource", csproj, StringComparison.Ordinal);
        Assert.Contains("SettlementErrors.fa.resx", csproj, StringComparison.Ordinal);
    }

    [Fact]
    public void Integration_events_live_in_contracts_with_an_unchanged_wire_contract()
    {
        var events = Path.Combine(Repo(), ModuleRootRelative, "Tooba.Settlement.Contracts/Events");
        Assert.True(Directory.Exists(events));
        var files = Directory.EnumerateFiles(events, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            ["PayoutFailedIntegrationEvent.cs", "PayoutSucceededIntegrationEvent.cs", "SettlementEntryPostedIntegrationEvent.cs"],
            files);

        // EventTypeName constants are the published outbox wire contract and must stay byte-identical.
        Assert.Contains("\"settlement.entry.posted.v1\"",
            Read($"{ModuleRootRelative}/Tooba.Settlement.Contracts/Events/SettlementEntryPostedIntegrationEvent.cs"), StringComparison.Ordinal);
        Assert.Contains("\"payout.succeeded.v1\"",
            Read($"{ModuleRootRelative}/Tooba.Settlement.Contracts/Events/PayoutSucceededIntegrationEvent.cs"), StringComparison.Ordinal);
        Assert.Contains("\"payout.failed.v1\"",
            Read($"{ModuleRootRelative}/Tooba.Settlement.Contracts/Events/PayoutFailedIntegrationEvent.cs"), StringComparison.Ordinal);

        var outbox = Read($"{ModuleRootRelative}/Tooba.Settlement.Infrastructure/Messaging/SettlementOutboxRegistration.cs");
        Assert.Contains("using Tooba.Settlement.Contracts.Events;", outbox, StringComparison.Ordinal);
        Assert.Contains("Version = 1", outbox, StringComparison.Ordinal);

        // The Application/Ports home of the events must stay retired.
        Assert.False(File.Exists(Path.Combine(
            Repo(), ModuleRootRelative, "Tooba.Settlement.Application/Ports/SettlementContracts.cs")));
        Assert.False(Directory.Exists(Path.Combine(
            Repo(), ModuleRootRelative, "Tooba.Settlement.Application/Ports")));
    }

    [Fact]
    public void Canonical_typed_fault_seam_replaces_message_text_classification()
    {
        var seam = Read($"{ModuleRootRelative}/Tooba.Settlement.Application/Composition/SettlementOperation.cs");
        Assert.Contains("namespace Tooba.Settlement.Application.Composition", seam, StringComparison.Ordinal);
        Assert.Contains("ContractOperationException", seam, StringComparison.Ordinal);
        Assert.Contains("SettlementErrorCodes.IsKnown", seam, StringComparison.Ordinal);
        Assert.Contains("SemanticException", seam, StringComparison.Ordinal);
        Assert.Contains("Task<Result<T>> ExecuteAsync<T>", seam, StringComparison.Ordinal);
        Assert.Contains("Task<Result> ExecuteAsync", seam, StringComparison.Ordinal);
        Assert.Contains("SemanticError", seam, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", seam, StringComparison.Ordinal);

        // The retired message-text mapper and its exact-match heuristic must stay retired.
        Assert.False(File.Exists(Path.Combine(
            Repo(), ModuleRootRelative, "Tooba.Settlement.Application/Errors/SettlementExceptionMapper.cs")));

        foreach (var project in ProductionProjects)
        {
            foreach (var file in ProductionSources(project))
            {
                var text = File.ReadAllText(file);
                Assert.DoesNotContain(".Message.Contains(", text, StringComparison.Ordinal);
                Assert.DoesNotContain(".Message.StartsWith(", text, StringComparison.Ordinal);
                Assert.DoesNotContain(".Message ==", text, StringComparison.Ordinal);
                Assert.DoesNotContain("TryMapExact", text, StringComparison.Ordinal);
                Assert.DoesNotContain("SettlementExceptionMapper", text, StringComparison.Ordinal);
            }
        }

        // The Application command handlers dispatch through the seam, not through raw try/catch mapping.
        foreach (var command in new[]
                 {
                     "Payouts/Commands/RequestSellerPayoutCommand.cs",
                     "Payouts/Commands/ProcessAdminPayoutCommand.cs",
                     "Payouts/Commands/RetryAdminPayoutCommand.cs",
                 })
        {
            Assert.Contains("SettlementOperation.ExecuteAsync", Read($"{ModuleRootRelative}/Tooba.Settlement.Application/{command}"),
                StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Application_is_capability_first_shallow_without_technical_axis_roots()
    {
        var applicationRoot = Path.Combine(Repo(), ModuleRootRelative, "Tooba.Settlement.Application");
        var topFolders = Directory.EnumerateDirectories(applicationRoot)
            .Select(Path.GetFileName)
            .Where(n => n is not "bin" and not "obj" and not "artifacts")
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(["Composition", "Payouts", "Validation"], topFolders);

        foreach (var retired in new[] { "Commands", "Queries", "Models", "Ports", "Validators", "Errors" })
        {
            Assert.False(Directory.Exists(Path.Combine(applicationRoot, retired)),
                $"Application/{retired} must stay retired");
        }

        var payouts = Path.Combine(applicationRoot, "Payouts");
        var subFolders = Directory.EnumerateDirectories(payouts)
            .Select(Path.GetFileName)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(["Commands", "Models", "Ports", "Queries"], subFolders);

        // No per-use-case subfolder explosion: Commands/Queries are flat single-file leaves.
        foreach (var axis in new[] { "Commands", "Queries" })
        {
            var axisRoot = Path.Combine(payouts, axis);
            Assert.Empty(Directory.EnumerateDirectories(axisRoot));
            Assert.NotEmpty(Directory.EnumerateFiles(axisRoot, "*.cs", SearchOption.TopDirectoryOnly));
        }

        // Every Application file's namespace matches its physical path exactly.
        foreach (var file in Directory.EnumerateFiles(applicationRoot, "*.cs", SearchOption.AllDirectories))
        {
            var relative = file[Path.GetFullPath(applicationRoot).Length..]
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (relative.StartsWith($"obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || relative.StartsWith($"bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var dir = Path.GetDirectoryName(relative);
            var expected = string.IsNullOrEmpty(dir)
                ? "Tooba.Settlement.Application"
                : "Tooba.Settlement.Application." + dir.Replace(Path.DirectorySeparatorChar, '.');
            var match = Regex.Match(File.ReadAllText(file), @"^namespace\s+([A-Za-z0-9_.]+)", RegexOptions.Multiline);
            Assert.True(match.Success, $"no namespace in {relative}");
            Assert.Equal(expected, match.Groups[1].Value);
        }
    }

    [Fact]
    public void Transport_validators_cover_the_endpoint_reachable_requests()
    {
        var validators = Read($"{ModuleRootRelative}/Tooba.Settlement.Application/Validation/SettlementRequestValidators.cs");
        foreach (var type in new[]
                 {
                     "RequestSellerPayoutCommandValidator",
                     "ProcessAdminPayoutCommandValidator",
                     "RetryAdminPayoutCommandValidator",
                     "QueryAdminPayoutGridQueryValidator",
                 })
        {
            Assert.Contains(type, validators, StringComparison.Ordinal);
        }

        Assert.Equal(4, Regex.Matches(validators, @"class \w+Validator : AbstractValidator<").Count);

        // Validators emit stable machine codes, never user-facing prose.
        Assert.DoesNotContain("WithMessage(", validators, StringComparison.Ordinal);
        Assert.Contains("SettlementValidationCodes.", validators, StringComparison.Ordinal);

        var codes = Read($"{ModuleRootRelative}/Tooba.Settlement.Application/Validation/SettlementValidationCodes.cs");
        Assert.Contains("namespace Tooba.Settlement.Application.Validation", codes, StringComparison.Ordinal);
        Assert.All(
            Regex.Matches(codes, @"""([^""]+)""").Select(m => m.Groups[1].Value),
            code => Assert.StartsWith("settlement.validation.", code, StringComparison.Ordinal));

        // The audience-first Validators/{Seller,Admin} tree must stay retired.
        var validationRoot = Path.Combine(Repo(), ModuleRootRelative, "Tooba.Settlement.Application/Validation");
        Assert.False(Directory.Exists(Path.Combine(validationRoot, "Seller")));
        Assert.False(Directory.Exists(Path.Combine(validationRoot, "Admin")));

        // The route inventory stays exactly 5 seller + 5 admin = 10 reachable requests.
        var seller = Read($"{ModuleRootRelative}/Tooba.Settlement.Endpoints/Seller/SettlementSellerEndpoints.cs");
        var admin = Read($"{ModuleRootRelative}/Tooba.Settlement.Endpoints/Admin/SettlementAdminEndpoints.cs");
        Assert.Equal(5, Regex.Matches(seller, @"group\.Map(Get|Post|Put|Delete)\(").Count);
        Assert.Equal(5, Regex.Matches(admin, @"group\.Map(Get|Post|Put|Delete)\(").Count);

        foreach (var endpoint in new[] { seller, admin })
        {
            Assert.Contains("ISender sender", endpoint, StringComparison.Ordinal);
            Assert.Contains("ApiResponseFactory api", endpoint, StringComparison.Ordinal);
            Assert.Contains("api.From(", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("ex.Message", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.Json", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.BadRequest", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.Problem", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("DbContext", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("SettlementDirectory", endpoint, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Domain_raises_invariants_by_the_single_canonical_code_constants()
    {
        foreach (var file in ProductionSources("Tooba.Settlement.Domain"))
        {
            var text = File.ReadAllText(file);
            foreach (var code in new[]
                     {
                         "\"settlement.amount.invalid\"", "\"settlement.idempotency.required\"",
                         "\"settlement.payout.invalid_state\"", "\"settlement.account.missing\"",
                         "\"payout.gateway.unconfigured\"", "\"settlement.outbox.unmapped_event\"",
                     })
            {
                Assert.DoesNotContain(code, text, StringComparison.Ordinal);
            }
        }

        var payoutRequest = Read($"{ModuleRootRelative}/Tooba.Settlement.Domain/Aggregates/PayoutRequest.cs");
        Assert.Contains("SettlementErrorCodes.AmountInvalid", payoutRequest, StringComparison.Ordinal);
        Assert.Contains("SettlementErrorCodes.IdempotencyRequired", payoutRequest, StringComparison.Ordinal);
        Assert.Contains("SettlementErrorCodes.PayoutInvalidState", payoutRequest, StringComparison.Ordinal);

        var entry = Read($"{ModuleRootRelative}/Tooba.Settlement.Domain/Aggregates/SettlementEntry.cs");
        Assert.Contains("SettlementErrorCodes.AmountInvalid", entry, StringComparison.Ordinal);
        Assert.Contains("SettlementErrorCodes.IdempotencyRequired", entry, StringComparison.Ordinal);

        // The Domain keeps exactly the one legal self-module Contracts edge (Cart/Payment/BulkInquiry precedent).
        var domainRefs = ProjectRefs("Tooba.Settlement.Domain");
        Assert.Contains(domainRefs, r => r.Contains("Tooba.Settlement.Contracts", StringComparison.Ordinal));
        Assert.DoesNotContain(domainRefs, r => r.Contains("Settlement.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(domainRefs, r => r.Contains("Settlement.Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(domainRefs, r => r.Contains("Tooba.Host", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Settlement_never_references_foreign_application_infrastructure_or_domain()
    {
        var infrastructureCsproj = Read($"{ModuleRootRelative}/Tooba.Settlement.Infrastructure/Tooba.Settlement.Infrastructure.csproj");
        foreach (var legal in new[]
                 {
                     "Tooba.Order.Contracts", "Tooba.Payment.Contracts", "Tooba.Returns.Contracts", "Tooba.Party.Contracts",
                 })
        {
            Assert.Contains(legal, infrastructureCsproj, StringComparison.Ordinal);
        }

        foreach (var project in ProductionProjects)
        {
            foreach (var file in ProductionSources(project))
            {
                var text = File.ReadAllText(file);
                foreach (var foreign in new[]
                         {
                             "Order.Application", "Order.Infrastructure", "Order.Domain",
                             "Payment.Application", "Payment.Infrastructure", "Payment.Domain",
                             "Returns.Application", "Returns.Infrastructure", "Returns.Domain",
                             "Party.Application", "Party.Infrastructure", "Party.Domain",
                             "Cart.Application", "Cart.Infrastructure", "Cart.Domain",
                             "Offer.Application", "Offer.Infrastructure", "Offer.Domain",
                         })
                {
                    Assert.DoesNotContain($"using Tooba.{foreign}", text, StringComparison.Ordinal);
                    Assert.DoesNotContain($"Tooba.{foreign}.", text, StringComparison.Ordinal);
                }

                Assert.DoesNotContain("OrderDbContext", text, StringComparison.Ordinal);
                Assert.DoesNotContain("PaymentDbContext", text, StringComparison.Ordinal);
                Assert.DoesNotContain("ReturnsDbContext", text, StringComparison.Ordinal);
                Assert.DoesNotContain("PartyDbContext", text, StringComparison.Ordinal);
                Assert.DoesNotContain("CartDbContext", text, StringComparison.Ordinal);
            }
        }

        // Endpoints must never reach Infrastructure or Host.
        var endpointsRefs = ProjectRefs("Tooba.Settlement.Endpoints");
        Assert.Contains(endpointsRefs, r => r.Contains("Settlement.Application", StringComparison.Ordinal));
        Assert.Contains(endpointsRefs, r => r.Contains("Settlement.Contracts", StringComparison.Ordinal));
        Assert.DoesNotContain(endpointsRefs, r => r.Contains("Settlement.Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(endpointsRefs, r => r.Contains("Tooba.Host", StringComparison.OrdinalIgnoreCase));

        // Application must never reach Infrastructure or Host.
        var applicationRefs = ProjectRefs("Tooba.Settlement.Application");
        Assert.DoesNotContain(applicationRefs, r => r.Contains("Settlement.Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(applicationRefs, r => r.Contains("Tooba.Host", StringComparison.OrdinalIgnoreCase));

        // No global-using alias workaround files survive anywhere in the module.
        var globalUsings = Directory.EnumerateFiles(Path.Combine(Repo(), ModuleRootRelative), "GlobalUsings*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(p => Path.GetRelativePath(Repo(), p).Replace('\\', '/'))
            .ToArray();
        Assert.Empty(globalUsings);
    }

    [Fact]
    public void Host_has_no_settlement_authority_beyond_composition_and_two_security_adapters()
    {
        var hostRoot = Path.Combine(Repo(), "src/backend/Host/Tooba.Host");
        Assert.False(Directory.Exists(Path.Combine(hostRoot, "Settlement")));
        Assert.False(Directory.Exists(Path.Combine(hostRoot, "Grid")));

        var program = File.ReadAllText(Path.Combine(hostRoot, "Program.cs"));
        Assert.Contains("MapSettlementEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("AddSettlementEndpointPresentation()", program, StringComparison.Ordinal);
        Assert.Contains("Tooba.Settlement.Endpoints.Seller.ISettlementSellerAuthorizer", program, StringComparison.Ordinal);
        Assert.Contains("Tooba.Settlement.Endpoints.Admin.ISettlementAdminAuthorizer", program, StringComparison.Ordinal);

        // The CQRS assembly registration points at the capability-first namespace.
        Assert.Contains("Tooba.Settlement.Application.Payouts.Queries.GetSellerSettlementBalanceQuery", program, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Settlement.Application.Queries.GetSellerSettlementBalance", program, StringComparison.Ordinal);
        Assert.DoesNotContain("using Tooba.Host.Settlement;", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Observability_uses_the_single_tooba_meter()
    {
        var instrumentation = Read($"{ModuleRootRelative}/Tooba.Settlement.Infrastructure/Observability/SettlementInstrumentation.cs");
        Assert.Contains("ToobaTelemetry.Meter", instrumentation, StringComparison.Ordinal);
        Assert.DoesNotContain("new Meter(", instrumentation, StringComparison.Ordinal);
        Assert.DoesNotContain("new ActivitySource(", instrumentation, StringComparison.Ordinal);
        Assert.DoesNotContain("StartActivity(", instrumentation, StringComparison.Ordinal);
        Assert.Contains("RecordEntryPosted", instrumentation, StringComparison.Ordinal);
        Assert.Contains("RecordPayoutSucceeded", instrumentation, StringComparison.Ordinal);
        Assert.Contains("RecordPayoutFailed", instrumentation, StringComparison.Ordinal);
    }

    [Fact]
    public void Migration_did_not_change_the_settlement_schema_migrations()
    {
        var migrations = Path.Combine(Repo(), ModuleRootRelative, "Tooba.Settlement.Infrastructure/Persistence/Migrations");
        var files = Directory.EnumerateFiles(migrations, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            [
                "20260827030000_InitialSettlement.Designer.cs",
                "20260827030000_InitialSettlement.cs",
                "SettlementDbContextModelSnapshot.cs",
            ],
            files);

        var outbox = Read($"{ModuleRootRelative}/Tooba.Settlement.Infrastructure/Messaging/SettlementOutboxRegistration.cs");
        Assert.Contains("SettlementDbContext.Schema", outbox, StringComparison.Ordinal);
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
        var root = Path.Combine(Repo(), ModuleRootRelative, project);
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
