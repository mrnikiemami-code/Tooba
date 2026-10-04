# TB-TMAR-ADDRESSBOOK-AMSC-001-W0 — Validation

## 1. Environment

| Item | Value |
| --- | --- |
| Branch | `main` |
| HEAD at start | `3256fc7acf581232d2a14eba30e1cd0f52fa486e` |
| `origin/main` at start | `3256fc7acf581232d2a14eba30e1cd0f52fa486e` |
| HEAD == origin/main | ✅ |
| Solution | `src/backend/Tooba.slnx` |
| SDK | .NET 8 |

## 2. Build baseline

```text
dotnet build src/backend/Tooba.slnx -v q --nologo
→ 146 Warning(s)
→   0 Error(s)
→ Time Elapsed 00:00:31.01
```

## 3. AddressBook test baseline

```text
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --no-build --filter FullyQualifiedName~AddressBook
→ Failed: 1, Passed: 20, Skipped: 4, Total: 25
```

| Test | Result |
| --- | --- |
| `AddressBookFoundationTests` (all) | ✅ PASS |
| `AddressBookPhysicalStructureGuardTests` (all) | ✅ PASS |
| `AddressBookCanonicalPresentationGuardTests` (all) | ✅ PASS |
| `AddressBookValidatorCoverageGuardTests` (other 6) | ✅ PASS |
| `AddressBookValidatorCoverageGuardTests.AddressBook_is_certified_with_exactly_one_manifest_entry` | ❌ **FAIL** |
| Testcontainers-gated behaviour tests | ⏭ 4 skipped |

### Failure detail (reproduced, deterministic)

```text
Failed Tooba.Host.Tests.Architecture.AddressBookValidatorCoverageGuardTests.AddressBook_is_certified_with_exactly_one_manifest_entry [4 ms]
  Error Message:
   preCertModules must be removed after promotion
  Stack Trace:
     at …AddressBookValidatorCoverageGuardTests.cs:line 148
```

Root cause: the guard asserts `!doc.RootElement.TryGetProperty("preCertModules", out _)`. The
property legitimately still exists because `preCertModules` contains `ProductWorkspace`
(`structureCertified: false`, `tmar-module-structure-manifests.json` line 1555) — an unrelated
uncertified module. The guard's *documented* intent (exactly one certified AddressBook entry, no
AddressBook duplicate) is already satisfied. This is a stale over-broad assertion, to be repaired in
W1 without weakening any AddressBook assertion.

## 4. Pre-existing repo-wide gate baseline (proven pre-existing)

```text
dotnet test … --filter "TmarCompleteReferenceStructureGateTests|TmarSourceSizeAndInfraAppTests|TmarDurableGuardTests"
→ Failed: 7, Passed: 8, Total: 15
```

| Test | Result | Classification |
| --- | --- | --- |
| `TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment` | ❌ | pre-existing (Catalog drift) |
| `TmarCompleteReferenceStructureGateTests.Manifest_is_well_formed_and_only_declared_modules_are_certified` | ❌ | pre-existing (Catalog drift) |
| `TmarCompleteReferenceStructureGateTests.Uncertified_modules_are_explicitly_not_claimed` | ❌ | pre-existing (Catalog drift) |
| `TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` | ❌ | pre-existing |
| `TmarSourceSizeAndInfraAppTests.Hand_written_source_size_does_not_expand_beyond_baseline` | ❌ | pre-existing (stale `.tmp-baseline` worktree) |
| `TmarSourceSizeAndInfraAppTests.Infrastructure_to_foreign_Application_edges_do_not_expand_beyond_baseline` | ❌ | pre-existing (stale `.tmp-baseline` worktree) |
| `TmarSourceSizeAndInfraAppTests.Source_size_inventory_evidence_exists_and_matches_scan_count` | ❌ | pre-existing |
| 8 others | ✅ | — |

None of these is an AddressBook regression. They must be **reproduced identically** after each wave
and reported, not repaired (bounded scope).

## 5. Static scans performed

| Scan | Result |
| --- | --- |
| Foreign `using Tooba.<Module>.` (non-platform) in AddressBook | only `Tooba.Order.Contracts.Fulfillment` (2 sites) |
| `Results.` in Endpoints | 6 sites (all success paths) — finding F1 |
| `InvalidOperationException` in AddressBook | 14 sites — finding F2 |
| Hard-coded Persian string literals (non-doc) | 14 fault-message sites + 7 development-seed data values |
| `Console.` / `Debug.Write` / `ActivitySource` / `new Meter` / `AsyncLocal` / `traceparent` | 0 |
| Duplicate physical type names | 0 |
| Root `.cs` files | 1 (`Endpoints/AddressBookModule.cs`, allowlisted) |
| AddressBook entries in `tmar-source-size-baseline.json` | 0 |
| `preCertModules` AddressBook duplicates | 0 |

## 6. Wave gates

| Wave | Gate |
| --- | --- |
| W0 (this) | evidence written, SoT updated, commit + push |
| W1 | AddressBook tests 21 passed / 0 failed / 4 skipped; build 0 errors; pre-existing gate set unchanged |
| W2 | `AddressBookPhysicalStructureGuardTests` green on the flattened tree; folder-granularity `PROFESSIONAL_SHALLOW`; manifest + guard updated honestly |
| W3 | `AddressBook` re-certified `COMPLETE_REFERENCE_PATTERN`; strengthened presentation guard green; SoT closure |

## 7. Recovery status

`HEAD == origin/main`, working tree contains only this wave's new evidence files plus the
pre-existing untracked Order artifact (`analyze.md` F10). No `RECOVERY_CONFLICT`.
