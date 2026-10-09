using System.Text.Json;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-STORY-AMSC-001-W3-R1 — durable recovery/reconciliation lock: the Story AMSC wave lineage is
/// fully reconciled with real SHAs (no self-referential placeholder survives), the additive R1 record and
/// Master Recovery checkpoint exist, the current certification truth is preserved unchanged, the
/// historical AMC-001 lineage is not rewritten, and the repository-global Host root checkpoint is intact.
/// </summary>
public sealed class StoryModuleAmsc001W3R1RecoveryGuardTests
{
    private const string W0Commit = "0c73390a3211e0ee9057e9234d62d3e4f14b5e4e";
    private const string W1Commit = "2a09e7bb7f1ab687435007951160b4bcfefb4c18";
    private const string W2Commit = "4cd9a6cc543ccd307d775dfe703459de7b12c95d";
    private const string W3Commit = "39ab324e9c57e342516202bdd8df95953dda6439";

    /// <summary>The W3 commit placeholder is reconciled to the real SHA and the chain is fully chained.</summary>
    [Fact]
    public void Story_amsc_lineage_is_fully_reconciled_with_real_shas()
    {
        var root = Repo();
        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));

        var w3 = sot.RootElement.GetProperty("storyAmsc001W3");
        Assert.Equal(W3Commit, w3.GetProperty("commit").GetString());
        Assert.DoesNotContain("PENDING", w3.GetProperty("commit").GetString()!, StringComparison.Ordinal);
        Assert.Equal(W2Commit, w3.GetProperty("startingHead").GetString());

        var r1 = sot.RootElement.GetProperty("storyAmsc001W3R1");
        Assert.Equal("TB-TMAR-STORY-AMSC-001-W3-R1", r1.GetProperty("task").GetString());
        Assert.Equal("TB-TMAR-STORY-AMSC-001-W3", r1.GetProperty("parentTask").GetString());
        Assert.Equal("RECOVERY_SOT_RECONCILIATION_ONLY", r1.GetProperty("mode").GetString());
        Assert.Equal(W3Commit, r1.GetProperty("startingHead").GetString());
        Assert.Equal("STORY_AMSC_001_RECOVERY_RECONCILED", r1.GetProperty("state").GetString());
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", r1.GetProperty("verdict").GetString());
        Assert.Equal("ARCH-COMPLETE-002", r1.GetProperty("lockVersion").GetString());
        Assert.True(r1.GetProperty("structureCertified").GetBoolean());
        Assert.False(r1.GetProperty("productionCodeChanged").GetBoolean());
        Assert.Equal("RECOVERY_SOT_EVIDENCE_ONLY", r1.GetProperty("productionScopeState").GetString());
        Assert.Equal(W3Commit, r1.GetProperty("certifiedCommit").GetString());
        Assert.Equal("PLACEHOLDER_THIS_COMMIT", r1.GetProperty("masterRecoveryW3ShaBefore").GetString());
        Assert.Equal("RECORDED_39AB324E", r1.GetProperty("masterRecoveryW3ShaState").GetString());
        Assert.Equal("RECONCILED", r1.GetProperty("masterRecoveryState").GetString());

        var lineage = r1.GetProperty("acceptedLineage");
        Assert.Equal("0c73390a", lineage.GetProperty("w0").GetString());
        Assert.Equal("2a09e7bb", lineage.GetProperty("w1").GetString());
        Assert.Equal("4cd9a6cc", lineage.GetProperty("w2").GetString());
        Assert.Equal("39ab324e", lineage.GetProperty("w3").GetString());

        var full = r1.GetProperty("acceptedLineageFull");
        Assert.Equal(W0Commit, full.GetProperty("w0").GetString());
        Assert.Equal(W1Commit, full.GetProperty("w1").GetString());
        Assert.Equal(W2Commit, full.GetProperty("w2").GetString());
        Assert.Equal(W3Commit, full.GetProperty("w3").GetString());

        Assert.Equal("NONE", r1.GetProperty("guardsWeakened").GetString());
        Assert.Equal(0, r1.GetProperty("guardsWeakenedCount").GetInt32());
        Assert.Equal("NONE", r1.GetProperty("baselinesWidened").GetString());
        Assert.Equal("PASS", r1.GetProperty("jsonParseState").GetString());
        Assert.Contains("green at the untouched W2 HEAD",
            r1.GetProperty("gateLiteralRepair").GetString()!, StringComparison.Ordinal);
        Assert.Equal("USER_REVIEW_STORY_AMSC_001_W3_R1", r1.GetProperty("workflowStop").GetString());
        Assert.Equal("NONE", r1.GetProperty("automaticNextImplementationTask").GetString());
        Assert.Equal("REPORTED_VIA_BRIDGE_RESULT_ONLY_NO_SELF_REFERENTIAL_PLACEHOLDER",
            r1.GetProperty("commitState").GetString());
        Assert.False(r1.TryGetProperty("commit", out _), "the R1 block must not carry a self-referential commit placeholder");

        // No wave-own self-referential placeholder survives on the Story AMSC line.
        var raw = File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"));
        Assert.DoesNotContain("\"PENDING_W3_COMMIT\"", raw, StringComparison.Ordinal);
        Assert.DoesNotContain("\"PENDING_W3R1_COMMIT\"", raw, StringComparison.Ordinal);

        // The recorded SHAs are real ancestors of the current history.
        foreach (var commit in new[] { W0Commit, W1Commit, W2Commit, W3Commit })
        {
            Assert.True(IsAncestor(root, commit), $"{commit} is not in the current history");
        }
    }

    /// <summary>The certification truth is preserved unchanged by the recovery wave.</summary>
    [Fact]
    public void Story_w3_certification_truth_is_preserved()
    {
        var root = Repo();
        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));

        var w3 = sot.RootElement.GetProperty("storyAmsc001W3");
        Assert.Equal("STORY_AMSC_001_CERTIFIED", w3.GetProperty("state").GetString());
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", w3.GetProperty("verdict").GetString());
        Assert.Equal("ARCH-COMPLETE-002", w3.GetProperty("lockVersion").GetString());
        Assert.Equal("CERTIFIED", w3.GetProperty("structureState").GetString());
        Assert.True(w3.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("HTTP_OWNING", w3.GetProperty("httpApplicability").GetString());
        Assert.Equal("MODULE_OWNED", w3.GetProperty("endpointOwnershipState").GetString());
        Assert.Equal(0, w3.GetProperty("hostOwnedRouteCount").GetInt32());
        Assert.Equal(25, w3.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal(0, w3.GetProperty("unmappedEndpointReachableRequests").GetInt32());
        Assert.Equal("ZERO", w3.GetProperty("foreignModuleLayerCoupling").GetString());
        Assert.Equal("NONE", w3.GetProperty("crossModuleJoinState").GetString());
        Assert.True(w3.GetProperty("microserviceExtractable").GetBoolean());
        Assert.Equal("ZERO", w3.GetProperty("blockingResidualDebt").GetString());

        // The W3 self-description truth repair landed and did not weaken any guard.
        Assert.Equal("StoryModuleAmsc001W3CertGuardTests (8 facts)", w3.GetProperty("guardsAdded").GetString());
        Assert.Contains("StoryModuleAmsc001W3CertGuardTests 8/8",
            w3.GetProperty("focusedValidation").GetString()!, StringComparison.Ordinal);

        // Promotion is intact and unique.
        var certified = sot.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Equal(1, certified.Count(x => x == "Story"));
        Assert.Equal(29, certified.Length);
    }

    /// <summary>The historical AMC-001 Story lineage is preserved, not rewritten, and the global lock is intact.</summary>
    [Fact]
    public void Historical_amc_lineage_and_global_host_checkpoint_are_preserved()
    {
        var root = Repo();
        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));

        foreach (var key in new[]
                 {
                     "storyModuleAmc001", "storyModuleAmc001W1", "storyModuleAmc001W2Cert",
                     "storyModuleAmc001W3", "storyModuleAmc001W4", "storyModuleAmc001W5",
                     "storyModuleAmc001W6Cert",
                 })
        {
            Assert.True(sot.RootElement.TryGetProperty(key, out _), $"historical AMC record {key} is missing");
        }

        Assert.True(sot.RootElement.GetProperty("storyModuleAmc001W6Cert")
            .GetProperty("structureCertified").GetBoolean());
        Assert.Equal("HISTORICAL_SUPERSEDED_NOT_REWRITTEN",
            sot.RootElement.GetProperty("storyAmsc001W3R1").GetProperty("priorAmc001CertificationState").GetString());
        Assert.Equal("STORY_AMSC_001_W0_TO_W3",
            sot.RootElement.GetProperty("storyAmsc001W3R1").GetProperty("currentAuthority").GetString());
        Assert.Equal("NOT_TOUCHED",
            sot.RootElement.GetProperty("storyAmsc001W3R1").GetProperty("manifestStructuralState").GetString());
        Assert.Equal("PRESERVED",
            sot.RootElement.GetProperty("storyAmsc001W3R1").GetProperty("globalHostCheckpointState").GetString());

        // Repository-global Host root checkpoint must remain untouched by this module-local task.
        Assert.Equal("TB-TMAR-HOST-ROOT-FINAL-CERT-001", sot.RootElement.GetProperty("lastAcceptedTask").GetString());
        Assert.Equal("HOST_ROOT_FINAL_CERTIFIED", sot.RootElement.GetProperty("currentHostCheckpoint").GetString());
        Assert.Equal("USER_REVIEW_HOST_ROOT_FINAL_CERT_001", sot.RootElement.GetProperty("workflowStop").GetString());
        Assert.Equal("NONE", sot.RootElement.GetProperty("automaticNextImplementationTask").GetString());
    }

    /// <summary>The Master Recovery records the reconciled W3 SHA and the W3-R1 checkpoint.</summary>
    [Fact]
    public void Master_recovery_records_the_reconciled_w3_sha_and_checkpoint()
    {
        var root = Repo();
        var recovery = File.ReadAllText(Path.Combine(root, "docs", "architecture", "TOOBA-TMAR-MASTER-RECOVERY.md"));

        Assert.Contains("Story AMSC W3-R1 recovery reconciliation", recovery, StringComparison.Ordinal);
        Assert.Contains("RECORDED_39AB324E", recovery, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_STORY_AMSC_001_W3_R1", recovery, StringComparison.Ordinal);
        Assert.Contains(W3Commit, recovery, StringComparison.Ordinal);

        var storyLine = recovery.Split('\n')
            .Single(line => line.Contains("TB-TMAR-STORY-AMSC-001-W3` Certify", StringComparison.Ordinal));
        Assert.Contains("`39ab324e`", storyLine, StringComparison.Ordinal);
        Assert.DoesNotContain("the Architect reconciles the final SHA separately", storyLine, StringComparison.Ordinal);
    }

    /// <summary>The four AMSC waves plus the W3-R1 evidence wave exist on disk.</summary>
    [Fact]
    public void Story_amsc_evidence_tree_including_w3r1_is_present()
    {
        var root = Repo();
        foreach (var wave in new[] { "W0", "W1", "W2", "W3", "W3-R1" })
        {
            Assert.True(Directory.Exists(Path.Combine(
                root, "docs", "architecture", "evidence", $"TB-TMAR-STORY-AMSC-001-{wave}")), wave);
        }

        var reconciliation = File.ReadAllText(Path.Combine(
            root, "docs/architecture/evidence/TB-TMAR-STORY-AMSC-001-W3-R1/reconciliation.md"));
        Assert.Contains("RECOVERY_SOT_RECONCILIATION_ONLY", reconciliation, StringComparison.Ordinal);
        Assert.Contains("ARCH-COMPLETE-002", reconciliation, StringComparison.Ordinal);
        Assert.Contains("39ab324e", reconciliation, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_STORY_AMSC_001_W3_R1", reconciliation, StringComparison.Ordinal);
    }

    private static bool IsAncestor(string root, string commit)
    {
        using var process = new System.Diagnostics.Process();
        process.StartInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"merge-base --is-ancestor {commit} HEAD",
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        process.Start();
        process.WaitForExit();
        return process.ExitCode == 0;
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
