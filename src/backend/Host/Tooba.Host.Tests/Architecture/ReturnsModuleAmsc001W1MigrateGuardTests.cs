using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Tooba.Returns.Contracts.Errors;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-RETURNS-AMSC-001-W1 — migrate wave durable guard.
/// Locks the single canonical Contracts stable-code home with its declared-code guard and reachability
/// split, the canonical typed-fault seam + transport destination parser, the localization resource set
/// and bilingual resources, the transport validator coverage, the removal of the duplicate command
/// shapes / message-text fault classification, the capability-first Application layout, the cohesion
/// splits, and the Contracts-only boundary that lets Returns be extracted as an isolated microservice.
/// </summary>
public sealed class ReturnsModuleAmsc001W1MigrateGuardTests
{
    private const string ModuleRootRelative = "src/backend/Modules/Returns";

    private static readonly string[] ProductionProjects =
    [
        "Tooba.Returns.Contracts",
        "Tooba.Returns.Domain",
        "Tooba.Returns.Application",
        "Tooba.Returns.Infrastructure",
        "Tooba.Returns.Endpoints",
    ];

    [Fact]
    public void Stable_codes_live_in_the_single_canonical_contracts_errors_home()
    {
        Assert.True(File.Exists(Path.Combine(
            Repo(), ModuleRootRelative, "Tooba.Returns.Contracts/Errors/ReturnsErrorCodes.cs")),
            "Contracts/Errors/ReturnsErrorCodes.cs must exist");
        Assert.Contains(
            "namespace Tooba.Returns.Contracts.Errors",
            Read($"{ModuleRootRelative}/Tooba.Returns.Contracts/Errors/ReturnsErrorCodes.cs"),
            StringComparison.Ordinal);

        var declarations = ProductionSources("Tooba.Returns.Contracts")
            .Concat(ProductionSources("Tooba.Returns.Domain"))
            .Concat(ProductionSources("Tooba.Returns.Application"))
            .Concat(ProductionSources("Tooba.Returns.Infrastructure"))
            .Concat(ProductionSources("Tooba.Returns.Endpoints"))
            .Count(file => File.ReadAllText(file).Contains("class ReturnsErrorCodes", StringComparison.Ordinal));
        Assert.Equal(1, declarations);
    }

