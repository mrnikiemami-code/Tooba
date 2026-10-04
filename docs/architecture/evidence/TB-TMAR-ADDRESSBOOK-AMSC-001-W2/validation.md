# TB-TMAR-ADDRESSBOOK-AMSC-001-W2 — validation

- Branch: `main`
- HEAD at W2 start: `3080d3e2eda5b30e65b501ca74873cdbd029365d` (== `origin/main`)
- Focused validation only (skill section 25). No broad solution-wide test sweep.

## Build

```text
dotnet build src/backend/Tooba.slnx
Build succeeded.
    146 Warning(s)
    0 Error(s)
```

`146` warnings == the W0/W1 baseline. **0 errors.** No new warning introduced by W2.

## Focused module tests

```text
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter "FullyQualifiedName~AddressBook"
Passed!  - Failed: 0, Passed: 23, Skipped: 4, Total: 27
```

| Suite | Before W2 | After W2 |
| --- | --- | --- |
| `AddressBookFoundationTests` | PASS | PASS |
| `AddressBookPhysicalStructureGuardTests` | PASS (old 4 tests) | PASS (**6 tests** — 2 new) |
| `AddressBookValidatorCoverageGuardTests` | PASS (F8 repaired in W1) | PASS |
| `StorefrontRecipientCanonicalizationTests` | PASS | PASS |
| `AddressBookPostgresTests` | 4 SKIP (no Postgres) | 4 SKIP (unchanged) |

The two new durable guards are exercised and green:

```text
AddressBookPhysicalStructureGuardTests.AddressBook_application_is_capability_first_with_no_technical_axis_root  PASS
AddressBookPhysicalStructureGuardTests.AddressBook_has_no_single_file_use_case_leaf_folders                     PASS
```

They were **red-by-construction before the repair** and are green after it (verified during W2: the first run
against the pre-repair tree reported
`single-file use-case leaf folders are OVER_FOLDERED: Tooba.AddressBook.Application/Validators/AddressBookFluentRules.cs`).

## Structure-specific guards

```text
AddressBookPhysicalStructureGuardTests.AddressBook_production_files_live_under_approved_offer_style_folders     PASS
AddressBookPhysicalStructureGuardTests.AddressBook_namespaces_equal_path_derived_namespaces_exactly             PASS
AddressBookPhysicalStructureGuardTests.AddressBook_domain_aggregate_and_contracts_ports_are_in_offer_style_locations PASS
AddressBookPhysicalStructureGuardTests.AddressBook_has_no_stale_or_duplicate_physical_type_copies               PASS
AddressBookPhysicalStructureGuardTests.AddressBook_application_is_capability_first_with_no_technical_axis_root  PASS
AddressBookPhysicalStructureGuardTests.AddressBook_has_no_single_file_use_case_leaf_folders                     PASS
```

## Solution grouping

No `.slnx` change was needed; the `AddressBook` Solution Folder already groups 5/5 projects (verified by
inspection, see `solution-explorer.md`). `dotnet build src/backend/Tooba.slnx` parsed the solution
successfully.

## Pre-existing repo-wide drift — unchanged, out of scope

Re-run after W2 (`FullyQualifiedName~TmarCompleteReferenceStructureGateTests|~TmarSourceSizeAndInfraApp|~TmarDurableGuard`):

```text
Failed!  - Failed: 7, Passed: 16, Skipped: 0, Total: 23
```

Identical count and identical membership to the W0/W1 baseline:

| Guard | Nature | AddressBook involved? |
| --- | --- | --- |
| `TmarCompleteReferenceStructureGateTests.Manifest_is_well_formed_and_only_declared_modules_are_certified` | `Catalog` present in manifest but missing from SoT `structureLock.certifiedModules` | No |
| `TmarCompleteReferenceStructureGateTests.Uncertified_modules_are_explicitly_not_claimed` | same Catalog/SoT divergence | No |
| `TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment` | `Expected "Tooba.Catalog.Contracts.Cart" Actual "Tooba.Catalog.Contracts"` | No |
| `TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` | same Catalog/SoT divergence, driven by the SoT `certifiedModules` list | No |
| `TmarSourceSizeAndInfraAppTests.Hand_written_source_size_does_not_expand_beyond_baseline` | stale untracked sibling `.tmp-baseline` worktree scanned by the shared size guard | No |
| `TmarSourceSizeAndInfraAppTests.Infrastructure_to_foreign_Application_edges_do_not_expand_beyond_baseline` | `Tooba.Promotion.Infrastructure -> Tooba.Inventory/Party/Pricing.Application` | No |
| `TmarSourceSizeAndInfraAppTests.Source_size_inventory_evidence_exists_and_matches_scan_count` | same `.tmp-baseline` scan-count inflation | No |

**AddressBook appears in none of the seven.** The failure set is byte-for-byte the same membership as the W0
Analyze baseline (F9) and the W1 verification, so W2 widened nothing.

`AddressBook` also does not appear in the `TmarSourceSizeAndInfraAppTests` violation inventory: the
`AddressBookDirectory.cs` (~220 LOC) is far below the 800 threshold and the module has no baseline entry.

## Behaviour preservation

Pure structure/namespace move:

- no type renamed, no member signature changed, no logic edited
- no DI registration changed (`AddressBookModule.cs` still registers
  `IAddressBookDirectory -> AddressBookDirectory`; the `Ports` namespace did not move)
- no route added/removed/renamed (the 6 module-owned routes are untouched)
- no DTO, error code, catalog descriptor or resx key changed
- no EF migration regenerated, no schema change

`AddressBookFoundationTests` (endpoint/session/actor semantics, `api.From(result)`, `AddressMissing` ownership,
`OwnerUserId` stripping, country defaulting) all pass unchanged, confirming behaviour preservation.

## Structure-State

```text
Structure-State = READY_FOR_CERTIFY
```

All completion gates hold:

| Gate | Value |
| --- | --- |
| Folder-Granularity-State | `PROFESSIONAL_SHALLOW` |
| Solution-Explorer-State | `CANONICAL` |
| Path-Namespace-State | `EXACT` |
| Physical-Copy-State | `CLEAN` |
| Root-Allowlist-State | `ENFORCED` |
| unjustified single-file request leaf folders | none |
| unjustified technical-axis-first request tree | none |
| root dump | none |
| unresolved god-file / over-split blocker | none |
| Host final closure preserved | `HOST_FINAL_CLOSURE_REGRESSION = NONE` (no Host file touched) |
| focused structure guards | PASS |

Handoff to `tooba-architecture-certify` (W3).
