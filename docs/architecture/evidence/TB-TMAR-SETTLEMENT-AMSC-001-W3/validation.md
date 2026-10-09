# Validation (certify §18 — focused, no broad solution-wide runs)

Baseline for every command below: `main` @ `86ebb4dd` (W2 Structure), `HEAD == origin/main`,
known/safe working tree.

## 1. Durable certify guard (new in W3)

```text
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj \
  --filter "FullyQualifiedName~SettlementModuleAmsc001"

Passed!  - Failed: 0, Passed: 27, Skipped: 0, Total: 27, Duration: 98 ms - Tooba.Host.Tests.dll (net8.0)
```

27 = `SettlementModuleAmsc001W1MigrateGuardTests` (12) + `…W2StructureGuardTests` (9) +
`…W3CertGuardTests` (6). The W3 guard locks:

| Test | Locks |
|---|---|
| `Settlement_is_arch_complete_002_certified_in_sot_and_manifest` | single certified manifest entry + `certificationNote` + 3 projects; absence from `preCertModules` / `uncertifiedHttpOwningModules`; the `settlementAmsc001W3` SoT verdict, applicability, structural states, boundary/schema/closure states, stop gate; `certifiedModules` membership exactly once; the W0/W1/W2 wave lineage |
| `Settlement_owns_its_http_surface_with_host_route_count_zero` | 10 module routes (5 Seller + 5 Admin), the real route mapper + both `MapGroup` prefixes, no `Host/Settlement`, no `Host/Grid`, 0 `/settlement` literals in Host |
| `Settlement_cqrs_and_validator_matrix_are_exhaustive_and_discoverable` | each of the 10 requests declared exactly once with exactly one `IRequestHandler<,>`; 10 `sender.Send(`; endpoints free of `DbContext`/`ISettlementDirectory`/`ex.Message`; exactly 4 validators with stable `settlement.validation.*` codes; discovery through `AddToobaCqrsFoundation` → `AddValidatorsFromAssembly` → `ValidationBehavior<,>` |
| `Settlement_api_results_localization_and_catalog_are_canonical` | zero `Results.*`/`ProblemDetails`; zero message-text classification; retired mapper absent; the typed seam filtering on `IsKnown`; 17 + 1 declared codes; 17 descriptors registered once; 18 EN + 18 FA resources; resource set registered exactly once; zero ad-hoc logging/telemetry/correlation |
| `Settlement_is_contracts_only_and_microservice_extractable` | every foreign project edge is `*.Contracts`; Endpoints → Application + Contracts only; Application never reaches Infrastructure/Host; zero foreign DbContext; zero `TypeForwardedTo`/global usings; own `settlement` schema + outbox; the single-migration set |
| `Settlement_wave_evidence_and_host_closure_are_preserved` | W0–W3 evidence directories + the W3 verdict tokens; the repository-global Host root checkpoint; the Master Recovery Settlement entry and stop gate |

## 2. Module test suite

```text
dotnet test src/backend/Modules/Settlement/Tooba.Settlement.Tests/Tooba.Settlement.Tests.csproj

Passed!  - Failed: 0, Passed: 30, Skipped: 0, Total: 30, Duration: 501 ms - Tooba.Settlement.Tests.dll (net8.0)
```

Covers `Architecture/SettlementArchitectureGuardTests`,
`Architecture/SettlementValidatorCoverageGuardTests`, `Behavior/SettlementErrorAndGridTests`,
`Endpoints/SettlementEndpointOwnershipTests`, `Validation/SettlementValidatorTests`.

## 3. Error-catalog guard

`ErrorCatalogUniqueCodeGuardTests` → pass (composed production catalog has no duplicate machine code;
the Settlement surface is unchanged by this wave).

## 4. Structure gate + recovery pins — baseline comparison (clean worktree)

```text
--filter "FullyQualifiedName~TmarCompleteReferenceStructureGateTests
         |FullyQualifiedName~TmarDurableGuardTests
         |FullyQualifiedName~ErrorCatalog"

W3 working tree:                     Failed: 3, Passed: 10, Total: 13
clean worktree @ 86ebb4dd (W2):      Failed: 3, Passed: 10, Total: 13
```

