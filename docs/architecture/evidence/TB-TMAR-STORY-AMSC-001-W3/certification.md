# TB-TMAR-STORY-AMSC-001 — Wave 3 (Certify)

- **Skill:** `tooba-architecture-certify` (V2)
- **Mode:** `ARCHITECT_DIRECT_AMSC`
- **Target:** `src/backend/Modules/Story/Tooba.Story.*`
- **Starting HEAD:** `4cd9a6cc543ccd307d775dfe703459de7b12c95d` (Wave 2)
- **Branch:** `main`
- **Production files changed by this wave:** **0**
- **Verdict:** `COMPLETE_REFERENCE_PATTERN` / `ARCH-COMPLETE-002` **`STRUCTURE_CERTIFIED`**
- **Structure gate:** W2 `Structure-State = READY_FOR_CERTIFY` (read and required — see §2)

---

## 1. Final physical tree (five projects, 40 production `.cs`)

```text
src/backend/Modules/Story/
├─ Tooba.Story.Contracts/                 (root .cs: 0)
│  ├─ Errors/StoryErrorCodes.cs
│  └─ Tooba.Story.Contracts.csproj
├─ Tooba.Story.Domain/                    (root .cs: 0)
│  ├─ Aggregates/{Story.cs, StoryItem.cs}
│  ├─ Enums/{StoryOrigin.cs, StoryReviewStatus.cs, StoryStatus.cs}
│  ├─ Rules/StoryRules.cs
│  ├─ Tenant/StoryTenantIds.cs
│  └─ Tooba.Story.Domain.csproj
├─ Tooba.Story.Application/               (root .cs: 0)
│  ├─ Composition/StoryOperation.cs
│  ├─ Stories/
│  │  ├─ Commands/Admin/AdminStoryCommands.cs
│  │  ├─ Commands/Seller/SellerStoryCommands.cs
│  │  ├─ Models/StoryModels.cs
│  │  ├─ Ports/{IAdminStoryGridPort.cs, IStoryDirectory.cs}
│  │  ├─ Presentation/StoryPresentationComposer.cs
│  │  ├─ Queries/Admin/AdminStoryQueries.cs
│  │  ├─ Queries/Seller/SellerStoryQueries.cs
│  │  ├─ Queries/Storefront/GetPublicStoriesQuery.cs
│  │  ├─ StoryFailureMapper.cs
│  │  └─ Validators/StoryValidators.cs
│  └─ Tooba.Story.Application.csproj
├─ Tooba.Story.Infrastructure/            (root .cs: 0)
│  ├─ Adapters/AdminStoryGridAdapter.cs
│  ├─ DependencyInjection/StoryModule.cs
│  ├─ Development/StoryDevelopmentSeed.cs
│  ├─ Directories/StoryDirectory.cs
│  ├─ Grid/{AdminStoryGridQueryEngine.cs, StoryAdminGridPolicies.cs}
│  ├─ Messaging/StoryOutboxRegistration.cs
│  ├─ Persistence/{StoryDbContext.cs, Migrations/*}
│  └─ Tooba.Story.Infrastructure.csproj
└─ Tooba.Story.Endpoints/                 (root .cs: 1 allowlisted)
   ├─ Admin/{IStoryAdminAuthorizer.cs, StoryAdminEndpoints.cs}
   ├─ Errors/{StoryErrorCatalogContributor.cs, StoryHttpErrors.cs}
   ├─ Models/StoryHttpModels.cs
   ├─ Resources/{StoryErrorResources.cs, StoryErrors.resx, StoryErrors.fa.resx}
   ├─ Seller/{IStorySellerAuthorizer.cs, StorySellerEndpoints.cs}
   ├─ Storefront/StoryStorefrontEndpoints.cs
   ├─ StoryEndpointModule.cs               (allowlisted composition entry)
   └─ Tooba.Story.Endpoints.csproj
```

## 2. Structure gate (mandatory precondition — satisfied)

`tooba-architecture-structure` was read and its current evidence for this exact surface was required
before any certification check:

