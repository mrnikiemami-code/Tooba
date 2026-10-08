# TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3-R2 — Fresh Certify (after blocker repair)

- **Task**: `TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3-R2`
- **Parent-Task**: `TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3-R1`
- **Skill**: `tooba-architecture-certify`
- **Mode**: `FRESH_CERTIFY_AFTER_BLOCKER_REPAIR`
- **Channel**: `tooba-main` / worker `tooba-worker-01` / agent `cursor`
- **Starting HEAD**: `e5575d68e72171f8bd09458e2fc91a4432de5cb5` (`main`, `HEAD == origin/main`, clean tree)
- **Lock**: `ARCH-COMPLETE-002`
- **Verdict**: `COMPLETE_REFERENCE_PATTERN`
- **State**: `PRODUCTWORKSPACE_AMSC_001_RECERTIFIED`
- **Production code changed**: `false` (certification-only wave)

## Authorities

| Role | Task | Commit |
| --- | --- | --- |
| Structure | `TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W2` | `592c346e91d22cb5c31fb50f560a049a4883f19e` |
| Blocker repair | `TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3-R1` | `f0500c29d9d97e8761f70ce0036452d50bdeefe8` |
| Superseded certification (historical only) | `TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3` | `c0b86feacd897d26172fef7dff47b93db75d8e9d` |

All three SHAs were re-resolved with `git rev-list --max-count=1 <sha>` during this wave.

## Independent disk verification (re-derived, not assumed)

Every check below was performed against the working tree at the starting HEAD; no check was
copied from the W3 or W3-R1 prose.

| Check | Required | Observed | Result |
| --- | --- | --- | --- |
| Module applicability | `HTTP_OWNING` | 17 mapped routes under `/v1/admin/products` | PASS |
| Module-owned routes | exactly 17 | 17 `Map{Get,Post,Put,Patch,Delete}` in `ProductWorkspaceEndpointModule` | PASS |
| Endpoint-reachable requests | exactly 17 | 17 distinct `public sealed record … : IRequest<…>` | PASS |
| Read/write split | 3 queries + 14 commands | 3 `*Query` + 14 `*Command` (files and declarations agree) | PASS |
| Handler binding | 17 handlers, 1:1 | 17 `IRequestHandler<…>`, zero missing, zero extra | PASS |
| Orphan requests | zero | every declared request dispatched by the endpoints module (`new <Request>(`) | PASS |
| Validator matrix | exhaustive 17 | 0 `VALIDATOR_REQUIRED` + 17 `NO_VALIDATOR_REQUIRED`, each classified once with reason + detail | PASS |
| Module-local validator tree | absent by design | zero `AbstractValidator` / `IValidator<`, no `Validation`/`Validators` folder | PASS |
| Raw API result mappings | zero | zero `Results.Json` / `Results.BadRequest` / `Results.Problem` / `statusCode:` / `Status201Created` | PASS |
| Canonical 201 paths | exactly two | two `api.Created($"/v1/admin/products/{workspace.Value.ProductId}", workspace)` | PASS |
| Structure | `PROFESSIONAL_SHALLOW` / `EXACT` / `CLEAN` / `ENFORCED` | capability-first `Composition/ProductManagement/{Commands,Queries,Models,Grid}`, 5 projects, zero `.gitkeep`, no technical-axis roots | PASS |
| Solution Explorer | canonical | `/Modules/ProductWorkspace/` group holds exactly 5 projects | PASS |
| Contracts-only boundaries | zero foreign App/Infra/Domain edge | foreign edges are `Catalog.Contracts` + `OperatorProfile.Contracts` only | PASS |
| Cross-module join / persistence | zero | no `DbContext`/EF/`using Tooba.Host`/`AddDbContext<` in the module | PASS |
| Schema / migrations | unchanged | `ModuleMigrationRegistry` has no `ProductWorkspace` entry; no `Infrastructure/Persistence` | PASS |
| Host authority | zero business / zero schema | no `Host/Tooba.Host/ProductWorkspace`; composition-root references + route map only | PASS |
| Error descriptor ownership | unique | module declares + localizes the three codes; descriptor registration stays with `CatalogErrorCatalogContributor` | PASS |
| Global Host checkpoint | preserved | `currentHostCheckpoint = HOST_ROOT_FINAL_CERTIFIED`, `lastAcceptedTask = TB-TMAR-HOST-ROOT-FINAL-CERT-001` | PASS |