Identical failure counts and names in both runs — **pre-existing at the W2 starting head, not
introduced by this wave**:

| Test | Failure | Nature |
|---|---|---|
| `TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment` | the pre-existing `Tooba.Catalog.Contracts.Cart` namespace deviation, documented in that test's own Inventory note as out of scope for a module-local certification | stale Catalog expectation |
| `TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` | repository-global `structureLock.certifiedModules` count vs the manifest's certified-module set | stale SoT snapshot |
| `TmarDurableGuardTests.Recovery_sot_sync_001_current_checkpoint_is_unique_and_stop_is_authoritative` | repository-global recovery checkpoint substring | stale SoT checkpoint |

The third is the repository-global recovery pin region, which a module-local wave is not authorized to
rewrite (it is owned by the repository-global Host/recovery lineage, whose `lastAcceptedTask` is
`TB-TMAR-HOST-ROOT-FINAL-CERT-001` and is asserted unchanged by this wave's guard). This wave's SoT edit
is confined to the Settlement certification block and the three appended `settlementAmsc001W*` lineage
members; it neither widens nor weakens any guard or baseline.

## 5. Build

```text
dotnet build src/backend/Tooba.slnx

Build succeeded.
    0 Error(s)
```

No new warning or error originates from `src/backend/Modules/Settlement`.

## 6. Path ↔ namespace script

```text
node .git/pn-check-settlement.js

production .cs checked: 66
locked exemptions (Migrations/Designer/Snapshot): 3
mismatches: 0
```

## 7. Migration safety proof

```text
git diff bac4dbe3 4ca4aafc -- src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/Persistence
(empty)
git diff 54b1c8ff 4ca4aafc -- .../Persistence/Migrations
(empty)
```

The single `20260827030000_InitialSettlement` migration, its designer and the model snapshot are
byte-identical to the W0 baseline; no migration was regenerated for the structural cleanup.

## 8. SoT / manifest parse check

```text
node -e "JSON.parse(...tmar-module-structure-manifests.json)"  → OK
node -e "JSON.parse(...tmar-current-state.json)"               → OK

modules: 29 entries (Settlement present exactly once, certificationNote present)
preCert: []            uncertified: ["Support","Wallet"]
SoT: settlementAmsc001W3 + settlementAmsc001W0/W1/W2 lineage blocks present
     structureLock.certifiedModules contains Settlement exactly once
     lastAcceptedTask = TB-TMAR-HOST-ROOT-FINAL-CERT-001 (unchanged)
     automaticNextImplementationTask = NONE
```

## 9. Production-change proof

This wave changed **no production file**:

```text
git diff 86ebb4dd -- src/backend/Modules/Settlement
(empty)
```

## 10. Verdict

| Gate | Result |
|---|---|
| Applicability | `HTTP_OWNING` |
| Endpoint ownership | module-owned 10 routes / Host 0 |
| CQRS | MediatR 12.5.0, `ISender` 10/10, real handler 10/10 |
| Validator matrix | `EXHAUSTIVE_4_VALIDATOR_REQUIRED_6_NO_VALIDATOR_REQUIRED`, discoverable + executed |
| API result / error mapping | canonical `ApiResponseFactory`, zero ad-hoc |
| Localization | 17 descriptors + 18 EN/FA resources, registered once |
| Logging / correlation | canonical, no sensitive data, no parallel pipeline |
| Cross-module boundary | `LEGAL_CONTRACTS_ONLY`, zero foreign App/Infra/Domain, zero join |
| Persistence | own `settlement` schema, migrations unchanged |
| Host authority | composition root + 2 security adapters only; ILLEGAL = 0 |
| Host final closure | preserved |
| Structure | `PROFESSIONAL_SHALLOW` / `CANONICAL` / `EXACT` / `CLEAN` / `ENFORCED` |
| Durable guards | W1 12/12, W2 9/9, W3 6/6, module suite 30/30, ErrorCatalog pass |
| New failures vs baseline | 0 |

**`COMPLETE_REFERENCE_PATTERN` / `ARCH-COMPLETE-002 STRUCTURE_CERTIFIED`**