| Gate field | Required | Observed (W2) |
| --- | --- | --- |
| `Structure-State` | `READY_FOR_CERTIFY` | `READY_FOR_CERTIFY` ✅ |
| `Folder-Granularity-State` | `PROFESSIONAL_SHALLOW` | `PROFESSIONAL_SHALLOW` ✅ |
| `Solution-Explorer-State` | `CANONICAL` | `CANONICAL` ✅ |
| `Path-Namespace-State` | `EXACT` | `EXACT` (0 mismatches) ✅ |
| `Physical-Copy-State` | `CLEAN` | `CLEAN` ✅ |
| `Root-Allowlist-State` | `ENFORCED` | `ENFORCED` ✅ |
| Unjustified single-file request leaf | none | none ✅ |
| Unjustified technical-axis-first tree | none | none ✅ |
| Structure god-file blocker | none | `WATCH` only, below ceiling ✅ |
| Host final closure | preserved | preserved ✅ |

Evidence: `docs/architecture/evidence/TB-TMAR-STORY-AMSC-001-W2/structure.md`. This certification does
**not** infer or recreate that PASS; it re-checks the invariants as defense in depth (§12, §13).

## 3. Applicability classification

`HTTP_OWNING`. The module ships a real `Tooba.Story.Endpoints` project with a non-empty
`MapStoryModuleEndpoints` composition entry, three non-empty route groups, and Host maps it. Therefore
the normal endpoint-ownership, CQRS, validator and API-mapping requirements all apply.

## 4. Endpoint ownership + route count

| Group | Prefix | Routes |
| --- | --- | --- |
| Admin | `/v1/admin/stories` | 15 |
| Seller | `/v1/seller/stories` | 9 |
| Storefront | `/v1/storefront/stories` | 1 |
| **Total** | | **25** |

Host-owned route count: **ZERO**. Host only calls `app.MapStoryModuleEndpoints();`
(`Program.cs`). No duplicate mapping, no Host route group.

## 5. CQRS / MediatR

- 25 endpoint-reachable requests, all real `IRequest` / `IRequest<T>`.
- 25 real `IRequestHandler<,>` implementations.
- 25 `sender.Send(...)` dispatches — every endpoint goes through `ISender`.
- Zero endpoint direct `DbContext`/`DbSet`/directory call; zero custom dispatcher; zero Host bypass.
- MediatR **12.5** via the canonical `AddToobaCqrsFoundation`; no second pipeline.

## 6. Request → handler → validator matrix (independently re-derived)

The matrix is not adopted from Analyze/Migrate: routes were re-enumerated from the endpoint sources,
each `sender.Send` call was followed to its authoritative `IRequest`/handler, and the transport shape of
every bound value was re-classified.

| Requests | Classification | Validators |
| --- | --- | --- |
| 25 | 16 `VALIDATOR_REQUIRED` + 9 `NO_VALIDATOR_REQUIRED` | 17 `AbstractValidator` classes |

**Set equality holds:** classified requests = 25 = endpoint-reachable requests, each exactly once;
unmapped requests = **0**; every `VALIDATOR_REQUIRED` request has a concrete validator discovered by the
existing `ValidationBehavior`; every `NO_VALIDATOR_REQUIRED` row carries no malformable transport shape.

The 9 exempt rows are structurally safe and independently verifiable:

- `Guid` route ids (existence/ownership belongs to the handler, not transport shape);
- actor ids derived from the authorization seam (`ICurrentUser`), never from client text;
- bodyless state transitions (`Submit`/`Remove`/`Approve`/`Enable`/`Disable`).

The W1 closure of the `GetPublicStoriesQuery` `locale`/`market` provenance gap is included: those
optional query strings are caller-controlled text and are now classified `VALIDATOR_REQUIRED` with a
transport-shape-only validator (`StoryRules.LocaleMaxLength` / `MarketMaxLength`, empty allowed). An
optional input was **not** treated as evidence of safety.

Validators emit stable machine codes, never localized text (`Assert.DoesNotContain("WithMessage(")`).

## 7. Semantic Contracts + folder granularity

- `Tooba.Story.Contracts` holds **error codes only** — no DTO dump, no command-shaped model, no
  `*Contracts.cs` mixed bundle under Application.
- Application-internal models/ports live in `Application/Stories/{Models,Ports}`.
- Exactly one authoritative request type per use case; **no duplicate** command/query shape kept beside
  the real MediatR request.
- Capability is the primary axis (`Stories/{Commands,Queries,Models,Ports,Presentation,Validators}`);
  no technical-axis-first root (`Application/Commands`, `Queries`, `Validators`, `Models`, `Ports`).
- `Commands/{Admin,Seller}` and `Queries/{Admin,Seller,Storefront}` are audience-grouped request axes
  (the canonical precedent used by the certified sibling modules), not use-case-named single-file
  leaves: **no folder named `*Command`/`*Query`/`*Request` wraps a single source file**.

