# TB-TMAR-RETURNS-AMSC-001 — Wave 3 (Certify)

Skill: `tooba-architecture-certify` (V2) · Wave 3 (final) of the AMSC re-standardization of
`src/backend/Modules/Returns`.

Baseline: `main` @ `6cab1b87` (W2 Structure; `HEAD == origin/main` at wave start).
Wave lineage: W0 `f5c5a6db` Analyze → W1 `0a573864` Migrate → W2 `6cab1b87` Structure → **W3 (this wave)**.

Structure gate consumed (mandatory): `docs/architecture/evidence/TB-TMAR-RETURNS-AMSC-001-W2/structure.md`

```text
Structure-State          = READY_FOR_CERTIFY
Folder-Granularity-State = PROFESSIONAL_SHALLOW
Solution-Explorer-State  = CANONICAL
Path-Namespace-State     = EXACT
Physical-Copy-State      = CLEAN
Root-Allowlist-State     = ENFORCED
Host final closure       = PRESERVED
```

The Structure PASS is current, scoped to this exact surface (`src/backend/Modules/Returns`) and not
contradicted by current disk state; Certify did **not** infer it from compilation, manifest membership
or spot checks. Certify re-checked the structural invariants as defense in depth (see
`structural-recheck.md`) and found no regression.

## FINAL VERDICT

```text
COMPLETE_REFERENCE_PATTERN
ARCH-COMPLETE-002 STRUCTURE_CERTIFIED
```

- `microserviceExtractable` = `ARCHITECTURAL_READY_RUNTIME_PENDING_MICROSERVICE_EXTRACTION`
- Blocking residual debt: `ZERO`
- This wave changed **no production file**. Guards weakened: NONE. Baselines widened: NONE.

---

## 1. Applicability classification (0b)

`HTTP_OWNING` — proven, not assumed: 11 real module routes, a real route mapper
(`MapReturnEndpoints()`), and Host owning zero Returns routes. Therefore the endpoint ownership,
CQRS and validator requirements apply normally.

## 2. Touched-surface certification (0a)

The active surface is the whole Returns module (all 71 production `.cs` plus the module's csproj
graph and endpoint composition). Every production file was re-read against the touched-surface
checklist via the durable guards and the checks below.

## 3. Endpoint ownership (4)

| Audience | File | Routes |
|---|---|---|
| Customer | `Endpoints/Customer/ReturnCustomerEndpoints.cs` | `GET /v1/customer/returns`, `GET /v1/customer/returns/{returnRequestId:guid}`, `POST /v1/customer/returns` |
| Seller | `Endpoints/Seller/ReturnSellerEndpoints.cs` | `GET /v1/seller/returns`, `GET /v1/seller/returns/{returnRequestId:guid}`, `POST .../approve`, `POST .../reject` |
| Admin | `Endpoints/Admin/ReturnAdminEndpoints.cs` | `GET /v1/admin/returns`, `POST /v1/admin/returns/query`, `GET /v1/admin/returns/{returnRequestId:guid}`, `POST .../retry-refund` |

- Module-owned routes: **11**
- Host-owned Returns routes: **0** (no `/returns` literal anywhere under `Host/Tooba.Host`; no
  `Host/Returns` folder; only `MapReturnEndpoints()` composition + the two authorizer adapters)
- Duplicate mapping: none (one `MapGroup` per audience, one composition entry)
- Host role: legitimate composition/security adaptation only

## 4. CQRS / MediatR (5)

MediatR 12.5.0. All 11 endpoint-reachable requests are `IRequest<Result<…>>` with a real
`IRequestHandler<,>`, dispatched by `ISender` (11/11 `sender.Send(` call sites; no endpoint direct
persistence/directory call, no Host bypass, no custom dispatcher).

See `request-handler-validator-matrix.md` for the full inventory.

## 5. Validator coverage (6 + 6a hard blocker)

Independent input-provenance matrix, re-derived from shipped routes and actual `ISender.Send` call
sites — not adopted from W0/W1:

```text
EXHAUSTIVE_4_VALIDATOR_REQUIRED_7_NO_VALIDATOR_REQUIRED
```

