# TB-TMAR-SETTLEMENT-AMSC-001 — Wave 3 (Certify)

Skill: `tooba-architecture-certify` (V2) · Wave 3 (final) of the AMSC re-standardization of
`src/backend/Modules/Settlement`.

Baseline: `main` @ `86ebb4dd` (W2 Structure; `HEAD == origin/main` at wave start).
Wave lineage: W0 `bac4dbe3` Analyze → W1 `4ca4aafc` Migrate → W2 `86ebb4dd` Structure → **W3 (this wave)**.

Structure gate consumed (mandatory):
`docs/architecture/evidence/TB-TMAR-SETTLEMENT-AMSC-001-W2/structure.md`

```text
Structure-State          = READY_FOR_CERTIFY
Folder-Granularity-State = PROFESSIONAL_SHALLOW
Solution-Explorer-State  = CANONICAL
Path-Namespace-State     = EXACT
Physical-Copy-State      = CLEAN
Root-Allowlist-State     = ENFORCED
Host final closure       = PRESERVED
```

The Structure PASS is current, scoped to this exact surface (`src/backend/Modules/Settlement`) and not
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

`HTTP_OWNING` — proven, not assumed: 10 real module routes (5 Seller + 5 Admin), a real route mapper
(`MapSettlementEndpoints()`), and Host owning zero Settlement routes. Therefore the endpoint ownership,
CQRS and validator requirements apply normally.

## 2. Touched-surface certification (0a)

The active surface is the whole Settlement module (all 66 production `.cs` plus the module's csproj
graph and endpoint composition). Every production file was re-read against the touched-surface
checklist via the durable guards and the checks below.

## 3. Endpoint ownership (4)

| Audience | File | Routes |
|---|---|---|
| Seller | `Endpoints/Seller/SettlementSellerEndpoints.cs` | `GET /v1/seller/settlements/settlement/balance`, `GET …/entries`, `GET …/statements`, `GET …/payout-requests`, `POST …/payout-requests` |
| Admin | `Endpoints/Admin/SettlementAdminEndpoints.cs` | `GET /v1/admin/settlements/settlement/balances`, `GET …/payout-queue`, `POST …/payout-queue/query`, `POST …/payout-requests/{payoutRequestId:guid}/process`, `POST …/payout-requests/{payoutRequestId:guid}/retry` |

- Module-owned routes: **10** (5 Seller + 5 Admin)
- Host-owned Settlement routes: **0** (no `/settlement` literal anywhere under `Host/Tooba.Host`; no
  `Host/Settlement` folder; no `Host/Grid` folder; only `MapSettlementEndpoints()` composition +
  `AddSettlementEndpointPresentation()` + the two authorizer adapters)
- Duplicate mapping: none (one `MapGroup` per audience, one composition entry)
- Host role: legitimate composition/security adaptation only

## 4. CQRS / MediatR (5)

MediatR 12.5.0. All 10 endpoint-reachable requests are `IRequest<Result<…>>` with a real
`IRequestHandler<,>` declared exactly once, dispatched by `ISender` (10/10 `sender.Send(` call sites; no
endpoint direct persistence/directory call — 0 occurrences of `DbContext` / `ISettlementDirectory` in
the endpoint files; no Host bypass; no custom dispatcher).

See `request-handler-validator-matrix.md` for the full inventory.

## 5. Validator coverage (6 + 6a hard blocker)

Independent input-provenance matrix, re-derived from shipped routes and actual `ISender.Send` call
sites — not adopted from W0/W1:

```text
EXHAUSTIVE_4_VALIDATOR_REQUIRED_6_NO_VALIDATOR_REQUIRED
```

- **VALIDATOR_REQUIRED (4)**: `RequestSellerPayoutCommand`, `ProcessAdminPayoutCommand`,
  `RetryAdminPayoutCommand`, `QueryAdminPayoutGridQuery` — each has a concrete discoverable validator in
  `Application/Validation/SettlementRequestValidators.cs` using stable machine codes only (no
  `WithMessage(`; every literal is `settlement.validation.*`).
- **NO_VALIDATOR_REQUIRED (6)**: `GetSellerSettlementBalanceQuery`, `ListSellerSettlementEntriesQuery`,
  `ListSellerSettlementStatementsQuery`, `ListSellerPayoutRequestsQuery` (4 × `AUTH_SCOPED_QUERY` — the
  seller party is server-derived through the module `ISettlementSellerAuthorizer` seam, and the request
  carries only that server-derived `Guid SellerPartyId`; zero client-shaped body input) and
  `ListAdminSettlementBalancesQuery`, `ListAdminPayoutQueueQuery` (2 × `NO_INPUT` — parameterless
  requests, no bound input at all). Optional input was **not** treated as evidence of safety.