## 8. Path ↔ namespace exactness + alias/shim proof

Recomputed over all five production projects after the W2 moves: **0 mismatches** (Contracts 1/1,
Domain 7/7, Application 12/12, Endpoints 10/10, Infrastructure 10/10 — migrations and generated
designers/snapshots excluded by the canonical rule).

- No `using X = ...;` namespace alias workaround anywhere in the module.
- No `TypeForwardedTo`, no duplicate compatibility type, no stale root copy, no duplicate physical copy.
- `src/backend/Tooba.slnx` → `/Modules/Story/` group holds exactly the five projects, each once.

## 9. File cohesion / no god-file

- No file exceeds the architecture size baseline or `ARCH-SIZE-001`.
- No new god-file / multi-responsibility file (`ARCH-MODULE-FILE-001`).
- No artificial parallel decomposition to game the size guard.
- `Infrastructure/Directories/StoryDirectory.cs` (672 LOC) is a single-responsibility persistence
  directory for one aggregate, below the 800-LOC ceiling → `WATCH` only, no split required.
- Endpoint transport, request/response models, infrastructure and business logic are not collapsed.

## 10. Localization compliance

- Every user-facing message resolves through the canonical `IErrorResourceSet` +
  `IErrorMessageLocalizer` + `.resx` pair owned by the module (`StoryErrorResourceSet` owns the `story.`
  keyspace).
- **10** declared `story.*` validation codes and **5** stable error codes are all present in both
  `StoryErrors.resx` and `StoryErrors.fa.resx`.
- Every stable error code resolves to exactly one `ErrorDescriptor` in the composed
  `IErrorDefinitionCatalog`, owned by `StoryErrorCatalogContributor` — one descriptor owner, no
  first/last-wins, no overwrite, no `DistinctBy`, no catch-and-ignore, no duplicate descriptor.
- Validation codes deliberately travel inside the canonical foundation `validation.failed` envelope
  (`SafeErrorMapper.MapValidation`) and are **not** registered as `ErrorDescriptor` entries — the same
  certified Promotion/Returns precedent. This is not an unregistered-code violation: they are not
  stable semantic error codes.
- No hard-coded user-facing Persian/English text in Domain/Application/Endpoints/Infrastructure; no
  `exception.Message` used as a user-facing contract; no endpoint-level `Accept-Language` parsing.
- No existing key was silently renamed or repurposed.

## 11. Canonical API result / error mapping

- Every endpoint injects `ApiResponseFactory` and returns `api.From(result)` / `api.Created(...)`.
- Zero `Results.Json` / `Results.BadRequest` / `Results.Problem`; zero local `ProblemDetails` builder;
  zero local error mapper; zero `catch`-and-map in endpoints.
- Zero failure classification by parsing `ex.Message` / `Message.Contains`; the typed fault seam
  (`Application/Composition/StoryOperation.cs`) maps `SemanticException` → `Result<T>`.
- Unknown/unexpected exceptions are not silently converted to business failures.
- Success response shape preserved (raw DTO where that is the shipped contract).

## 12. Logging / sensitive data + correlation / trace continuity

- Canonical structured logging only: the module has **zero** `ILogger`, zero `Console.WriteLine`, zero
  `Debug.WriteLine`, zero second telemetry pipeline, zero `ActivitySource`, zero `AsyncLocal`.
- Zero sensitive value logged (no tokens, secrets, `Authorization` headers, cookies, session secrets,
  payment payloads).
- OpenTelemetry and distributed trace propagation preserved; no competing/parallel correlation id, no
  custom header/middleware, no manual `traceparent` parsing; cross-module calls are not broken.

## 13. Cross-module boundary audit (the microservice objective)

`ProjectReference` inventory — **exhaustive**:

| Project | References |
| --- | --- |
| `Tooba.Story.Contracts` | *(none)* |
| `Tooba.Story.Domain` | `Tooba.BuildingBlocks`, `Tooba.Story.Contracts` |
| `Tooba.Story.Application` | `Tooba.BuildingBlocks`, `Tooba.Story.Contracts`, `Tooba.Story.Domain` |
| `Tooba.Story.Endpoints` | `Tooba.BuildingBlocks`, `Tooba.Story.Application`, `Tooba.Story.Contracts` |
| `Tooba.Story.Infrastructure` | `Tooba.BuildingBlocks`, `Tooba.ModuleContracts`, `Tooba.Persistence`, `Tooba.Story.Application`, `Tooba.Story.Contracts`, `Tooba.Story.Domain` |

