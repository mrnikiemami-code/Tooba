using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-FULFILLMENT-AMSC-001-W3 (Certify) — durable certification locks.
/// Locks the ARCH-COMPLETE-002 certification verdict for the Fulfillment module: capability-first
/// Application structure, canonical dual typed-fault mapping, localization + unique descriptor
/// ownership, unchanged schema, module-owned HTTP surface, zero foreign coupling and an honest SoT
/// record. It reads real repository files, so a regression in any of these invariants fails here.
/// </summary>
public sealed class FulfillmentModuleAmsc001W3CertGuardTests
{
    private const string ModuleRelative = "src/backend/Modules/Fulfillment";

    [Fact]
    public void Fulfillment_certification_state_is_recorded_honestly_in_sot()
    {
        using var doc = ReadJson("docs/architecture/tmar-current-state.json");
        var root = doc.RootElement;

        var record = root.GetProperty("fulfillmentModuleAmsc001W3");
        Assert.Equal("FULFILLMENT_AMSC_001_CERTIFIED", record.GetProperty("state").GetString());
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", record.GetProperty("verdict").GetString());
        Assert.Equal("ARCH-COMPLETE-002", record.GetProperty("lockVersion").GetString());
        Assert.True(record.GetProperty("structureCertified").GetBoolean());
        Assert.True(record.GetProperty("microserviceExtractable").GetBoolean());
        Assert.Equal("ZERO", record.GetProperty("blockingResidualDebt").GetString());
        Assert.Equal("NONE", record.GetProperty("guardsWeakened").GetString());
        Assert.Equal("NONE", record.GetProperty("baselinesWidened").GetString());
        Assert.Equal("NONE", record.GetProperty("schemaChange").GetString());
        Assert.Equal("CERTIFIED", record.GetProperty("structureState").GetString());
        Assert.Equal("PROFESSIONAL_SHALLOW", record.GetProperty("folderGranularityState").GetString());
        Assert.Equal("EXACT", record.GetProperty("pathNamespaceState").GetString());
        Assert.Equal("ENFORCED", record.GetProperty("rootAllowlistState").GetString());

        // Honest behavior truth: W3 carries a bounded expected-failure repair (stale catch alignment),
        // never a blanket behaviorChange = NONE. The unaffected axes stay NONE so the claim cannot widen.
        Assert.Equal("BOUNDED_EXPECTED_FAILURE_REPAIR_CUSTOMER_AUTHORIZER_STALE_CATCH", record.GetProperty("behaviorChange").GetString());
        Assert.Equal("BOUNDED_EXPECTED_FAILURE_REMAP_500_TO_404_ON_STALE_CATCH_ALIGNMENT", record.GetProperty("statusCodesChanged").GetString());
        Assert.Equal("BOUNDED_STALE_CATCH_TYPE_ALIGNMENT_EXPECTED_FAILURE_REPAIR", record.GetProperty("w3RuntimeBehaviorChange").GetString());
        Assert.Equal("NONE", record.GetProperty("routesChanged").GetString());
        Assert.Equal("NONE", record.GetProperty("errorCodesChanged").GetString());
        Assert.Equal("NONE", record.GetProperty("dtoShapeChanged").GetString());

        // Host final closure must remain intact.
        Assert.Equal("HOST_ROOT_FINAL_CERTIFIED", root.GetProperty("currentHostCheckpoint").GetString());

        // The whole AMSC lineage stays recorded.
        foreach (var key in new[]
                 {
                     "fulfillmentModuleAmsc001W0", "fulfillmentModuleAmsc001W1",
                     "fulfillmentModuleAmsc001W2", "fulfillmentModuleAmsc001W3",
                 })
        {
            Assert.True(root.TryGetProperty(key, out _), $"missing SoT record {key}");
        }

        // W2's READY_FOR_CERTIFY handoff is correct historical W2 truth and must not be rewritten.
        Assert.Equal("READY_FOR_CERTIFY", root.GetProperty("fulfillmentModuleAmsc001W2").GetProperty("structureHandoffState").GetString());
    }