    [Fact]
    public void Declared_code_catalog_splits_http_reachable_from_platform_faults()
    {
        // 20 HTTP-reachable outcome codes + 1 platform-side outbox fault = 21 declared.
        Assert.Equal(20, ReturnsErrorCodes.HttpReachable.Count);
        Assert.Equal(1, ReturnsErrorCodes.PlatformFaults.Count);

        Assert.True(ReturnsErrorCodes.IsHttpReachable(ReturnsErrorCodes.Missing));
        Assert.True(ReturnsErrorCodes.IsHttpReachable(ReturnsErrorCodes.RefundDestinationInvalid));
        Assert.True(ReturnsErrorCodes.IsHttpReachable(ReturnsErrorCodes.RefundPaymentMissing));
        Assert.False(ReturnsErrorCodes.IsHttpReachable(ReturnsErrorCodes.OutboxUnmappedEvent));

        Assert.True(ReturnsErrorCodes.IsPlatformFault(ReturnsErrorCodes.OutboxUnmappedEvent));
        Assert.False(ReturnsErrorCodes.IsPlatformFault(ReturnsErrorCodes.Missing));

        Assert.True(ReturnsErrorCodes.IsKnown(ReturnsErrorCodes.OutboxUnmappedEvent));
        Assert.False(ReturnsErrorCodes.IsKnown(null));
        Assert.False(ReturnsErrorCodes.IsKnown(string.Empty));
        Assert.False(ReturnsErrorCodes.IsKnown(" "));

        // Foundation-owned cross-cutting codes are deliberately NOT declared by Returns.
        Assert.False(ReturnsErrorCodes.IsKnown("customer.session.required"));
        Assert.False(ReturnsErrorCodes.IsKnown("seller.authorization.denied"));
        Assert.False(ReturnsErrorCodes.IsKnown("admin.authorization.denied"));

        var declared = typeof(ReturnsErrorCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToArray();
        Assert.NotEmpty(declared);
        Assert.All(declared, code => Assert.True(ReturnsErrorCodes.IsKnown(code), code));
        Assert.Equal(declared.Length, declared.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(
            ReturnsErrorCodes.HttpReachable.Count + ReturnsErrorCodes.PlatformFaults.Count,
            declared.Length);

        // Every HTTP-reachable code is registered exactly once by the module contributor; the platform
        // fault is deliberately never catalogued.
        var contributor = Read($"{ModuleRootRelative}/Tooba.Returns.Infrastructure/Errors/ReturnsErrorCatalogContributor.cs");
        Assert.Equal(
            ReturnsErrorCodes.HttpReachable.Count,
            Regex.Matches(contributor, @"D\(ReturnsErrorCodes\.").Count);
        Assert.DoesNotContain("OutboxUnmappedEvent", contributor, StringComparison.Ordinal);
    }

    [Fact]
    public void Typed_fault_seam_maps_declared_codes_and_never_parses_message_text()
    {
        var seamPath = Path.Combine(Repo(), ModuleRootRelative, "Tooba.Returns.Application/Composition/ReturnsOperation.cs");
        Assert.True(File.Exists(seamPath), "Application/Composition/ReturnsOperation.cs must exist");
        var text = File.ReadAllText(seamPath);

        Assert.Contains("ContractOperationException", text, StringComparison.Ordinal);
        Assert.Contains("ReturnsErrorCodes.IsKnown", text, StringComparison.Ordinal);
        Assert.Contains("SemanticException", text, StringComparison.Ordinal);
        Assert.Contains("Task<Result<T>> ExecuteAsync<T>", text, StringComparison.Ordinal);
        Assert.Contains("Task<Result> ExecuteAsync", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", text, StringComparison.Ordinal);

        Assert.Equal(
            "Tooba.Returns.Application.Composition",
            typeof(Tooba.Returns.Application.Composition.ReturnsOperation).Namespace);

        // The retired message-heuristic mapper and the dead semantic-mapper facade must stay retired.
        Assert.False(File.Exists(Path.Combine(
            Repo(), ModuleRootRelative, "Tooba.Returns.Application/Errors/ReturnsExceptionMapper.cs")));
        Assert.False(Directory.Exists(Path.Combine(Repo(), ModuleRootRelative, "Tooba.Returns.Application/Errors")));
        Assert.False(File.Exists(Path.Combine(
            Repo(), ModuleRootRelative, "Tooba.Returns.Application/Ports/ReturnSemanticMapper.cs")));
    }

    [Fact]
    public void Refund_destination_parser_is_a_separate_transport_seam()
    {
        var parserPath = Path.Combine(Repo(), ModuleRootRelative, "Tooba.Returns.Application/Composition/ReturnRefundDestinationParser.cs");
        Assert.True(File.Exists(parserPath), "Application/Composition/ReturnRefundDestinationParser.cs must exist");
        var text = File.ReadAllText(parserPath);
        Assert.Contains("RefundDestination", text, StringComparison.Ordinal);
        Assert.Contains("ReturnsErrorCodes.RefundDestinationInvalid", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Error_localization_resource_set_is_returns_owned_and_registered_once()
    {
        Assert.True(File.Exists(Path.Combine(
            Repo(), ModuleRootRelative, "Tooba.Returns.Contracts/Errors/ReturnsErrorResourceSet.cs")));
        Assert.True(File.Exists(Path.Combine(
            Repo(), ModuleRootRelative, "Tooba.Returns.Contracts/Resources/ReturnsErrors.resx")));
        Assert.True(File.Exists(Path.Combine(
            Repo(), ModuleRootRelative, "Tooba.Returns.Contracts/Resources/ReturnsErrors.fa.resx")));

        var module = Read($"{ModuleRootRelative}/Tooba.Returns.Endpoints/ReturnEndpointModule.cs");
        Assert.Contains("ReturnsErrorResourceSet", module, StringComparison.Ordinal);
        Assert.Equal(1, Regex.Matches(module, @"AddSingleton<IErrorResourceSet").Count);

        // Every declared code has a bilingual entry (EN + FA) in the two resource pairs.
        var en = Read($"{ModuleRootRelative}/Tooba.Returns.Contracts/Resources/ReturnsErrors.resx");
        var fa = Read($"{ModuleRootRelative}/Tooba.Returns.Contracts/Resources/ReturnsErrors.fa.resx");
        var declared = typeof(ReturnsErrorCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToArray();
        foreach (var code in declared)
        {
            Assert.Contains($"name=\"{code}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{code}\"", fa, StringComparison.Ordinal);
        }

        // The Resources folder is a real embedded-resource folder, not a namespace-bearing .cs home.
        var csproj = Read($"{ModuleRootRelative}/Tooba.Returns.Contracts/Tooba.Returns.Contracts.csproj");
        Assert.Contains("EmbeddedResource", csproj, StringComparison.Ordinal);
        Assert.Contains("ReturnsErrors.fa.resx", csproj, StringComparison.Ordinal);
    }

    [Fact]
    public void Transport_validators_cover_the_endpoint_reachable_requests()
    {
        var validators = Read($"{ModuleRootRelative}/Tooba.Returns.Application/Validation/ReturnsRequestValidators.cs");
        foreach (var type in new[]
                 {
                     "CreateReturnCommandValidator",
                     "ApproveReturnCommandValidator",
                     "RejectReturnCommandValidator",
                     "QueryAdminReturnsGridQueryValidator",
                 })
        {
            Assert.Contains(type, validators, StringComparison.Ordinal);
        }

        // Exactly four transport validators — one per VALIDATOR_REQUIRED request.
        Assert.Equal(4, Regex.Matches(validators, @"class \w+Validator : AbstractValidator<").Count);

        // Validators emit stable machine codes, never user-facing prose.
        Assert.DoesNotContain("WithMessage(", validators, StringComparison.Ordinal);
        Assert.Contains("ReturnsValidationCodes.", validators, StringComparison.Ordinal);

        var codes = Read($"{ModuleRootRelative}/Tooba.Returns.Application/Validation/ReturnsValidationCodes.cs");
        Assert.Contains("namespace Tooba.Returns.Application.Validation", codes, StringComparison.Ordinal);
        Assert.All(
            Regex.Matches(codes, @"""([^""]+)""").Select(m => m.Groups[1].Value),
            code => Assert.StartsWith("returns.validation.", code, StringComparison.Ordinal));
    }

    [Fact]
    public void Application_is_capability_first_shallow_without_technical_axis_roots()
    {
        var applicationRoot = Path.Combine(Repo(), ModuleRootRelative, "Tooba.Returns.Application");
        var topFolders = Directory.EnumerateDirectories(applicationRoot)
            .Select(Path.GetFileName)
            .Where(n => n is not "bin" and not "obj" and not "artifacts")
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(["Composition", "ReturnRequests", "Validation"], topFolders);

        foreach (var retired in new[] { "Commands", "Queries", "Models", "Ports", "Validators", "Errors" })
        {
            Assert.False(Directory.Exists(Path.Combine(applicationRoot, retired)), $"Application/{retired} must stay retired");
        }

        var returnRequests = Path.Combine(applicationRoot, "ReturnRequests");
        var subFolders = Directory.EnumerateDirectories(returnRequests)
            .Select(Path.GetFileName)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(["Commands", "Models", "Ports", "Queries"], subFolders);

        // No per-use-case subfolder explosion: every Commands/Queries leaf folder is a single-file leaf.
        foreach (var axis in new[] { "Commands", "Queries" })
        {
            var axisRoot = Path.Combine(returnRequests, axis);
            Assert.Empty(Directory.EnumerateDirectories(axisRoot));
            Assert.NotEmpty(Directory.EnumerateFiles(axisRoot, "*.cs", SearchOption.TopDirectoryOnly));
        }
    }

    [Fact]
    public void Cohesion_splits_are_real_and_the_duplicate_command_shapes_stay_retired()
    {
        var modelsRoot = Path.Combine(Repo(), ModuleRootRelative, "Tooba.Returns.Application/ReturnRequests/Models");
        Assert.True(File.Exists(Path.Combine(modelsRoot, "AdminReturnWorkQueueRow.cs")));
        Assert.True(File.Exists(Path.Combine(modelsRoot, "AdminReturnQueueFilters.cs")));
        Assert.False(File.Exists(Path.Combine(modelsRoot, "AdminReturnWorkQueueModels.cs")),
            "the mixed read-model + policy file must stay retired");

        Assert.True(File.Exists(Path.Combine(
            Repo(), ModuleRootRelative, "Tooba.Returns.Infrastructure/Directories/OpenReturnUseCaseGuard.cs")));

        // Duplicate command-shaped records beside the authoritative MediatR requests must stay retired.
        foreach (var duplicate in new[]
                 {
                     "ApproveReturnCommand.cs", "CreateReturnCommand.cs", "RejectReturnCommand.cs", "RetryRefundCommand.cs",
                 })
        {
            Assert.False(File.Exists(Path.Combine(modelsRoot, duplicate)), $"Models/{duplicate} must stay retired");
        }

        // The dead hardcoded-Persian display helper was removed.
        var reasonCodes = Read($"{ModuleRootRelative}/Tooba.Returns.Application/ReturnRequests/Models/ReturnEligibilityReasonCodes.cs");
        Assert.DoesNotContain("ToFaMessage", reasonCodes, StringComparison.Ordinal);
        Assert.Contains("ToErrorCode", reasonCodes, StringComparison.Ordinal);
    }

    [Fact]
    public void Returns_never_references_foreign_application_infrastructure_or_domain()
    {
        var infrastructureCsproj = Read($"{ModuleRootRelative}/Tooba.Returns.Infrastructure/Tooba.Returns.Infrastructure.csproj");
        foreach (var legal in new[]
                 {
                     "Tooba.Order.Contracts", "Tooba.Fulfillment.Contracts", "Tooba.Payment.Contracts",
                     "Tooba.Wallet.Contracts", "Tooba.Inventory.Contracts", "Tooba.Party.Contracts", "Tooba.Catalog.Contracts",
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
                             "Fulfillment.Application", "Fulfillment.Infrastructure", "Fulfillment.Domain",
                             "Payment.Application", "Payment.Infrastructure", "Payment.Domain",
                             "Wallet.Application", "Wallet.Infrastructure", "Wallet.Domain",
                             "Inventory.Application", "Inventory.Infrastructure", "Inventory.Domain",
                             "Party.Application", "Party.Infrastructure", "Party.Domain",
                             "Catalog.Application", "Catalog.Infrastructure", "Catalog.Domain",
                         })
                {
                    Assert.DoesNotContain($"using Tooba.{foreign}", text, StringComparison.Ordinal);
                    Assert.DoesNotContain($"Tooba.{foreign}.", text, StringComparison.Ordinal);
                }

                Assert.DoesNotContain("OrderDbContext", text, StringComparison.Ordinal);
                Assert.DoesNotContain("PaymentDbContext", text, StringComparison.Ordinal);
                Assert.DoesNotContain("FulfillmentDbContext", text, StringComparison.Ordinal);
                Assert.DoesNotContain("InventoryDbContext", text, StringComparison.Ordinal);
                Assert.DoesNotContain("WalletDbContext", text, StringComparison.Ordinal);
            }
        }

        // Endpoints must never reach Infrastructure or Host.
        var endpointsRefs = ProjectRefs("Tooba.Returns.Endpoints");
        Assert.Contains(endpointsRefs, r => r.Contains("Returns.Application", StringComparison.Ordinal));
        Assert.DoesNotContain(endpointsRefs, r => r.Contains("Returns.Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(endpointsRefs, r => r.Contains("Tooba.Host", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Returns_production_never_classifies_faults_by_message_text_or_inlines_code_literals()
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
                Assert.DoesNotContain("ReturnsExceptionMapper", text, StringComparison.Ordinal);
            }
        }

        // The stable-code literal identity lives ONLY in the Contracts declaration/contract files;
        // Application, Infrastructure and Endpoints must reference the constants, never inline them.
        var declarationFile = Path.GetFullPath(Path.Combine(
            Repo(), ModuleRootRelative, "Tooba.Returns.Contracts/Errors/ReturnsErrorCodes.cs"));
        var boundaryContractFile = Path.GetFullPath(Path.Combine(
            Repo(), ModuleRootRelative, "Tooba.Returns.Contracts/Operations/ReturnAdminOperationsContracts.cs"));
        var resourceFolder = Path.GetFullPath(Path.Combine(
            Repo(), ModuleRootRelative, "Tooba.Returns.Contracts/Resources"));

        foreach (var project in new[]
                 {
                     "Tooba.Returns.Application", "Tooba.Returns.Infrastructure", "Tooba.Returns.Endpoints",
                 })
        {
            foreach (var file in ProductionSources(project))
            {
                var full = Path.GetFullPath(file);
                if (string.Equals(full, declarationFile, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(full, boundaryContractFile, StringComparison.OrdinalIgnoreCase)
                    || full.StartsWith(resourceFolder, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var text = File.ReadAllText(file);
                foreach (var code in new[]
                         {
                             "return.missing", "return.stale", "return.quantity_exceeded", "refund.destination.invalid",
                         })
                {
                    Assert.DoesNotContain($"\"{code}\"", text, StringComparison.Ordinal);
                }
            }
        }
    }

    [Fact]
    public void Customer_endpoints_use_the_foundation_session_code_and_host_has_no_returns_authority()
    {
        var customer = Read($"{ModuleRootRelative}/Tooba.Returns.Endpoints/Customer/ReturnCustomerEndpoints.cs");
        Assert.Contains("FoundationErrorCodes.CustomerSessionRequired", customer, StringComparison.Ordinal);
        Assert.DoesNotContain("\"customer.actor.missing\"", customer, StringComparison.Ordinal);

        var hostRoot = Path.Combine(Repo(), "src/backend/Host/Tooba.Host");
        Assert.False(Directory.Exists(Path.Combine(hostRoot, "Returns")));
        var program = File.ReadAllText(Path.Combine(hostRoot, "Program.cs"));
        Assert.Contains("MapReturnEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("AddReturnEndpointPresentation", program, StringComparison.Ordinal);

        // The CQRS assembly registration points at the capability-first namespace.
        Assert.Contains("Tooba.Returns.Application.ReturnRequests.Commands.CreateReturnCommand", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Migration_did_not_change_the_returns_schema_migrations()
    {
        var migrations = Path.Combine(Repo(), ModuleRootRelative, "Tooba.Returns.Infrastructure/Persistence/Migrations");
        var files = Directory.EnumerateFiles(migrations, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            [
                "20260827020000_InitialReturns.Designer.cs",
                "20260827020000_InitialReturns.cs",
                "20260827200000_AddRefundDestination.cs",
                "20260909130600_DecimalReturnQuantity.cs",
                "ReturnsDbContextModelSnapshot.cs",
            ],
            files);
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
