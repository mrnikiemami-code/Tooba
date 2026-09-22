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
        Assert.Equal("TB-TMAR-GOLDEN-WAVE-FINAL-CLOSURE-001", rootEl.GetProperty("goldenWaveClosedBy").GetString());
        Assert.False(string.IsNullOrWhiteSpace(rootEl.GetProperty("goldenWaveClosedCommit").GetString()));
        Assert.Equal("USER_REVIEW_GOLDEN_WAVE", rootEl.GetProperty("nextTask").GetString());
        Assert.Equal("USER_REVIEW_REQUIRED_BEFORE_NEXT_TMAR_WAVE", rootEl.GetProperty("nextTaskGate").GetString());
        Assert.Equal("PAUSED_AT_SAFE_W5_CHECKPOINT", rootEl.GetProperty("checkoutState").GetString());
        Assert.True(rootEl.GetProperty("frontendFrozen").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-001", rootEl.GetProperty("locksVersion").GetString());

        var completeModules = rootEl.GetProperty("completeReferenceModules").EnumerateArray().ToArray();
        var complete = completeModules
            .Select(x => x.GetProperty("module").GetString()!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            new[] { "Cart", "Fulfillment", "Inventory", "Notification", "Offer", "Payment", "Promotion", "Returns", "Settlement", "Support", "Wallet" },
            complete);

        Assert.Empty(rootEl.GetProperty("reopenedModules").EnumerateArray());
        Assert.Empty(rootEl.GetProperty("internalApplicabilityReviewModules").EnumerateArray());

        var httpModules = completeModules
            .Where(x => x.GetProperty("module").GetString() != "Inventory")
            .ToArray();
        Assert.Equal(10, httpModules.Length);
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

        var master = File.ReadAllText(Path.Combine(root, "docs", "architecture", "TOOBA-TMAR-MASTER-RECOVERY.md"));
        var bootstrap = File.ReadAllText(Path.Combine(root, "docs", "architecture", "TOOBA-ARCHITECT-BOOTSTRAP.md"));
        Assert.Contains("USER_REVIEW_GOLDEN_WAVE", master, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_GOLDEN_WAVE", bootstrap, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_REQUIRED_BEFORE_NEXT_TMAR_WAVE", master, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_REQUIRED_BEFORE_NEXT_TMAR_WAVE", bootstrap, StringComparison.Ordinal);
        Assert.Contains("ARCH-COMPLETE-001", master, StringComparison.Ordinal);
        Assert.Contains("ARCH-COMPLETE-001", bootstrap, StringComparison.Ordinal);
        const string currentCompleteList = "Cart, Settlement, Fulfillment, Returns, Notification, Support, Wallet, Payment, Promotion, Offer, Inventory";
        Assert.Contains(currentCompleteList, master, StringComparison.Ordinal);
        Assert.Contains(currentCompleteList, bootstrap, StringComparison.Ordinal);
        Assert.Contains("Inventory = INTERNAL_ONLY / NOT_APPLICABLE / INTERNAL_USE_CASE_BOUNDARIES", master, StringComparison.Ordinal);
        Assert.Contains("Inventory = INTERNAL_ONLY / NOT_APPLICABLE / INTERNAL_USE_CASE_BOUNDARIES", bootstrap, StringComparison.Ordinal);

        // Reject only authoritative stale next-task, not historical chronology mentions.
        var masterCurrent = master[..master.IndexOf("HISTORICAL / SUPERSEDED", StringComparison.Ordinal)];
        var bootstrapCurrent = bootstrap[..bootstrap.IndexOf("HISTORICAL / SUPERSEDED", StringComparison.Ordinal)];
        foreach (var stale in new[] { "TB-TMAR-PAYMENT-GOLDEN", "TB-TMAR-PROMOTION-GOLDEN", "TB-TMAR-OFFER-FINAL", "TB-TMAR-INVENTORY-APPLICABILITY" })
        {
            Assert.DoesNotContain("next task: " + stale, masterCurrent, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("next task: " + stale, bootstrapCurrent, StringComparison.OrdinalIgnoreCase);
        }
    }

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