    [Fact]
    public void Fulfillment_w3_r1_recovery_reconciliation_is_locked()
    {
        using var doc = ReadJson("docs/architecture/tmar-current-state.json");
        var root = doc.RootElement;

        // The additive R1 checkpoint records the reconciliation without rewriting W0..W3 history.
        var r1 = root.GetProperty("fulfillmentModuleAmsc001W3R1");
        Assert.Equal("TB-TMAR-FULFILLMENT-AMSC-001-W3-R1", r1.GetProperty("task").GetString());
        Assert.Equal("TB-TMAR-FULFILLMENT-AMSC-001-W3", r1.GetProperty("parentTask").GetString());
        Assert.Equal("FULFILLMENT_AMSC_001_RECOVERY_RECONCILED", r1.GetProperty("state").GetString());
        Assert.False(r1.GetProperty("productionCodeChanged").GetBoolean());
        Assert.Equal("6f878aedcbb1571e9c483bc36aa88b5b304f37a8", r1.GetProperty("certifiedCommit").GetString());
        Assert.Equal("CERTIFIED", r1.GetProperty("structureState").GetString());
        Assert.Equal("READY_FOR_CERTIFY", r1.GetProperty("structureStateBefore").GetString());
        Assert.Equal("RECONCILED", r1.GetProperty("masterRecoveryState").GetString());
        Assert.Equal("PRESERVED", r1.GetProperty("historicalLineageState").GetString());
        Assert.Equal("UNCHANGED", r1.GetProperty("schemaMigrationState").GetString());
        Assert.Equal("NOT_TOUCHED", r1.GetProperty("manifestStructureChange").GetString());
        Assert.Equal("PRESERVED", r1.GetProperty("globalHostRootCheckpointState").GetString());
        Assert.Equal("NONE", r1.GetProperty("automaticNextImplementationTask").GetString());
        Assert.Equal("USER_REVIEW_FULFILLMENT_AMSC_001_W3_R1", r1.GetProperty("workflowStop").GetString());

        // The repository-global Host recovery authority must remain untouched by a module-local repair.
        Assert.Equal("HOST_ROOT_FINAL_CERTIFIED", root.GetProperty("currentHostCheckpoint").GetString());
        Assert.Equal("TB-TMAR-HOST-ROOT-FINAL-CERT-001", root.GetProperty("lastAcceptedTask").GetString());
        Assert.Equal("7a6c353a98a761df9124beb1fce23ed8424230de", root.GetProperty("lastAcceptedCommit").GetString());
        Assert.Equal("TB-TMAR-HOST-ROOT-FINAL-CERT-001", root.GetProperty("latestAcceptedImplementationWave").GetString());
        Assert.Equal("USER_REVIEW_HOST_ROOT_FINAL_CERT_001", root.GetProperty("workflowStop").GetString());
        Assert.Equal("NONE", root.GetProperty("automaticNextImplementationTask").GetString());
    }

