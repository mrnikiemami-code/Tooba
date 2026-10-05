# TB-TMAR-FULFILLMENT-AMSC-001 — W3 validation

## 1. Build

```
dotnet build src/backend/Tooba.slnx
Build succeeded.
    113 Warning(s)   (solution-wide, pre-existing; no Fulfillment production warning added)
    0 Error(s)
```

The W2 move left duplicate `using` directives in three Fulfillment test files; W3 removed them
(13 CS0105 warnings eliminated), leaving the Fulfillment test project at 0 warnings.

## 2. Fulfillment module tests

```
dotnet test src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Tooba.Fulfillment.Tests.csproj
Passed!  - Failed: 0, Passed: 70, Skipped: 0, Total: 70
```

70 = W2's 69 + 1 new W3 test
(`Fulfillment_operation_maps_domain_contract_faults_by_stable_code`).

## 3. Host certification guard

```
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj ^
  --filter "FullyQualifiedName~FulfillmentModuleAmsc001W3CertGuardTests"
Passed!  - Failed: 0, Passed: 12, Skipped: 0, Total: 12
```

## 4. Host regression baseline (byte-identical failure set)

Command (identical for both runs):

```
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj ^
  --filter "FullyQualifiedName~Fulfillment|FullyQualifiedName~Shipping|FullyQualifiedName~Tmar|FullyQualifiedName~ErrorCatalog|FullyQualifiedName~AdminDbNativeGrid|FullyQualifiedName~PaidProjection"
```

| Run | Result |
| --- | --- |
| **Before W3** (`git stash push -u`, at `c0db0566`) | Failed: 24 / Passed: 124 / Skipped: 3 / Total: 151 |
| **After W3** | Failed: 24 / Passed: 136 / Skipped: 3 / Total: 163 |

The failing-test-name sets were extracted from TRX and compared with `Compare-Object`:

- **NEW failures (after ∖ before): none**
- **FIXED (before ∖ after): none**

`Passed` grew by 12 (the new cert guard) and `Total` by 12; the failure set is identical.

### One W3-caused failure caught and repaired

The first post-W3 run had **25** failures. The extra failure was
`ShippingServiceAdminTests.Unknown_language_is_rejected_as_semantic_result`: its in-test
`FixedLanguageGate` double still threw the untyped `InvalidOperationException("shipping_service.language_invalid")`
that production no longer emits. W3 aligned the double to the real gate's typed
`SemanticException(new SemanticError(FulfillmentErrorCodes.ShippingServiceLanguageInvalid))`. After the
fix the failure set is byte-identical to the baseline.

### Pre-existing failures (not introduced by W3)

- `FulfillmentLineQuantityOpsTests` / `FulfillmentLineQuantityOperationsTests` /
  `FulfillmentShipmentAllocationTests` — the Domain `InvalidOperationException` →
  `ContractOperationException` parity divergence recorded in W0/W1 (these Host tests assert the legacy
  untyped exception type; the Domain has thrown the typed one since before this run).
- `TmarSourceSizeAndInfraAppTests`, `TmarFoundationTests`, `TmarCompleteReferenceStructureGateTests`,
  `TmarDurableGuardTests`, `FulfillmentFoundationTests` — stale repo-wide baselines / SoT-freshness
  assertions from other modules' migrations.
- `AdminDbNativeGridQueryTests`, `PaidProjectionFinancialTests` — pre-existing.

## 5. Structure guards (Fulfillment module)

| Guard | Result |
| --- | --- |
| `Fulfillment_application_is_capability_first_with_no_technical_axis_root` | PASS |
| `Fulfillment_has_no_single_file_use_case_leaf_folders` | PASS |
| `Fulfillment_application_has_no_flat_mixed_contracts_bundles` | PASS |
| `Fulfillment_directory_stays_under_the_arch_size_ceiling` | PASS (794 / 440) |
| `Fulfillment_root_allowlists_and_forbidden_flattened_files_are_enforced` | PASS |
| `Fulfillment_golden_boundaries_and_physical_layout_remain_clean` | PASS |
| `Validator_folder_layout_is_capability_first_shallow` | PASS |
| `FulfillmentModuleAmsc001W3CertGuardTests` (12 locks) | PASS |

## 6. Host final closure

`currentHostCheckpoint = HOST_ROOT_FINAL_CERTIFIED` untouched; zero Host production files added or
modified by this wave (only the Host test double in `ShippingServiceAdminTests` and the new Host
architecture guard).
