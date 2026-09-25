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
        Assert.Equal("TB-TMAR-HOST-ADDRESSBOOK-INVENTORY-001", rootEl.GetProperty("nextTask").GetString());
        Assert.Equal("HOST_FIRST_FOLDER_BY_FOLDER_AFTER_ACCESSCONTROL_FINAL_CERTIFICATION", rootEl.GetProperty("nextTaskGate").GetString());
        Assert.Equal("PAUSED_AT_SAFE_W5_CHECKPOINT", rootEl.GetProperty("checkoutState").GetString());
        Assert.True(rootEl.GetProperty("frontendFrozen").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", rootEl.GetProperty("locksVersion").GetString());
        Assert.Equal(
            "COMPLETE_REFERENCE_PATTERN_REQUIRES_ENDPOINTS_CQRS_RESULT_CONTRACTS_VALIDATION_CAPABILITY_STRUCTURE_GUARDS_SOT",
            rootEl.GetProperty("definitionMarker").GetString());
        var structureLock = rootEl.GetProperty("structureLock");
        Assert.Equal("ARCH-COMPLETE-002", structureLock.GetProperty("version").GetString());
        Assert.Equal(
            new[] { "AccessControl", "Cart", "Fulfillment", "Offer", "Order", "Payment", "Settlement", "StoreContext" },
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

        var fulfillmentAudit = rootEl.GetProperty("fulfillmentArchComplete002Audit");
        Assert.Equal("TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-AUDIT-001", fulfillmentAudit.GetProperty("task").GetString());
        Assert.Equal("AUDIT_ONLY", fulfillmentAudit.GetProperty("state").GetString());
        Assert.False(fulfillmentAudit.GetProperty("productionCodeChanged").GetBoolean());
        Assert.False(fulfillmentAudit.GetProperty("structureCertifiedUnderArchComplete002").GetBoolean());
        Assert.Equal(15, fulfillmentAudit.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal(15, fulfillmentAudit.GetProperty("totalMediatRRequests").GetInt32());
        Assert.Equal(0, fulfillmentAudit.GetProperty("workerInternalRequests").GetInt32());
        Assert.Equal(10, fulfillmentAudit.GetProperty("validatorRequiredCount").GetInt32());
        Assert.Equal(0, fulfillmentAudit.GetProperty("validatorsPresentCount").GetInt32());
        Assert.Equal(10, fulfillmentAudit.GetProperty("validatorsMissingCount").GetInt32());
        Assert.Equal(5, fulfillmentAudit.GetProperty("noValidatorRequiredCount").GetInt32());
        Assert.Equal(2, fulfillmentAudit.GetProperty("noInputNoValidatorCount").GetInt32());
        Assert.Equal(1, fulfillmentAudit.GetProperty("authScopedNoValidatorCount").GetInt32());
        Assert.Equal(2, fulfillmentAudit.GetProperty("optionalPresentationLocaleNoValidatorCount").GetInt32());
        Assert.Equal("0_OF_10_REQUIRED_PRESENT_5_NO_VALIDATOR_REQUIRED",
            fulfillmentAudit.GetProperty("validatorCoverageState").GetString());
        Assert.Contains("ListCustomerCheckoutFulfillmentsQuery",
            fulfillmentAudit.GetProperty("requiredValidatorRequests").GetString(),
            StringComparison.Ordinal);
        Assert.Equal("ListAdminFulfillmentsQuery,EnsureShippingCatalogSeedCommand",
            fulfillmentAudit.GetProperty("noInputNoValidatorRequests").GetString());
        Assert.Equal("ListSellerFulfillmentsQuery",
            fulfillmentAudit.GetProperty("authScopedNoValidatorRequests").GetString());
        Assert.Equal("ListShippingServicesQuery,ListEnabledShippingMethodsTreeQuery",
            fulfillmentAudit.GetProperty("optionalPresentationLocaleNoValidatorRequests").GetString());
        Assert.Equal("VALIDATOR_REQUIRED_ROUTE_CHECKOUTID_ONLY_NOT_GUID_EMPTY",
            fulfillmentAudit.GetProperty("customerCheckoutClassification").GetString());
        Assert.Equal("EXACT", fulfillmentAudit.GetProperty("pathNamespaceState").GetString());
        Assert.Equal("ZERO", fulfillmentAudit.GetProperty("hostFulfillmentResidue").GetString());
        Assert.Equal("ZERO", fulfillmentAudit.GetProperty("fulfillmentToHostDependency").GetString());
        Assert.Equal("NEEDS_PRECERT_REPAIR_THEN_STRUCTURE",
            fulfillmentAudit.GetProperty("auditDecision").GetString());
        Assert.Equal(
            "ADD_10_TRANSPORT_VALIDATORS_AND_REMOVE_DEAD_HOST_FULFILLMENT_RETURNS_GRID_ALIASES_THEN_STRUCTURE",
            fulfillmentAudit.GetProperty("repairScope").GetString());
        Assert.DoesNotContain("Fulfillment", structureUncertified, StringComparer.Ordinal);
        Assert.Contains("Fulfillment", structureCertified, StringComparer.Ordinal);

        var fulfillmentPrecert = rootEl.GetProperty("fulfillmentPrecertValidation");
        Assert.Equal("TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-PRECERT-REPAIR-001",
            fulfillmentPrecert.GetProperty("task").GetString());
        Assert.Equal("FULFILLMENT_TRANSPORT_VALIDATORS_ADDED_AND_DEAD_HOST_ALIAS_RESIDUE_REMOVED",
            fulfillmentPrecert.GetProperty("state").GetString());
        Assert.Equal(15, fulfillmentPrecert.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal(10, fulfillmentPrecert.GetProperty("validatorRequiredCount").GetInt32());
        Assert.Equal(10, fulfillmentPrecert.GetProperty("validatorsPresentCount").GetInt32());
        Assert.Equal(0, fulfillmentPrecert.GetProperty("validatorsMissingCount").GetInt32());
        Assert.Equal(5, fulfillmentPrecert.GetProperty("noValidatorRequiredCount").GetInt32());
        Assert.Equal(2, fulfillmentPrecert.GetProperty("noInputNoValidatorCount").GetInt32());
        Assert.Equal(1, fulfillmentPrecert.GetProperty("authScopedNoValidatorCount").GetInt32());
        Assert.Equal(2, fulfillmentPrecert.GetProperty("optionalPresentationLocaleNoValidatorCount").GetInt32());
        Assert.Equal("COMPLETE_10_OF_10_REQUIRED_PRESENT_5_NO_VALIDATOR_REQUIRED",
            fulfillmentPrecert.GetProperty("validatorCoverage").GetString());
        Assert.Equal("TRANSPORT_INPUT_SHAPE_ONLY",
            fulfillmentPrecert.GetProperty("validationScope").GetString());
        Assert.Equal("12.5.0", fulfillmentPrecert.GetProperty("mediatR").GetString());
        Assert.Equal("ISENDER_ONLY_NO_DIRECT_VALIDATOR_INVOCATION",
            fulfillmentPrecert.GetProperty("senderState").GetString());
        Assert.Equal("REMOVED_ZERO_PRODUCTION_CONSUMERS_NO_COMPATIBILITY_ALIAS",
            fulfillmentPrecert.GetProperty("hostDeadAliasResidue").GetString());
        Assert.Equal("ZERO", fulfillmentPrecert.GetProperty("hostFulfillmentResidue").GetString());
        Assert.Equal("ZERO", fulfillmentPrecert.GetProperty("fulfillmentToHostDependency").GetString());
        Assert.Equal("CERTIFIED_TB_TMAR_FULFILLMENT_ARCH_COMPLETE_002_STRUCTURE_001",
            fulfillmentPrecert.GetProperty("structureCertification").GetString());

        var hostAliasResidue = Path.Combine(
            root, "src", "backend", "Host", "Tooba.Host", "FulfillmentReturnsGridAliases.cs");
        Assert.False(File.Exists(hostAliasResidue), hostAliasResidue);

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
        Assert.Equal("TB-TMAR-ACCESSCONTROL-FINAL-CERTIFICATION-AND-SOT-CLOSURE-001", rootEl.GetProperty("lastAcceptedTask").GetString());
        Assert.Equal("53365a7ec09f7d3123889cca008354857e16c56b", rootEl.GetProperty("lastAcceptedCommit").GetString());

        var paymentHostResidue = rootEl.GetProperty("paymentHostResidueRepair");
        Assert.Equal("TB-TMAR-PAYMENT-HOST-RESIDUE-REPAIR-001", paymentHostResidue.GetProperty("task").GetString());
        Assert.Equal("PAYMENT_RUNTIME_RESIDUE_REMOVED_SECURITY_ADAPTERS_ONLY", paymentHostResidue.GetProperty("state").GetString());
        Assert.Equal("ACCEPTED_AFTER_R1_BEHAVIOR_PARITY_REPAIR", paymentHostResidue.GetProperty("parentRepair").GetString());
        Assert.Equal("TB-TMAR-PAYMENT-HOST-RESIDUE-REPAIR-001-R1", paymentHostResidue.GetProperty("parityRepairTask").GetString());
        Assert.Equal("PAYMENT_INFRASTRUCTURE_WORKER_AND_OPTIONS", paymentHostResidue.GetProperty("reconciliationOwnership").GetString());
        Assert.Equal("MIN_15_SECONDS_PRESERVED", paymentHostResidue.GetProperty("reconciliationCadence").GetString());
        Assert.Equal("ALL_GRID_VALIDATION_ERRORS_STABLE_SEMANTIC_400", paymentHostResidue.GetProperty("gridValidationMapping").GetString());
        Assert.Equal("PAYMENT_RUNTIME_RESIDUE_REMOVED_SECURITY_ADAPTERS_ONLY", paymentHostResidue.GetProperty("hostResidue").GetString());
        Assert.Equal("PAYMENT_ENDPOINTS", paymentHostResidue.GetProperty("adminGridPolicyOwnership").GetString());
        Assert.Equal("ZERO", paymentHostResidue.GetProperty("paymentToHostDependency").GetString());
        Assert.False(paymentHostResidue.GetProperty("productionCodeChanged").GetBoolean() == false);

        var paymentPrecert = rootEl.GetProperty("paymentPrecertHygiene");
        Assert.Equal("TB-TMAR-PAYMENT-PRECERT-HYGIENE-001", paymentPrecert.GetProperty("task").GetString());
        Assert.Equal(
            "DEAD_PORTS_REMOVED_TYPED_FAULTS_NO_LOCALIZED_APPLICATION_FALLBACK",
            paymentPrecert.GetProperty("state").GetString());
        Assert.Equal("TB-TMAR-PAYMENT-HOST-RESIDUE-REPAIR-001-R1", paymentPrecert.GetProperty("parent").GetString());
        Assert.Equal("PAYMENT_CONTRACT_BRIDGE", paymentPrecert.GetProperty("internalBridge").GetString());
        Assert.Equal("REMOVED", paymentPrecert.GetProperty("messageClassification").GetString());
        Assert.Equal(
            "ContractOperationException_Code_WALLET_SPEND_REJECTED",
            paymentPrecert.GetProperty("walletTypedBoundary").GetString());
        Assert.Equal("ZERO", paymentPrecert.GetProperty("paymentToHostDependency").GetString());
        Assert.Equal("PENDING_TB_TMAR_PAYMENT_ARCH_COMPLETE_002_STRUCTURE_001",
            paymentPrecert.GetProperty("structureCertification").GetString());
        Assert.Equal(paymentPrecert.GetProperty("paymentDirectoryBaseline").GetInt32(),
            paymentPrecert.GetProperty("paymentDirectoryLines").GetInt32());

        var paymentSplit = rootEl.GetProperty("paymentPrecertDirectorySplit");
        Assert.Equal("TB-TMAR-PAYMENT-PRECERT-DIRECTORY-SPLIT-001", paymentSplit.GetProperty("task").GetString());
        Assert.Equal("BEHAVIOR_PRESERVING_FOCUSED_DIRECTORIES", paymentSplit.GetProperty("state").GetString());
        Assert.Equal("TB-TMAR-PAYMENT-PRECERT-HYGIENE-001", paymentSplit.GetProperty("parent").GetString());
        Assert.Equal(
            "FOCUSED_DIRECTORIES_PAYMENT_RECONCILIATION_ADMIN_EXPIRY",
            paymentSplit.GetProperty("paymentDirectoryArchitecture").GetString());
        Assert.Equal("REMOVED", paymentSplit.GetProperty("paymentDirectoryGodFile").GetString());
        Assert.Equal("REMOVED", paymentSplit.GetProperty("interfaceDowncast").GetString());
        Assert.Equal("NONE", paymentSplit.GetProperty("schemaMigration").GetString());
        Assert.Equal("PRESERVED", paymentSplit.GetProperty("behaviorParity").GetString());
        Assert.Equal("ZERO", paymentSplit.GetProperty("paymentToHostDependency").GetString());
        Assert.Equal(476, paymentSplit.GetProperty("paymentDirectoryLines").GetInt32());
        Assert.True(paymentSplit.GetProperty("paymentDirectoryLines").GetInt32() < 700);
        Assert.Equal("PENDING_TB_TMAR_PAYMENT_ARCH_COMPLETE_002_STRUCTURE_001",
            paymentSplit.GetProperty("structureCertification").GetString());
        Assert.Equal("PAYMENT_RUNTIME_RESIDUE_REMOVED_SECURITY_ADAPTERS_ONLY",
            paymentSplit.GetProperty("paymentHostResidue").GetString());
        Assert.Equal("REMOVED", paymentSplit.GetProperty("messageClassification").GetString());
        Assert.Equal("ACCEPTED_AFTER_R1_RUNTIME_DI_RESOLUTION_PROOF",
            paymentSplit.GetProperty("parentAcceptance").GetString());
        Assert.Equal("PROVEN", paymentSplit.GetProperty("runtimeDiResolution").GetString());

        var splitR1 = rootEl.GetProperty("paymentPrecertDirectorySplitR1");
        Assert.Equal("TB-TMAR-PAYMENT-PRECERT-DIRECTORY-SPLIT-001-R1", splitR1.GetProperty("task").GetString());
        Assert.Equal("ACCEPTED_AFTER_R1_RUNTIME_DI_RESOLUTION_PROOF",
            splitR1.GetProperty("parentProductionDecomposition").GetString());
        Assert.Equal("REPAIR_PAYMENT_DIRECTORY_DI_RESOLUTION_PROOF_ONLY", splitR1.GetProperty("objective").GetString());
        Assert.Equal("PROVEN", splitR1.GetProperty("runtimeDiResolution").GetString());
        Assert.Equal("NONE", splitR1.GetProperty("constructionCycle").GetString());
        Assert.False(splitR1.GetProperty("productionDiChanged").GetBoolean());
        Assert.Equal("ZERO", splitR1.GetProperty("paymentToHostDependency").GetString());
        Assert.Equal("PENDING_TB_TMAR_PAYMENT_ARCH_COMPLETE_002_STRUCTURE_001",
            splitR1.GetProperty("structureCertification").GetString());

        var validationStorefront = rootEl.GetProperty("paymentPrecertValidationStorefront");
        Assert.Equal("TB-TMAR-PAYMENT-PRECERT-VALIDATION-001", validationStorefront.GetProperty("task").GetString());
        Assert.Equal("TB-TMAR-PAYMENT-PRECERT-DIRECTORY-SPLIT-001-R1",
            validationStorefront.GetProperty("parent").GetString());
        Assert.Equal("ARCHITECT_ACCEPTED",
            validationStorefront.GetProperty("parentDirectorySplitR1").GetString());
        Assert.Equal("9_REQUIRED_PRESENT_1_NO_VALIDATOR_REQUIRED",
            validationStorefront.GetProperty("paymentValidationStorefront").GetString());
        Assert.Equal("SUPERSEDED_BY_TB_TMAR_PAYMENT_PRECERT_VALIDATION_002",
            validationStorefront.GetProperty("paymentValidationAdminWebhook").GetString());
        Assert.Equal(10, validationStorefront.GetProperty("storefrontEndpointReachableRequests").GetInt32());
        Assert.Equal(9, validationStorefront.GetProperty("storefrontValidatorRequiredCount").GetInt32());
        Assert.Equal(1, validationStorefront.GetProperty("storefrontNoValidatorRequiredCount").GetInt32());
        Assert.Equal("NO_VALIDATOR_REQUIRED_NO_INPUT",
            validationStorefront.GetProperty("listStorefrontPaymentMethodsQuery").GetString());
        Assert.Equal("TRANSPORT_INPUT_SHAPE_ONLY",
            validationStorefront.GetProperty("validationScope").GetString());
        Assert.Equal("NONE", validationStorefront.GetProperty("directValidatorInvocation").GetString());
        Assert.Equal("ZERO", validationStorefront.GetProperty("paymentToHostDependency").GetString());
        Assert.Equal("PENDING_TB_TMAR_PAYMENT_ARCH_COMPLETE_002_STRUCTURE_001",
            validationStorefront.GetProperty("structureCertification").GetString());

        var validationAdminWebhook = rootEl.GetProperty("paymentPrecertValidationAdminWebhook");
        Assert.Equal("TB-TMAR-PAYMENT-PRECERT-VALIDATION-002", validationAdminWebhook.GetProperty("task").GetString());
        Assert.Equal("TB-TMAR-PAYMENT-PRECERT-VALIDATION-001", validationAdminWebhook.GetProperty("parent").GetString());
        Assert.Equal("ARCHITECT_ACCEPTED", validationAdminWebhook.GetProperty("parentValidation001").GetString());
        Assert.Equal("5_REQUIRED_PRESENT", validationAdminWebhook.GetProperty("paymentValidationAdmin").GetString());
        Assert.Equal("1_REQUIRED_PRESENT", validationAdminWebhook.GetProperty("paymentValidationWebhook").GetString());
        Assert.Equal("COMPLETE_15_OF_15_REQUIRED_PRESENT_1_NO_VALIDATOR_REQUIRED",
            validationAdminWebhook.GetProperty("paymentValidationOverall").GetString());
        Assert.Equal(16, validationAdminWebhook.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal(15, validationAdminWebhook.GetProperty("validatorRequiredCount").GetInt32());
        Assert.Equal(1, validationAdminWebhook.GetProperty("noValidatorRequiredCount").GetInt32());
        Assert.Equal("ReconcileStalePaymentsCommand_NO_VALIDATOR_REQUIRED_INTERNAL_WORKER",
            validationAdminWebhook.GetProperty("workerOnlyRequest").GetString());
        Assert.Equal(6, validationAdminWebhook.GetProperty("validatorsAddedThisTask").GetInt32());
        Assert.Equal("GRID_POLICY_OWNED_BY_PAYMENT_ADMIN_GRID_NORMALIZER",
            validationAdminWebhook.GetProperty("gridValidationBoundary").GetString());
        Assert.Equal("SIGNATURE_AND_PAYLOAD_OWNED_BY_HANDLER_AND_SIGNATURE_VERIFIER",
            validationAdminWebhook.GetProperty("webhookValidationBoundary").GetString());
        Assert.Equal("NONE", validationAdminWebhook.GetProperty("directValidatorInvocation").GetString());
        Assert.Equal("ZERO", validationAdminWebhook.GetProperty("paymentToHostDependency").GetString());
        Assert.Equal("PENDING_TB_TMAR_PAYMENT_ARCH_COMPLETE_002_STRUCTURE_001",
            validationAdminWebhook.GetProperty("structureCertification").GetString());

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
        Assert.Equal("TB-TMAR-PAYMENT-ARCH-COMPLETE-002-STRUCTURE-001", paymentEntry.GetProperty("lastAcceptedTask").GetString());
        Assert.True(paymentEntry.GetProperty("structureCertifiedUnderArchComplete002").GetBoolean());
        Assert.Equal(
            "COMPLETE_15_OF_15_REQUIRED_PRESENT_1_NO_VALIDATOR_REQUIRED",
            paymentEntry.GetProperty("paymentValidatorCoverage").GetString());
        Assert.Equal("EXACT", paymentEntry.GetProperty("paymentPathNamespace").GetString());

        var paymentStructure = rootEl.GetProperty("paymentArchComplete002Structure");        Assert.True(paymentStructure.GetProperty("structureCertifiedUnderArchComplete002").GetBoolean());
        Assert.Equal("EXACT", paymentStructure.GetProperty("pathNamespace").GetString());
        Assert.Equal("ENFORCED", paymentStructure.GetProperty("rootAllowlist").GetString());
        Assert.Equal("NONE", paymentStructure.GetProperty("aliasWorkaround").GetString());
        Assert.Equal("COMPLETE_15_OF_15_REQUIRED_PRESENT_1_NO_VALIDATOR_REQUIRED",
            paymentStructure.GetProperty("validatorCoverage").GetString());
        Assert.Equal(16, paymentStructure.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal("ReconcileStalePaymentsCommand_NO_VALIDATOR_REQUIRED_INTERNAL_WORKER",
            paymentStructure.GetProperty("workerOnlyRequest").GetString());
        Assert.Equal("PAYMENT_RUNTIME_RESIDUE_REMOVED_SECURITY_ADAPTERS_ONLY",
            paymentStructure.GetProperty("hostResidue").GetString());
        Assert.Equal("ZERO", paymentStructure.GetProperty("paymentToHostDependency").GetString());
        Assert.True(paymentStructure.GetProperty("manifestCertified").GetBoolean());
        Assert.Equal("Order,Cart,StoreContext,Offer,Payment", paymentStructure.GetProperty("certifiedModules").GetString());
        Assert.Contains("Payment", structureCertified);

        var settlementEntry = rootEl.GetProperty("completeReferenceModules").EnumerateArray()
            .Single(x => x.GetProperty("module").GetString() == "Settlement");
        Assert.Equal("TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-STRUCTURE-001", settlementEntry.GetProperty("lastAcceptedTask").GetString());
        Assert.True(settlementEntry.GetProperty("structureCertifiedUnderArchComplete002").GetBoolean());
        Assert.Equal(
            "COMPLETE_4_OF_4_REQUIRED_PRESENT_6_NO_VALIDATOR_REQUIRED",
            settlementEntry.GetProperty("settlementValidatorCoverage").GetString());
        Assert.Equal("EXACT", settlementEntry.GetProperty("settlementPathNamespace").GetString());

        var settlementStructure = rootEl.GetProperty("settlementArchComplete002Structure");
        Assert.Equal("TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-STRUCTURE-001", settlementStructure.GetProperty("task").GetString());
        Assert.True(settlementStructure.GetProperty("structureCertifiedUnderArchComplete002").GetBoolean());
        Assert.Equal("EXACT", settlementStructure.GetProperty("pathNamespace").GetString());
        Assert.Equal("ENFORCED", settlementStructure.GetProperty("rootAllowlist").GetString());
        Assert.Equal("NONE", settlementStructure.GetProperty("aliasWorkaround").GetString());
        Assert.Equal(10, settlementStructure.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal(0, settlementStructure.GetProperty("workerInternalRequests").GetInt32());
        Assert.Equal(4, settlementStructure.GetProperty("validatorRequiredCount").GetInt32());
        Assert.Equal(4, settlementStructure.GetProperty("validatorsPresentCount").GetInt32());
        Assert.Equal(6, settlementStructure.GetProperty("noValidatorRequiredCount").GetInt32());
        Assert.Equal("COMPLETE_4_OF_4_REQUIRED_PRESENT_6_NO_VALIDATOR_REQUIRED",
            settlementStructure.GetProperty("validatorCoverage").GetString());
        Assert.Equal("12.5.0", settlementStructure.GetProperty("mediatR").GetString());
        Assert.Equal("TWO_THIN_HOST_SECURITY_ADAPTERS_ONLY", settlementStructure.GetProperty("hostResidue").GetString());
        Assert.Equal("ZERO", settlementStructure.GetProperty("settlementToHostDependency").GetString());
        Assert.True(settlementStructure.GetProperty("manifestCertified").GetBoolean());
        Assert.Equal("Order,Cart,StoreContext,Offer,Payment,Settlement", settlementStructure.GetProperty("certifiedModules").GetString());
        Assert.Equal("PAUSED_AT_SAFE_W5_CHECKPOINT", settlementStructure.GetProperty("checkoutState").GetString());
        Assert.True(settlementStructure.GetProperty("frontendFrozen").GetBoolean());
        Assert.Contains("Settlement", structureCertified);
        Assert.DoesNotContain("Settlement", structureUncertified, StringComparer.Ordinal);

        var fulfillmentEntry = rootEl.GetProperty("completeReferenceModules").EnumerateArray()
            .Single(x => x.GetProperty("module").GetString() == "Fulfillment");
        Assert.Equal("TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-STRUCTURE-001", fulfillmentEntry.GetProperty("lastAcceptedTask").GetString());
        Assert.True(fulfillmentEntry.GetProperty("structureCertifiedUnderArchComplete002").GetBoolean());

        var fulfillmentStructure = rootEl.GetProperty("fulfillmentArchComplete002Structure");
        Assert.Equal("TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-STRUCTURE-001", fulfillmentStructure.GetProperty("task").GetString());
        Assert.True(fulfillmentStructure.GetProperty("structureCertifiedUnderArchComplete002").GetBoolean());
        Assert.Equal("COMPLETE_10_OF_10_REQUIRED_PRESENT_5_NO_VALIDATOR_REQUIRED",
            fulfillmentStructure.GetProperty("validatorCoverage").GetString());
        Assert.Equal(15, fulfillmentStructure.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal(0, fulfillmentStructure.GetProperty("workerInternalRequests").GetInt32());
        Assert.Equal("EXACT", fulfillmentStructure.GetProperty("pathNamespace").GetString());
        Assert.Equal("ENFORCED", fulfillmentStructure.GetProperty("rootAllowlist").GetString());
        Assert.Equal("NONE", fulfillmentStructure.GetProperty("aliasWorkaround").GetString());
        Assert.Equal("MEDIATR_12_5", fulfillmentStructure.GetProperty("cqrs").GetString());
        Assert.Equal("ZERO", fulfillmentStructure.GetProperty("hostFulfillmentResidue").GetString());
        Assert.Equal("ZERO", fulfillmentStructure.GetProperty("fulfillmentToHostDependency").GetString());
        Assert.Equal(0, fulfillmentStructure.GetProperty("fulfillmentHostSpecificFileCount").GetInt32());
        Assert.Equal("CONTRACTS_ONLY", fulfillmentStructure.GetProperty("orderBoundary").GetString());
        Assert.True(fulfillmentStructure.GetProperty("manifestCertified").GetBoolean());
        Assert.Equal("Order,Cart,StoreContext,Offer,Payment,Settlement,Fulfillment", fulfillmentStructure.GetProperty("certifiedModules").GetString());
        Assert.Equal("PAUSED_AT_SAFE_W5_CHECKPOINT", fulfillmentStructure.GetProperty("checkoutState").GetString());
        Assert.True(fulfillmentStructure.GetProperty("frontendFrozen").GetBoolean());
        Assert.Equal("HOST_FIRST_FOLDER_BY_FOLDER", fulfillmentStructure.GetProperty("workflow").GetString());
        Assert.Equal("AccessControl", fulfillmentStructure.GetProperty("nextHostFolder").GetString());
        Assert.DoesNotContain("Fulfillment", structureUncertified, StringComparer.Ordinal);

        var accessControlStructure = rootEl.GetProperty("accessControlArchComplete002Structure");
        Assert.Equal("TB-TMAR-ACCESSCONTROL-FINAL-CERTIFICATION-AND-SOT-CLOSURE-001",
            accessControlStructure.GetProperty("task").GetString());
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", accessControlStructure.GetProperty("state").GetString());
        Assert.Equal("HTTP_OWNING", accessControlStructure.GetProperty("httpApplicability").GetString());
        Assert.Equal("MODULE_ENDPOINTS", accessControlStructure.GetProperty("endpointOwnership").GetString());
        Assert.Equal("MEDIATR_12_5", accessControlStructure.GetProperty("cqrs").GetString());
        Assert.True(accessControlStructure.GetProperty("structureCertifiedUnderArchComplete002").GetBoolean());
        Assert.Equal("COMPLETE_6_OF_6_REQUIRED_PRESENT_13_NO_VALIDATOR_REQUIRED",
            accessControlStructure.GetProperty("validatorCoverage").GetString());
        Assert.Equal(19, accessControlStructure.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal(6, accessControlStructure.GetProperty("validatorRequiredCount").GetInt32());
        Assert.Equal(6, accessControlStructure.GetProperty("validatorsPresentCount").GetInt32());
        Assert.Equal(0, accessControlStructure.GetProperty("validatorsMissingCount").GetInt32());
        Assert.Equal(13, accessControlStructure.GetProperty("noValidatorRequiredCount").GetInt32());
        Assert.Equal("EXACT", accessControlStructure.GetProperty("pathNamespace").GetString());
        Assert.Equal("ENFORCED", accessControlStructure.GetProperty("rootAllowlist").GetString());
        Assert.Equal("NONE", accessControlStructure.GetProperty("aliasWorkaround").GetString());
        Assert.Equal("ZERO", accessControlStructure.GetProperty("hostAccessControlResidue").GetString());
        Assert.Equal("ZERO", accessControlStructure.GetProperty("accessControlToHostDependency").GetString());
        Assert.Equal(0, accessControlStructure.GetProperty("accessControlHostSpecificFileCount").GetInt32());
        Assert.Equal("MapAccessControlModuleEndpoints", accessControlStructure.GetProperty("moduleMap").GetString());
        Assert.Equal("CLEAN_CONTRACTS_ONLY", accessControlStructure.GetProperty("contractsBoundary").GetString());
        Assert.True(accessControlStructure.GetProperty("manifestCertified").GetBoolean());
        Assert.Equal("Order,Cart,StoreContext,Offer,Payment,Settlement,Fulfillment,AccessControl",
            accessControlStructure.GetProperty("certifiedModules").GetString());
        Assert.Equal("PAUSED_AT_SAFE_W5_CHECKPOINT", accessControlStructure.GetProperty("checkoutState").GetString());
        Assert.True(accessControlStructure.GetProperty("frontendFrozen").GetBoolean());
        Assert.Equal("HOST_FIRST_FOLDER_BY_FOLDER", accessControlStructure.GetProperty("workflow").GetString());
        Assert.Equal("AddressBook", accessControlStructure.GetProperty("nextHostFolder").GetString());
        Assert.Contains("AccessControl", structureCertified, StringComparer.Ordinal);
        Assert.DoesNotContain("AccessControl", structureUncertified, StringComparer.Ordinal);

        var accessControlEvacuation = rootEl.GetProperty("currentHostEvacuation");
        Assert.Equal("AddressBook", accessControlEvacuation.GetProperty("activeModule").GetString());
        Assert.Equal("COMPLETE", accessControlEvacuation.GetProperty("accessControlClosure").GetString());
        Assert.Equal("AddressBook", accessControlEvacuation.GetProperty("nextHostFolderAfterAccessControl").GetString());
        Assert.Equal("TB-TMAR-HOST-ADDRESSBOOK-INVENTORY-001", accessControlEvacuation.GetProperty("currentTask").GetString());

        var settlementAudit = rootEl.GetProperty("settlementArchComplete002Audit");
        Assert.Equal("TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-AUDIT-001", settlementAudit.GetProperty("task").GetString());
        Assert.False(settlementAudit.GetProperty("productionCodeChanged").GetBoolean());
        Assert.False(settlementAudit.GetProperty("structureCertifiedUnderArchComplete002").GetBoolean());
        Assert.Equal(10, settlementAudit.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal(10, settlementAudit.GetProperty("totalMediatRRequests").GetInt32());
        Assert.Equal(0, settlementAudit.GetProperty("workerInternalRequests").GetInt32());
        Assert.Equal(4, settlementAudit.GetProperty("validatorRequiredCount").GetInt32());
        Assert.Equal(0, settlementAudit.GetProperty("validatorsPresentCount").GetInt32());
        Assert.Equal(4, settlementAudit.GetProperty("validatorsMissingCount").GetInt32());
        Assert.Equal(6, settlementAudit.GetProperty("noValidatorRequiredCount").GetInt32());
        Assert.Equal(4, settlementAudit.GetProperty("authScopedNoValidatorCount").GetInt32());
        Assert.Equal(2, settlementAudit.GetProperty("noInputNoValidatorCount").GetInt32());
        Assert.Equal(
            "RequestSellerPayoutCommand,ProcessAdminPayoutCommand,RetryAdminPayoutCommand,QueryAdminPayoutGridQuery",
            settlementAudit.GetProperty("requiredValidatorRequests").GetString());
        Assert.Equal("0_OF_4_REQUIRED_PRESENT_6_NO_VALIDATOR_REQUIRED",
            settlementAudit.GetProperty("validatorCoverageState").GetString());
        Assert.Equal("EXACT", settlementAudit.GetProperty("pathNamespaceState").GetString());
        Assert.Equal("NO_ALIAS_NO_FOREIGN_GLOBAL_ALIAS", settlementAudit.GetProperty("aliasWorkaroundState").GetString());
        Assert.Equal("TWO_THIN_HOST_SECURITY_ADAPTERS_ONLY_KEEP",
            settlementAudit.GetProperty("hostSettlementResidue").GetString());
        Assert.Equal("ZERO", settlementAudit.GetProperty("settlementToHostDependency").GetString());
        Assert.Equal("NEEDS_PRECERT_REPAIR_THEN_STRUCTURE", settlementAudit.GetProperty("auditDecision").GetString());

        var settlementValidation = rootEl.GetProperty("settlementPrecertValidation");
        Assert.Equal("TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-PRECERT-REPAIR-001",
            settlementValidation.GetProperty("task").GetString());
        Assert.Equal(10, settlementValidation.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal(4, settlementValidation.GetProperty("validatorRequiredCount").GetInt32());
        Assert.Equal(4, settlementValidation.GetProperty("validatorsPresentCount").GetInt32());
        Assert.Equal(0, settlementValidation.GetProperty("validatorsMissingCount").GetInt32());
        Assert.Equal(6, settlementValidation.GetProperty("noValidatorRequiredCount").GetInt32());
        Assert.Equal("COMPLETE_4_OF_4_REQUIRED_PRESENT_6_NO_VALIDATOR_REQUIRED",
            settlementValidation.GetProperty("validatorCoverage").GetString());
        Assert.Equal("NONE", settlementValidation.GetProperty("directValidatorInvocation").GetString());
        Assert.Equal("12.5.0", settlementValidation.GetProperty("mediatR").GetString());
        Assert.Equal("ZERO", settlementValidation.GetProperty("settlementToHostDependency").GetString());
        Assert.Equal("SUPERSEDED_BY_TB_TMAR_SETTLEMENT_ARCH_COMPLETE_002_STRUCTURE_001",
            settlementValidation.GetProperty("structureCertification").GetString());
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
