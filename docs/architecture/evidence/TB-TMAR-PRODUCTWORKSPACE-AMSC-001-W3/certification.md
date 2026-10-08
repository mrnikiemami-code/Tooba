# TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3 — Certify (tooba-architecture-certify)

## Mode

`ARCHITECT_DIRECT_AMSC` — Certify. No production behavior change: this wave records and locks the
ARCH-COMPLETE-002 certification state produced by W0 → W2 and adds the durable certification guard.

Baseline: `branch = main`, `HEAD == origin/main == 592c346e` (W2 Structure).

---

## Verdict

| Field | Value |
|---|---|
| Verdict | `COMPLETE_REFERENCE_PATTERN` |
| Lock | `ARCH-COMPLETE-002` |
| Structure-State | `CERTIFIED` (W2 `READY_FOR_CERTIFY` preserved as historical W2 truth) |
| Module-Applicability | `HTTP_OWNING` |
| Folder-Granularity-State | `PROFESSIONAL_SHALLOW` |
| SolutionExplorer-State | `CANONICAL` (`/Modules/ProductWorkspace/`, exactly 5 projects) |
| PathNamespace-State | `EXACT` |
| RootAllowlist-State | `ENFORCED` |
| PhysicalCopy-State | `CLEAN` |
| File-Cohesion-State | `COHESIVE` |
| Empty-Domain-State | `DECLARED_BOUNDARY_JUSTIFIED` |
| Endpoint-Ownership-State | `MODULE_OWNED` (17 routes) |
| Endpoint-reachable requests | `18` (4 read + 14 write) |
| Validator-Coverage-State | `EXHAUSTIVE_0_REQUIRED_0_NO_VALIDATOR_REQUIRED_BY_CONSTRUCTION` |
| Foreign App/Infra/Domain coupling | `ZERO` |
| Cross-module join | `NONE` |
| Persistence ownership | `CORRECT_NO_PERSISTENCE` |
| Schema/migrations | `UNCHANGED` |
| Blocking residual debt | `ZERO` |
| `microserviceExtractable` | `true` |
| `automaticNextImplementationTask` | `NONE` |

## Wave lineage (accepted)

| Wave | Skill | Commit |
|---|---|---|
| W0 Analyze | `tooba-architecture-analyze` | `56909122` |
| W1 Migrate | `tooba-architecture-migrate` | `e0fe2885` |
| W2 Structure | `tooba-architecture-structure` | `592c346e` |
| W3 Certify | `tooba-architecture-certify` | *(this commit)* |

Each wave is a separate commit **and** a separate push to `origin/main`.

---

## ARCH-COMPLETE-002 certification checklist

