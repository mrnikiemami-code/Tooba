using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-CART-AMSC-001-W3 (Certify) — durable certification locks.
///
/// Locks the ARCH-COMPLETE-002 certification verdict for the Cart module: capability-first
/// Application structure, canonical Result/error mapping, localization + unique descriptor
/// ownership, unchanged schema, module-owned HTTP surface, zero foreign coupling and an honest SoT
/// record. It reads real repository files, so a regression in any of these invariants fails here.
/// </summary>
public sealed class CartModuleAmsc001W3CertGuardTests
{
    private const string ModuleRelative = "src/backend/Modules/Cart";

    [Fact]
    public void Cart_certification_state_is_recorded_honestly_in_sot()
    {
        using var doc = ReadJson("docs/architecture/tmar-current-state.json");
        var root = doc.RootElement;

        var record = root.GetProperty("cartModuleAmsc001W3");
        Assert.Equal("CART_AMSC_001_CERTIFIED", record.GetProperty("state").GetString());
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", record.GetProperty("verdict").GetString());
        Assert.Equal("ARCH-COMPLETE-002", record.GetProperty("lockVersion").GetString());
        Assert.True(record.GetProperty("structureCertified").GetBoolean());
        Assert.True(record.GetProperty("microserviceExtractable").GetBoolean());
        Assert.Equal("ZERO", record.GetProperty("blockingResidualDebt").GetString());
        Assert.Equal("NONE", record.GetProperty("guardsWeakened").GetString());
        Assert.Equal("NONE", record.GetProperty("baselinesWidened").GetString());
        Assert.Equal("NONE", record.GetProperty("schemaChange").GetString());
        Assert.Equal("READY_FOR_CERTIFY", record.GetProperty("structureState").GetString());
        Assert.Equal("PROFESSIONAL_SHALLOW", record.GetProperty("folderGranularityState").GetString());
        Assert.Equal("EXACT", record.GetProperty("pathNamespaceState").GetString());
        Assert.Equal("ENFORCED", record.GetProperty("rootAllowlistState").GetString());

        // Host final closure must remain intact.
        Assert.Equal("HOST_ROOT_FINAL_CERTIFIED", root.GetProperty("currentHostCheckpoint").GetString());

        // The whole AMSC lineage stays recorded.
        foreach (var key in new[]
                 {
                     "cartModuleAmsc001W0", "cartModuleAmsc001W1",
                     "cartModuleAmsc001W2", "cartModuleAmsc001W3",
                 })
        {
            Assert.True(root.TryGetProperty(key, out _), $"missing SoT record {key}");
        }
    }

