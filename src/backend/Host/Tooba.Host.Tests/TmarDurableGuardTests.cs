using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// Durable TMAR locks: FE freeze, baseline integrity, folder ownership cross-check.
/// </summary>
public sealed class TmarDurableGuardTests
{
    [Fact]
    public void Backend_only_execution_mode_is_canonical_and_frontend_frozen()
    {
        var root = FindRepoRoot();
        var modePath = Path.Combine(root, "docs", "architecture", "tmar-execution-mode.json");
        Assert.True(File.Exists(modePath), modePath);
        using var doc = JsonDocument.Parse(File.ReadAllText(modePath));
        Assert.Equal("BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE", doc.RootElement.GetProperty("mode").GetString());
        Assert.True(doc.RootElement.GetProperty("frontendFreeze").GetBoolean());

        var locks = File.ReadAllText(Path.Combine(root, "docs", "architecture", "TMAR-architecture-locks.md"));
        foreach (var id in new[]
                 {
                     "ARCH-FE-FREEZE-001",
                     "ARCH-FOLDER-OWNERSHIP-001",
                     "ARCH-RECOVERY-001",
                     "ARCH-USERWORK-001",
                     "ARCH-BASELINE-001",
                     "ARCH-NOWORKAROUND-001",
                     "ARCH-DATA-001",
                 })
        {
            Assert.Contains("## " + id, locks, StringComparison.Ordinal);
        }

        var master = File.ReadAllText(Path.Combine(root, "docs", "architecture", "TOOBA-TMAR-MASTER-RECOVERY.md"));
        var bootstrap = File.ReadAllText(Path.Combine(root, "docs", "architecture", "TOOBA-ARCHITECT-BOOTSTRAP.md"));
        Assert.Contains("TMAR-Execution-Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE", master, StringComparison.Ordinal);
        Assert.Contains("TMAR-Execution-Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE", bootstrap, StringComparison.Ordinal);
    }

    [Fact]
    public void Backend_only_mode_rejects_production_frontend_diff_vs_origin_main()
    {
        var root = FindRepoRoot();
        var modePath = Path.Combine(root, "docs", "architecture", "tmar-execution-mode.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(modePath));
        if (doc.RootElement.GetProperty("mode").GetString() != "BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE")
        {
            return; // deliberate release
        }

        var diff = RunGit(root, "diff", "--name-only", "origin/main", "--", "src/frontend/");
        var staged = RunGit(root, "diff", "--cached", "--name-only", "--", "src/frontend/");
        var unstaged = RunGit(root, "diff", "--name-only", "--", "src/frontend/");
        var dirty = diff.Concat(staged).Concat(unstaged)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(p => IsProductionFrontendPath(p))
            .ToArray();
        Assert.True(
            dirty.Length == 0,
            "ARCH-FE-FREEZE-001 frontend production changes while BACKEND_ONLY: " + string.Join("; ", dirty));
    }

    [Fact]
    public void Architecture_baselines_have_no_wildcard_suppression()
    {
        var root = FindRepoRoot();
        var baselineDir = Path.Combine(root, "src", "backend", "Host", "Tooba.Host.Tests", "Baselines");
        foreach (var path in Directory.GetFiles(baselineDir, "*.json"))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            AssertNoWildcardTokens(doc.RootElement, Path.GetFileName(path));
        }
    }

    [Fact]
    public void Protected_user_work_ancestor_18ca10c9_remains()
    {
        var root = FindRepoRoot();
        var psi = new ProcessStartInfo("git", "merge-base --is-ancestor 18ca10c9 HEAD")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var p = Process.Start(psi)!;
        p.WaitForExit();
        Assert.Equal(0, p.ExitCode);
    }