- Set equality: 10 shipped routes = 10 endpoint-reachable requests = 4 + 6 classified exactly once.
- Discovery: `AddToobaCqrsFoundation(typeof(...Settlement.Application.Payouts.Queries.GetSellerSettlementBalanceQuery).Assembly)`
  runs `AddValidatorsFromAssembly` over the Settlement Application assembly and registers
  `ValidationBehavior<,>` (verified in `src/backend/BuildingBlocks/Tooba.BuildingBlocks/TmarFoundation.cs`).
- The grid request is additionally guarded by the module-owned `AdminPayoutGridQueryPolicy` executed
  inside the handler, which keeps the grid whitelist/normalization authority; the transport validator
  validates only the client-shaped body.

## 6. Localization (7)

- `SettlementErrorResourceSet` (`IErrorResourceSet`) owns the `settlement.` / `payout.` keyspace and is
  registered **exactly once** by `SettlementEndpointModule.AddSettlementEndpointPresentation`.
- Bilingual `Contracts/Resources/SettlementErrors.resx` + `SettlementErrors.fa.resx`: **18 EN + 18 FA**
  entries, one per declared code.
- Every stable code used by the certified surface resolves to exactly one `ErrorDescriptor` in the
  composed `IErrorDefinitionCatalog`; the 17 HTTP-reachable codes are each registered exactly once by
  `SettlementErrorCatalogContributor`; the platform fault (`settlement.outbox.unmapped_event`) is
  deliberately never catalogued.
- Foundation-owned cross-cutting codes (`customer.session.required`, `seller.authorization.denied`,
  `admin.authorization.denied`) are consumed **without** re-registration.
- No hard-coded user-facing error prose in Domain/Application/Endpoints/Infrastructure; no `ex.Message`
  used as a user-facing contract; no endpoint-level `Accept-Language` parsing.
- Accepted display-label composition (recorded honestly, matching the certified
  Returns/Fulfillment/Catalog/Promotion work-queue convention): `SettlementDisplayLabels.UnknownSeller`
  (`"فروشنده"`, consumed by `ListAdminSettlementBalancesQuery` and `AdminPayoutGridQueryEngine`) is a
  fallback label for optional Party display data — not error text and not part of the stable
  localization contract.

## 7. Canonical API result / error mapping (8)

- Every endpoint maps outcomes through `ApiResponseFactory.From`.
- `Results.Json` / `Results.BadRequest` / `Results.Problem` / local `ProblemDetails` builder: **0**.
- Catch-and-map blocks in endpoints for expected failures: **0**.
- Failure classification by message text: **0** (typed code only, via `SettlementOperation` →
  `SettlementErrorCodes.IsKnown`).
- Unknown/unexpected exceptions are **not** converted to business failures — they propagate to the
  canonical global boundary.
- No duplicate-suppression mechanism, no shared-errors layer, no unresolved conflicting descriptor
  owner.

## 8. Logging / sensitive data (9)

`ILogger<T>` structured logging + `ObservabilityLogScope` only. `Console.WriteLine` /
`Debug.WriteLine`: 0. No second telemetry pipeline. No secret/credential/authorization-header/cookie/
payment-payload logging.

## 9. OpenTelemetry / correlation (10)

OpenTelemetry integration and the canonical `TracingBehavior<,>` pipeline order preserved; no competing
correlation ID, no `ActivitySource.StartActivity(...)` in Application/Endpoints, no manual
`traceparent` parsing, no second `Meter` (`SettlementInstrumentation` runs on the single
`ToobaTelemetry.Meter` with the unchanged counter names `tooba.settlement.entry.posted`,
`tooba.settlement.payout.succeeded`, `tooba.settlement.payout.failed`). Cross-module calls continue to
use Contracts ports; trace continuity is not broken. W2/W3 changed no telemetry code.

## 10. Cross-module boundary audit (11)

`LEGAL_CONTRACTS_ONLY`. The only foreign edges are four `*.Contracts` project references
(Order, Payment, Returns, Party) consumed from `Tooba.Settlement.Infrastructure`, plus
`Tooba.BuildingBlocks` / `Tooba.Persistence` / `Tooba.ModuleContracts` platform references.

- Foreign `*.Application` / `*.Infrastructure` / `*.Domain` edges: **ZERO**
- Foreign `DbContext` / `DbSet` reach-through: **ZERO**
- Cross-module SQL/EF joins: **ZERO**
- `Endpoints` → `Settlement.Application` + `Settlement.Contracts` only (never `Settlement.Infrastructure`,
  never Host)
