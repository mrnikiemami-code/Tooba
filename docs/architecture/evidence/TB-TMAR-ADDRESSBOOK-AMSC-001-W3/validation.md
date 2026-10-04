# TB-TMAR-ADDRESSBOOK-AMSC-001-W3 — validation

Focused validation only (skill section 18). No broad solution-wide test sweep, no open-ended repair loop.

## Builds

```text
dotnet build src/backend/Tooba.slnx --no-incremental
Build succeeded.
    146 Warning(s)
    0 Error(s)
```

`146` warnings == the W0/W1/W2 baseline exactly. **0 errors.** W3 introduced no new warning
(one initial `CS8631` nullability warning in the new guard was repaired with a bounded edit).

```text
dotnet build src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj
Build succeeded.  0 Error(s)
```

## Focused module tests

```text
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter "FullyQualifiedName~AddressBook"
Passed!  - Failed: 0, Passed: 33, Skipped: 4, Total: 37
```

| Suite | Tests | Result |
| --- | --- | --- |
| `AddressBookModuleAmsc001W3CertGuardTests` | 10 | PASS |
| `AddressBookPhysicalStructureGuardTests` | 6 | PASS |
| `AddressBookValidatorCoverageGuardTests` | 6 | PASS |
| `AddressBookCanonicalPresentationGuardTests` | 3 | PASS |
| `AddressBookFoundationTests` | 8 | PASS |
| `StorefrontRecipientCanonicalizationTests` | (AddressBook-related) | PASS |
| `AddressBookPostgresTests` | 4 | SKIP (Testcontainers-gated; no Postgres in this environment — unchanged from the W0 baseline) |

Progression: W0 `20 passed / 1 failed / 4 skipped` → W1 `21/0/4` → W2 `23/0/4` → W3 `33/0/4`.

## Focused canonical guards

```text
dotnet test ... --filter "FullyQualifiedName~AddressBookCanonicalPresentationGuardTests|FullyQualifiedName~ErrorCatalogUniqueCodeGuardTests|FullyQualifiedName~StorefrontRecipientCanonicalization"
Passed!  - Failed: 0, Passed: 14, Skipped: 0, Total: 14
```

This covers the canonical presentation stack, the composed `IErrorDefinitionCatalog` uniqueness
(no duplicate machine-code descriptors) and the cross-module consumer of the aggregate's fault type.

## Pre-existing repo-wide drift — unchanged, out of scope

```text
dotnet test ... --filter "FullyQualifiedName~TmarCompleteReferenceStructureGateTests|FullyQualifiedName~TmarSourceSizeAndInfraApp|FullyQualifiedName~TmarDurableGuard"
Failed!  - Failed: 7, Passed: 8, Skipped: 0, Total: 15
```

Identical count and identical membership to the W0 Analyze baseline (`3256fc7a`) and to the W1/W2 runs:

| # | Guard | Nature | AddressBook involved? |
| --- | --- | --- | --- |
| 1 | `TmarCompleteReferenceStructureGateTests.Manifest_is_well_formed_and_only_declared_modules_are_certified` | `Catalog` in manifest but missing from SoT `structureLock.certifiedModules` | No |
| 2 | `TmarCompleteReferenceStructureGateTests.Uncertified_modules_are_explicitly_not_claimed` | same `Catalog`/SoT divergence | No |
| 3 | `TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment` | `Expected "Tooba.Catalog.Contracts.Cart" Actual "Tooba.Catalog.Contracts"` | No |
| 4 | `TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` | same `Catalog`/SoT divergence via `certifiedModules` | No |
| 5 | `TmarSourceSizeAndInfraAppTests.Hand_written_source_size_does_not_expand_beyond_baseline` | stale untracked sibling `.tmp-baseline` worktree scanned by the shared size guard | No |
| 6 | `TmarSourceSizeAndInfraAppTests.Infrastructure_to_foreign_Application_edges_do_not_expand_beyond_baseline` | `Tooba.Promotion.Infrastructure -> Tooba.Inventory/Party/Pricing.Application` | No |
| 7 | `TmarSourceSizeAndInfraAppTests.Source_size_inventory_evidence_exists_and_matches_scan_count` | same `.tmp-baseline` scan-count inflation | No |

**AddressBook appears in none of the seven.** Every AddressBook assertion inside
`TmarCompleteReferenceStructureGateTests` passes (project paths, root allowlists, namespace alignment,
forbidden files/folders). AddressBook has no entry in the size baseline and no oversized file, so it is not
in the size-guard violation inventory.

These are not AddressBook regressions and are deliberately **not repaired** (out of this task's bounded
scope; recorded as residual watch R5).

## Guard-required checks

| Required guard | Result |
| --- | --- |
| module behaviour tests (`AddressBookFoundationTests`) | PASS |
| validator coverage guard | PASS |
| structure gate (`AddressBookPhysicalStructureGuardTests`) | PASS |
| localization / error catalog guard | PASS |
| tracing / correlation guard | PASS (no offenders in the module scan) |
| source-size / cohesion guard | PASS for AddressBook (no entry, no violation) |
| durable recovery guard | pre-existing failure driven by `Catalog`, not AddressBook |
| module-specific architecture guard | PASS (new W3 cert guard) |

**No certification with a known failing required guard**: every guard that covers AddressBook passes.

## Post-certification behaviour preservation

| Concern | Result |
| --- | --- |
| routes | unchanged (6) |
| status codes | unchanged for success paths; the W1 bounded defect repair is the only outcome change and was declared in W1 |
| error codes | unchanged (the W1 additions are the ones certified here) |
| DTO shape | unchanged |
| schema / migrations | unchanged |
| DI registrations | unchanged |
| behaviour change attributable to W2/W3 | **NONE** (pure structure move + verification/guards/docs) |