    [Fact]
    public void Recovery_current_state_is_fresh_and_machine_readable()
    {
        var root = FindRepoRoot();
        var statePath = Path.Combine(root, "docs", "architecture", "tmar-current-state.json");
        Assert.True(File.Exists(statePath), statePath);
        using var doc = JsonDocument.Parse(File.ReadAllText(statePath));
        var rootEl = doc.RootElement;
        Assert.Equal("BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE", rootEl.GetProperty("executionMode").GetString());
        Assert.Equal("COMPLETE", rootEl.GetProperty("goldenWaveState").GetString());
        Assert.Equal("USER_ACCEPTED", rootEl.GetProperty("goldenWaveUserReview").GetString());
        Assert.Equal("TB-TMAR-GOLDEN-WAVE-FINAL-CLOSURE-001", rootEl.GetProperty("goldenWaveClosedBy").GetString());
        Assert.False(string.IsNullOrWhiteSpace(rootEl.GetProperty("goldenWaveClosedCommit").GetString()));
        Assert.Equal("TB-TMAR-PAYMENT-ARCH-COMPLETE-002-STRUCTURE-001", rootEl.GetProperty("nextTask").GetString());
        Assert.Equal("NEXT_TMAR_WAVE_AFTER_PAYMENT_HOST_RESIDUE_REPAIR", rootEl.GetProperty("nextTaskGate").GetString());
        Assert.Equal("PAUSED_AT_SAFE_W5_CHECKPOINT", rootEl.GetProperty("checkoutState").GetString());
        Assert.True(rootEl.GetProperty("frontendFrozen").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", rootEl.GetProperty("locksVersion").GetString());
        Assert.Equal(
            "COMPLETE_REFERENCE_PATTERN_REQUIRES_ENDPOINTS_CQRS_RESULT_CONTRACTS_VALIDATION_CAPABILITY_STRUCTURE_GUARDS_SOT",
            rootEl.GetProperty("definitionMarker").GetString());
        var structureLock = rootEl.GetProperty("structureLock");
        Assert.Equal("ARCH-COMPLETE-002", structureLock.GetProperty("version").GetString());
        Assert.Equal(
            new[] { "Cart", "Offer", "Order", "StoreContext" },
            structureLock.GetProperty("certifiedModules").EnumerateArray()
                .Select(x => x.GetString()!)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());
        Assert.Contains(
            "APPLICATION_CAPABILITY_FOLDERS",
            structureLock.GetProperty("rules").EnumerateArray().Select(x => x.GetString()!).ToArray());
        Assert.Contains(
            "NO_NAMESPACE_ALIAS_WORKAROUND",
            structureLock.GetProperty("rules").EnumerateArray().Select(x => x.GetString()!).ToArray());

        // ARCH-COMPLETE-002 SoT coherence: every certified module must have a matching manifest entry,
        // and Cart's certification line must be consistent across SoT locations (no contradiction recurrence).
        var structureManifestPath = Path.Combine(root, "docs", "architecture", "tmar-module-structure-manifests.json");
        using var structureManifestDoc = JsonDocument.Parse(File.ReadAllText(structureManifestPath));
        var structureCertifiedByModule = structureManifestDoc.RootElement.GetProperty("modules").EnumerateArray()
            .ToDictionary(
                m => m.GetProperty("module").GetString()!,
                m => m.GetProperty("structureCertified").GetBoolean(),
                StringComparer.Ordinal);
        var structureUncertified = structureManifestDoc.RootElement.GetProperty("uncertifiedHttpOwningModules")
            .EnumerateArray().Select(x => x.GetString()!).ToArray();

        var structureCertified = structureLock.GetProperty("certifiedModules").EnumerateArray()
            .Select(x => x.GetString()!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        foreach (var module in structureCertified)
        {
            Assert.True(
                structureCertifiedByModule.TryGetValue(module, out var certifiedInManifest) && certifiedInManifest,
                $"structureLock.certifiedModules lists {module} but the manifest does not declare structureCertified=true");
            Assert.DoesNotContain(module, structureUncertified, StringComparer.Ordinal);
        }

        Assert.Contains("Order", structureCertified);
        Assert.Contains("Cart", structureCertified);
        Assert.Contains("Offer", structureCertified);
        Assert.True(rootEl.GetProperty("hostCartBoundary").GetProperty("structureCertifiedUnderArchComplete002").GetBoolean());

        var offerStructure = rootEl.GetProperty("offerArchComplete002Structure");
        Assert.True(offerStructure.GetProperty("structureCertifiedUnderArchComplete002").GetBoolean());
        Assert.Equal("EXACT", offerStructure.GetProperty("pathNamespace").GetString());
        Assert.Equal("5_REQUIRED_PRESENT_1_NO_VALIDATOR_REQUIRED", offerStructure.GetProperty("validatorCoverage").GetString());
        Assert.Equal("OFFER_CONTRACT_PORT_APPLICATION_POLICY", offerStructure.GetProperty("selectionOwner").GetString());
        Assert.Equal("BUSINESS_RESIDUE_REMOVED_THIN_SECURITY_ADAPTER_ONLY", offerStructure.GetProperty("hostResidue").GetString());

        var offerEntry = rootEl.GetProperty("completeReferenceModules").EnumerateArray()
            .Single(x => x.GetProperty("module").GetString() == "Offer");
        Assert.Equal("TB-TMAR-OFFER-ARCH-COMPLETE-002-STRUCTURE-001", offerEntry.GetProperty("lastStructureCertificationTask").GetString());
        Assert.True(offerEntry.GetProperty("structureCertifiedUnderArchComplete002").GetBoolean());

        var cartEntry = rootEl.GetProperty("completeReferenceModules").EnumerateArray()
            .Single(x => x.GetProperty("module").GetString() == "Cart");
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", cartEntry.GetProperty("state").GetString());
        Assert.StartsWith(
            "TB-TMAR-CART-ARCH-COMPLETE-002-REVERIFY-001",
            cartEntry.GetProperty("lastAcceptedTask").GetString(),
            StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(cartEntry.GetProperty("lastAcceptedCommit").GetString()));
        Assert.Equal("TB-TMAR-PAYMENT-ARCH-COMPLETE-002-AUDIT-001", rootEl.GetProperty("lastAcceptedTask").GetString());
        Assert.Equal("dcb8b416b19d8f9db90e8162754723e4cfbbfb14", rootEl.GetProperty("lastAcceptedCommit").GetString());

        var paymentHostResidue = rootEl.GetProperty("paymentHostResidueRepair");
        Assert.Equal("TB-TMAR-PAYMENT-HOST-RESIDUE-REPAIR-001", paymentHostResidue.GetProperty("task").GetString());
        Assert.Equal("PAYMENT_RUNTIME_RESIDUE_REMOVED_SECURITY_ADAPTERS_ONLY", paymentHostResidue.GetProperty("state").GetString());
        Assert.Equal("PAYMENT_INFRASTRUCTURE_WORKER_AND_OPTIONS", paymentHostResidue.GetProperty("reconciliationOwnership").GetString());
        Assert.Equal("PAYMENT_ENDPOINTS", paymentHostResidue.GetProperty("adminGridPolicyOwnership").GetString());
        Assert.Equal("ZERO", paymentHostResidue.GetProperty("paymentToHostDependency").GetString());
        Assert.False(paymentHostResidue.GetProperty("productionCodeChanged").GetBoolean() == false);

        var paymentAudit = rootEl.GetProperty("paymentArchComplete002Audit");
        Assert.Equal("TB-TMAR-PAYMENT-ARCH-COMPLETE-002-AUDIT-001", paymentAudit.GetProperty("task").GetString());
        Assert.False(paymentAudit.GetProperty("productionCodeChanged").GetBoolean());
        Assert.False(paymentAudit.GetProperty("structureCertifiedUnderArchComplete002").GetBoolean());
        Assert.Equal("PAYMENT_INFRASTRUCTURE_OWNS_WORKER_AND_OPTIONS", paymentAudit.GetProperty("reconciliationWorkerOwnership").GetString());
        Assert.Equal(16, paymentAudit.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal(1, paymentAudit.GetProperty("workerReachableRequests").GetInt32());
        Assert.Equal(15, paymentAudit.GetProperty("validatorsMissing").GetInt32());

        var paymentEntry = rootEl.GetProperty("completeReferenceModules").EnumerateArray()
            .Single(x => x.GetProperty("module").GetString() == "Payment");
        Assert.Equal("TB-TMAR-PAYMENT-GOLDEN-001-R2", paymentEntry.GetProperty("lastAcceptedTask").GetString());
        Assert.False(paymentEntry.GetProperty("structureCertifiedUnderArchComplete002").GetBoolean());
        Assert.Equal(
            "TB-TMAR-CART-POSTCERT-SEMANTIC-HOST-CLOSURE-001-R1",
            cartEntry.GetProperty("lastPostCertificationRepairTask").GetString());
        Assert.Equal("CART_OWNED_ICartExpiryReconciler", rootEl.GetProperty("hostCartBoundary").GetProperty("cartExpiryOwnership").GetString());
        Assert.Equal(
            "REMOVED_HOST_OWNS_ZERO_CART_IMPLEMENTATION",
            rootEl.GetProperty("hostCartBoundary").GetProperty("cartExpiryHostWorker").GetString());
        Assert.False(string.IsNullOrWhiteSpace(rootEl.GetProperty("lastAcceptedCommit").GetString()));
        Assert.DoesNotContain("PENDING_FINAL_CLOSURE_COMMIT", rootEl.GetProperty("lastAcceptedCommit").GetString(), StringComparison.Ordinal);
        Assert.DoesNotContain("PLACEHOLDER_STAMP_AFTER_COMMIT", rootEl.GetProperty("lastAcceptedCommit").GetString(), StringComparison.Ordinal);

        var completeModules = rootEl.GetProperty("completeReferenceModules").EnumerateArray().ToArray();
        Assert.Equal(12, completeModules.Length);
        var complete = completeModules
            .Select(x => x.GetProperty("module").GetString()!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            new[] { "Cart", "Fulfillment", "Inventory", "Notification", "Offer", "Order", "Payment", "Promotion", "Returns", "Settlement", "Support", "Wallet" },
            complete);

        Assert.Empty(rootEl.GetProperty("reopenedModules").EnumerateArray());
        Assert.Empty(rootEl.GetProperty("internalApplicabilityReviewModules").EnumerateArray());

        var httpModules = completeModules
            .Where(x => x.GetProperty("module").GetString() != "Inventory")
            .ToArray();
        Assert.Equal(11, httpModules.Length);
        Assert.All(httpModules, module =>
        {
            Assert.Equal("COMPLETE_REFERENCE_PATTERN", module.GetProperty("state").GetString());
            Assert.Equal("HTTP_OWNING", module.GetProperty("httpApplicability").GetString());
            Assert.Equal("MODULE_ENDPOINTS", module.GetProperty("endpointOwnership").GetString());
            Assert.Equal("MEDIATR_12_5", module.GetProperty("cqrs").GetString());
        });

        var inventory = completeModules
            .Single(x => x.GetProperty("module").GetString() == "Inventory");
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", inventory.GetProperty("state").GetString());
        Assert.Equal("INTERNAL_ONLY", inventory.GetProperty("httpApplicability").GetString());
        Assert.Equal("NOT_APPLICABLE", inventory.GetProperty("endpointOwnership").GetString());
        Assert.Equal("INTERNAL_USE_CASE_BOUNDARIES", inventory.GetProperty("cqrs").GetString());
        Assert.Equal("2814da32245b25a718aa952ba0e836d7550a3ee0", inventory.GetProperty("lastAcceptedCommit").GetString());

        var order = completeModules.Single(x => x.GetProperty("module").GetString() == "Order");
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", order.GetProperty("state").GetString());
        Assert.Equal("TB-TMAR-ORDER-GOLDEN-001-FINAL-CLOSURE", order.GetProperty("lastAcceptedTask").GetString());
        Assert.False(string.IsNullOrWhiteSpace(order.GetProperty("lastAcceptedCommit").GetString()));
        Assert.DoesNotContain("PENDING_FINAL_CLOSURE_COMMIT", order.GetProperty("lastAcceptedCommit").GetString(), StringComparison.Ordinal);

        Assert.Empty(rootEl.GetProperty("activeModuleRecovery").EnumerateObject());

        var master = File.ReadAllText(Path.Combine(root, "docs", "architecture", "TOOBA-TMAR-MASTER-RECOVERY.md"));
        var bootstrap = File.ReadAllText(Path.Combine(root, "docs", "architecture", "TOOBA-ARCHITECT-BOOTSTRAP.md"));
        Assert.Contains("TB-TMAR-ORDER-GOLDEN-001-R3", master, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-ORDER-GOLDEN-001-R3B", master, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-ORDER-GOLDEN-001-R5", master, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-ORDER-GOLDEN-001-R5-R1", master, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-ORDER-GOLDEN-001-R6", master, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-ORDER-GOLDEN-001-R7", master, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-ORDER-GOLDEN-001-R8", master, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-ORDER-GOLDEN-001-R9", master, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-ORDER-GOLDEN-001-R10", master, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-ORDER-GOLDEN-001-R11", master, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-ORDER-GOLDEN-001-R11-R1", master, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-ORDER-GOLDEN-001-FINAL-CLOSURE", master, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-ORDER-GOLDEN-001-R3", bootstrap, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-ORDER-GOLDEN-001-R3B", bootstrap, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-ORDER-GOLDEN-001-R5", bootstrap, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-ORDER-GOLDEN-001-R5-R1", bootstrap, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-ORDER-GOLDEN-001-R6", bootstrap, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-ORDER-GOLDEN-001-R7", bootstrap, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-ORDER-GOLDEN-001-R8", bootstrap, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-ORDER-GOLDEN-001-R9", bootstrap, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-ORDER-GOLDEN-001-R10", bootstrap, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-ORDER-GOLDEN-001-R11", bootstrap, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-ORDER-GOLDEN-001-R11-R1", bootstrap, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-ORDER-GOLDEN-001-FINAL-CLOSURE", bootstrap, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-LOCK-001", master, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-LOCK-001", bootstrap, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-LOCK-001", master, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-LOCK-001", bootstrap, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-001", master, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-001", bootstrap, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-ORDER-POSTCLOSURE-QUALITY-001", master, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-ORDER-POSTCLOSURE-QUALITY-001", bootstrap, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_REQUIRED_BEFORE_NEXT_TMAR_WAVE", master, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_REQUIRED_BEFORE_NEXT_TMAR_WAVE", bootstrap, StringComparison.Ordinal);
        Assert.Contains("ARCH-COMPLETE-002", master, StringComparison.Ordinal);
        Assert.Contains("ARCH-COMPLETE-002", bootstrap, StringComparison.Ordinal);
        Assert.Contains("COMPLETE_REFERENCE_PATTERN", master, StringComparison.Ordinal);
        Assert.Contains("COMPLETE_REFERENCE_PATTERN", bootstrap, StringComparison.Ordinal);
        const string currentCompleteList = "Cart, Settlement, Fulfillment, Returns, Notification, Support, Wallet, Payment, Promotion, Offer, Order, Inventory";
        Assert.Contains(currentCompleteList, master, StringComparison.Ordinal);
        Assert.Contains(currentCompleteList, bootstrap, StringComparison.Ordinal);
        Assert.Contains("Inventory = INTERNAL_ONLY / NOT_APPLICABLE / INTERNAL_USE_CASE_BOUNDARIES", master, StringComparison.Ordinal);
        Assert.Contains("Inventory = INTERNAL_ONLY / NOT_APPLICABLE / INTERNAL_USE_CASE_BOUNDARIES", bootstrap, StringComparison.Ordinal);

        // Current authority ends at the explicit historical boundary; chronology after it remains legitimate evidence.
        var masterCurrent = CurrentAuthority(master);
        var bootstrapCurrent = CurrentAuthority(bootstrap);
        Assert.Equal(1, CountAuthoritativeCurrentHeadings(masterCurrent));
        Assert.Equal(1, CountAuthoritativeCurrentHeadings(bootstrapCurrent));
        Assert.DoesNotContain("Remaining:", masterCurrent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("NEEDS_APPLICABILITY_REVERIFY", masterCurrent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch(
            new Regex(
                @"COMPLETE_REFERENCE_PATTERN \(internal-only[^\r\n]*\)[\s\S]{0,500}^- Offer\b",
                RegexOptions.IgnoreCase | RegexOptions.Multiline),
            masterCurrent);

        var expectedNextTask = rootEl.GetProperty("nextTask").GetString()!;
        var expectedGate = rootEl.GetProperty("nextTaskGate").GetString()!;
        Assert.Contains(expectedNextTask, masterCurrent, StringComparison.Ordinal);
        Assert.Contains(expectedGate, masterCurrent, StringComparison.Ordinal);
        Assert.Contains(expectedNextTask, bootstrapCurrent, StringComparison.Ordinal);
        Assert.Contains(expectedGate, bootstrapCurrent, StringComparison.Ordinal);
        Assert.Contains("Golden wave: COMPLETE", masterCurrent, StringComparison.Ordinal);
        Assert.Contains("Golden wave = COMPLETE", bootstrapCurrent, StringComparison.Ordinal);

        var endpointGuard = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Host", "Tooba.Host.Tests", "HostModuleEndpointOwnershipTests.cs"));
        var manifest = Regex.Match(
            endpointGuard,
            @"CompleteHttpModules\s*=\s*\[(?<entries>[\s\S]*?)\];",
            RegexOptions.CultureInvariant);
        Assert.True(manifest.Success, "CompleteHttpModules manifest not found");
        var manifestModules = Regex.Matches(manifest.Groups["entries"].Value, @"new\(""(?<module>[^""]+)""")
            .Select(x => x.Groups["module"].Value)
            .ToArray();
        Assert.Equal(11, manifestModules.Length);
        Assert.DoesNotContain("Inventory", manifestModules, StringComparer.Ordinal);
        Assert.Equal(
            httpModules.Select(x => x.GetProperty("module").GetString()!).OrderBy(x => x, StringComparer.Ordinal),
            manifestModules.OrderBy(x => x, StringComparer.Ordinal));

        foreach (var stale in new[] { "TB-TMAR-PAYMENT-GOLDEN", "TB-TMAR-PROMOTION-GOLDEN", "TB-TMAR-OFFER-FINAL", "TB-TMAR-INVENTORY-APPLICABILITY" })
        {
            Assert.DoesNotContain("next task: " + stale, masterCurrent, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("next task: " + stale, bootstrapCurrent, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string CurrentAuthority(string text)
    {
        var historicalBoundary = text.IndexOf("HISTORICAL / SUPERSEDED", StringComparison.Ordinal);
        Assert.True(historicalBoundary >= 0, "explicit HISTORICAL / SUPERSEDED boundary is required");
        return text[..historicalBoundary];
    }

    private static int CountAuthoritativeCurrentHeadings(string current) =>
        Regex.Matches(
                current,
                @"(?im)^(?:#+\s*)?Current[^\r\n]*\(authoritative\)[^\r\n]*\r?$")
            .Count;

    private static bool IsProductionFrontendPath(string relative)
    {
        var n = relative.Replace('\\', '/');
        if (!n.StartsWith("src/frontend/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // allow nothing under production tree while frozen
        return true;
    }

    private static void AssertNoWildcardTokens(JsonElement el, string file)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.String:
                var s = el.GetString() ?? "";
                Assert.False(
                    s is "*" or "**" or "*.*" || s.Contains("/**", StringComparison.Ordinal),
                    $"ARCH-BASELINE-001 wildcard in {file}: {s}");
                break;
            case JsonValueKind.Array:
                foreach (var child in el.EnumerateArray())
                {
                    AssertNoWildcardTokens(child, file);
                }

                break;
            case JsonValueKind.Object:
                foreach (var prop in el.EnumerateObject())
                {
                    AssertNoWildcardTokens(prop.Value, file);
                }

                break;
        }
    }

    private static IEnumerable<string> RunGit(string root, params string[] args)
    {
        var psi = new ProcessStartInfo("git", string.Join(' ', args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a)))
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var p = Process.Start(psi)!;
        var output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        Assert.True(p.ExitCode == 0, p.StandardError.ReadToEnd());
        return output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs", "PROJECT-STATE.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