- `Application` → `BuildingBlocks`, `Settlement.Contracts`, `Settlement.Domain`, `Party.Contracts`
  (never Infrastructure, never Host)
- `Domain` → `BuildingBlocks` + the one self-module `Settlement.Contracts` edge (error-code constants)
- `TypeForwardedTo` / namespace-alias workaround: **ZERO**; `GlobalUsings*.cs`: **ZERO**

## 11. Persistence ownership (12)

One module DbContext (`SettlementDbContext`) owning the `settlement` schema
(`public const string Schema = "settlement"`, `HasDefaultSchema(Schema)`), module-owned migrations
(`20260827030000_InitialSettlement`), module-owned inbox records and outbox registration
(`SettlementDbContext.Schema`), no cross-module FK, no Application/Endpoints DbContext access,
`ARCH-DATA-001` intact.

## 12. Host authority audit (13)

| Host artifact | Classification |
|---|---|
| `Program.cs` (`AddSettlementEndpointPresentation`, `MapSettlementEndpoints()`, CQRS assembly) | `ALLOWED_COMPOSITION_ROOT` |
| `Security/Seller/HostSettlementSellerAuthorizer.cs` | `ALLOWED_SECURITY_ADAPTER` |
| `Admin/Access/Authorizers/HostSettlementAdminAuthorizer.cs` | `ALLOWED_SECURITY_ADAPTER` |
| `Tooba.Host.csproj` project references | `ALLOWED_COMPOSITION_ROOT` |

`ILLEGAL_BUSINESS_AUTHORITY` / `ILLEGAL_PERSISTENCE_AUTHORITY` / `ILLEGAL_ENDPOINT_OWNERSHIP`: **ZERO**.
No `Host/Settlement`, no `Host/Grid`, no Settlement DbContext in Host.

## 13. Closed-folder regression audit (13b)

No previously closed/non-active folder received production code. `Host/Settlement` and `Host/Grid`
remain absent. No new Host production folder or file was created. `SINK_FOLDER_REGRESSION`: none.

## 14. Persistence / migration safety (14)

`UNCHANGED`. The single migration ID `20260827030000_InitialSettlement`, its Up/Down and the model
snapshot are untouched: `git diff bac4dbe3 4ca4aafc -- …/Persistence` is empty and `git diff 54b1c8ff 4ca4aafc -- …/Persistence/Migrations`
is empty. No migration was regenerated for structural cleanup.

## 15. Durable structure guard (15)

| Guard | Scope |
|---|---|
| `SettlementModuleAmsc001W1MigrateGuardTests` (12) | single code home + reachability split, typed seam (retired mapper), Contracts home + unchanged wire contract of the 3 events, bilingual coverage, 4-validator matrix, capability-first layout, Domain constant invariants, Contracts-only boundary, zero global usings, Host composition residue, single-meter observability, unchanged migrations |
| `SettlementModuleAmsc001W2StructureGuardTests` (9) | capability-first shallow layout, no single-file request leaves, exact path↔namespace, manifest↔disk root allowlists, certified-manifest honesty, `/Modules/Settlement/` grouping, no stale/duplicate copies, single-home rule |
| `SettlementModuleAmsc001W3CertGuardTests` (6, this wave) | SoT/manifest certification records + wave lineage, Host route count zero, CQRS/validator matrix + discovery path, canonical result/localization/catalog/logging, Contracts-only microservice boundary + unchanged schema, wave evidence + Host closure preserved |
| `Tooba.Settlement.Tests/Architecture/SettlementArchitectureGuardTests` | module architecture invariants |
| `Tooba.Settlement.Tests/Architecture/SettlementValidatorCoverageGuardTests` | validator coverage classification |
| `ErrorCatalogUniqueCodeGuardTests` | composed production catalog uniqueness (no duplicate machine code) |

No guard was weakened and no baseline was widened by this wave. The repository-global certified-module
pin lists in `TmarCompleteReferenceStructureGateTests` already include `Settlement` (it has been
certified under ARCH-COMPLETE-002 since `54b1c8ff`), so no repoint was required.

## 16. Manifest promotion (16)

`docs/architecture/tmar-module-structure-manifests.json`: exactly one certified `Settlement` entry with
`structureCertified: true`, `lockVersion: ARCH-COMPLETE-002`, the per-project `rootAllowlist` /
`forbiddenRootFiles` / `forbiddenTopLevelFolders`, and now a `certificationNote` + `evidence` recording
this certification (added in W2 because the W1 edit had removed the W0 note without a replacement).
No temporary pre-cert duplicate exists; Settlement is absent from `preCertModules` and
`uncertifiedHttpOwningModules`.

