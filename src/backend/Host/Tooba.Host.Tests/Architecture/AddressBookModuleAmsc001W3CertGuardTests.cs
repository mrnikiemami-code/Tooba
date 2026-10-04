using System.Text.Json;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-ADDRESSBOOK-AMSC-001-W3 (Certify) — durable certification locks.
///
/// Locks the ARCH-COMPLETE-002 certification verdict for the AddressBook module: capability-first
/// structure, canonical result/error mapping, localization + unique descriptor ownership, unchanged
/// schema, module-owned HTTP surface, zero foreign coupling and an honest SoT record. It reads real
/// repository files, so a regression in any of these invariants fails here.
/// </summary>
public sealed class AddressBookModuleAmsc001W3CertGuardTests
{
    private const string ModuleRelative = "src/backend/Modules/AddressBook";

    [Fact]
    public void AddressBook_certification_state_is_recorded_honestly_in_sot()
    {
        using var doc = ReadJson("docs/architecture/tmar-current-state.json");
        var root = doc.RootElement;

        var record = root.GetProperty("addressBookModuleAmsc001W3");
        Assert.Equal("ADDRESSBOOK_AMSC_001_CERTIFIED", record.GetProperty("state").GetString());
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
                     "addressBookModuleAmsc001W0", "addressBookModuleAmsc001W1",
                     "addressBookModuleAmsc001W2", "addressBookModuleAmsc001W3",
                 })
        {
            Assert.True(root.TryGetProperty(key, out _), $"missing SoT record {key}");
        }
    }

    /// <summary>
    /// W3-R1 reconciliation lock: the certification record may never regress to claiming
    /// <c>behaviorChange = NONE</c> / <c>statusCodesChanged = NONE</c> while W1 still records the accepted
    /// bounded expected-failure repair (unexpected 500 -> catalogued 404/400). The W1 record is read from
    /// the same SoT document, so this is a real cross-record consistency lock, not a tautological
    /// self-comparison.
    /// </summary>
    [Fact]
    public void AddressBook_certification_truth_records_the_accepted_w1_bounded_defect_repair()
    {
        using var doc = ReadJson("docs/architecture/tmar-current-state.json");
        var root = doc.RootElement;

        // W1 is the wave that performed the accepted repair and must still record it.
        var w1 = root.GetProperty("addressBookModuleAmsc001W1");
        var w1Behavior = w1.GetProperty("behaviorChange").GetString()!;
        Assert.StartsWith("BOUNDED_DEFECT_REPAIR", w1Behavior, StringComparison.Ordinal);
        Assert.Contains("500", w1Behavior, StringComparison.Ordinal);
        Assert.Contains("404", w1Behavior, StringComparison.Ordinal);
        Assert.Contains("400", w1Behavior, StringComparison.Ordinal);

        // W3 (certification) must not contradict W1 with a NONE behavior claim.
        var w3 = root.GetProperty("addressBookModuleAmsc001W3");
        Assert.Equal(
            "BOUNDED_DEFECT_REPAIR_EXPECTED_FAILURE_MAPPING",
            w3.GetProperty("behaviorChange").GetString());
        Assert.Equal(
            "BOUNDED_EXPECTED_FAILURE_REMAP_500_TO_404_400",
            w3.GetProperty("statusCodesChanged").GetString());

        // The unaffected axes stay NONE, so the bounded claim cannot silently widen.
        Assert.Equal("NONE", w3.GetProperty("schemaChange").GetString());
        Assert.Equal("NONE", w3.GetProperty("routesChanged").GetString());
        Assert.Equal("NONE", w3.GetProperty("errorCodesChanged").GetString());
        Assert.Equal("NONE", w3.GetProperty("dtoShapeChanged").GetString());

        // Certification itself must remain valid and unreconciled-away.
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", w3.GetProperty("verdict").GetString());
        Assert.True(w3.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("W3_R1_BEHAVIOR_TRUTH_EXACT", w3.GetProperty("certificationReconciliation").GetString());
        Assert.Equal("USER_REVIEW_ADDRESSBOOK_AMSC_001_W3_R1", w3.GetProperty("workflowStop").GetString());
        Assert.Equal("NONE", w3.GetProperty("automaticNextImplementationTask").GetString());

        // The reconciliation checkpoint record must exist with the same truth.
        var r1 = root.GetProperty("addressBookModuleAmsc001W3R1");
        Assert.Equal("ADDRESSBOOK_AMSC_001_CERTIFICATION_TRUTH_RECONCILED", r1.GetProperty("state").GetString());
        Assert.Equal("BOUNDED_DEFECT_REPAIR_RECORDED", r1.GetProperty("behaviorChangeTruthState").GetString());
        Assert.Equal("BOUNDED_500_TO_404_400_RECORDED", r1.GetProperty("statusCodeTruthState").GetString());
        Assert.Equal("ELEVEN", r1.GetProperty("addressBookNewCodeCountState").GetString());
        Assert.Equal("TWELVE", r1.GetProperty("addressBookOwnedDescriptorCountState").GetString());
        Assert.Equal("CONSUMED_NOT_OWNED", r1.GetProperty("sharedSessionCodeState").GetString());
        Assert.Equal("ZERO", r1.GetProperty("duplicateDescriptorOwnershipState").GetString());
        Assert.Equal("ZERO", r1.GetProperty("productionCodeChangeState").GetString());
        Assert.Equal("ZERO", r1.GetProperty("schemaMigrationChangeState").GetString());
        Assert.Equal("DOCUMENTATION_ONLY", r1.GetProperty("manifestStructureChangeState").GetString());
        Assert.Equal(
            "COMPLETE_REFERENCE_PATTERN_ARCH_COMPLETE_002_STRUCTURE_CERTIFIED",
            r1.GetProperty("finalCertificationState").GetString());
        Assert.Equal("NONE", r1.GetProperty("automaticNextImplementationTask").GetString());
    }

    [Fact]
    public void AddressBook_manifest_entry_is_single_and_disk_reconciled()
    {
        using var doc = ReadJson("docs/architecture/tmar-module-structure-manifests.json");
        var entries = doc.RootElement.GetProperty("modules").EnumerateArray()
            .Where(m => m.GetProperty("module").GetString() == "AddressBook")
            .ToArray();

        Assert.Single(entries);
        Assert.True(entries[0].GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", entries[0].GetProperty("lockVersion").GetString());

        // The Application justification must describe the capability-first truth, never the removed
        // per-use-case leaf shape it used to document as accepted.
        var application = entries[0].GetProperty("projects").EnumerateArray()
            .Single(p => p.GetProperty("projectName").GetString() == "Tooba.AddressBook.Application");
        var justification = application.GetProperty("rootAllowlistJustification").GetString()!;
        Assert.Contains("Addresses", justification, StringComparison.Ordinal);
        Assert.Contains("AMSC-001 W2 removed", justification, StringComparison.Ordinal);
        Assert.Contains("single-file leaf folders", justification, StringComparison.Ordinal);
        Assert.DoesNotContain("carry every type", justification, StringComparison.Ordinal);

        // Manifest projects must match disk exactly.
        var moduleRoot = ModuleRoot();
        var onDisk = Directory.GetDirectories(moduleRoot)
            .Select(d => Path.GetFileName(d)!)
            .Where(n => n.StartsWith("Tooba.AddressBook.", StringComparison.Ordinal))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        var manifested = entries[0].GetProperty("projects").EnumerateArray()
            .Select(p => p.GetProperty("projectName").GetString()!)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(manifested, onDisk);
    }

    [Fact]
    public void AddressBook_owns_exactly_one_error_descriptor_per_owned_code()
    {
        var contracts = File.ReadAllText(Path.Combine(
            ModuleRoot(), "Tooba.AddressBook.Contracts", "Errors", "AddressBookErrorCodes.cs"));
        var constants = System.Text.RegularExpressions.Regex
            .Matches(contracts, @"public const string (?<name>\w+) = ""(?<code>[^""]+)"";")
            .ToDictionary(m => m.Groups["name"].Value, m => m.Groups["code"].Value, StringComparer.Ordinal);

        Assert.Equal(13, constants.Count);
        Assert.Equal(constants.Count, constants.Values.Distinct(StringComparer.Ordinal).Count());

        var contributor = File.ReadAllText(Path.Combine(
            ModuleRoot(), "Tooba.AddressBook.Endpoints", "Errors", "AddressBookErrorCatalogContributor.cs"));

        // customer.session.required is a shared cross-cutting code owned by the Foundation
        // contributor; AddressBook consumes it without re-registering a descriptor.
        var locallyOwned = constants.Where(kv => kv.Key != "SessionRequired").ToArray();
        foreach (var (name, _) in locallyOwned)
        {
            Assert.Contains($"AddressBookErrorCodes.{name}", contributor, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("AddressBookErrorCodes.SessionRequired", contributor, StringComparison.Ordinal);
        Assert.DoesNotContain("Contains(", contributor, StringComparison.Ordinal);
        Assert.DoesNotContain("StartsWith(", contributor, StringComparison.Ordinal);

        // No other contributor may register an AddressBook-owned code.
        var backendRoot = Path.Combine(RepoRoot(), "src", "backend");
        foreach (var file in Directory.GetFiles(backendRoot, "*ErrorCatalogContributor.cs", SearchOption.AllDirectories))
        {
            if (file.EndsWith("AddressBookErrorCatalogContributor.cs", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            foreach (var code in constants.Values)
            {
                Assert.DoesNotContain($"\"{code}\"", text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void AddressBook_error_codes_resolve_to_localized_resources_in_both_cultures()
    {
        var constants = System.Text.RegularExpressions.Regex
            .Matches(
                File.ReadAllText(Path.Combine(
                    ModuleRoot(), "Tooba.AddressBook.Contracts", "Errors", "AddressBookErrorCodes.cs")),
                @"public const string \w+ = ""(?<code>[^""]+)"";")
            .Select(m => m.Groups["code"].Value)
            .Where(code => !string.Equals(code, "customer.session.required", StringComparison.Ordinal))
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToArray();

        foreach (var culture in new[] { "AddressBookErrors.resx", "AddressBookErrors.fa.resx" })
        {
            var resx = File.ReadAllText(Path.Combine(
                ModuleRoot(), "Tooba.AddressBook.Endpoints", "Resources", culture));
            foreach (var code in constants)
            {
                Assert.Contains($"name=\"{code}\"", resx, StringComparison.Ordinal);
            }
        }

        // The canonical resource set seam must be wired for the module.
        var module = File.ReadAllText(Path.Combine(
            ModuleRoot(), "Tooba.AddressBook.Endpoints", "AddressBookEndpointModule.cs"));
        Assert.Contains("IErrorResourceSet, AddressBookErrorResourceSet", module, StringComparison.Ordinal);
        Assert.Contains("IErrorCatalogContributor, AddressBookErrorCatalogContributor", module, StringComparison.Ordinal);
    }

    [Fact]
    public void AddressBook_endpoints_use_only_the_canonical_result_factory()
    {
        var endpoints = Path.Combine(ModuleRoot(), "Tooba.AddressBook.Endpoints");

        foreach (var file in Directory.GetFiles(endpoints, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            var relative = file[endpoints.Length..];

            Assert.DoesNotContain("Results.Json", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.NoContent", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.BadRequest", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.Problem", text, StringComparison.Ordinal);
            Assert.DoesNotContain("new ProblemDetails", text, StringComparison.Ordinal);
            Assert.DoesNotContain("StatusCodes.Status201Created", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Accept-Language", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.AddressBook.Domain", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.AddressBook.Infrastructure", text, StringComparison.Ordinal);

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
    public void AddressBook_has_no_raw_error_code_literals_ad_hoc_logging_or_typed_fault_residue()
    {
        var root = ModuleRoot();
        var offenders = new List<string>();

        foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var relative = file[root.Length..];
            if (relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || relative.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || relative.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || relative.EndsWith("ErrorCodes.cs", StringComparison.Ordinal)
                || relative.EndsWith("ErrorCatalogContributor.cs", StringComparison.Ordinal)
                || relative.Contains("Errors.resx", StringComparison.Ordinal)
                // Declared transport-validation code owner: AddressBookFluentRules.cs hosts
                // AddressBookValidationCodes, exactly as AccessControlValidationCodes.cs and
                // ContentValidationCodes.cs do in the already-certified sibling modules.
                || relative.EndsWith("AddressBookFluentRules.cs", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"""customer\.address\.[a-z_]+""")
                || System.Text.RegularExpressions.Regex.IsMatch(text, @"""customer\.session\.[a-z_]+""")
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

        // Typed faults (not raw InvalidOperationException) are mandatory on the two files that
        // produce user-facing failures. The only remaining InvalidOperationException in the module
        // is the unreachable IOutboxModuleRegistration guard in AddressBookOutboxRegistration.cs,
        // which carries no user-facing text and is recorded as residual watch R1.
        foreach (var relative in new[]
                 {
                     Path.Combine("Tooba.AddressBook.Domain", "Aggregates", "CustomerAddress.cs"),
                     Path.Combine("Tooba.AddressBook.Infrastructure", "Adapters", "AddressBookDirectory.cs"),
                 })
        {
            var text = File.ReadAllText(Path.Combine(root, relative));
            Assert.Contains("SemanticException", text, StringComparison.Ordinal);
            Assert.DoesNotContain("InvalidOperationException", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void AddressBook_has_no_foreign_module_project_or_namespace_dependency()
    {
        var root = ModuleRoot();
        var forbidden = new[]
        {
            "Tooba.Order.Application", "Tooba.Order.Domain", "Tooba.Order.Infrastructure",
            "Tooba.CustomerProfile.Application", "Tooba.CustomerProfile.Domain", "Tooba.CustomerProfile.Infrastructure",
            "Tooba.Catalog.Application", "Tooba.Catalog.Domain", "Tooba.Catalog.Infrastructure",
            "Tooba.Party.Application", "Tooba.Party.Domain", "Tooba.Party.Infrastructure",
            "Tooba.Identity.Application", "Tooba.Identity.Domain", "Tooba.Identity.Infrastructure",
            "Tooba.Media.Application", "Tooba.Media.Domain", "Tooba.Media.Infrastructure",
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
                || relative.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            foreach (var needle in forbidden)
            {
                Assert.DoesNotContain($"using {needle}", text, StringComparison.Ordinal);
            }
        }

        // The only legal foreign edge is the Order.Contracts boundary constant.
        var actorResolver = File.ReadAllText(Path.Combine(
            root, "Tooba.AddressBook.Endpoints", "Customer", "AddressBookCustomerActorResolver.cs"));
        Assert.Contains("using Tooba.Order.Contracts.Fulfillment;", actorResolver, StringComparison.Ordinal);
    }

    [Fact]
    public void AddressBook_owns_its_http_surface_with_zero_host_http_ownership()
    {
        var endpointsRoot = Path.Combine(ModuleRoot(), "Tooba.AddressBook.Endpoints");

        Assert.True(File.Exists(Path.Combine(endpointsRoot, "AddressBookEndpointModule.cs")));
        Assert.False(File.Exists(Path.Combine(endpointsRoot, "AddressBookCustomerReadEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(endpointsRoot, "AddressBookCustomerWriteEndpoints.cs")));
        Assert.True(Directory.Exists(Path.Combine(endpointsRoot, "Customer")));
        Assert.True(Directory.Exists(Path.Combine(endpointsRoot, "Errors")));
        Assert.True(Directory.Exists(Path.Combine(endpointsRoot, "Resources")));

        // Exactly 6 module-owned routes.
        var routes = Directory.GetFiles(endpointsRoot, "*.cs", SearchOption.AllDirectories)
            .Sum(f => System.Text.RegularExpressions.Regex.Matches(
                File.ReadAllText(f), @"group\.Map(Get|Post|Put|Delete|Patch)\(").Count);
        Assert.Equal(6, routes);

        // The Host HTTP surface for AddressBook must never be resurrected.
        Assert.False(Directory.Exists(Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host", "AddressBook")));
    }

    [Fact]
    public void AddressBook_schema_and_migrations_are_unchanged()
    {
        var migrations = Path.Combine(
            ModuleRoot(), "Tooba.AddressBook.Infrastructure", "Persistence", "Migrations");
        var files = Directory.GetFiles(migrations, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(f => Path.GetFileName(f)!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "20260825171858_InitialAddressBook.Designer.cs",
                "20260825171858_InitialAddressBook.cs",
                "20260913180000_AddRecipientNameParts.cs",
                "AddressBookDbContextModelSnapshot.cs",
            ],
            files);
    }

    [Fact]
    public void AddressBook_application_is_capability_first_with_no_single_file_use_case_leaves()
    {
        var appRoot = Path.Combine(ModuleRoot(), "Tooba.AddressBook.Application");

        foreach (var axis in new[] { "Commands", "Queries" })
        {
            Assert.False(
                Directory.Exists(Path.Combine(appRoot, axis)),
                $"Application/{axis}/ is a technical-axis-first top-level folder");
        }

        Assert.True(Directory.Exists(Path.Combine(appRoot, "Addresses", "Commands")));
        Assert.True(Directory.Exists(Path.Combine(appRoot, "Addresses", "Queries")));
        Assert.True(Directory.Exists(Path.Combine(appRoot, "Addresses", "Validators")));

        // The 11 legacy use-case leaf folders must stay gone.
        foreach (var legacy in new[]
                 {
                     "Commands/CreateCustomerAddress", "Commands/DeleteCustomerAddress",
                     "Commands/SetDefaultCustomerAddress", "Commands/UpdateCustomerAddress",
                     "Queries/GetCustomerAddress", "Queries/ListCustomerAddresses",
                     "Validators/CreateCustomerAddress", "Validators/DeleteCustomerAddress",
                     "Validators/GetCustomerAddress", "Validators/SetDefaultCustomerAddress",
                     "Validators/UpdateCustomerAddress",
                 })
        {
            Assert.False(Directory.Exists(Path.Combine(appRoot, legacy)), $"legacy leaf folder {legacy} resurrected");
        }
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