- Foreign module `Application` / `Infrastructure` / `Domain` / `Endpoints` edge: **ZERO** — at `using`
  **and** `ProjectReference` level.
- Foreign `DbContext` / `DbSet` / repository implementation: **NONE**.
- Cross-module SQL/EF join: **NONE**. Direct table/schema reach-through: **NONE**.
- Shared mutable aggregate: **NONE**.
- No namespace alias and no `TypeForwardedTo` hiding coupling.
- The module never references `Tooba.Host`.

**Microservice extractability: `ARCHITECTURAL_READY`.** Drop `/Modules/Story/` into its own host, keep
the three platform references (`BuildingBlocks`, `ModuleContracts`, `Persistence`), supply a
Contracts-shaped inbound seam for the seller/tenant context, and the module compiles and runs with zero
foreign module dependency.

## 14. Persistence ownership audit

- Exactly one module `DbContext`: `StoryDbContext` with `Schema = "story"`, tables `stories`,
  `story_items`, `outbox_messages`.
- Migrations owned by the module: `20260827070104_InitialStory` and
  `20260827080717_AddStoryReviewOwnership` (plus the model snapshot).
- Zero cross-module FK; zero foreign `DbSet`; zero foreign schema read.
- Application and Endpoints contain zero `DbContext`/`DbSet` access. `ARCH-DATA-001` intact.

## 15. Migration / schema safety

No migration was added, regenerated, reordered or edited by the AMSC wave line. `Up`/`Down`
semantics, snapshot semantics, tables, columns, indexes, constraints and transaction behavior are
unchanged. No migration was regenerated for structural cleanup.

## 16. Host authority audit