- **VALIDATOR_REQUIRED (4)**: `CreateReturnCommand`, `ApproveReturnCommand`, `RejectReturnCommand`,
  `QueryAdminReturnsGridQuery` — each has a concrete discoverable validator in
  `Application/Validation/ReturnsRequestValidators.cs` using stable machine codes only (no
  `WithMessage(`).
- **NO_VALIDATOR_REQUIRED (7)**: `ListCustomerReturnsQuery`, `GetCustomerReturnQuery`,
  `ListSellerReturnsQuery`, `GetSellerReturnQuery`, `ListAdminReturnsQuery`, `GetAdminReturnQuery`,
  `RetryReturnRefundCommand` — route-constrained `:guid` identifier and/or server-derived actor/seller
  party. Optional input was **not** treated as evidence of safety.
- Set equality: 11 shipped routes = 11 endpoint-reachable requests = 4 + 7 classified exactly once.
- Discovery: `AddToobaCqrsFoundation(typeof(...Returns.Application.ReturnRequests.Commands.CreateReturnCommand).Assembly)`
  runs `AddValidatorsFromAssembly` over the Returns Application assembly and registers
  `ValidationBehavior<,>`; invalid input surfaces through the canonical `SafeErrorMapper` validation
  path with the stable `returns.validation.*` codes.

## 6. Localization (7)

- `ReturnsErrorResourceSet` (`IErrorResourceSet`) owns the `return.` / `refund.` keyspace and is
  registered **exactly once** by `ReturnEndpointModule.AddReturnEndpointPresentation`.
- Bilingual `Contracts/Resources/ReturnsErrors.resx` + `ReturnsErrors.fa.resx`: **21 EN + 21 FA**
  entries, one per declared code.
- Every stable code used by the certified surface resolves to exactly one `ErrorDescriptor` in the
  composed `IErrorDefinitionCatalog`; the 20 HTTP-reachable codes are each registered exactly once by
  `ReturnsErrorCatalogContributor`; the platform fault (`return.outbox.unmapped_event`) is deliberately
  never catalogued.
- `customer.session.required` remains Foundation-owned and is consumed **without** re-registration.
- No hard-coded user-facing error prose in Domain/Application/Endpoints/Infrastructure; no
  `ex.Message` used as a user-facing contract; no endpoint-level `Accept-Language` parsing.
- Accepted display-label composition (recorded honestly, matching the certified Fulfillment/Catalog/
  Promotion work-queue convention): `AdminReturnQueueFilters.ComposeEligibilitySummary` and the
  `AdminReturnGridQueryEngine` display-label fallbacks. These are not error text and not part of the
  stable localization contract; `AdminReturnWorkQueueTests.Eligibility_summary_uses_snapshot_not_current_offer`
  locks the behavior.

## 7. Canonical API result / error mapping (8)

- Every endpoint maps outcomes through `ApiResponseFactory.From` / `FromFailure`.
- `Results.Json` / `Results.BadRequest` / `Results.Problem` / local `ProblemDetails` builder: **0**.
- Catch-and-map blocks in endpoints for expected failures: **0**.
- Failure classification by message text: **0** (typed code only, via
  `ReturnsOperation` → `ReturnsErrorCodes.IsKnown`).
- Unknown/unexpected exceptions are **not** converted to business failures — they propagate to the
  canonical global boundary.
- No duplicate-suppression mechanism, no shared-errors layer, no unresolved conflicting descriptor
  owner.

## 8. Logging / sensitive data (9)

`ILogger<T>` structured logging + `ObservabilityLogScope` only. `Console.WriteLine` /
`Debug.WriteLine`: 0. No second telemetry pipeline. No secret/credential/authorization-header/cookie/
payment-payload logging.

## 9. OpenTelemetry / correlation (10)

OpenTelemetry integration and `TracingBehavior<,>` pipeline order preserved; no competing correlation
ID, no `ActivitySource.StartActivity(...)` in Application/Endpoints, no manual `traceparent` parsing.
Cross-module calls continue to use Contracts ports; trace continuity is not broken. W2 changed no
telemetry code, and W3 changed no production code.

## 10. Cross-module boundary audit (11)

`LEGAL_CONTRACTS_ONLY`. The only foreign edges are seven `*.Contracts` project references
(Order, Fulfillment, Payment, Wallet, Inventory, Party, Catalog) consumed from
`Tooba.Returns.Infrastructure`, plus `Tooba.BuildingBlocks` / `Tooba.Persistence` /
`Tooba.ModuleContracts` platform references.

