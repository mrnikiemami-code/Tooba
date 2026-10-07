# TB-TMAR-PRICING-AMSC-001-W3-R3 — validation

- Module: `Pricing`
- Starting HEAD: `7159c8f773c1faa9b4b6d425b19067f50ca27572` (branch `main`, `HEAD == origin/main`)
- Baseline: the measured starting-head tree itself (`7159c8f7`), captured before any W3-R3 edit.

## 1. Change surface

Tracked modifications (certification wave only — no production code):

```text
M  docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
M  docs/architecture/tmar-current-state.json
M  docs/architecture/tmar-module-structure-manifests.json
M  src/backend/Host/Tooba.Host.Tests/Architecture/PricingModuleAmsc001W2StructureGuardTests.cs
M  src/backend/Host/Tooba.Host.Tests/Architecture/PricingModuleAmsc001W3R2RepairGuardTests.cs
M  src/backend/Host/Tooba.Host.Tests/Architecture/TmarCompleteReferenceStructureGateTests.cs
A  src/backend/Host/Tooba.Host.Tests/Architecture/PricingModuleAmsc001W3R3CertGuardTests.cs
A  docs/ai/tasks/TB-TMAR-PRICING-AMSC-001-W3-R3.task.md
A  docs/architecture/evidence/TB-TMAR-PRICING-AMSC-001-W3-R3/*
```

No Pricing production file, project file, `.slnx`, schema, migration, error code, resource or Contracts
API was changed. `productionCodeChanged = false`.

## 2. Machine-checked audit

`node docs/architecture/evidence/TB-TMAR-PRICING-AMSC-001-W3-R3/certify-audit.cjs` → `audit-after.json`

```json
{
  "structure": { "projectDirs": 5, "endpointProjectDir": false, "endpointTestDir": false,
                 "slnxPricingProjectCount": 5, "slnxHasEndpoints": false, "namespaceMismatches": [] },
  "httpApplicability": { "production_MapGet": 0, "production_MapPost": 0, "production_MapPut": 0,
                         "production_MapPatch": 0, "production_MapDelete": 0, "production_MapGroup": 0,
                         "production_IEndpointRouteBuilder": 0, "production_ISender": 0,
                         "production_MediatR": 0, "production_EndpointsRef": 0,
                         "host_MapPricingModule": 0, "host_AddPricingEndpointPresentation": 0,
                         "host_EndpointsRef": 0, "host_csproj_EndpointsRef": 0,
                         "host_csproj_PricingInfraRef": 1 },
  "errorsAndLocalization": { "declaredCodes": 11, "knownCodesGuard": true, "isKnownGuard": true,
                             "catalogContributorClasses": 1, "descriptorFactories": 11,
                             "resourceSetClasses": 1, "enKeys": 11, "faKeys": 11, "enFaIdentical": true,
                             "registrationContributor": 1, "registrationResourceSet": 1 },
  "typedFault": { "contractOperationIsKnownFilter": 2, "semanticExceptionCatch": 2,
                  "messageClassification": 0, "production_ExMessage": 0 },
  "boundaries": { "foreignAppInfraDomainSources": [], "promotionReferencesPricingApplication": [],
                  "promotionReferencesPricingContracts": 1 },
  "persistence": { "schemaLiteral": 1, "dbSetAuthoredPrice": 1, "schemaMigratorRegistration": 1,
                   "outboxRegistration": 1, "migrations": ["20260823085546_InitialPricing.Designer.cs",
                   "20260823085546_InitialPricing.cs", "PricingDbContextModelSnapshot.cs"] },
  "hostAuthority": { "hits": [], "hostPricingFolder": false },
  "cohesion": { "over800": [] },
  "manifest": { "certifiedCount": 26, "pricingInModules": true, "pricingInPreCert": false },
  "sot": { "certifiedModuleCount": 25, "pricingInCertifiedExactlyOnce": 1, "hasW3R3Block": true,
           "hostCheckpoint": "HOST_ROOT_FINAL_CERTIFIED",
           "lastAcceptedTask": "TB-TMAR-HOST-ROOT-FINAL-CERT-001" }
}
```

Manifest integrity: the Pricing entry's structural payload is byte-preserved across the promotion
(`pricing structure preserved: true`), the rest of the manifest is untouched
(`rest of manifest untouched: true`), and the SoT change is exactly one `certifiedModules` insertion plus
the appended `pricingAmsc001W3R3` block (`@@ -1895 +1895,2 @@`, `@@ -12982,0 +12984,65 @@`).

## 3. Focused builds

| Build | Result |
| --- | --- |
| `dotnet build src/backend/Tooba.slnx` | **0 Error(s)**, 36 Warning(s) (pre-existing NU1900/analyzer noise) |
| `Tooba.Host.Tests` build (via the test run) | succeeded, **0 errors** |