### Validator matrix re-derivation (why 0 / 17 is correct)

The 17 classifications were re-derived from the request shapes on disk rather than repeated
from the W3-R1 record:

- **14 mutation commands** — the only transport input they own is the body DTO that is handed to
  `ICatalogAdminProductWorkspaceMutationGateway`; the owning Catalog write capability owns the
  transport shape and returns the stable `workspace.*` codes. A local validator would fork that
  canonical code set.
- **`GetProductWorkspaceQuery`** — inputs are the route-constraint `Guid` plus a permission scope
  the endpoint derives server-side from `X-Tooba-Workspace-Scope`; no body shape exists.
- **`ListProductWorkspaceQuery`** — parameterless; no transport input at all.
- **`QueryProductWorkspaceGridQuery`** — the grid envelope is normalized and validated by the
  module's own canonical `AdminProductGridQueryPolicy`, which throws `GridQueryValidationException`
  mapped to a `SemanticError`. A FluentValidation validator here would duplicate that policy.

## Changes in this wave (certification-only)

- `docs/architecture/tmar-current-state.json` — added `productWorkspaceAmsc001W3R2`
  (`state = PRODUCTWORKSPACE_AMSC_001_RECERTIFIED`, `verdict = COMPLETE_REFERENCE_PATTERN`,
  `structureAuthority = W2 @ 592c346e…`, `repairAuthority = W3-R1 @ f0500c29…`,
  `supersededCertification = W3 @ c0b86fea…`, `httpApplicability = HTTP_OWNING`,
  `moduleOwnedRoutes = 17`, `endpointReachableRequests = 17`, `endpointReachableQueries = 3`,
  `endpointReachableCommands = 14`, `validatorCoverageState = EXHAUSTIVE_0_REQUIRED_17_…`,
  `apiResultPatternState = CANONICAL`, `rawResultsState = ZERO`, `microserviceExtractable = true`,
  `productionCodeChanged = false`, `guardsWeakened = NONE`, `baselinesWidened = NONE`,
  `automaticNextImplementationTask = NONE`,
  `workflowStop = USER_REVIEW_PRODUCTWORKSPACE_AMSC_001_W3_R2`). No self-referential SHA is
  recorded; `commit = null` with an explicit `commitState`.
- `docs/architecture/tmar-module-structure-manifests.json` — `modules[ProductWorkspace]`
  `certificationNote` refreshed only: lineage now `… -> W2 592c346e -> W3-R1 f0500c29 -> W3-R2`,
  the validator sentence is now the exhaustive 17-request matrix (replacing the stale
  `0-REQUIRED 0-NO_VALIDATOR_REQUIRED` / "4 queries" phrasing), the canonical API-result sentence
  was added, and the durable-guard list now names the W3-R2 and W3-R1 guards. No project entry and
  no allowlist was added, removed or reordered; the entry stays promoted exactly once with no
  pre-cert duplicate.
- `src/backend/Host/Tooba.Host.Tests/Architecture/ProductWorkspaceModuleAmsc001W3R2CertGuardTests.cs`
  — new durable guard (6 facts) pinning the fresh certification truth: SoT + manifest certification
  state and authorities, exact 17/17/3+14 route/request/handler inventory with 1:1 handler binding
  and zero orphan, the exhaustive matrix bound back to the mapped route literals, zero raw
  `Results.*` with two canonical `api.Created` paths, W2 structure + Contracts-only boundaries on
  disk with resolvable authority SHAs, schema/migration and global Host checkpoint preservation, and
  the R2 evidence + recovery checkpoint.
- `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md` — short R2 fresh-Certify checkpoint appended.
- `docs/architecture/evidence/TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3-R2/` — this certification
  evidence, `validation.md`, the task artifact and the Bridge result artifacts.

### Explicitly not changed

Production code, routes, verbs, DTO shapes, permission predicates, error-code values, resources,
project structure, `.slnx` grouping, allowlists, schema and migrations, Host composition, the
repository-global Host checkpoint, and any other module. No guard was weakened and no baseline was
widened.

## Handoff

- Workflow stop: `USER_REVIEW_PRODUCTWORKSPACE_AMSC_001_W3_R2`.
- `automaticNextImplementationTask = NONE` — no recovery follow-up and no next module is
  self-authorized; the Architect reconciles the final SHA from the Bridge result.