- Foreign `*.Application` / `*.Infrastructure` / `*.Domain` edges: **ZERO**
- Foreign `DbContext` / `DbSet` reach-through: **ZERO**
- Cross-module SQL/EF joins: **ZERO**
- `Endpoints` → `Returns.Application` only (never `Returns.Infrastructure`, never Host)
- `TypeForwardedTo` / namespace-alias workarounds: **ZERO**

## 11. Persistence ownership (12)

One module DbContext (`ReturnsDbContext`) owning the `returns` schema
(`HasDefaultSchema(Schema)`, tables `return_requests` / `return_items` / `refund_attempts`), module-owned
migrations, no cross-module FK, no Application/Endpoints DbContext access, `ARCH-DATA-001` intact.

## 12. Host authority audit (13)

| Host artifact | Classification |
|---|---|
| `Program.cs` (`AddReturnEndpointPresentation`, `MapReturnEndpoints()`, CQRS assembly) | `ALLOWED_COMPOSITION_ROOT` |
| `Security/Seller/HostReturnSellerAuthorizer.cs` | `ALLOWED_SECURITY_ADAPTER` |
| `Admin/Access/Authorizers/HostReturnAdminAuthorizer.cs` | `ALLOWED_SECURITY_ADAPTER` |
| `Composition/ToobaModuleComposition.cs` (`new ReturnsModule()`) | `ALLOWED_COMPOSITION_ROOT` |
| `Tooba.Host.csproj` project references | `ALLOWED_COMPOSITION_ROOT` |

`ILLEGAL_BUSINESS_AUTHORITY` / `ILLEGAL_PERSISTENCE_AUTHORITY` / `ILLEGAL_ENDPOINT_OWNERSHIP`: **ZERO**.
No `Host/Returns`, no `Host/Grid`, no Returns DbContext in Host.

## 13. Closed-folder regression audit (13b)

No previously closed/non-active folder received production code. `Host/Returns` remains absent. No new
Host production folder or file was created. `SINK_FOLDER_REGRESSION`: none.

## 14. Persistence / migration safety (14)

`UNCHANGED`. Three migration IDs, order, Up/Down and snapshot semantics untouched; `git diff f5c5a6db 0a573864`
on `Persistence/` is empty, and W2/W3 touched no production file. No migration was regenerated for
structural cleanup.

## 15. Durable structure guard (15)

| Guard | Scope |
|---|---|
| `ReturnsModuleAmsc001W1MigrateGuardTests` (12) | single code home + reachability split, typed seam, destination parser, bilingual coverage, 4-validator matrix, capability-first layout, retired duplicates/dead mapper, Contracts-only, Foundation session code, unchanged migrations |
| `ReturnsModuleAmsc001W2StructureGuardTests` (7) | capability-first shallow layout, no single-file request leaves, exact path↔namespace, manifest↔disk root allowlists, certified-manifest honesty, `/Modules/Returns/` grouping, no stale/duplicate copies |
| `ReturnsModuleAmsc001W3CertGuardTests` (6, this wave) | SoT/manifest certification records + wave lineage, Host route count zero, CQRS/validator matrix + discovery path, canonical result/localization/catalog/logging, Contracts-only microservice boundary + unchanged schema, wave evidence + Host closure preserved |
| `Tooba.Returns.Tests/Architecture/ReturnsArchitectureGuardTests` | module architecture invariants |
| `ErrorCatalogUniqueCodeGuardTests` | composed production catalog uniqueness (no duplicate machine code) |

Guard repoints required by the promotion (no assertion weakened, no baseline widened — same precedent as the
`Promotion` certification at `6bf74745`): the three repository-global certified-module pin lists in
`TmarCompleteReferenceStructureGateTests` were extended to include `Returns` (certified-module set,
uncertified-exclusion set, and the SoT `structureLock.certifiedModules` expectation), and the
`ReturnsModuleAmsc001W2StructureGuardTests` manifest lookup was repointed from `preCertModules` to the
certified `modules[]` array. No guard was weakened and no baseline was widened.

## 16. Manifest promotion (16)