| Class | Observed |
| --- | --- |
| `ALLOWED_COMPOSITION_ROOT` | `Composition/ToobaModuleComposition.cs` (`new StoryModule()`, one `using Tooba.Story.Infrastructure.DependencyInjection;`), `Program.cs` (`app.MapStoryModuleEndpoints();`), development seed/migrator lines |
| `ALLOWED_SECURITY_ADAPTER` | `Host/Security/Seller/HostStorySellerAuthorizer.cs` (platform adapter implementing the module's `IStorySellerAuthorizer` seam) |
| `ALLOWED_CONTRACT_CONSUMPTION` | none beyond the above |
| `ILLEGAL_BUSINESS_AUTHORITY` | **ZERO** |
| `ILLEGAL_PERSISTENCE_AUTHORITY` | **ZERO** |
| `ILLEGAL_ENDPOINT_OWNERSHIP` | **ZERO** |

`src/backend/Host/Tooba.Host/Story` remains **ABSENT**; zero `Story*.cs` under Host. No Host business
rule, endpoint, DbContext, policy or composer was added.

## 17. Closed-folder regression audit + post-Host-final-closure guard

- No previously closed/non-active folder received a production file in this wave.
- No new Host production folder or source file was created.
- `HOST_ROOT_FINAL_CERTIFIED` / `HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED` not regressed:
  `hostIllegalAuthorityState = ZERO`, Host folder ABSENT, no module business logic reintroduced under
  Host, no baseline widened and no allowlist exemption added.

## 18. Durable guards

**Added** `StoryModuleAmsc001W3CertGuardTests` (8 facts):

1. `Story_is_arch_complete_002_certified_in_sot_and_manifest`
2. `Story_amsc_wave_lineage_is_recorded_and_chained` (includes real `git merge-base` ancestry proof)
3. `Story_http_cqrs_and_validator_matrix_match_the_certified_truth`
4. `Story_w2_structure_verdict_is_preserved_on_disk`
5. `Story_contracts_only_boundary_makes_microservice_extraction_possible`
6. `Story_uses_canonical_mechanisms_with_no_parallel_invention`
7. `Story_host_closure_is_preserved`
8. `Story_amsc_evidence_tree_is_present`

Together with `StoryModuleAmsc001W1MigrateGuardTests`, `StoryModuleAmsc001W2StructureGuardTests`,
`StoryModuleAmcW1/W3/W4/W5/W6` and `HostStoryAmc`, the certified structure is locked. **No guard was
weakened and no baseline was widened** to reach this verdict.

## 19. Manifest state

`docs/architecture/tmar-module-structure-manifests.json` → `Story`:

- `structureCertified: true`, `lockVersion: "ARCH-COMPLETE-002"` (unchanged).
- `certificationNote` now records the AMSC-001 certification truth (verdict, applicability, route/CQRS/
  validator counts, structure, canonical mechanisms, boundaries, extractability, Host closure, guards,
  stop gate, evidence path). The AMC-001-W6 history is retained inside the same note.
- Root allowlists: empty on Contracts/Domain/Application/Infrastructure; `StoryEndpointModule.cs` on
  Endpoints.
- Exactly one certified entry for `Story`; no pre-cert duplicate.

## 20. SoT state

`docs/architecture/tmar-current-state.json`:

- New `storyAmsc001W3` record: `STORY_AMSC_001_CERTIFIED` / `COMPLETE_REFERENCE_PATTERN` /
  `structureState = CERTIFIED` / `blockingResidualDebt = ZERO`.
- `storyAmsc001W2.commit` stamped to the real W2 SHA `4cd9a6cc…` (was `PENDING_W2_COMMIT`), so the
  recorded lineage `W0 0c73390a → W1 2a09e7bb → W2 4cd9a6cc → W3 this commit` is a verifiable ancestry
  chain.
- `structureLock.certifiedModules` now lists `Story` once (29 entries).
- All historical AMC records, `completeReferenceModules` (12) and the repository-global Host root
  checkpoint are untouched.

## 21. Focused builds / tests

```text
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj   -> 0 errors

dotnet test --filter "StoryModuleAmsc001W3CertGuardTests|StoryModuleAmsc001W2StructureGuardTests|
                      StoryModuleAmsc001W1MigrateGuardTests|StoryModuleAmcW1|StoryModuleAmcW3|
                      StoryModuleAmcW4|StoryModuleAmcW5|StoryModuleAmcW6|HostStoryAmc|StoryFoundation"
  -> every Story-owned fact passes
```

`ErrorCatalogUniqueCodeGuardTests` continues to pass, confirming the composed catalog has no duplicate
machine-code descriptor.

## 22. Declared pre-existing, unrelated red guards (NOT Story debt)

These are red at the **untouched W2 HEAD**, proven by running them against a clean `git worktree` at
`4cd9a6cc` with no wave changes applied. They are deliberately **not** repaired by this module-local
wave (no unrelated-file edits):

| Guard | Cause |
| --- | --- |
| `HostGridAmcR3GuardTests` | Catalog composition-entry path drift (moved by a later Catalog task) |
| `HostGridAmcR4GuardTests` | Party contracts path drift (`AdminSellersGridContracts.cs` → `Ports/`) |
| `HostGridAmcR5R1GuardTests` | Catalog contracts path drift + a repository-global `lastAcceptedTask` pin that has legitimately advanced |
| `TmarDurableGuardTests` (`certifiedModules` literal) | literal list frozen at 16 entries while the SoT legitimately grew to 28–29 |
| `TmarCompleteReferenceStructureGateTests` (`certifiedModules` literal) | same frozen-literal drift |
| `TmarSourceSizeAndInfraAppTests` | stale git-ignored `.tmp-baseline` worktree (environment) |

No Story-owned fact fails.

## 23. Residual debt

- **Blocking: ZERO.**
- Non-blocking: `Infrastructure/Directories/StoryDirectory.cs` 672 LOC stays `WATCH` (single
  responsibility, below the 800-LOC ceiling).

## 24. Verdict

```text
VERDICT: COMPLETE_REFERENCE_PATTERN
         ARCH-COMPLETE-002 STRUCTURE_CERTIFIED
```

Ownership is correct, foundation usage is canonical, structure is capability-first shallow with exact
path↔namespace alignment and no stale copy, endpoints are module-owned (25 routes, Host ZERO), CQRS is
real MediatR dispatched through `ISender`, the validator matrix is exhaustive and set-equal, Contracts
ownership is semantic and clean, localization/API-result/logging/telemetry/correlation are canonical
with no parallel invention, persistence is single-context/schema-preserved, cross-module coupling and
joins are zero, Host authority is limited to the allowed composition root and security adapter, durable
guards pass and the SoT/manifest are honest.

The module is cleanly extractable as an independent microservice.

```text
Structure-State      = CERTIFIED
Applicability        = HTTP_OWNING
Endpoint-Ownership   = MODULE_OWNED (25 routes, Host ZERO)
Path-Namespace       = EXACT
Root-Allowlist       = ENFORCED
Cross-Module Coupling= ZERO
Host-Final-Closure   = PRESERVED
Stop gate            = USER_REVIEW_STORY_AMSC_001_W3
automaticNextImplementationTask = NONE
```