    [Fact]
    public void Master_recovery_records_the_fulfillment_amsc_lineage_and_preserves_history()
    {
        var master = File.ReadAllText(Path.Combine(RepoRoot(), "docs", "architecture", "TOOBA-TMAR-MASTER-RECOVERY.md"));

        // The current Fulfillment module recovery checkpoint records the accepted AMSC lineage.
        Assert.Contains("Fulfillment AMSC module recovery checkpoint (authoritative, module-local)", master, StringComparison.Ordinal);
        foreach (var needle in new[]
                 {
                     "TB-TMAR-FULFILLMENT-AMSC-001-W0", "TB-TMAR-FULFILLMENT-AMSC-001-W1",
                     "TB-TMAR-FULFILLMENT-AMSC-001-W2", "TB-TMAR-FULFILLMENT-AMSC-001-W3",
                     "9fe50047", "bfd53da4", "c0db0566", "6f878aed",
                     "COMPLETE_REFERENCE_PATTERN", "ARCH-COMPLETE-002", "structureState = CERTIFIED",
                     "21 module-owned routes", "Contracts-only",
                     "automaticNextImplementationTask = NONE", "USER_REVIEW_FULFILLMENT_AMSC_001_W3_R1",
                 })
        {
            Assert.Contains(needle, master, StringComparison.Ordinal);
        }

        // The older Fulfillment audit/precert/host-evacuation/structure lineage stays present as history.
        foreach (var historical in new[]
                 {
                     "TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-AUDIT-001",
                     "TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-PRECERT-REPAIR-001",
                     "TB-TMAR-FULFILLMENT-HOST-EVACUATION-001",
                     "TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-STRUCTURE-001",
                     "HISTORICAL / SUPERSEDED FOR CURRENT FULFILLMENT MODULE RECOVERY",
                 })
        {
            Assert.Contains(historical, master, StringComparison.Ordinal);
        }

        // The global Host root closure is still recorded as the repository-global authority.
        Assert.Contains("HOST_ROOT_FINAL_CERTIFIED", master, StringComparison.Ordinal);
    }