| Requirement | Evidence | State |
|---|---|---|
| Application capability folders | `Composition/` holds only the shared `ProductWorkspaceOperation.cs` typed-fault seam; `Composition/ProductManagement/{Commands,Queries,Models,Grid}` is the single visible capability. Root `.cs` = none. No `Commands`/`Queries`/`Models`/`Ports`/`Validators`/`Grid` technical axis at the Application root. | PASS |
| Endpoints capability folders | `ProductWorkspaceEndpointModule.cs` root composition entry + `Admin/` capability (`IProductWorkspaceAdminAuthorizer` + `ProductWorkspaceAdminAuthorizer`). The `/v1/admin/products` capability group is visible in Solution Explorer; root holds the composition entry only. | PASS |
| Infrastructure capability folders | Root composition entry only (`ProductWorkspaceModule.cs`); no capability implementation lives at root because the module owns no persistence and no business service. | PASS |
| Contracts capability folders | `Errors/` (`ProductWorkspaceErrorCodes`, `ProductWorkspaceErrorResourceSet`) + `Resources/` (bilingual `.resx` pair); root holds the module marker only. | PASS |
| Domain capability folders | `Tooba.ProductWorkspace.Domain` is an explicitly justified empty boundary assembly (zero `.cs`, zero `ProjectReference`) — a composition/Admin BFF owns no domain types. | PASS |
| Path ↔ namespace exact equality | all 5 projects, path-derived equality (0 mismatches, no exemption needed) | PASS |
| Root allowlists | Contracts `[ProductWorkspaceContractsMarker.cs]`, Infrastructure `[ProductWorkspaceModule.cs]`, Endpoints `[ProductWorkspaceEndpointModule.cs]`, Application/Domain empty — each equal to disk; forbidden roots absent | PASS |
| CQRS / MediatR | module-local only: 4 read requests + 14 write commands with real `IRequestHandler<,>`; endpoints are thin `ISender`-only delegates | PASS |
| Validator coverage | `EXHAUSTIVE_0_REQUIRED_0_NO_VALIDATOR_REQUIRED_BY_CONSTRUCTION`: transport shape (primitive shape, ids, ranges, lengths) is validated by the Catalog write capability behind the Contracts boundary, which returns the stable `workspace.*` codes; a module-local validator tree would fork that canonical code set, so none was invented to satisfy a count. | PASS |
| API result pattern | `Result`/`Result<T>` + `ApiResponseFactory` (`api.From` / `api.FromFailure`) on every route; the only raw payload is the canonical `Results.Json(..., 201)` variant-create shape; zero `Results.BadRequest` / `Results.Problem` | PASS |
| Stable error-code owner | `ProductWorkspaceErrorCodes` declares `workspace.product.missing`, `workspace.permission.denied`, `catalog.category.assignment.stale` with an ordinal `IsKnown(string?)` declared-code guard; descriptor ownership stays unique with `CatalogErrorCatalogContributor` (404/403/400) — zero duplicate descriptor, zero module contributor (`ErrorCatalogUniqueCodeGuardTests` 3/3 green) | PASS |
| Localization | module-owned `ProductWorkspaceErrorResourceSet` + `Resources/ProductWorkspaceErrors.resx` / `.fa.resx` carry every declared `workspace.*` key in both cultures | PASS |
| Typed faults | `Composition/ProductWorkspaceOperation.cs` is the single typed-fault → `Result` seam: `catch (ContractOperationException ex) when (ProductWorkspaceErrorCodes.IsKnown(ex.Code))` + `catch (SemanticException ex)`; classified by typed code only, never by message text; unknown faults propagate to the global boundary | PASS |
| Logging / telemetry / correlation | canonical — no ad-hoc pipeline, no `Console.WriteLine`, no parallel trace mechanism, no sensitive value logged | PASS |
| Contracts-only boundaries | the only foreign seams are `Tooba.Catalog.Contracts` (`ICatalogAdminProductWorkspaceMutationGateway`) and `Tooba.OperatorProfile.Contracts` (`IActorDisplayLookup`); zero foreign Application/Infrastructure/Domain/Endpoints project edge | PASS |
| No cross-module persistence/join | the module owns no schema, no `DbContext`, no EF usage, no `IQueryable` cross-module join | PASS |
| Schema preservation | 0 migration files touched; the module registers no migration descriptor in `ModuleMigrationRegistry` | PASS |
| Durable guards | W3 cert guard + W2 structure guard (6) + W1 migrate guard (6) + `ErrorCatalogUniqueCodeGuardTests` (3) + the repointed `HostAdminAmcW16/W19/W26/W27/W29/W30/W31` guards | PASS |
| Manifest + SoT | manifest `modules` ProductWorkspace entry `structureCertified: true` with the AMSC-001 note; `preCertModules` empty; `structureLock.certifiedModules` contains `ProductWorkspace` exactly once; AMSC W0→W3 SoT records; Master Recovery module checkpoint | PASS |
| Host closure | no `Host/Tooba.Host/ProductWorkspace` folder, no `Tooba.Host.ProductWorkspace` namespace; Host keeps only the composition root (`Program.cs` DI + route map, `ToobaModuleComposition` module-list entry) | PASS |

---

## Microservice-extractability

`true`. The HTTP surface depends on **Contracts only**:

```text
ProductWorkspace.Endpoints  ->  ProductWorkspace.Application, ProductWorkspace.Contracts,
                                Catalog.Contracts, OperatorProfile.Contracts, BuildingBlocks
```

No `Catalog.Application`, no `Catalog.Domain`, no `Catalog.Infrastructure`, no foreign `DbContext`, no
cross-module join, no schema. The Catalog write capability is reached exclusively through the narrow
Contracts port `ICatalogAdminProductWorkspaceMutationGateway` (15 methods returning `Result`/`Result<Guid>`,
narrow transport DTOs, the acting admin as an explicit stateless parameter), implemented by
`Tooba.Catalog.Infrastructure/Adapters/CatalogAdminProductWorkspaceMutationGateway.cs`, which maps the DTOs
onto the existing Catalog Application commands, dispatches via `ISender` and binds actor attribution
owner-side. `blockingResidualDebt = ZERO`.

---

## Promotion applied