    [Fact]
    public void Cart_manifest_entry_is_single_and_disk_reconciled()
    {
        using var doc = ReadJson("docs/architecture/tmar-module-structure-manifests.json");
        var entries = doc.RootElement.GetProperty("modules").EnumerateArray()
            .Where(m => m.GetProperty("module").GetString() == "Cart")
            .ToArray();

        Assert.Single(entries);
        Assert.True(entries[0].GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", entries[0].GetProperty("lockVersion").GetString());

        var application = entries[0].GetProperty("projects").EnumerateArray()
            .Single(p => p.GetProperty("projectName").GetString() == "Tooba.Cart.Application");
        var forbidden = application.GetProperty("forbiddenTopLevelFolders").EnumerateArray()
            .Select(x => x.GetString())
            .ToArray();
        Assert.Contains("Commands", forbidden);
        Assert.Contains("Queries", forbidden);
        Assert.Contains("Models", forbidden);

        // Manifest projects must match disk exactly (five production projects + the test project).
        var moduleRoot = ModuleRoot();
        var onDisk = Directory.GetDirectories(moduleRoot)
            .Select(d => Path.GetFileName(d)!)
            .Where(n => n.StartsWith("Tooba.Cart.", StringComparison.Ordinal)
                        && !n.Equals("Tooba.Cart.Tests", StringComparison.Ordinal))
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
    public void Cart_owns_exactly_one_error_descriptor_per_owned_code()
    {
        var contracts = File.ReadAllText(Path.Combine(
            ModuleRoot(), "Tooba.Cart.Contracts", "Errors", "CartErrorCodes.cs"));
        var constants = Regex
            .Matches(contracts, @"public const string (?<name>\w+) = ""(?<code>[^""]+)"";")
            .ToDictionary(m => m.Groups["name"].Value, m => m.Groups["code"].Value, StringComparer.Ordinal);

        Assert.Equal(27, constants.Count);
        Assert.Equal(constants.Count, constants.Values.Distinct(StringComparer.Ordinal).Count());

        var contributor = File.ReadAllText(Path.Combine(
            ModuleRoot(), "Tooba.Cart.Endpoints", "Errors", "CartErrorCatalogContributor.cs"));

        // Two declared codes are intentionally not registered by the Cart contributor:
        // - checkout.authentication_required is owned/registered by the Foundation contributor (consumed, not re-registered);
        // - cart.line.currency_missing is declared+localized+thrown but registered by no contributor (SoT watch R1).
        var locallyOwned = constants
            .Where(kv => kv.Value != "checkout.authentication_required"
                         && kv.Value != "cart.line.currency_missing")
            .ToArray();
        Assert.Equal(25, locallyOwned.Length);
        Assert.Equal(locallyOwned.Length, Regex.Matches(contributor, @"^\s*D\(", RegexOptions.Multiline).Count);
        foreach (var (name, _) in locallyOwned)
        {
            Assert.Contains($"CartErrorCodes.{name}", contributor, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("CartErrorCodes.AuthenticationRequired", contributor, StringComparison.Ordinal);
        Assert.DoesNotContain("Contains(", contributor, StringComparison.Ordinal);
        Assert.DoesNotContain("StartsWith(", contributor, StringComparison.Ordinal);

        // No other contributor may register a Cart-owned code.
        var backendRoot = Path.Combine(RepoRoot(), "src", "backend");
        foreach (var file in Directory.GetFiles(backendRoot, "*ErrorCatalogContributor.cs", SearchOption.AllDirectories))
        {
            if (file.EndsWith("CartErrorCatalogContributor.cs", StringComparison.Ordinal)
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
    public void Cart_error_codes_resolve_to_localized_resources_in_both_cultures()
    {
        var constants = Regex
            .Matches(
                File.ReadAllText(Path.Combine(
                    ModuleRoot(), "Tooba.Cart.Contracts", "Errors", "CartErrorCodes.cs")),
                @"public const string \w+ = ""(?<code>[^""]+)"";")
            .Select(m => m.Groups["code"].Value)
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToArray();

        foreach (var culture in new[] { "CartErrors.resx", "CartErrors.fa.resx" })
        {
            var resx = File.ReadAllText(Path.Combine(
                ModuleRoot(), "Tooba.Cart.Endpoints", "Resources", culture));
            foreach (var code in constants)
            {
                Assert.Contains($"name=\"{code}\"", resx, StringComparison.Ordinal);
            }
        }

        // The canonical resource set + catalog seams must be wired for the module.
        var module = File.ReadAllText(Path.Combine(
            ModuleRoot(), "Tooba.Cart.Endpoints", "CartEndpointModule.cs"));
        Assert.Contains("IErrorResourceSet, CartErrorResourceSet", module, StringComparison.Ordinal);
        Assert.Contains("IErrorCatalogContributor, CartErrorCatalogContributor", module, StringComparison.Ordinal);
    }

    [Fact]
    public void Cart_endpoints_use_only_the_canonical_result_factory()
    {
        var endpoints = Path.Combine(ModuleRoot(), "Tooba.Cart.Endpoints");

        foreach (var file in Directory.GetFiles(endpoints, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            var relative = file[endpoints.Length..];

            Assert.DoesNotContain("Results.Json", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.NoContent", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.BadRequest", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.Problem", text, StringComparison.Ordinal);
            Assert.DoesNotContain("new ProblemDetails", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Accept-Language", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Cart.Domain", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Cart.Infrastructure", text, StringComparison.Ordinal);

            // Expected failures must never be classified by parsing exception prose.
            Assert.DoesNotContain(".Message.StartsWith", text, StringComparison.Ordinal);
            Assert.DoesNotContain(".Message.Contains", text, StringComparison.Ordinal);

            if (relative.EndsWith("Endpoints.cs", StringComparison.Ordinal))
            {
                Assert.Contains("api.From(", text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Cart_has_no_raw_error_code_literals_ad_hoc_logging_or_untyped_fault_residue()
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
                || relative.StartsWith($"{Path.DirectorySeparatorChar}Tooba.Cart.Tests{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || relative.EndsWith("CartErrorCodes.cs", StringComparison.Ordinal)
                || relative.EndsWith("CartErrorCatalogContributor.cs", StringComparison.Ordinal)
                || relative.Contains("Errors.resx", StringComparison.Ordinal)
                || relative.EndsWith("CartValidationCodes.cs", StringComparison.Ordinal)
                // Technical non-user-facing invariant: the outbox registration guard throws
                // "cart.outbox.unmapped_event" for an event type that is never registered unmapped
                // (mirrors the AddressBook outbox guard recorded as watch R1). It carries no
                // user-facing text, resolves through no HTTP/error catalog and is unreachable.
                || relative.EndsWith("CartOutboxRegistration.cs", StringComparison.Ordinal)
                // Durable structural guard tests intentionally assert the literal names they forbid.
                || relative.Contains($"{Path.DirectorySeparatorChar}Architecture{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            if (Regex.IsMatch(text, @"""(?:cart|checkout)\.[a-z_]+(?:\.[a-z_]+)*""")
                || text.Contains("Console.Write", StringComparison.Ordinal)
                || text.Contains("Debug.Write", StringComparison.Ordinal)
                || text.Contains("ActivitySource.StartActivity", StringComparison.Ordinal)
                || text.Contains("traceparent", StringComparison.Ordinal)
                || text.Contains("exception.Message", StringComparison.Ordinal))
            {
                offenders.Add(relative);
            }
        }

        Assert.Empty(offenders);
    }

    [Fact]
    public void Cart_has_no_foreign_module_application_infrastructure_or_domain_dependency()
    {
        var root = ModuleRoot();
        var forbidden = new[]
        {
            "Tooba.Order.Application", "Tooba.Order.Domain", "Tooba.Order.Infrastructure",
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

        // Only the approved foreign Contracts edges may exist.
        var cartDirectory = File.ReadAllText(Path.Combine(
            root, "Tooba.Cart.Infrastructure", "Directories", "CartDirectory.cs"));
        Assert.Contains("using Tooba.Catalog.Contracts", cartDirectory, StringComparison.Ordinal);
        Assert.Contains("using Tooba.Pricing.Contracts", cartDirectory, StringComparison.Ordinal);
        Assert.Contains("using Tooba.Inventory.Contracts", cartDirectory, StringComparison.Ordinal);
        Assert.Contains("using Tooba.Offer.Contracts", cartDirectory, StringComparison.Ordinal);
    }

    [Fact]
    public void Cart_owns_its_http_surface_with_zero_host_http_ownership()
    {
        var endpointsRoot = Path.Combine(ModuleRoot(), "Tooba.Cart.Endpoints");

        Assert.True(File.Exists(Path.Combine(endpointsRoot, "CartEndpointModule.cs")));
        Assert.True(Directory.Exists(Path.Combine(endpointsRoot, "Storefront")));
        Assert.True(Directory.Exists(Path.Combine(endpointsRoot, "Errors")));
        Assert.True(Directory.Exists(Path.Combine(endpointsRoot, "Resources")));

        // Exactly 7 module-owned routes.
        var routes = Directory.GetFiles(endpointsRoot, "*.cs", SearchOption.AllDirectories)
            .Sum(f => Regex.Matches(
                File.ReadAllText(f), @"group\.Map(Get|Post|Put|Delete|Patch)\(").Count);
        Assert.Equal(7, routes);

        // The Host HTTP surface for Cart must never be resurrected.
        Assert.False(Directory.Exists(Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Storefront")));
        var hostProgram = File.ReadAllText(Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Program.cs"));
        Assert.Contains("MapCartEndpoints()", hostProgram, StringComparison.Ordinal);
    }

    [Fact]
    public void Cart_schema_and_migrations_are_unchanged()
    {
        var migrations = Path.Combine(
            ModuleRoot(), "Tooba.Cart.Infrastructure", "Persistence", "Migrations");
        var files = Directory.GetFiles(migrations, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(f => Path.GetFileName(f)!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "20260823100150_InitialCart.Designer.cs",
                "20260823100150_InitialCart.cs",
                "20260909130200_DecimalCartQuantity.cs",
                "20260919183000_CartLineMerchandisingCampaignId.cs",
                "CartDbContextModelSnapshot.cs",
            ],
            files);
    }

    [Fact]
    public void Cart_application_is_capability_first_with_no_single_file_use_case_leaves()
    {
        var appRoot = Path.Combine(ModuleRoot(), "Tooba.Cart.Application");

        foreach (var axis in new[] { "Commands", "Queries", "Models" })
        {
            Assert.False(
                Directory.Exists(Path.Combine(appRoot, axis)),
                $"Application/{axis}/ is a forbidden technical-axis top-level folder");
        }

        Assert.True(Directory.Exists(Path.Combine(appRoot, "Carts", "Commands")));
        Assert.True(Directory.Exists(Path.Combine(appRoot, "Carts", "Queries")));
        Assert.True(Directory.Exists(Path.Combine(appRoot, "Carts", "Validators")));

        // The 7 legacy single-file use-case leaf folders must stay gone.
        foreach (var legacy in new[]
                 {
                     "Commands/AddCartLine", "Commands/ChangeCartLineQuantity", "Commands/CreateGuestCart",
                     "Commands/MergeCartAfterLogin", "Commands/RemoveCartLine",
                     "Queries/GetCart", "Queries/GetCurrentCart",
                 })
        {
            Assert.False(Directory.Exists(Path.Combine(appRoot, legacy)), $"legacy leaf folder {legacy} resurrected");
        }

        // Capability-first-shallow: the secondary axes carry no deeper leaves.
        foreach (var axis in Directory.EnumerateDirectories(Path.Combine(appRoot, "Carts")))
        {
            Assert.Empty(Directory.EnumerateDirectories(axis));
        }
    }

    [Fact]
    public void Cart_typed_faults_and_handler_result_seam_remain_canonical()
    {
        var application = Path.Combine(ModuleRoot(), "Tooba.Cart.Application");

        Assert.True(File.Exists(Path.Combine(application, "Composition", "CartOperation.cs")));

        // Every CQRS request in the capability tree is dispatched through the canonical CartOperation
        // seam; validators (which implement no handler) are the only files without the seam.
        foreach (var file in Directory.GetFiles(Path.Combine(application, "Carts"), "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            var isValidator = Path.GetFileName(file).EndsWith("Validator.cs", StringComparison.Ordinal);
            if (isValidator)
            {
                Assert.DoesNotContain("CartOperation.ExecuteAsync", text, StringComparison.Ordinal);
                continue;
            }

            Assert.Contains("IRequestHandler<", text, StringComparison.Ordinal);
            Assert.Contains("CartOperation.ExecuteAsync", text, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// TB-TMAR-CART-AMSC-001-W3-R1 reconciliation lock. W3 originally recorded
    /// <c>behaviorChange = NONE</c> while W1 (read from the same SoT document) records the accepted
    /// bounded expected-failure repair (unexpected 500 -> catalogued 400/409/503). This is a real
    /// cross-record consistency lock, not a tautological self-comparison.
    /// </summary>
    [Fact]
    public void Cart_certification_truth_records_the_accepted_w1_bounded_defect_repair()
    {
        using var doc = ReadJson("docs/architecture/tmar-current-state.json");
        var root = doc.RootElement;

        // W1 is the wave that performed the accepted repair and must still record it.
        var w1 = root.GetProperty("cartModuleAmsc001W1");
        var w1Behavior = w1.GetProperty("behaviorChangeState").GetString()!;
        Assert.StartsWith("PREVIOUSLY_500", w1Behavior, StringComparison.Ordinal);
        Assert.Contains("400", w1Behavior, StringComparison.Ordinal);
        Assert.Contains("409", w1Behavior, StringComparison.Ordinal);
        Assert.Contains("503", w1Behavior, StringComparison.Ordinal);

        // The final certification record must not contradict W1 with a NONE behavior claim.
        var w3 = root.GetProperty("cartModuleAmsc001W3");
        Assert.Equal(
            "BOUNDED_DEFECT_REPAIR_EXPECTED_FAILURE_MAPPING",
            w3.GetProperty("behaviorChange").GetString());
        Assert.Equal(
            "BOUNDED_EXPECTED_FAILURE_REMAP_500_TO_400_409_503",
            w3.GetProperty("statusCodesChanged").GetString());
        Assert.NotEqual("NONE", w3.GetProperty("statusCodesChanged").GetString());

        // W3 itself introduced no runtime behavior change; the repair belongs to W1.
        Assert.Equal("NONE", w3.GetProperty("w3RuntimeBehaviorChange").GetString());

        // Unaffected axes stay NONE so the bounded claim cannot silently widen.
        Assert.Equal("NONE", w3.GetProperty("schemaChange").GetString());
        Assert.Equal("NONE", w3.GetProperty("routesChanged").GetString());
        Assert.Equal("NONE", w3.GetProperty("errorCodesChanged").GetString());
        Assert.Equal("NONE", w3.GetProperty("dtoShapeChanged").GetString());

        // 27 declared/consumed codes != 25 Cart-registered descriptors; the shared checkout code is
        // Foundation-owned and consumed-not-registered by Cart.
        Assert.Equal(27, w3.GetProperty("declaredOrConsumedCodeCount").GetInt32());
        Assert.Equal(25, w3.GetProperty("cartOwnedDescriptorCount").GetInt32());
        Assert.NotEqual(
            w3.GetProperty("declaredOrConsumedCodeCount").GetInt32(),
            w3.GetProperty("cartOwnedDescriptorCount").GetInt32());
        Assert.Equal("checkout.authentication_required", w3.GetProperty("sharedConsumedCode").GetString());
        Assert.Equal("Foundation", w3.GetProperty("sharedConsumedCodeOwner").GetString());
        Assert.False(w3.GetProperty("sharedConsumedCodeRegisteredByCart").GetBoolean());
        Assert.Equal("ZERO", w3.GetProperty("duplicateErrorDescriptorState").GetString());

        // The reconciliation checkpoint record must exist with the same truth.
        var r1 = root.GetProperty("cartModuleAmsc001W3R1");
        Assert.Equal("CART_AMSC_001_CERTIFICATION_TRUTH_RECONCILED", r1.GetProperty("state").GetString());
        Assert.Equal("VERIFIED", r1.GetProperty("w1BehaviorRepairState").GetString());
        Assert.Equal("BOUNDED_DEFECT_REPAIR_RECORDED", r1.GetProperty("behaviorChangeTruthState").GetString());
        Assert.Equal("BOUNDED_500_TO_400_409_503_RECORDED", r1.GetProperty("statusCodeTruthState").GetString());
        Assert.Equal("TWENTY_SEVEN", r1.GetProperty("declaredOrConsumedCodeCountState").GetString());
        Assert.Equal("TWENTY_FIVE", r1.GetProperty("cartOwnedDescriptorCountState").GetString());
        Assert.Equal("ZERO", r1.GetProperty("duplicateDescriptorOwnershipState").GetString());
        Assert.Equal("ZERO", r1.GetProperty("productionCodeChangeState").GetString());
        Assert.Equal("ZERO", r1.GetProperty("schemaMigrationChangeState").GetString());
        Assert.Equal("ZERO", r1.GetProperty("resourceChangeState").GetString());
        Assert.Equal("DOCUMENTATION_ONLY", r1.GetProperty("manifestStructureChangeState").GetString());
        Assert.Equal(
            "COMPLETE_REFERENCE_PATTERN_ARCH_COMPLETE_002_STRUCTURE_CERTIFIED",
            r1.GetProperty("finalCertificationState").GetString());
        Assert.Equal("NONE", r1.GetProperty("automaticNextImplementationTask").GetString());
        Assert.Equal("USER_REVIEW_CART_AMSC_001_W3_R1", r1.GetProperty("workflowStop").GetString());
    }

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
