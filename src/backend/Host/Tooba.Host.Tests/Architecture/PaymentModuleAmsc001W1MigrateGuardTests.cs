using System.Text.RegularExpressions;
using Tooba.Payment.Application.Composition;
using Tooba.Payment.Contracts.Errors;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-PAYMENT-AMSC-001-W1 — migrate wave durable guard.
/// Locks the canonical typed-fault seam, the declared-code catalog + Contracts error-code home,
/// the bilingual Payment-owned resource pair, the retired legacy mapper and the zero-foreign-coupling
/// boundary established by the migrate wave.
/// </summary>
public sealed class PaymentModuleAmsc001W1MigrateGuardTests
{
    private static readonly string[] ProductionProjects =
    [
        "Tooba.Payment.Contracts",
        "Tooba.Payment.Domain",
        "Tooba.Payment.Application",
        "Tooba.Payment.Infrastructure",
        "Tooba.Payment.Endpoints",
    ];

    [Fact]
    public void Operation_seam_maps_typed_codes_and_never_parses_message_text()
    {
        var text = Read("src/backend/Modules/Payment/Tooba.Payment.Application/Composition/PaymentOperation.cs");

        Assert.Contains("ContractOperationException", text, StringComparison.Ordinal);
        Assert.Contains("PaymentErrorCodes.IsKnown", text, StringComparison.Ordinal);
        Assert.Contains("SemanticException", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
        Assert.DoesNotContain("StartsWith", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Contains(\"", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Declared_code_catalog_is_the_single_known_code_source()
    {
        Assert.True(PaymentErrorCodes.IsKnown(PaymentErrorCodes.Missing));
        Assert.True(PaymentErrorCodes.IsKnown(PaymentErrorCodes.AlreadySucceeded));
        Assert.True(PaymentErrorCodes.IsKnown(PaymentErrorCodes.UnpaidSupplyUnavailable));
        Assert.True(PaymentErrorCodes.IsKnown(PaymentErrorCodes.SupplyUnavailable));
        Assert.False(PaymentErrorCodes.IsKnown("payment.not_found"));
        Assert.False(PaymentErrorCodes.IsKnown("cart.line.currency_missing"));
        Assert.False(PaymentErrorCodes.IsKnown(null));
        Assert.False(PaymentErrorCodes.IsKnown(" "));
    }

    [Fact]
    public void Stable_codes_live_in_the_contracts_errors_home()
    {
        Assert.True(File.Exists(Path.Combine(
            Repo(), "src/backend/Modules/Payment/Tooba.Payment.Contracts/Errors/PaymentErrorCodes.cs")));
        Assert.True(File.Exists(Path.Combine(
            Repo(), "src/backend/Modules/Payment/Tooba.Payment.Contracts/Errors/PaymentErrorResourceSet.cs")));
        Assert.False(File.Exists(Path.Combine(
            Repo(), "src/backend/Modules/Payment/Tooba.Payment.Application/Errors/PaymentErrorCodes.cs")),
            "Application/Errors duplicate home must not resurrect");
        Assert.False(Directory.Exists(Path.Combine(
            Repo(), "src/backend/Modules/Payment/Tooba.Payment.Application/Errors")),
            "Application/Errors folder must stay retired");
    }

    [Fact]
    public void Legacy_message_classification_mapper_is_retired()
    {
        Assert.False(File.Exists(Path.Combine(
            Repo(), "src/backend/Modules/Payment/Tooba.Payment.Application/Errors/PaymentExceptionMapper.cs")),
            "legacy PaymentExceptionMapper must not resurrect");

        foreach (var project in ProductionProjects)
        {
            foreach (var file in ProductionSources(project))
            {
                var text = File.ReadAllText(file);
                Assert.DoesNotContain("PaymentExceptionMapper", text, StringComparison.Ordinal);
                Assert.DoesNotContain("PublicAliases", text, StringComparison.Ordinal);
                Assert.DoesNotContain(".Message.Contains(", text, StringComparison.Ordinal);
                Assert.DoesNotContain(".Message.StartsWith(", text, StringComparison.Ordinal);
                Assert.DoesNotContain(".Message ==", text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Bilingual_resource_pair_covers_the_declared_payment_keyspace()
    {
        var contractsRoot = Path.Combine(Repo(), "src/backend/Modules/Payment/Tooba.Payment.Contracts");
        Assert.True(File.Exists(Path.Combine(contractsRoot, "Errors", "PaymentErrorResourceSet.cs")));

        var en = File.ReadAllText(Path.Combine(contractsRoot, "Resources", "PaymentErrors.resx"));
        var fa = File.ReadAllText(Path.Combine(contractsRoot, "Resources", "PaymentErrors.fa.resx"));
        foreach (var key in new[]
                 {
                     "payment.already_succeeded",
                     "payment.missing",
                     "payment.attempt.missing",
                     "payment.access.denied",
                     "payment.guest.invalid",
                     "payment.wallet.mixed_deferred",
                     "payment.method.unavailable",
                     "payment.tracking.required",
                     "payment.proof.required",
                     "payment.proof.foreign",
                     "payment.sandbox.unavailable",
                     "payment.unpaid.supply_unavailable",
                     "payment.unpaid.retry.invalid",
                     "payment.rejected",
                     "payment.webhook.invalid_signature",
                     "payment.webhook.invalid_payload",
                     "payment.webhook.amount_mismatch",
                     "payment.webhook.attempt_mismatch",
                     "payment.webhook.provider_mismatch",
                     "payment.gateway.unconfigured",
                     "admin.payment.missing",
                     "payment.method.not_manual",
                     "payment.confirm.invalid_state",
                     "payment.reject.invalid_state",
                 })
        {
            Assert.Contains($"\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"\"{key}\"", fa, StringComparison.Ordinal);
        }

        var module = Read("src/backend/Modules/Payment/Tooba.Payment.Endpoints/PaymentEndpointModule.cs");
        Assert.Contains("IErrorResourceSet, PaymentErrorResourceSet", module, StringComparison.Ordinal);
        Assert.Equal(1, Regex.Matches(module, @"AddSingleton<\s*IErrorResourceSet").Count);
    }

    [Fact]
    public void Payment_resource_set_claims_only_payment_owned_keyspaces()
    {
        var set = new PaymentErrorResourceSet();
        Assert.True(set.Owns("payment.missing"));
        Assert.True(set.Owns("admin.payment.missing"));
        Assert.False(set.Owns("order.missing"));
        Assert.False(set.Owns("inventory.supply.unavailable"));
        Assert.False(set.Owns("admin.authorization.denied"));
    }

    [Fact]
    public void Application_commands_and_queries_use_the_canonical_operation_seam()
    {
        // W2 Structure: requests live on the capability's own technical axes.
        var applicationRoot = Path.Combine(Repo(), "src/backend/Modules/Payment/Tooba.Payment.Application");
        var requestSources = Directory.EnumerateFiles(applicationRoot, "*Command.cs", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(applicationRoot, "*Query.cs", SearchOption.AllDirectories))
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

        foreach (var file in requestSources)
        {
            var text = File.ReadAllText(file);
            if (!text.Contains("IRequestHandler<", StringComparison.Ordinal))
            {
                continue;
            }

            Assert.DoesNotContain("InvalidOperationException", text, StringComparison.Ordinal);
            Assert.DoesNotContain("PaymentExceptionMapper", text, StringComparison.Ordinal);
            Assert.DoesNotContain(".Message.Contains(", text, StringComparison.Ordinal);

            // Worker-only reconciliation never crosses a faulting boundary; every other handler
            // maps typed faults through the canonical PaymentOperation seam or builds a Result directly.
            var isWorkerOnly = Path.GetFileName(file).Equals("ReconcileStalePaymentsCommand.cs", StringComparison.Ordinal);
            Assert.True(
                isWorkerOnly
                || text.Contains("PaymentOperation.ExecuteAsync", StringComparison.Ordinal)
                || text.Contains("Result.", StringComparison.Ordinal),
                Path.GetFileName(file));
        }
    }

    [Fact]
    public void Cross_module_boundary_stays_contracts_only()
    {
        foreach (var project in ProductionProjects)
        {
            var refs = ProjectRefs(project);
            Assert.DoesNotContain(refs, r => r.Contains("Host", StringComparison.OrdinalIgnoreCase));
            if (project is not ("Tooba.Payment.Infrastructure" or "Tooba.Payment.Endpoints"))
            {
                Assert.DoesNotContain(refs, r => Regex.IsMatch(
                    r, @"Tooba\.(Order|Fulfillment|Returns|Support|Cart|Offer)\.(Application|Infrastructure|Domain)"));
            }
        }

        var application = ProductionSources("Tooba.Payment.Application")
            .Select(File.ReadAllText)
            .ToArray();
        Assert.DoesNotContain(application, x => x.Contains("using Tooba.Order.Application", StringComparison.Ordinal));
        Assert.DoesNotContain(application, x => x.Contains("using Tooba.Order.Infrastructure", StringComparison.Ordinal));
        Assert.DoesNotContain(application, x => x.Contains("using Tooba.Order.Domain", StringComparison.Ordinal));
    }

    [Fact]
    public void Legacy_pre_alias_wire_literals_are_retired_to_declared_codes()
    {
        // W1: the four PaymentExceptionMapper PublicAliases source literals were private wire
        // aliases. They are now real declared PaymentErrorCodes members, so the raw literals must
        // not reappear in Payment production.
        foreach (var project in ProductionProjects)
        {
            foreach (var file in ProductionSources(project))
            {
                if (file.EndsWith("PaymentErrorCodes.cs", StringComparison.Ordinal))
                {
                    continue; // the declaration file itself is the single literal home
                }

                var text = File.ReadAllText(file);
                Assert.DoesNotContain("\"payment.not_found\"", text, StringComparison.Ordinal);
                Assert.DoesNotContain("\"payment.tracking_reference.required\"", text, StringComparison.Ordinal);
                Assert.DoesNotContain("\"payment.unpaid.retry.invalid_state\"", text, StringComparison.Ordinal);
                Assert.DoesNotContain("\"payment.already_succeeded\"", text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Payment_owned_error_codes_are_declared_in_the_contracts_home()
    {
        // Every Payment-owned descriptor registered by the catalog contributor must be declared in
        // the single Contracts/Errors home (descriptor ownership is unique and must not be duplicated).
        var contributor = Read("src/backend/Modules/Payment/Tooba.Payment.Endpoints/Errors/PaymentErrorCatalogContributor.cs");
        Assert.Contains("PaymentErrorCodes.", contributor, StringComparison.Ordinal);

        var declared = typeof(PaymentErrorCodes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToArray();

        Assert.Equal(28, declared.Length);
        Assert.Contains("payment.missing", declared);
        Assert.Contains("payment.unpaid.supply_unavailable", declared);
        Assert.Contains("inventory.reservation.retry_limit_reached", declared);
        Assert.Contains("admin.authorization.denied", declared);
        Assert.Contains("checkout.authentication_required", declared);
        Assert.Contains("inventory.supply.unavailable", declared);
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
        var root = Path.Combine(Repo(), "src", "backend", "Modules", "Payment", project);
        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
    }

    private static IReadOnlyList<string> ProjectRefs(string project)
    {
        var doc = System.Xml.Linq.XDocument.Load(Path.Combine(
            Repo(), "src", "backend", "Modules", "Payment", project, $"{project}.csproj"));
        return doc.Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Where(x => x.Length > 0)
            .ToArray();
    }
}