    [Fact]
    public void Fulfillment_manifest_entry_is_single_and_disk_reconciled()
    {
        using var doc = ReadJson("docs/architecture/tmar-module-structure-manifests.json");
        var entries = doc.RootElement.GetProperty("modules").EnumerateArray()
            .Where(m => m.GetProperty("module").GetString() == "Fulfillment")
            .ToArray();

        Assert.Single(entries);
        Assert.True(entries[0].GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", entries[0].GetProperty("lockVersion").GetString());

        var application = entries[0].GetProperty("projects").EnumerateArray()
            .Single(p => p.GetProperty("projectName").GetString() == "Tooba.Fulfillment.Application");
        var forbidden = application.GetProperty("forbiddenTopLevelFolders").EnumerateArray()
            .Select(x => x.GetString())
            .ToArray();
        Assert.Contains("Commands", forbidden);
        Assert.Contains("Queries", forbidden);
        Assert.Contains("Models", forbidden);
        Assert.Contains("Ports", forbidden);

        // Manifest projects must match disk exactly (five production projects; the test project is excluded).
        var moduleRoot = ModuleRoot();
        var onDisk = Directory.GetDirectories(moduleRoot)
            .Select(d => Path.GetFileName(d)!)
            .Where(n => n.StartsWith("Tooba.Fulfillment.", StringComparison.Ordinal)
                        && !n.Equals("Tooba.Fulfillment.Tests", StringComparison.Ordinal))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        var manifested = entries[0].GetProperty("projects").EnumerateArray()
            .Select(p => p.GetProperty("projectName").GetString()!)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(manifested, onDisk);
        Assert.Equal(5, onDisk.Length);
    }

    [Fact]
    public void Fulfillment_owns_exactly_one_error_descriptor_per_owned_code()
    {
        var constants = DeclaredCodes();
        Assert.Equal(86, constants.Count);
        Assert.Equal(constants.Count, constants.Values.Distinct(StringComparer.Ordinal).Count());

        var contributor = File.ReadAllText(Path.Combine(
            ModuleRoot(), "Tooba.Fulfillment.Infrastructure", "Errors", "FulfillmentErrorCatalogContributor.cs"));

        // Exactly two declared codes are owned/registered by the Order contributor and are only
        // declared/consumed by Fulfillment (never re-registered): seller.order.missing and
        // customer.order.missing. Every other Fulfillment-declared code is registered exactly once.
        var foreignOwned = new[] { "SellerOrderMissing", "CustomerOrderMissing" };
        var locallyOwned = constants.Where(kv => !foreignOwned.Contains(kv.Key)).ToArray();
        Assert.Equal(84, locallyOwned.Length);
        Assert.Equal(locallyOwned.Length, Regex.Matches(contributor, @"^\s*D\(", RegexOptions.Multiline).Count);

        foreach (var (name, _) in locallyOwned)
        {
            Assert.Contains($"FulfillmentErrorCodes.{name}", contributor, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("FulfillmentErrorCodes.SellerOrderMissing", contributor, StringComparison.Ordinal);
        Assert.DoesNotContain("FulfillmentErrorCodes.CustomerOrderMissing", contributor, StringComparison.Ordinal);
        Assert.DoesNotContain("Contains(", contributor, StringComparison.Ordinal);
        Assert.DoesNotContain("StartsWith(", contributor, StringComparison.Ordinal);

        // No other contributor may register a Fulfillment-owned code.
        var backendRoot = Path.Combine(RepoRoot(), "src", "backend");
        foreach (var file in Directory.GetFiles(backendRoot, "*ErrorCatalogContributor.cs", SearchOption.AllDirectories))
        {
            if (file.EndsWith("FulfillmentErrorCatalogContributor.cs", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            foreach (var code in locallyOwned.Select(kv => kv.Value))
            {
                Assert.DoesNotContain($"\"{code}\"", text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Fulfillment_error_codes_resolve_to_localized_resources_in_both_cultures()
    {
        var constants = DeclaredCodes().Values
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToArray();

        foreach (var culture in new[] { "FulfillmentErrors.resx", "FulfillmentErrors.fa.resx" })
        {
            var resx = File.ReadAllText(Path.Combine(
                ModuleRoot(), "Tooba.Fulfillment.Endpoints", "Resources", culture));
            foreach (var code in constants)
            {
                Assert.Contains($"name=\"{code}\"", resx, StringComparison.Ordinal);
            }
        }

        // The canonical resource set + catalog seams must be wired for the module.
        var module = File.ReadAllText(Path.Combine(
            ModuleRoot(), "Tooba.Fulfillment.Endpoints", "FulfillmentEndpointModule.cs"));
        Assert.Contains("IErrorResourceSet, FulfillmentErrorResourceSet", module, StringComparison.Ordinal);
        var di = File.ReadAllText(Path.Combine(
            ModuleRoot(), "Tooba.Fulfillment.Infrastructure", "DependencyInjection", "FulfillmentModule.cs"));
        Assert.Contains("IErrorCatalogContributor, Errors.FulfillmentErrorCatalogContributor", di, StringComparison.Ordinal);
    }

    [Fact]
    public void Fulfillment_endpoints_use_only_the_canonical_result_factory()
    {
        var endpoints = Path.Combine(ModuleRoot(), "Tooba.Fulfillment.Endpoints");

        foreach (var file in Directory.GetFiles(endpoints, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            var relative = file[endpoints.Length..];

            Assert.DoesNotContain("Results.BadRequest", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.Problem", text, StringComparison.Ordinal);
            Assert.DoesNotContain("new ProblemDetails", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Accept-Language", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Fulfillment.Domain", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Fulfillment.Infrastructure", text, StringComparison.Ordinal);
            Assert.DoesNotContain("FulfillmentDbContext", text, StringComparison.Ordinal);

            // Expected failures must never be classified by parsing exception prose.
            Assert.DoesNotContain(".Message.StartsWith", text, StringComparison.Ordinal);
            Assert.DoesNotContain(".Message.Contains", text, StringComparison.Ordinal);
            Assert.DoesNotContain("catch (InvalidOperationException", text, StringComparison.Ordinal);

            if (relative.EndsWith("Endpoints.cs", StringComparison.Ordinal))
            {
                Assert.Contains("api.From(", text, StringComparison.Ordinal);
            }
        }

        // The one raw/anonymous success envelope that IS the shipped customer contract is preserved.
        var customer = File.ReadAllText(Path.Combine(endpoints, "Customer", "FulfillmentCustomerEndpoints.cs"));
        Assert.Contains("Results.Json(new", customer, StringComparison.Ordinal);

        // The shipping write endpoints were moved to the canonical factory in W1 (no raw Results.Json).
        var shipping = File.ReadAllText(Path.Combine(endpoints, "Shipping", "ShippingServiceEndpoints.cs"));
        Assert.DoesNotContain("Results.Json", shipping, StringComparison.Ordinal);
    }

    [Fact]
    public void Fulfillment_has_no_prose_classification_or_untyped_expected_fault_residue()
    {
        var root = ModuleRoot();
        var offenders = new List<string>();

        foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var relative = file[root.Length..];
            if (relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || relative.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || relative.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                // The module test project legitimately constructs/asserts literal machine codes.
                || relative.StartsWith($"{Path.DirectorySeparatorChar}Tooba.Fulfillment.Tests{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || relative.EndsWith("FulfillmentErrorCodes.cs", StringComparison.Ordinal)
                || relative.EndsWith("FulfillmentErrorCatalogContributor.cs", StringComparison.Ordinal)
                || relative.Contains("Errors.resx", StringComparison.Ordinal)
                || relative.EndsWith("FulfillmentValidationCodes.cs", StringComparison.Ordinal)
                // The typed fault helper is the single seam that validates a stable code; its own
                // InvalidOperationException carries a bare code for an unreachable mis-declaration.
                || relative.EndsWith($"{Path.DirectorySeparatorChar}FulfillmentErrors.cs", StringComparison.Ordinal)
                // Technical non-user-facing invariant: the outbox registration guard throws the declared
                // fulfillment.outbox.unmapped_event for an event type that is never registered unmapped.
                || relative.EndsWith("FulfillmentOutboxRegistration.cs", StringComparison.Ordinal)
                // Durable structural guard tests intentionally assert the literal names they forbid.
                || relative.Contains($"{Path.DirectorySeparatorChar}Architecture{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            if (text.Contains("Console.Write", StringComparison.Ordinal)
                || text.Contains("Debug.Write", StringComparison.Ordinal)
                || text.Contains("ActivitySource.StartActivity", StringComparison.Ordinal)
                || text.Contains("traceparent", StringComparison.Ordinal)
                || text.Contains("exception.Message", StringComparison.Ordinal)
                || text.Contains(".Message.Contains", StringComparison.Ordinal)
                || text.Contains(".Message.StartsWith", StringComparison.Ordinal))
            {
                offenders.Add(relative);
            }
        }

        Assert.Empty(offenders);
    }

    [Fact]
    public void Fulfillment_typed_fault_seam_maps_both_code_carrying_mechanisms()
    {
        var seam = File.ReadAllText(Path.Combine(
            ModuleRoot(), "Tooba.Fulfillment.Application", "Composition", "FulfillmentOperation.cs"));

        // Fulfillment's two typed, code-carrying fault mechanisms are both mapped by stable code:
        // ContractOperationException (Domain/Infrastructure) and SemanticException (Application/Domain).
        Assert.Contains("catch (ContractOperationException ex) when (FulfillmentErrors.IsKnown(ex.Code))", seam, StringComparison.Ordinal);
        Assert.Contains("catch (SemanticException ex)", seam, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", seam, StringComparison.Ordinal);
        Assert.DoesNotContain(".Message", seam, StringComparison.Ordinal);

        // The legacy message-parsing mapper is gone.
        Assert.False(File.Exists(Path.Combine(
            ModuleRoot(), "Tooba.Fulfillment.Application", "Errors", "FulfillmentExceptionMapper.cs")));
    }

    [Fact]
    public void Fulfillment_has_no_foreign_module_application_infrastructure_or_domain_dependency()
    {
        var root = ModuleRoot();
        var forbidden = new[]
        {
            "Tooba.Order.Application", "Tooba.Order.Domain", "Tooba.Order.Infrastructure",
            "Tooba.Cart.Application", "Tooba.Cart.Domain", "Tooba.Cart.Infrastructure",
            "Tooba.Catalog.Application", "Tooba.Catalog.Domain", "Tooba.Catalog.Infrastructure",
            "Tooba.Inventory.Application", "Tooba.Inventory.Domain", "Tooba.Inventory.Infrastructure",
            "Tooba.Pricing.Application", "Tooba.Pricing.Domain", "Tooba.Pricing.Infrastructure",
            "Tooba.Party.Application", "Tooba.Party.Domain", "Tooba.Party.Infrastructure",
            "Tooba.Offer.Application", "Tooba.Offer.Domain", "Tooba.Offer.Infrastructure",
            "Tooba.StoreContext.Application", "Tooba.StoreContext.Domain", "Tooba.StoreContext.Infrastructure",
        };

        foreach (var csproj in Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(csproj);
            foreach (var needle in forbidden)
            {
                Assert.DoesNotContain($"{needle}.csproj", text, StringComparison.Ordinal);
            }
        }

        foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var relative = file[root.Length..];
            if (relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || relative.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || relative.Contains($"{Path.DirectorySeparatorChar}Architecture{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            foreach (var needle in forbidden)
            {
                Assert.DoesNotContain($"using {needle}", text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Fulfillment_owns_its_http_surface_with_zero_host_http_ownership()
    {
        var endpointsRoot = Path.Combine(ModuleRoot(), "Tooba.Fulfillment.Endpoints");

        Assert.True(File.Exists(Path.Combine(endpointsRoot, "FulfillmentEndpointModule.cs")));
        Assert.True(Directory.Exists(Path.Combine(endpointsRoot, "Seller")));
        Assert.True(Directory.Exists(Path.Combine(endpointsRoot, "Admin")));
        Assert.True(Directory.Exists(Path.Combine(endpointsRoot, "Customer")));
        Assert.True(Directory.Exists(Path.Combine(endpointsRoot, "Shipping")));
        Assert.True(Directory.Exists(Path.Combine(endpointsRoot, "Resources")));

        // Exactly 21 module-owned routes.
        var routes = Directory.GetFiles(endpointsRoot, "*.cs", SearchOption.AllDirectories)
            .Sum(f => Regex.Matches(
                File.ReadAllText(f), @"\.Map(Get|Post|Put|Delete|Patch)\(").Count);
        Assert.Equal(21, routes);

        var hostProgram = File.ReadAllText(Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Program.cs"));
        Assert.Contains("MapFulfillmentEndpoints()", hostProgram, StringComparison.Ordinal);
        Assert.DoesNotContain("MapShippingServiceEndpoints()", hostProgram, StringComparison.Ordinal);

        // No Host Fulfillment-specific file may be resurrected.
        var hostRoot = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host");
        var hostFulfillment = Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                           && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(path => Path.GetFileName(path).Contains("Fulfillment", StringComparison.Ordinal))
            .ToList();
        Assert.True(hostFulfillment.Count == 0, "Host Fulfillment-specific files: " + string.Join("; ", hostFulfillment));
    }

    [Fact]
    public void Fulfillment_schema_and_migrations_are_unchanged()
    {
        var migrations = Path.Combine(
            ModuleRoot(), "Tooba.Fulfillment.Infrastructure", "Persistence", "Migrations");
        var files = Directory.GetFiles(migrations, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(f => Path.GetFileName(f)!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "20260827010000_InitialFulfillment.Designer.cs",
                "20260827010000_InitialFulfillment.cs",
                "20260907090000_AddFulfillmentItemQuantityPacked.cs",
                "20260907121000_AddShipmentProviderMetadata.cs",
                "20260908010000_ShipmentPreviousTracking.cs",
                "20260908120000_AddFulfillmentItemQuantityProcessing.cs",
                "20260909130500_DecimalFulfillmentQuantity.cs",
                "20260910120000_AddConsolidatedPackages.cs",
                "20260910170000_AddShippingServiceCatalog.cs",
                "FulfillmentDbContextModelSnapshot.cs",
            ],
            files);
    }

    [Fact]
    public void Fulfillment_application_is_capability_first_with_no_single_file_use_case_leaves()
    {
        var appRoot = Path.Combine(ModuleRoot(), "Tooba.Fulfillment.Application");

        foreach (var axis in new[] { "Commands", "Queries", "Models", "Ports" })
        {
            Assert.False(
                Directory.Exists(Path.Combine(appRoot, axis)),
                $"Application/{axis}/ is a forbidden technical-axis top-level folder");
        }

        foreach (var capability in new[] { "Shipping", "Fulfillments", "WorkQueue", "Checkout" })
        {
            Assert.True(Directory.Exists(Path.Combine(appRoot, capability)), $"missing capability {capability}");
        }

        // Cross-capability shared seams stay at the Application root: the typed-fault execution seam
        // (Composition) and the shared validation rules (Validators).
        Assert.True(File.Exists(Path.Combine(appRoot, "Composition", "FulfillmentOperation.cs")));
        Assert.True(File.Exists(Path.Combine(appRoot, "Validators", "FulfillmentFluentRules.cs")));

        // The legacy technical-axis single-file leaf folders must stay gone.
        foreach (var legacy in new[]
                 {
                     "Commands/CreateShippingService", "Commands/UpdateShippingService",
                     "Commands/DeactivateShippingService", "Commands/EnsureShippingCatalogSeed",
                     "Commands/SellerMutateFulfillment", "Commands/ExecuteAdminFulfillmentBulk",
                     "Queries/GetShippingService", "Queries/ListShippingServices",
                     "Queries/ListEnabledShippingMethodsTree", "Queries/GetAdminFulfillment",
                     "Queries/GetSellerFulfillment", "Queries/ListAdminFulfillments",
                     "Queries/ListSellerFulfillments", "Queries/QueryAdminFulfillmentWorkQueue",
                     "Queries/ListCustomerCheckoutFulfillments",
                 })
        {
            Assert.False(Directory.Exists(Path.Combine(appRoot, legacy)), $"legacy leaf folder {legacy} resurrected");
        }

        // Capability-first-shallow: the secondary axes carry no deeper leaves.
        foreach (var capability in new[] { "Shipping", "Fulfillments", "WorkQueue", "Checkout" })
        {
            foreach (var axis in Directory.EnumerateDirectories(Path.Combine(appRoot, capability)))
            {
                Assert.Empty(Directory.EnumerateDirectories(axis));
            }
        }
    }

    [Fact]
    public void Fulfillment_directory_stays_under_the_arch_size_ceiling()
    {
        var directories = Path.Combine(ModuleRoot(), "Tooba.Fulfillment.Infrastructure", "Directories");
        var main = File.ReadAllLines(Path.Combine(directories, "FulfillmentDirectory.cs")).Length;
        var packages = File.ReadAllLines(Path.Combine(directories, "FulfillmentDirectory.Packages.cs")).Length;

        Assert.True(main < 800, $"FulfillmentDirectory.cs is {main} LOC (ARCH-SIZE-001 ceiling 800)");
        Assert.True(packages < 800, $"FulfillmentDirectory.Packages.cs is {packages} LOC");
    }

    private static Dictionary<string, string> DeclaredCodes() =>
        Regex
            .Matches(
                File.ReadAllText(Path.Combine(
                    ModuleRoot(), "Tooba.Fulfillment.Contracts", "Errors", "FulfillmentErrorCodes.cs")),
                @"public const string (?<name>\w+) = ""(?<code>[^""]+)"";")
            .ToDictionary(m => m.Groups["name"].Value, m => m.Groups["code"].Value, StringComparer.Ordinal);

    private static string ModuleRoot() =>
        Path.Combine(RepoRoot(), ModuleRelative.Replace('/', Path.DirectorySeparatorChar));

    private static JsonDocument ReadJson(string relativePath) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepoRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar))));

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "src", "backend", "Tooba.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