## 4. Focused test runs

### 4.1 `Tooba.Pricing.Tests`

```text
Passed!  - Failed: 0, Passed: 13, Skipped: 0, Total: 13
```

### 4.2 Pricing AMSC + ownership + structure-gate family

Filter (Host test project): `~PricingModuleAmsc001W1MigrateGuardTests | ~PricingModuleAmsc001W2StructureGuardTests | ~PricingModuleAmsc001W3R2RepairGuardTests | ~PricingModuleAmsc001W3R3CertGuardTests | ~ErrorCatalogUniqueCodeGuardTests | ~HostModuleEndpointOwnershipTests`

```text
Test Run Successful.  Total tests: 42  Passed: 42
```

| Suite | Tests | Result |
| --- | --- | --- |
| `PricingModuleAmsc001W3R3CertGuardTests` (new) | 8 | passed |
| `PricingModuleAmsc001W3R2RepairGuardTests` (repointed) | 11 | passed |
| `PricingModuleAmsc001W2StructureGuardTests` (repointed) | 8 | passed |
| `PricingModuleAmsc001W1MigrateGuardTests` | 9 | passed |
| `ErrorCatalogUniqueCodeGuardTests` | 3 | passed |
| `HostModuleEndpointOwnershipTests` | 3 | passed |

Module-owned Pricing guards (in `Tooba.Pricing.Tests`, part of the 13/13 run above):

| Suite | Tests | Result |
| --- | --- | --- |
| `PricingArchitectureGuardTests` | 7 | passed |
| `PricingDbContextOwnershipTests` | 1 | passed |
| `PricingErrorCatalogTests` | 1 | passed |

Both repointed guards kept their full test count and every structural assertion; only the certification
claim (pre-cert → certified) was repointed. `PricingModuleAmsc001W3R2RepairGuardTests` grew from 9 to 11
assertions because the renamed supersession test now additionally pins the W3-R3 recertification state and
the exactly-once `structureLock` membership.

### 4.3 Full `Tooba.Host.Tests` suite — new-failure proof

| Run | Result |
| --- | --- |
| Starting-head baseline `7159c8f7` (measured on the tree before any W3-R3 edit) | `Failed: 79, Passed: 2036, Skipped: 130, Total: 2245` |
| Current tree (W3-R3) | `Failed: 79, Passed: 2044, Skipped: 130, Total: 2253` |
| **New failures introduced by W3-R3** | **ZERO** (set-difference of failing test ids: empty in both directions) |
| Failures fixed by W3-R3 | none |
| Guards weakened / baselines widened | **NONE** |

The `+8 passed` delta is exactly the new `PricingModuleAmsc001W3R3CertGuardTests` suite. All 79 remaining
failures are byte-identical identifiers to the starting-head baseline and are unrelated to Pricing:

```text
TaxFoundationTests.Pricing_and_catalog_do_not_own_tax_amounts_and_outcomes_stay_distinct   (missing Modules/Tax/Tooba.Tax.Domain/TaxDomain.cs — pre-existing)
TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment
    (repository-global Tooba.Catalog.Contracts/Cart path↔namespace deviation — pre-existing, unchanged by W3-R3)
HostDevelopmentEnricherClosureGuardTests.New_development_gateways_live_in_owning_module_contracts  (pre-existing)
TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable / Recovery_sot_sync_001_*  (repository-global Host-root pins — pre-existing)
TmarSourceSizeAndInfraAppTests.* , CorrelationRuntimeTests.* , ConsolidatedPackageTests.* ,
PaidProjectionFinancialTests.* , Fulfillment*/Return*/Promotion*/Reservation*/AdminReturn*/ProductHistory* …  (pre-existing)
```

`Pricing` appears in exactly one of them (`TaxFoundationTests`, whose cause is the missing Tax domain file,
not Pricing) and none was touched, weakened or repaired.

### 4.4 Bounded-validation compliance

- No repository-global unrelated failing guard was required to turn green.
- The one deterministic local failure found during implementation (a guard-side expected-list omission for
  the newly promoted `Story` module in the manifest) was repaired with one bounded edit, and the one
  self-referential-placeholder assertion was replaced with the explicit
  `certificationCommitState` assertion rather than a `PENDING_THIS_COMMIT` value.
- No guard, baseline or assertion was weakened to reach PASS.

## 5. Verdict

```text
Certification-State = COMPLETE_REFERENCE_PATTERN
Structure-State = CERTIFIED
Guards-Weakened-State = NONE
Baselines-Widened-State = NONE
Schema-Migration-State = UNCHANGED
Evidence-State = COMPLETE
Post-Cert-Recovery-State = REQUIRED
Automatic-Next-Implementation-Task-State = NONE
Workflow-Stop-State = USER_REVIEW_PRICING_AMSC_001_W3_R3
```