| Location | Before | After |
|---|---|---|
| `tmar-module-structure-manifests.json` | `preCertModules: [ProductWorkspace]`, `structureCertified: false` | `modules += ProductWorkspace`, `structureCertified: true`, `lockVersion: ARCH-COMPLETE-002`; `preCertModules: []` |
| `tmar-module-structure-manifests.json` | — | `uncertifiedHttpOwningModules` unchanged (`Returns`, `Support`, `Wallet`, `Promotion`) |
| `tmar-current-state.json` | `structureLock.certifiedModules` 25 entries | 26 entries (`ProductWorkspace` exactly once) |
| `tmar-current-state.json` | W1 `commit: null` | `e0fe2885` (reconciled); W2 `commit: null` → `592c346e`; new `productWorkspaceAmsc001W3` block |
| `TOOBA-TMAR-MASTER-RECOVERY.md` | W0/W1/W2 checkpoints | W3 Certify checkpoint appended |

The repository-global recovery lock is **preserved exactly**: `currentHostCheckpoint = HOST_ROOT_FINAL_CERTIFIED`,
`lastAcceptedTask = TB-TMAR-HOST-ROOT-FINAL-CERT-001`,
`lastAcceptedCommit = 7a6c353a98a761df9124beb1fce23ed8424230de`,
`workflowStop = USER_REVIEW_HOST_ROOT_FINAL_CERT_001`, `automaticNextImplementationTask = NONE`.

---

## Durable certification guard

`src/backend/Host/Tooba.Host.Tests/Architecture/ProductWorkspaceModuleAmsc001W3CertGuardTests.cs`

- `ProductWorkspace_is_arch_complete_002_certified_in_sot_and_manifest`
- `ProductWorkspace_uses_canonical_result_localization_and_typed_fault_mechanisms`
- `ProductWorkspace_boundaries_stay_contracts_only_and_schema_is_preserved`
- `ProductWorkspace_host_residue_is_composition_only_and_routes_are_preserved`
- `ProductWorkspace_amsc_evidence_and_recovery_checkpoint_exist`
- `ProductWorkspace_amsc_lineage_commits_resolve_and_global_host_closure_is_preserved`

The last fact resolves each recorded wave SHA with `git rev-list`, so an unresolved placeholder can never
pass as a certification.

---

## Validation

```text
dotnet build src/backend/Tooba.slnx                     -> Build succeeded, 0 errors
ProductWorkspaceModuleAmsc001W3CertGuardTests          -> 6 passed / 0 failed
ProductWorkspaceModuleAmsc001W2StructureGuardTests     -> 6 passed / 0 failed
ProductWorkspaceModuleAmsc001W1MigrateGuardTests       -> 6 passed / 0 failed
focused ProductWorkspace* | HostAdminAmcW18 | ErrorCatalogUniqueCodeGuard -> 50 passed / 0 failed
full Tooba.Host.Tests suite                            -> 2078 passed / 79 failed / 130 skipped (2287)
```

The 79 full-suite failures are **byte-identical to the pre-W2 baseline** captured before any file was
moved (compared name-by-name): this wave introduces zero new failures and silently fixes none of the
pre-existing out-of-scope repository debt (Host/Admin evacuation count drift, legacy Catalog domain
guards, repository-global recovery pins).

### Repository-global gate reconciliation (certification-obligatory, not cosmetic)

Promoting a module into `structureLock.certifiedModules` makes three repository-global gates stale by
construction. They were reconciled to the new truth — **no assertion was weakened or deleted**:

| File | Change |
|---|---|
| `TmarCompleteReferenceStructureGateTests` | `ProductWorkspace` added to the expected `modules` array and to the `structureLock.certifiedModules` array in `Uncertified_modules_are_explicitly_not_claimed`, plus a `DoesNotContain(..., uncertified)` claim check. |
| `PricingModuleAmsc001W3R3CertGuardTests` | `ProductWorkspace` added to the `ManifestCertifiedModules` / `LockCertifiedModules` fixtures (both are full-set equality assertions, so they must track the certified set). |
| `HostAdminAmcW18GuardTests` | the W18 `ProductWorkspace_is_not_structure_certified_...` fact was superseded: it now asserts the W18 checkpoint is still recorded **and** that `ProductWorkspace` appears exactly once in `structureLock` and once in the certified `modules` section while being absent from `preCertModules`. |

The pre-existing red `TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment`
(the `Tooba.Catalog.Contracts/Cart` project-level namespace aggregation that pre-dates this module) stays red
and is out of scope for a module-local certification; ProductWorkspace's own exact path↔namespace alignment
is asserted by its W2 guard instead.

---

## Stop

Stop gate `USER_REVIEW_PRODUCTWORKSPACE_AMSC_001_W3`; `automaticNextImplementationTask = NONE`; no next
module. `ProductWorkspace` is certified under `ARCH-COMPLETE-002` and is microservice-extractable.