`docs/architecture/tmar-module-structure-manifests.json`: the `Returns` entry was promoted from
`preCertModules` into the certified `modules[]` array with `structureCertified: true`,
`lockVersion: ARCH-COMPLETE-002`, and the unchanged per-project `rootAllowlist` /
`forbiddenRootFiles` / `forbiddenTopLevelFolders`. Exactly one certified entry exists for Returns;
`preCertModules` is now empty; no temporary pre-cert duplicate remains.

## 17. Recovery SoT (17)

`docs/architecture/tmar-current-state.json`:

- `structureLock.certifiedModules` now includes `Returns` (28 entries).
- New `returnsAmsc001W3` block recording the verdict, applicability, route/request counts, validator
  matrix, path/namespace, root allowlist, alias state, cohesion, localization, API mapping, logging,
  correlation, Host residue/closure, cross-module boundary, schema state, microservice extractability,
  manifest state, evidence and stop gate.
- `returnsAmsc001W0` / `returnsAmsc001W1` / `returnsAmsc001W2` lineage blocks with their commits.
- The edit is surgical: one list insertion plus three appended top-level members; no unrelated
  history rewritten (see `validation.md` for the diff proof).

`docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md`: appended the module-local Returns AMSC W3 recovery
checkpoint.

## 18. Focused validation (18)

See `validation.md`. All required guards pass. Two pre-existing `TmarDurableGuardTests` recovery-pin
failures are reproduced byte-identically at the wave starting head and are outside this module-local
wave's authorized scope.

## 19. Residual non-blocking debt

| Item | Classification |
|---|---|
| `Infrastructure/Directories/ReturnDirectory.cs` (384 LOC), `Queries/AdminReturnGridQueryEngine.cs` (288), `Evaluators/ReturnEligibilityEvaluator.cs` (247) | `OVERSIZED_ONLY` — single-responsibility cohesive; WATCH only, no structure blocker |
| Admin grid display-label Persian fallbacks | Accepted display-label composition, matches certified work-queue convention |
| Runtime microservice extraction (separate deployment, own DB instance, transport split) | Out of scope for an architecture certification; architecture is extractable-ready |

No violation from the Certification Result list remains: `RAW_RESULTS`, `AD_HOC`,
`PARALLEL_MAPPER`, `UNREGISTERED_CODES`, `DUPLICATE_ERROR_DESCRIPTOR`, `UNRESOLVED_ERROR_OWNER`,
`HARDCODED_TEXT`, `NON_STANDARD`, `DUPLICATE_TELEMETRY`, `SECOND_PIPELINE`, `PARALLEL_CORRELATION`,
`LOST_PROPAGATION`, `VIOLATION`, `ILLEGAL`, `FOREIGN_ACCESS`, foreign App/Infra/Domain dependency,
cross-module join, path/namespace mismatch, stale physical copy, missing solution grouping,
cohesion/root-dump violation, semantic Contracts/Application ownership violation, single-file request
folder explosion, duplicate CQRS request shape, duplicate/legacy type — all **ZERO**.

## 20. Post-Host-final-closure guard (580)

`HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED` and `HOST_ROOT_FINAL_CERTIFIED` are preserved. No new
Host production folder/file, no reintroduced module business logic, endpoint, policy, worker,
repository, DbContext or module-specific adapter under Host. `HOST_FINAL_CLOSURE_REGRESSION` not
triggered.

## 21. Microservice extractability statement

Returns can be lifted into an independent service without touching other modules:

- owns its HTTP surface, CQRS layer, Domain, persistence (`returns` schema), migrations, outbox
  registration and observability;
- its Contracts project is the only inbound boundary and carries zero foreign module type in any
  signature;
- its outbound dependencies are seven `*.Contracts` ports, each replaceable by an HTTP/gRPC client
  without changing Application/Domain;
- no foreign Application/Infrastructure/Domain reference, no foreign DbContext, no cross-module join;
- its localization resources, error codes and catalog contribution are module-owned and registered
  once, so extraction leaves no copy behind.

The only remaining work for a real deployment is runtime/infrastructure (host process, connection
string, transport), not architecture.

## 22. Wave disposition

`COMPLETE_REFERENCE_PATTERN` / `ARCH-COMPLETE-002 STRUCTURE_CERTIFIED`.
Stop gate `USER_REVIEW_RETURNS_AMSC_001_W3`; `automaticNextImplementationTask = NONE`.
Do not self-authorize the next Architect task.