## 17. Recovery SoT (17)

`docs/architecture/tmar-current-state.json`:

- `structureLock.certifiedModules` includes `Settlement` exactly once (unchanged from before this wave;
  no duplicate inserted).
- The existing `completeReferenceModules[]` Settlement entry was refreshed to the AMSC-001 W3
  certification (task, commit semantics, validator matrix, wave lineage, evidence).
- New `settlementAmsc001W3` block recording the verdict, applicability, route/request counts, validator
  matrix, path/namespace, root allowlist, alias state, cohesion, localization, API mapping, logging,
  correlation, Host residue/closure, cross-module boundary, schema state, microservice extractability,
  manifest state, evidence and stop gate.
- `settlementAmsc001W0` / `settlementAmsc001W1` / `settlementAmsc001W2` lineage blocks with their
  commits.
- The edit is surgical: one entry refresh plus appended top-level members; no unrelated history rewritten
  (see `validation.md` for the diff proof).

`docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md`: appended the module-local Settlement AMSC W3 recovery
checkpoint.

## 18. Focused validation (18)

See `validation.md`. All required guards pass. The pre-existing unrelated failures
(`PaidProjectionFinancialTests`, `TmarSourceSizeAndInfraAppTests`, the `Tooba.Catalog.Contracts.Cart`
namespace deviation, the stale `TmarDurableGuardTests` recovery pins) are reproduced identically at the
wave starting head and are outside this module-local wave's authorized scope.

## 19. Residual non-blocking debt

| Item | Classification |
|---|---|
| `Infrastructure/Directories/SettlementDirectory.cs` (695 LOC) | `OVERSIZED_ONLY` — single-responsibility cohesive orchestrator; WATCH only, no structure blocker |
| `Domain/Aggregates/SettlementEntry.cs` (251), `Application/Payouts/Ports/SettlementDirectoryPorts.cs` (171), `Infrastructure/Persistence/SettlementDbContext.cs` (166), `Infrastructure/Queries/AdminPayoutGridQueryEngine.cs` (129) | `COHESIVE` |
| Admin grid / balance display-label Persian fallbacks | Accepted display-label composition, matches certified work-queue convention |
| Runtime microservice extraction (separate deployment, own DB instance, transport split) | Out of scope for an architecture certification; architecture is extractable-ready |

No violation from the Certification Result list remains: `RAW_RESULTS`, `AD_HOC`, `PARALLEL_MAPPER`,
`UNREGISTERED_CODES`, `DUPLICATE_ERROR_DESCRIPTOR`, `UNRESOLVED_ERROR_OWNER`, `HARDCODED_TEXT`,
`NON_STANDARD`, `DUPLICATE_TELEMETRY`, `SECOND_PIPELINE`, `PARALLEL_CORRELATION`, `LOST_PROPAGATION`,
`VIOLATION`, `ILLEGAL`, `FOREIGN_ACCESS`, foreign App/Infra/Domain dependency, cross-module join,
path/namespace mismatch, stale physical copy, missing solution grouping, cohesion/root-dump violation,
semantic Contracts/Application ownership violation, single-file request folder explosion, duplicate
CQRS request shape, duplicate/legacy type — all **ZERO**.

## 20. Post-Host-final-closure guard (580)

`HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED` and `HOST_ROOT_FINAL_CERTIFIED` are preserved. No new
Host production folder/file, no reintroduced module business logic, endpoint, policy, worker,
repository, DbContext or module-specific adapter under Host. `HOST_FINAL_CLOSURE_REGRESSION` not
triggered.

## 21. Microservice extractability statement

Settlement can be lifted into an independent service without touching other modules:

- owns its HTTP surface, CQRS layer, Domain, persistence (`settlement` schema), migrations, outbox
  registration and observability;
- its Contracts project is the only inbound boundary and carries zero foreign module type in any
  signature;
- its outbound dependencies are four `*.Contracts` ports (Order, Payment, Returns, Party), each
  replaceable by an HTTP/gRPC client without changing Application/Domain;
- no foreign Application/Infrastructure/Domain reference, no foreign DbContext, no cross-module join;
- its localization resources, error codes and catalog contribution are module-owned and registered
  once, so extraction leaves no copy behind.

The only remaining work for a real deployment is runtime/infrastructure (host process, connection
string, transport), not architecture.

## 22. Wave disposition

`COMPLETE_REFERENCE_PATTERN` / `ARCH-COMPLETE-002 STRUCTURE_CERTIFIED`.
Stop gate `USER_REVIEW_SETTLEMENT_AMSC_001_W3`; `automaticNextImplementationTask = NONE`.
Do not self-authorize the next Architect task.
