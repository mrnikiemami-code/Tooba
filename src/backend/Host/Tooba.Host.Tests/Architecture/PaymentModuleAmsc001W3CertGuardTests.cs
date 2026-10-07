using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Payment.Application.Composition;
using Tooba.Payment.Application.Storefront.Commands;
using Tooba.Payment.Contracts.Errors;
using Tooba.Payment.Endpoints.Errors;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-PAYMENT-AMSC-001-W3 — ARCH-COMPLETE-002 certification lock.
/// Pins the AMSC certification lineage (W0 analyze / W1 migrate / W2 structure SHAs + certified
/// verdict), the structure-gate fields handed off by W2, the canonical seam single-ownership
/// (declared-code guard + dual-mechanism typed-fault seam + one catalog contributor + one resource
/// set), composed-catalog uniqueness, bilingual composed resolution, the exhaustive 16-request
/// validator matrix, module-owned HTTP ownership with ISender dispatch, the Contracts-only/own-schema
/// boundaries, and Host final closure.
/// </summary>
public sealed class PaymentModuleAmsc001W3CertGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/Payment";

    private static readonly ErrorDefinitionCatalog Catalog = new(
    [
        new FoundationErrorCatalogContributor(),
        new PaymentErrorCatalogContributor(),
    ]);

    private static readonly ResourceErrorMessageLocalizer Localizer = new(
        [new FoundationErrorResourceSet(), new PaymentErrorResourceSet()],
        Array.Empty<IErrorMessageContributor>());

    [Fact]
    public void Payment_is_amsc001_certified_in_manifest_and_sot()
    {
        var root = Repo();

        using var manifest = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-module-structure-manifests.json"))
                .Replace("\uFEFF", string.Empty));
        var moduleEntry = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Single(m => m.GetProperty("module").GetString() == "Payment");
        Assert.True(moduleEntry.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", moduleEntry.GetProperty("lockVersion").GetString());
        Assert.Contains(
            "TB-TMAR-PAYMENT-AMSC-001",
            moduleEntry.GetProperty("certificationNote").GetString(),
            StringComparison.Ordinal);

        using var sot = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"))
                .Replace("\uFEFF", string.Empty));
        var block = sot.RootElement.GetProperty("paymentAmsc001W3");
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", block.GetProperty("verdict").GetString());
        Assert.Equal("ARCH-COMPLETE-002", block.GetProperty("lockVersion").GetString());
        Assert.True(block.GetProperty("structureCertified").GetBoolean());
        Assert.True(block.GetProperty("microserviceExtractable").GetBoolean());
        Assert.Equal("HTTP_OWNING", block.GetProperty("httpApplicability").GetString());
        Assert.Equal("MODULE_ENDPOINTS", block.GetProperty("endpointOwnership").GetString());
        Assert.Equal("ZERO", block.GetProperty("hostPaymentHttpOwnership").GetString());
        Assert.Equal("MEDIATR_12_5", block.GetProperty("cqrs").GetString());
        Assert.Equal(16, block.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal("NONE", block.GetProperty("crossModuleJoinState").GetString());
        Assert.Equal("ZERO", block.GetProperty("foreignAppInfraDomainCoupling").GetString());
        Assert.Equal("UNCHANGED", block.GetProperty("schemaMigrationState").GetString());
        Assert.Equal(0, block.GetProperty("migrationFilesChanged").GetInt32());

        var certified = sot.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Equal(1, certified.Count(x => x == "Payment"));
    }

    [Fact]
    public void Amsc_lineage_pins_analyze_migrate_and_structure_shas()
    {
        using var sot = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(Repo(), "docs/architecture/tmar-current-state.json"))
                .Replace("\uFEFF", string.Empty));

        var w0 = sot.RootElement.GetProperty("paymentAmsc001W0");
        Assert.Equal("ANALYZE_COMPLETE", w0.GetProperty("state").GetString());
        Assert.Equal("READY_TO_MIGRATE", w0.GetProperty("verdict").GetString());

        var w1 = sot.RootElement.GetProperty("paymentAmsc001W1");
        Assert.Equal("MIGRATE_COMPLETE", w1.GetProperty("state").GetString());
        Assert.Equal("READY_TO_STRUCTURE", w1.GetProperty("verdict").GetString());
        Assert.Equal("OWNED_BY_PAYMENT", w1.GetProperty("localizationState").GetString());
        Assert.Equal("COMPLIANT", w1.GetProperty("contractsBoundaryState").GetString());

        var w2 = sot.RootElement.GetProperty("paymentAmsc001W2");
        Assert.Equal("STRUCTURE_COMPLETE", w2.GetProperty("state").GetString());
        Assert.Equal("READY_FOR_CERTIFY", w2.GetProperty("verdict").GetString());
        Assert.Equal("PROFESSIONAL_SHALLOW", w2.GetProperty("folderGranularityState").GetString());
        Assert.Equal("EXACT", w2.GetProperty("pathNamespaceState").GetString());
        Assert.Equal("ENFORCED", w2.GetProperty("rootAllowlistState").GetString());
        Assert.Equal("CLEAN", w2.GetProperty("physicalCopyState").GetString());
        Assert.Equal("CANONICAL", w2.GetProperty("solutionExplorerState").GetString());
        Assert.Equal("PRESERVED", w2.GetProperty("hostFinalClosure").GetString());

        // The certification wave must record the accepted AMSC lineage SHAs.
        var lineage = sot.RootElement.GetProperty("paymentAmsc001W3").GetProperty("acceptedLineage");
        Assert.Equal("6839bb4a", lineage.GetProperty("w0").GetString());
        Assert.Equal("2d69d828", lineage.GetProperty("w1").GetString());
        Assert.Equal("a138ec61", lineage.GetProperty("w2").GetString());
        Assert.Equal("502d73e0", lineage.GetProperty("w3").GetString());
        Assert.Equal(
            "502d73e0e9ccfb277a06ad397b1f0a511f586921",
            lineage.GetProperty("w3CertificationCommit").GetString());
    }

    [Fact]
    public void Recovery_closure_pins_exact_semantic_lineage_and_error_code_truth()
    {
        using var sot = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(Repo(), "docs/architecture/tmar-current-state.json"))
                .Replace("\uFEFF", string.Empty));

        // Durable semantic commit truth on the historical wave blocks (no history rewrite).
        Assert.Equal("6839bb4a", sot.RootElement.GetProperty("paymentAmsc001W0").GetProperty("commit").GetString());
        Assert.Equal(
            "6839bb4a5f75a954817719386de04e36ff96c305",
            sot.RootElement.GetProperty("paymentAmsc001W0").GetProperty("commitFull").GetString());
        Assert.Equal("2d69d828", sot.RootElement.GetProperty("paymentAmsc001W1").GetProperty("commit").GetString());
        Assert.Equal(
            "2d69d82808178e786f0673dc725baeb238987829",
            sot.RootElement.GetProperty("paymentAmsc001W1").GetProperty("commitFull").GetString());
        Assert.Equal("a138ec61", sot.RootElement.GetProperty("paymentAmsc001W2").GetProperty("commit").GetString());
        Assert.Equal(
            "a138ec61bcbbb719fed2949c60e21fa77104cfd1",
            sot.RootElement.GetProperty("paymentAmsc001W2").GetProperty("commitFull").GetString());

        // W3-R1 must be recorded as an explicit recovery block with its own SHA.
        var r1 = sot.RootElement.GetProperty("paymentAmsc001W3R1");
        Assert.Equal("PAYMENT_AMSC_001_RECOVERY_RECONCILED", r1.GetProperty("state").GetString());
        Assert.Equal("affdfba4", r1.GetProperty("commit").GetString());
        Assert.Equal(
            "affdfba4fc5fce402d05e68131bdb4a01899b93c",
            r1.GetProperty("commitFull").GetString());
        Assert.Equal(
            "502d73e0e9ccfb277a06ad397b1f0a511f586921",
            r1.GetProperty("certifiedCommit").GetString());

        // R2 closes the recovery with exact error-code truth; W3 stays the certification authority.
        var r2 = sot.RootElement.GetProperty("paymentAmsc001W3R2");
        Assert.Equal("PAYMENT_AMSC_001_RECOVERY_CLOSED_RECONCILED", r2.GetProperty("state").GetString());
        Assert.Equal("RECONCILED", r2.GetProperty("actualParentChainState").GetString());
        Assert.Equal(
            "502d73e0e9ccfb277a06ad397b1f0a511f586921",
            r2.GetProperty("currentCertifiedCommit").GetString());
        Assert.Equal("TB-TMAR-PAYMENT-AMSC-001-W3", r2.GetProperty("currentCertificationAuthority").GetString());
        Assert.Equal(28, r2.GetProperty("declaredStableCodeCount").GetInt32());
        Assert.Equal(27, r2.GetProperty("knownCodeGuardMemberCount").GetInt32());
        Assert.Equal(24, r2.GetProperty("paymentOwnedDescriptorCount").GetInt32());
        Assert.Equal(4, r2.GetProperty("foreignOwnedDeclaredConsumedCount").GetInt32());
        Assert.Equal(3, r2.GetProperty("foreignOwnedKnownGuardCount").GetInt32());
        Assert.Equal("admin.authorization.denied", r2.GetProperty("foreignOwnedNotInKnownGuard").GetString());
        Assert.False(r2.GetProperty("productionErrorCatalogChangedByR2").GetBoolean());
        Assert.False(r2.GetProperty("productionCodeChanged").GetBoolean());
        Assert.Equal("AUTHORITATIVE_28_DECLARED_27_KNOWN_24_OWNED_DESCRIPTORS", r2.GetProperty("stableErrorTruthState").GetString());
        Assert.Equal("UNCHANGED", r2.GetProperty("schemaMigrationState").GetString());
        Assert.Equal("PRESERVED", r2.GetProperty("globalHostCheckpointState").GetString());
        Assert.Equal("NONE", r2.GetProperty("guardsWeakened").GetString());
        Assert.Equal("NONE", r2.GetProperty("baselinesWidened").GetString());
        Assert.Equal("NONE", r2.GetProperty("automaticNextImplementationTask").GetString());

        // Production truth: 28 declared / 27 KnownCodes / 3 foreign codes admitted, admin auth excluded.
        Assert.True(PaymentErrorCodes.IsKnown(PaymentErrorCodes.ReservationRetryLimit));
        Assert.True(PaymentErrorCodes.IsKnown(PaymentErrorCodes.SupplyUnavailable));
        Assert.True(PaymentErrorCodes.IsKnown(PaymentErrorCodes.CheckoutAuthenticationRequired));
        Assert.False(PaymentErrorCodes.IsKnown(PaymentErrorCodes.AdminAuthorizationDenied));
    }

    [Fact]
    public void Structure_gate_fields_are_ready_for_certify_on_disk()
    {
        var applicationRoot = Path.Combine(Repo(), ModuleRoot, "Tooba.Payment.Application");

        // Structure-State: capability-first root with zero technical-axis-first root.
        var rootFolders = Directory.GetDirectories(applicationRoot)
            .Select(Path.GetFileName!)
            .Where(name => name is not ("bin" or "obj" or "artifacts") && !name.StartsWith('.'))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            new[] { "Admin", "Composition", "Models", "Ports", "Reconciliation", "Storefront", "Validators", "Webhooks" },
            rootFolders);
        foreach (var banned in new[] { "Commands", "Queries", "Orchestration" })
        {
            Assert.False(Directory.Exists(Path.Combine(applicationRoot, banned)));
        }

        // Folder-Granularity-State: zero per-use-case single-file request leaves.
        foreach (var project in new[] { "Tooba.Payment.Application", "Tooba.Payment.Endpoints" })
        {
            var projectDir = Path.Combine(Repo(), ModuleRoot, project);
            foreach (var dir in Directory.EnumerateDirectories(projectDir, "*", SearchOption.AllDirectories))
            {
                if (dir.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || dir.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                var files = Directory.EnumerateFiles(dir, "*.cs", SearchOption.TopDirectoryOnly).ToList();
                if (files.Count != 1 || Directory.EnumerateDirectories(dir).Any())
                {
                    continue;
                }

                var folderName = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar));
                Assert.False(
                    folderName.EndsWith("Command", StringComparison.Ordinal)
                        || folderName.EndsWith("Query", StringComparison.Ordinal)
                        || folderName.EndsWith("UseCase", StringComparison.Ordinal),
                    $"per-use-case request leaf folder {dir}");
            }
        }

        // Root-Allowlist-State: only the Endpoints composition entry is a root source file.
        Assert.Empty(Directory.GetFiles(applicationRoot, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Equal(
            new[] { "PaymentEndpointModule.cs" },
            Directory.GetFiles(Path.Combine(Repo(), ModuleRoot, "Tooba.Payment.Endpoints"), "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName!).ToArray());

        // Solution-Explorer-State: canonical /Modules/Payment/ grouping for all six projects.
        var slnx = File.ReadAllText(Path.Combine(Repo(), "src/backend/Tooba.slnx"));
        var folderStart = slnx.IndexOf("<Folder Name=\"/Modules/Payment/\">", StringComparison.Ordinal);
        Assert.True(folderStart >= 0);
        var group = slnx[folderStart..slnx.IndexOf("</Folder>", folderStart, StringComparison.Ordinal)];
        foreach (var project in new[]
                 {
                     "Tooba.Payment.Contracts", "Tooba.Payment.Domain", "Tooba.Payment.Application",
                     "Tooba.Payment.Infrastructure", "Tooba.Payment.Endpoints", "Tooba.Payment.Tests",
                 })
        {
            Assert.Contains($"Modules/Payment/{project}/{project}.csproj", group, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Canonical_seams_are_present_and_single_owned()
    {
        var root = Repo();

        var codes = Read("src/backend/Modules/Payment/Tooba.Payment.Contracts/Errors/PaymentErrorCodes.cs");
        Assert.Contains("public static bool IsKnown(string? code)", codes, StringComparison.Ordinal);
        Assert.Contains("private static readonly HashSet<string> KnownCodes", codes, StringComparison.Ordinal);
        Assert.Equal(28, Regex.Matches(codes, "public const string ").Count);

        var operation = Read("src/backend/Modules/Payment/Tooba.Payment.Application/Composition/PaymentOperation.cs");
        Assert.Contains(
            "catch (ContractOperationException ex) when (PaymentErrorCodes.IsKnown(ex.Code))",
            operation,
            StringComparison.Ordinal);
        Assert.Contains("catch (SemanticException ex)", operation, StringComparison.Ordinal);
        Assert.Contains("public static async Task<Result> ExecuteAsync(Func<Task> action)", operation, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", operation, StringComparison.Ordinal);

        // Exactly one catalog contributor and one resource set, registered once by the module composition.
        Assert.Equal(
            new[] { "PaymentErrorCatalogContributor.cs" },
            Directory.EnumerateFiles(
                    Path.Combine(root, ModuleRoot, "Tooba.Payment.Endpoints", "Errors"), "*Contributor.cs")
                .Select(Path.GetFileName).ToArray());
        var module = Read("src/backend/Modules/Payment/Tooba.Payment.Endpoints/PaymentEndpointModule.cs");
        Assert.Contains("IErrorCatalogContributor, PaymentErrorCatalogContributor>", module, StringComparison.Ordinal);
        Assert.Contains("IErrorResourceSet, PaymentErrorResourceSet>", module, StringComparison.Ordinal);
        Assert.Equal(1, Regex.Matches(module, @"AddSingleton<\s*IErrorCatalogContributor").Count);
        Assert.Equal(1, Regex.Matches(module, @"AddSingleton<\s*IErrorResourceSet").Count);

        // The legacy parallel mechanism must never resurrect.
        Assert.False(File.Exists(Path.Combine(
            root, ModuleRoot, "Tooba.Payment.Application", "Errors", "PaymentExceptionMapper.cs")));
        Assert.False(Directory.Exists(Path.Combine(root, ModuleRoot, "Tooba.Payment.Application", "Errors")));
    }

    [Fact]
    public void Composed_catalog_has_no_duplicate_payment_descriptor()
    {
        var contributor = new PaymentErrorCatalogContributor();
        var codes = contributor.Contribute().Select(d => d.Code).ToArray();

        Assert.Equal(24, codes.Length);
        Assert.Equal(codes.Length, codes.Distinct(StringComparer.OrdinalIgnoreCase).Count());

        // Descriptor ownership is unique: the four declared foreign-owned codes are never re-registered.
        foreach (var foreign in new[]
                 {
                     PaymentErrorCodes.ReservationRetryLimit,
                     PaymentErrorCodes.SupplyUnavailable,
                     PaymentErrorCodes.AdminAuthorizationDenied,
                     PaymentErrorCodes.CheckoutAuthenticationRequired,
                 })
        {
            Assert.DoesNotContain(foreign, codes);
        }

        // Every registered descriptor resolves to exactly one canonical owner in the composed catalog.
        foreach (var code in codes)
        {
            Assert.True(Catalog.TryGet(code, out var descriptor), "missing descriptor " + code);
            Assert.Equal(code, descriptor.LocalizationKey);
        }
    }

    [Fact]
    public void Bilingual_resources_resolve_every_payment_owned_code_through_the_composed_localizer()
    {
        var contractsRoot = Path.Combine(Repo(), ModuleRoot, "Tooba.Payment.Contracts");
        Assert.True(File.Exists(Path.Combine(contractsRoot, "Errors", "PaymentErrorResourceSet.cs")));
        var en = File.ReadAllText(Path.Combine(contractsRoot, "Resources", "PaymentErrors.resx"));
        var fa = File.ReadAllText(Path.Combine(contractsRoot, "Resources", "PaymentErrors.fa.resx"));

        var codes = new PaymentErrorCatalogContributor().Contribute().Select(d => d.Code).ToArray();
        Assert.Equal(24, codes.Length);

        var cultureEn = CultureInfo.GetCultureInfo("en");
        var cultureFa = CultureInfo.GetCultureInfo("fa");
        foreach (var code in codes)
        {
            Assert.Contains($"\"{code}\"", en, StringComparison.Ordinal);
            Assert.Contains($"\"{code}\"", fa, StringComparison.Ordinal);

            var titleEn = Localizer.Localize(code, cultureEn, new Dictionary<string, string?>(), "fallback");
            var titleFa = Localizer.Localize(code, cultureFa, new Dictionary<string, string?>(), "fallback");
            Assert.NotEqual("fallback", titleEn);
            Assert.NotEqual("fallback", titleFa);
            Assert.True(titleFa.Any(ch => ch is >= '\u0600' and <= '\u06ff'), "expected Persian for " + code);
        }

        // The three keys historically resolved by Order must keep byte-identical text from Payment.
        var orderEn = File.ReadAllText(Path.Combine(
            Repo(), "src/backend/Modules/Order/Tooba.Order.Endpoints/Resources/OrderErrors.resx"));
        foreach (var shared in new[]
                 {
                     PaymentErrorCodes.Missing,
                     PaymentErrorCodes.Rejected,
                     PaymentErrorCodes.UnpaidSupplyUnavailable,
                 })
        {
            Assert.Contains($"\"{shared}\"", orderEn, StringComparison.Ordinal);
            Assert.Contains($"\"{shared}\"", en, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Validator_matrix_is_exhaustive_for_all_sixteen_endpoint_reachable_requests()
    {
        var root = Repo();
        var applicationRoot = Path.Combine(root, ModuleRoot, "Tooba.Payment.Application");

        var validatorFiles = Directory
            .EnumerateFiles(applicationRoot, "*Validator.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(15, validatorFiles.Length);

        // 15 of 15 required validators present; the only NO_VALIDATOR_REQUIRED request has no input.
        var required = new[]
        {
            "InitiateStorefrontPaymentCommandValidator",
            "GetStorefrontWalletQuoteQueryValidator",
            "GetStorefrontPaymentQueryValidator",
            "GetStorefrontPaymentSandboxContextQueryValidator",
            "CompleteSandboxPaymentCommandValidator",
            "SubmitManualPaymentEvidenceCommandValidator",
            "RetryManualPaymentCommandValidator",
            "RetryUnpaidPaymentCommandValidator",
            "UploadManualPaymentProofCommandValidator",
            "GetAdminPaymentQueryValidator",
            "ReconcileAdminPaymentCommandValidator",
            "ConfirmAdminDepositCommandValidator",
            "RejectAdminDepositCommandValidator",
            "QueryAdminPaymentsGridQueryValidator",
            "ProcessPaymentWebhookCommandValidator",
        };
        Assert.Equal(
            required.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            validatorFiles.Select(Path.GetFileNameWithoutExtension).OrderBy(x => x, StringComparer.Ordinal).ToArray());

        // Every validator emits stable machine codes (directly or through the shared PaymentFluentRules
        // fragments, which call WithErrorCode), never localized prose.
        var rules = File.ReadAllText(Path.Combine(applicationRoot, "Validators", "PaymentFluentRules.cs"));
        Assert.Contains("WithErrorCode(errorCode)", rules, StringComparison.Ordinal);
        foreach (var file in validatorFiles)
        {
            var text = File.ReadAllText(file);
            Assert.True(
                text.Contains("WithErrorCode(", StringComparison.Ordinal)
                || text.Contains("PaymentFluentRules.", StringComparison.Ordinal),
                $"validator without stable machine code emission: {file}");
            Assert.DoesNotContain("پرداخت", text, StringComparison.Ordinal);
        }

        // Endpoint-reachable inventory: 10 storefront + 5 admin + 1 webhook, all through ISender + ApiResponseFactory.
        var endpoints = new[]
        {
            Path.Combine(root, ModuleRoot, "Tooba.Payment.Endpoints", "Storefront", "PaymentStorefrontEndpoints.cs"),
            Path.Combine(root, ModuleRoot, "Tooba.Payment.Endpoints", "Admin", "PaymentAdminEndpoints.cs"),
            Path.Combine(root, ModuleRoot, "Tooba.Payment.Endpoints", "Webhooks", "PaymentWebhookEndpoints.cs"),
        };
        var routeCount = endpoints.Sum(f => Regex.Matches(File.ReadAllText(f), @"\.Map(Get|Post|Put|Delete)\(").Count);
        Assert.Equal(16, routeCount);
        foreach (var file in endpoints)
        {
            var text = File.ReadAllText(file);
            Assert.Contains("ISender sender", text, StringComparison.Ordinal);
            Assert.Contains("sender.Send(", text, StringComparison.Ordinal);
            Assert.Contains("api.From", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.BadRequest", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.Problem", text, StringComparison.Ordinal);
            Assert.DoesNotContain("IValidator", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Accept-Language", text, StringComparison.Ordinal);
        }

        // The worker-only reconciliation request stays out of the HTTP inventory.
        Assert.True(File.Exists(Path.Combine(
            applicationRoot, "Reconciliation", "Commands", "ReconcileStalePaymentsCommand.cs")));
    }

    [Fact]
    public void Boundaries_stay_contracts_only_and_persistence_is_module_owned()
    {
        var root = Repo();
        var foreignPattern =
            @"Tooba\.(Order|Cart|Catalog|Identity|AccessControl|Content|CustomerProfile|Notification|Media|Fulfillment|Inventory|BulkInquiry|Localization|Settlement|Story|Wishlist|UserPreference|ProductQnA|Reviews|OperatorProfile|PageComposition|Promotion|Pricing|Tax|Returns|Wallet|Support|AddressBook)\.(Application|Infrastructure|Domain|Endpoints)";

        foreach (var project in new[]
                 {
                     "Tooba.Payment.Contracts", "Tooba.Payment.Domain", "Tooba.Payment.Application",
                     "Tooba.Payment.Infrastructure", "Tooba.Payment.Endpoints",
                 })
        {
            var projectDir = Path.Combine(root, ModuleRoot, project);
            foreach (var file in Directory.EnumerateFiles(projectDir, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                Assert.False(
                    Regex.IsMatch(File.ReadAllText(file), foreignPattern),
                    $"foreign module Application/Infrastructure/Domain/Endpoints reference in {file}");
            }
        }

        // Every outbound project edge is Contracts-only (or internal own-module layering).
        foreach (var project in new[]
                 {
                     "Tooba.Payment.Contracts", "Tooba.Payment.Domain", "Tooba.Payment.Application",
                     "Tooba.Payment.Infrastructure", "Tooba.Payment.Endpoints",
                 })
        {
            foreach (var reference in ProjectRefs(project))
            {
                Assert.DoesNotContain("Host", reference, StringComparison.OrdinalIgnoreCase);
                Assert.False(
                    Regex.IsMatch(reference, @"Tooba\.(?!Payment\.)[A-Za-z]+\.(Application|Infrastructure|Domain|Endpoints)\.csproj"),
                    $"{project} -> {reference}");
            }
        }

        // Endpoints must not reach Infrastructure or Domain.
        var endpointsCsproj = Read("src/backend/Modules/Payment/Tooba.Payment.Endpoints/Tooba.Payment.Endpoints.csproj");
        Assert.DoesNotContain("Tooba.Payment.Infrastructure.csproj", endpointsCsproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Payment.Domain.csproj", endpointsCsproj, StringComparison.Ordinal);

        // Own schema + own DbContext + own outbox + own webhook inbox; no foreign DbSet.
        var db = Read("src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Persistence/PaymentDbContext.cs");
        Assert.Contains("\"payment\"", db, StringComparison.Ordinal);
        foreach (var set in new[]
                 {
                     "DbSet<CustomerPayment> Payments",
                     "DbSet<PaymentAttempt> Attempts",
                     "DbSet<PaymentAllocation> Allocations",
                     "DbSet<PaymentProofAsset> ProofAssets",
                     "DbSet<PaymentMethodHoldOverride> MethodHoldOverrides",
                     "DbSet<OutboxMessage> OutboxMessages",
                     "DbSet<PaymentWebhookInboxRecord> WebhookInbox",
                 })
        {
            Assert.Contains(set, db, StringComparison.Ordinal);
        }

        Assert.Equal(1, Regex.Matches(db, @"class PaymentDbContext\b").Count);
        Assert.Contains("AddModuleSchemaMigrator<PaymentDbContext>", Read(
            "src/backend/Modules/Payment/Tooba.Payment.Infrastructure/DependencyInjection/PaymentModule.cs"), StringComparison.Ordinal);

        // Five canonical migrations, unchanged and never regenerated by this AMSC run.
        var migrations = Directory
            .EnumerateFiles(Path.Combine(root, ModuleRoot, "Tooba.Payment.Infrastructure", "Persistence", "Migrations"), "*.cs")
            .Select(Path.GetFileName!)
            .Where(n => !n.EndsWith("Designer.cs", StringComparison.Ordinal)
                        && !n.EndsWith("ModelSnapshot.cs", StringComparison.Ordinal))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            new[]
            {
                "20260823140000_InitialPayment.cs",
                "20260827000000_PaymentWebhookInbox.cs",
                "20260910180000_AddPaymentAllocationTargetKind.cs",
                "20260911080000_AddManualPaymentEvidence.cs",
                "20260912080000_AddUnpaidTimeoutAndMethodHolds.cs",
            },
            migrations);
    }

    [Fact]
    public void Host_final_closure_is_preserved_for_payment()
    {
        var root = Repo();

        // No Host-owned Payment business folder and no Host-owned Payment route.
        Assert.False(Directory.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Payments")));
        var program = Read("src/backend/Host/Tooba.Host/Program.cs");
        Assert.Contains("MapPaymentEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("AddPaymentEndpointPresentation()", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGroup(\"/v1/storefront\")", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGroup(\"/v1/admin\")", program, StringComparison.Ordinal);

        // Host residue stays exactly the two thin security adapters plus composition.
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Security/Payment/HostPaymentStorefrontAuthorizer.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Admin/Access/Authorizers/HostPaymentAdminAuthorizer.cs")));

        // The module owns its HTTP composition entry and its authorizer seams.
        Assert.Contains(
            "interface IPaymentStorefrontAuthorizer",
            Read("src/backend/Modules/Payment/Tooba.Payment.Endpoints/Storefront/IPaymentStorefrontAuthorizer.cs"),
            StringComparison.Ordinal);
        Assert.Contains(
            "interface IPaymentAdminAuthorizer",
            Read("src/backend/Modules/Payment/Tooba.Payment.Endpoints/Admin/IPaymentAdminAuthorizer.cs"),
            StringComparison.Ordinal);

        // Host final closure flags stay certified.
        using var sot = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"))
                .Replace("\uFEFF", string.Empty));
        Assert.Equal("HOST_ROOT_FINAL_CERTIFIED", sot.RootElement.GetProperty("currentHostCheckpoint").GetString());
    }

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(Repo(), relativePath));

    private static IReadOnlyList<string> ProjectRefs(string project)
    {
        var doc = System.Xml.Linq.XDocument.Load(Path.Combine(
            Repo(), ModuleRoot, project, $"{project}.csproj"));
        return doc.Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Where(x => x.Length > 0)
            .ToArray();
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
}
