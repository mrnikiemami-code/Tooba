# TB-TMAR-RETURNS-AMSC-001 — Wave 0 (Analyze)

Skill: `tooba-architecture-analyze` (V2) · ANALYSIS-ONLY (no production change in this wave)
Module: `src/backend/Modules/Returns`
Baseline: `main` @ `8b1b80430222d688d6a26466bd75d9ad93b427c9` (`HEAD == origin/main`, clean tree)
Reference (read-only): `src/backend/Modules/Offer` (certified) + `src/backend/Modules/Promotion` (AMSC-001 precedent)

---

## 1. Structured State Fields

| # | Field | Value |
|---|-------|-------|
| 1 | Foundation-State | `FOUNDATION_READY` |
| 2 | Ownership-State | `correct` (all responsibilities owned by Returns; see §3) |
| 3 | File-Cohesion-State | `MULTI_RESPONSIBILITY_COHESION_VIOLATION` (3 files; see §13) |
| 4 | Oversized/God-File-State | `ReturnDirectory.cs` 426 LOC (cohesive-but-large), `AdminReturnGridQueryEngine.cs` 311 LOC (cohesive-but-large), `AdminReturnWorkQueueModels.cs` 163 LOC (mixed presentation policy + row model) |
| 5 | Localization-State | `HARDCODED_TEXT` (Persian in Application/Infrastructure, see §9) |
| 6 | API-Result-Pattern-State | `CANONICAL` (endpoints use `ApiResponseFactory`; zero raw `Results.*`) |
| 7 | Stable-Error-Code-State | `CATALOGUED` (20 codes → 20 descriptors, single owner) |
| 8 | Logging-State | `CANONICAL` (`ILogger<T>`, no Console/Debug, no second pipeline) |
| 9 | Sensitive-Logging-State | `NONE` |
| 10 | OpenTelemetry-State | `CANONICAL` (`ToobaTelemetry.Meter`, no direct `StartActivity`) |
| 11 | Correlation-Trace-State | `CANONICAL` |
| 12 | CQRS-State | `COMPLIANT` (11 `IRequest` + 11 `IRequestHandler` + `ISender`) |
| 13 | Validator-Coverage-State | `GAPS` (zero FluentValidation validators for 11 endpoint-reachable requests; see §8a) |
| 14 | Contracts-Boundary-State | `CLEAN` |
| 15 | Cross-Module-Coupling-State | `LEGAL_CONTRACTS_ONLY` (Infrastructure → 7 foreign `*.Contracts` only; zero foreign Application/Infrastructure/Domain) |
| 16 | Cross-Module-Join-State | `NONE` |
| 17 | Persistence-Ownership-State | `CORRECT` (own schema `returns`, own DbContext, own outbox) |
| 18 | Endpoint-Ownership-State | `MODULE_OWNED` (11 routes, Host residue ZERO) |
| 19 | Host-Residue-State | `ALLOWED_COMPOSITION_ROOT` + `ALLOWED_SECURITY_ADAPTER` only (see §13-Host) |
| 20 | Schema-Migration-State | `UNCHANGED` (3 migrations + snapshot, untouched) |
| 21 | Behavior-Preservation-Risk | `MEDIUM` (fault-mapping semantics + localization must be preserved exactly) |
| 22 | Canonical-Reference-Used | API/error/localization → `Offer`+`Promotion`; CQRS → `TmarFoundation.AddToobaCqrsFoundation`; typed-fault seam → `PromotionOperation` precedent; structure → `Promotion` W2 guard |
| 23 | Final-Disposition | `READY_TO_MIGRATE` |

`Structure-Handoff-State = REQUIRED` (W1 changes physical layout; W2 owns the final physical gate).

---

## 2. Target Analyzed

`src/backend/Modules/Returns` — 6 projects, 68 production `.cs` files:

```text
Tooba.Returns.Contracts       7 files (Errors, Events x3, History, Operations, Settlement)
Tooba.Returns.Domain         10 files (Aggregates x3, Events x3, ValueObjects x4)
Tooba.Returns.Application    28 files (Commands/4 leaves, Queries/7 leaves, Models/12, Ports/5, Errors/1)
Tooba.Returns.Infrastructure 18 files (Directories, Evaluators, Gateways, Bridges, Adapters x2,
                                       Queries, Messaging, Observability, Errors, DependencyInjection,
                                       Persistence (+3 migrations + snapshot))
Tooba.Returns.Endpoints      12 files (Admin x2, Seller x2, Customer x3, ReturnEndpointModule)
```

Applicability Gate: **`HTTP_OWNING`** — 11 real module-owned routes dispatched via `ISender`, real
module-owned presentation composition (`ReturnEndpointModule.MapReturnEndpoints`), and Host performs
only composition (`app.MapReturnEndpoints()`) + two security adapters.

---

## 3. Responsibility Map / Ownership Map

| Responsibility | Class | True Owner | Current location | Verdict |
|---|---|---|---|---|
| ReturnRequest aggregate + lifecycle | DOMAIN_RULE | Returns | Domain/Aggregates | correct |
| RefundAttempt / ReturnItem aggregates | DOMAIN_RULE | Returns | Domain/Aggregates | correct |
| Return statuses/destinations VOs | DOMAIN_RULE | Returns | Domain/ValueObjects | correct |
| Domain events (3) | DOMAIN_RULE | Returns | Domain/Events | correct |
| Create/Approve/Reject/Retry use cases | APPLICATION_USE_CASE | Returns | Application/Commands | correct |
| Get/List/QueryGrid use cases | APPLICATION_USE_CASE | Returns | Application/Queries | correct |
| Return orchestration + persistence | PERSISTENCE | Returns | Infra/Directories | correct |
| Eligibility evaluation (Order+Fulfillment via Contracts) | APPLICATION_USE_CASE | Returns | Infra/Evaluators | correct |
| Admin grid DB-native query | APPLICATION_USE_CASE | Returns | Infra/Queries | correct |
| Inventory restock adapter | INTEGRATION_ADAPTER | Returns | Infra/Gateways | correct (Inventory.Contracts) |
| Settlement snapshot bridge | INTEGRATION_ADAPTER | Returns | Infra/Bridges | correct (own DbContext) |
| History/AdminOperations contract adapters | INTEGRATION_ADAPTER | Returns | Infra/Adapters | correct |
| Outbox registration | INTEGRATION_ADAPTER | Returns | Infra/Messaging | correct |
| Metrics | OBSERVABILITY | Returns | Infra/Observability | correct |
| Stable error codes | CONTRACT | Returns | Contracts/Errors | correct |
| History/Settlement/Operations/Events contracts | CONTRACT | Returns | Contracts/* | correct |
| 11 HTTP routes | HTTP_ENDPOINT | Returns | Endpoints/{Admin,Seller,Customer} | correct |
| Admin/Seller authorizer seams | AUTHORIZATION_ADAPTER | Returns (seam) | Endpoints/* | correct |
| Host `MapReturnEndpoints()` + 2 authorizer impls | HOST_COMPOSITION_ROOT / SECURITY_ADAPTER | Host | Host | ALLOWED |
| Exception→SemanticError mapper | APPLICATION_USE_CASE (presentation seam) | Returns | Application/Errors | correct owner, **non-canonical mechanism** |
| `ReturnEligibilityReasonCodes.ToFaMessage` | PRESENTATION (localization) | Returns | Application/Models | **misplaced + hardcoded FA** |
| `AdminReturnQueueFilters.ComposeEligibilitySummary` | PRESENTATION (localization) | Returns | Application/Models | **hardcoded FA** |
| Persian fallbacks in grid engine | PRESENTATION (localization) | Returns | Infra/Queries | **hardcoded FA** |
| `AdminReturnOperationsContracts.ReturnEligibilityReasons` | CONTRACT | Returns | Contracts/Operations | correct |

`Ownership-State = correct` — every responsibility is owned by the right module. No `UNKNOWN_OWNER`.

---

## 4. MUST_SPLIT Decisions

1. **Duplicate command-shaped records in `Application/Models`** must be removed; the MediatR request is
   the single authoritative shape. `IReturnDirectory` (the module's internal port) must consume the
   MediatR requests directly.
   - `Models/ApproveReturnCommand.cs` → duplicate of `Commands/ApproveReturn/ApproveReturnCommand`
   - `Models/CreateReturnCommand.cs` → duplicate of `Commands/CreateReturn/CreateReturnCommand`
   - `Models/RejectReturnCommand.cs` → duplicate of `Commands/RejectReturn/RejectReturnCommand`
   - `Models/RetryRefundCommand.cs` → duplicate of `Commands/RetryReturnRefund/RetryReturnRefundCommand`
2. **`Application/Errors/ReturnsExceptionMapper.cs`** → split by responsibility:
   - typed-fault seam (`TryAsync`, `ToSemanticError`) → `Application/Composition/ReturnsOperation.cs`
     using `ContractOperationException.Code` + `ReturnsErrorCodes.IsKnown` (no `ex.Message`).
   - `ParseDestination` → `Application/Composition/ReturnRefundDestinationParser.cs`.
3. **`Application/Models/AdminReturnWorkQueueModels.cs`** → split:
   - `AdminReturnWorkQueueRow` (internal read model) stays `Application/Models`.
   - `AdminReturnQueueFilters` (status→label projection policy) → cohesive own file.
   - Persian display text leaves Application (localization repair, §9).
4. **`Application/Models/ReturnEligibilityReasonCodes.cs`** → `ToFaMessage` (hardcoded FA) removed;
   `ToErrorCode` (stable-code mapping) retained; duplicated stable mapping de-duplicated against
   `Contracts/Operations/ReturnEligibilityReasons`.
5. **`Application/Ports/ReturnSemanticMapper.cs`** → dead (zero callers); remove.
6. **Technical-axis-first Application tree** → capability-first shallow (W1/W2):
   `Application/Returns/{Commands,Queries,Models,Ports}` + shared `Application/Composition` +
   `Application/Validation`. The 11 single-file use-case leaf folders are `OVER_FOLDERED`.

---

## 5. Current Illegal Dependencies

None at the project-reference level. Verified:

- `Tooba.Returns.Contracts` → BuildingBlocks only.
- `Tooba.Returns.Domain` → BuildingBlocks only.
- `Tooba.Returns.Application` → BuildingBlocks, Returns.Contracts, Returns.Domain.
- `Tooba.Returns.Infrastructure` → BuildingBlocks, ModuleContracts, Persistence, Returns.Application,
  Returns.Contracts, and **7 foreign `*.Contracts`** (Order, Fulfillment, Payment, Wallet, Inventory,
  Party, Catalog). Zero foreign `.Application`/`.Infrastructure`/`.Domain`.
- `Tooba.Returns.Endpoints` → BuildingBlocks, Returns.Application. Zero Infrastructure/Host.

`Cross-Module-Coupling-State = LEGAL_CONTRACTS_ONLY`. No foreign DbContext/DbSet; no foreign
repository/store; no cross-module EF navigation; no shared mutable entity.

---

## 6. Cross-Module Join Inventory

`NONE`. `ReturnDirectory`, `ReturnEligibilityEvaluator`, `AdminReturnGridQueryEngine`,
`ReturnSettlementBridge`, `ReturnHistoryReader`, `ReturnAdminOperationsAdapter` each query **only**
`ReturnsDbContext` (schema `returns`) and enrich via foreign **Contracts ports**:

- `IOrderReturnReader`, `IOrderGridEnrichmentReader` (Order.Contracts)
- `IPaymentReturnReader`, `IPaymentRefundGateway` (Payment.Contracts)
- `IWalletRefundCreditPort` (Wallet.Contracts)
- `IInventoryReturnGateway` (Inventory.Contracts)
- `IFulfillmentReturnReader` (Fulfillment.Contracts)
- `IPartyLookup` (Party.Contracts), `ICatalogVariantLookup` (Catalog.Contracts)

No SQL/EF join touches two module-owned schemas.

---

## 7. Contracts-Only Replacement Map

Already Contracts-only. W1 does not add new cross-module edges. Semantic contracts audit:

- `Contracts/Operations/ReturnAdminOperationsContracts.cs` (197 LOC) — a single mixed file holding the
  module-boundary `IReturnAdminOperations` port, the stable boundary DTOs/enums it needs, **and**
  `ReturnEligibilityReasons`. This is a boundary contract (consumed by Order admin), so it belongs in
  Contracts, but it should be split by capability/responsibility for cohesion (`Operations/` vs the
  reason-code catalog). Recorded as a W1 cohesion item; **not** a boundary violation.
- `Contracts/Errors/ReturnsErrorCodes.cs` — module-owned stable codes. Canonical.
- Application `Models/` snapshots and ports are Application-internal (correct) and must **not** move to
  Contracts.

---

## 8. CQRS / MediatR Gaps

`CQRS-State = COMPLIANT` at the shape level: 11 `IRequest<Result<…>>` + 11 `IRequestHandler<,>` +
`ISender` dispatch in every endpoint; zero endpoint→DbContext/Directory/Infrastructure; zero Host
bypass; `AddToobaCqrsFoundation` registers the module Application assembly (`Program.cs:175`).

The only CQRS-adjacent defect is the **duplicate command-shaped records** (§4.1) and the **zero
validators** (§8a), both fixed in W1.

---

## 8a. Request Input-Provenance & Validator Matrix (MANDATORY — HARD BLOCKER)

11 shipped routes, 11 `ISender.Send` call sites, 11 distinct reachable request types.

| # | Route+Verb | Request type | Caller-controlled inputs (source) | Transport-shape risk | Validator | Class |
|---|---|---|---|---|---|---|
| 1 | `GET /v1/customer/returns` | `ListCustomerReturnsQuery(Guid)` | actor (server-derived from `ICurrentAuthenticatedUser`) | none | — | `NO_VALIDATOR_REQUIRED` (server-derived actor) |
| 2 | `GET /v1/customer/returns/{id:guid}` | `GetCustomerReturnQuery(Guid, Guid)` | actor (server), `returnRequestId` (route, `:guid`) | route `:guid` constraint enforces shape | — | `NO_VALIDATOR_REQUIRED` (route-constrained + server actor) |
| 3 | `POST /v1/customer/returns` | `CreateReturnCommand` | body: `SellerOrderId`(Guid), `IdempotencyKey`(string), `Reason`(string?), `Items[]{OrderLineId,Quantity}`, `RefundDestination`(string?); actor(server) | **`IdempotencyKey` required/len, `Items` non-empty, item `Quantity>0`, `OrderLineId != Guid.Empty`, destination parse** | **required** | `VALIDATOR_REQUIRED` |
| 4 | `GET /v1/seller/returns` | `ListSellerReturnsQuery(Guid)` | `sellerPartyId` (server-derived by authorizer) | none | — | `NO_VALIDATOR_REQUIRED` (server-derived) |
| 5 | `GET /v1/seller/returns/{id:guid}` | `GetSellerReturnQuery(Guid, Guid)` | `sellerPartyId`(server), `returnRequestId`(route `:guid`) | route-constrained | — | `NO_VALIDATOR_REQUIRED` |
| 6 | `POST /v1/seller/returns/{id:guid}/approve` | `ApproveReturnCommand` | route `:guid`; body `RefundDestination`/`Destination` (string?) | destination must parse to `RefundDestination` (currently ad-hoc `ParseDestination` in endpoint) | **required** | `VALIDATOR_REQUIRED` |
| 7 | `POST /v1/seller/returns/{id:guid}/reject` | `RejectReturnCommand` | route `:guid`; body `Reason`(string?); actor/seller(server) | `Reason` bounded length | **required** | `VALIDATOR_REQUIRED` |
| 8 | `GET /v1/admin/returns` | `ListAdminReturnsQuery` | none (no input) | none | — | `NO_VALIDATOR_REQUIRED` |
| 9 | `POST /v1/admin/returns/query` | `QueryAdminReturnsGridQuery(GridQueryRequest)` | body `GridQueryRequest` (page/pageSize/search/sort/filters/advanced) | grid transport shape | **required** | `VALIDATOR_REQUIRED` |
| 10 | `GET /v1/admin/returns/{id:guid}` | `GetAdminReturnQuery(Guid)` | route `:guid` | route-constrained | — | `NO_VALIDATOR_REQUIRED` |
| 11 | `POST /v1/admin/returns/{id:guid}/retry-refund` | `RetryReturnRefundCommand(Guid, Guid)` | route `:guid`; actor (server) | route-constrained | — | `NO_VALIDATOR_REQUIRED` |

**Set-equality:** reachable requests {1..11} == classified {1..11}; each exactly once. No orphans.
Split: **5 `VALIDATOR_REQUIRED`** + **6 `NO_VALIDATOR_REQUIRED`**.

**Exemption provenance proof (6 rows):** each exemption's only caller-controlled input is either
(a) a route `:guid` constrained by the minimal-API `:guid` route constraint, or (b) a value derived
server-side by the module-owned authorizer / `ICurrentAuthenticatedUser` seam, or (c) absent. Optional
query/header values: none exist on these routes (no optional query/header inputs are read).

**Current gap:** `VALIDATOR_REQUIRED` count = 5 but implemented validators = 0. The destination parse
(#6, #3) is handled ad-hoc in endpoints via `ReturnsExceptionMapper.ParseDestination`, not through the
canonical FluentValidation pipeline. W1 must add the 5 validators + `ReturnsValidationCodes` and move
destination validation into the validator (or a proven canonical policy with correct failure mapping).

---

## 9. Localization Findings (`HARDCODED_TEXT`)

Canonical mechanism exists and is used elsewhere (Offer/Promotion): `IErrorResourceSet` +
`*.Errors.<Module>ErrorCodes` + `.resx`/`.fa.resx` + `IErrorMessageLocalizer`. Returns **does not**
participate:

1. `Contracts/Errors/ReturnsErrorCodes.cs` — 20 stable codes, no `IErrorResourceSet`, no `.resx`.
2. `Infrastructure/Errors/ReturnsErrorCatalogContributor.cs` — 20 descriptors, `LocalizationKey = code`
   but **no resource set owns the `return.`/`refund.` keyspace** → Persian text cannot resolve.
3. Hardcoded Persian (non-comment) production text:
   - `Application/Models/ReturnEligibilityReasonCodes.cs:36-46` `ToFaMessage` — **zero callers** (dead)
   - `Application/Models/AdminReturnWorkQueueModels.cs:139-161` `ComposeEligibilitySummary` — Persian
     display strings reachable from the admin grid read model
   - `Infrastructure/Queries/AdminReturnGridQueryEngine.cs:271,288,296,297,300` — Persian fallbacks
     (`"کالای سفارش"`, `"مرجوعی"`, `"مشتری"`, `"فروشنده"`, `"واحد"`) **reachable** via
     `POST /v1/admin/returns/query` (`ProductLabel`, `ReturnReference`, `CustomerDisplayName`,
     `SellerDisplayName`, `UnitLabel`).
4. `Endpoints/Customer/ReturnCustomerEndpoints.cs:47,57,67` — hardcoded English literal
   `"customer.actor.missing"` instead of the canonical `FoundationErrorCodes`-owned
   `customer.session.required` code.

W1 must add `ReturnsErrorResourceSet` + `ReturnsErrors.resx`/`.fa.resx` (bilingual) and route the
reachable user-facing text through the canonical localizer, and replace the endpoint literal with the
foundation-owned code (never re-register the foundation descriptor).

---

## 10. API Result / Error Mapping Findings (`CANONICAL`)

- Every endpoint injects `ApiResponseFactory` and returns `api.From(...)` / `api.FromFailure(...)`.
- Zero `Results.Json/BadRequest/Problem` in Returns production.
- **Finding:** `Application/Errors/ReturnsExceptionMapper.TryMapExact` classifies expected failures by
  **exact `ex.Message`/`InvalidOperationException.Message` text matching** and carries ~18 legacy
  alias literals. This violates "never classify failures by parsing `ex.Message`". W1 replaces it with
  the canonical typed seam: `catch (ContractOperationException ex) when (ReturnsErrorCodes.IsKnown(ex.Code))`
  + a `SemanticException` path, using `Code` (property), never `Message`.
- Grid validation path is already canonical (`GridQueryValidationException.ErrorCode` → `SemanticError`).

---

## 11. Logging / Sensitive-Data Findings (`CANONICAL` / `NONE`)

- `ILogger<T>` structured template only (`ReturnDirectory.cs:347`). No `Console.WriteLine`,
  `Debug.WriteLine`, custom framework, or second telemetry pipeline.
- `ReturnsInstrumentation` reuses `ToobaTelemetry.Meter`. No PII/secrets logged; no
  password/token/OTP/Authorization/cookie/payment-payload logging.

---

## 12. OpenTelemetry / Correlation Findings (`CANONICAL`)

- No direct `ActivitySource.StartActivity`; no manual `traceparent`; no competing correlation ID/header.
- Cross-module calls go through Contracts ports; `ProblemDetails` trace/correlation supplied by the
  Host foundation. No bypass.

---

## 13. File Cohesion / Splitting Plan

| File | LOC | Classification | Action (W1) |
|---|---|---|---|
| `Infrastructure/Directories/ReturnDirectory.cs` | 426 | `OVERSIZED_ONLY` (single cohesive orchestration) | optional split: `OpenReturnUseCaseGuard` → own file (currently co-located) |
| `Infrastructure/Queries/AdminReturnGridQueryEngine.cs` | 311 | `OVERSIZED_ONLY` (cohesive grid engine) | keep; localize FA fallbacks |
| `Application/Models/AdminReturnWorkQueueModels.cs` | 163 | `MULTI_RESPONSIBILITY_COHESION_VIOLATION` | split row model vs filters/policy; localize |
| `Application/Errors/ReturnsExceptionMapper.cs` | 169 | `MULTI_RESPONSIBILITY_COHESION_VIOLATION` | split typed seam vs destination parser; drop message parsing |
| `Contracts/Operations/ReturnAdminOperationsContracts.cs` | 197 | cohesive boundary bundle | optional split: reason catalog vs operations port/DTOs |

No new god-file may be created; splits must be by real responsibility, not cosmetic.

### Host residue (W1 must preserve)

`src/backend/Host/Tooba.Host`:
- `Program.cs` — `using Tooba.Returns.Endpoints`, CQRS assembly registration, 2 authorizer
  registrations, `app.MapReturnEndpoints()` → `ALLOWED_COMPOSITION_ROOT`.
- `Security/Seller/HostReturnSellerAuthorizer.cs`, `Admin/Access/Authorizers/HostReturnAdminAuthorizer.cs`
  → `ALLOWED_SECURITY_ADAPTER` (implement module-owned seams).
- No `Host/Returns`, `Host/Grid` folder; no Returns business/persistence/DbContext in Host. Host
  residue = ZERO for illegal categories.

---

## 14. Exact Target Paths / Namespaces (capability-first, shallow)

```text
Application/
  Returns/
    Commands/   CreateReturnCommand.cs, ApproveReturnCommand.cs, RejectReturnCommand.cs, RetryReturnRefundCommand.cs
    Queries/    GetAdminReturnQuery.cs, GetCustomerReturnQuery.cs, GetSellerReturnQuery.cs,
                ListAdminReturnsQuery.cs, ListCustomerReturnsQuery.cs, ListSellerReturnsQuery.cs,
                QueryAdminReturnsGridQuery.cs
    Models/     ReturnSnapshot.cs, ReturnItemSnapshot.cs, RefundAttemptSnapshot.cs,
                ReturnLineCommand.cs, ReturnEligibilityResult.cs, ReturnLineEligibility.cs,
                ReturnEligibilityReasonCodes.cs, AdminReturnWorkQueueRow.cs, AdminReturnQueueFilters.cs
    Ports/      IReturnDirectory.cs, IReturnEligibilityEvaluator.cs, IReturnInventoryGateway.cs,
                IReturnUseCaseGuard.cs
  Composition/  ReturnsOperation.cs, ReturnRefundDestinationParser.cs
  Validation/   ReturnsRequestValidators.cs, ReturnsValidationCodes.cs

Contracts/
  Errors/       ReturnsErrorCodes.cs, ReturnsErrorResourceSet.cs
  Resources/    ReturnsErrors.resx, ReturnsErrors.fa.resx
  Events/ History/ Operations/ Settlement/   (unchanged)

Endpoints/
  Admin/ Seller/ Customer/ ReturnEndpointModule.cs   (unchanged)
  Errors/       ReturnsErrorCatalogContributor.cs
  Resources/    (resource set may live in Contracts per Promotion precedent)

Infrastructure/  (unchanged capability/integration folders)
```

Namespace must exactly match path. The 11 single-file use-case leaf folders are removed.

---

## 15. Behavior-Preservation Checklist

Must remain byte-for-byte behaviorally identical: 11 routes + verbs + audience groups
(`/v1/customer|seller|admin`); response shapes and status codes; the 20 stable codes and their
HTTP classifications/statuses; authorization semantics (customer actor resolution + dev/testing header;
seller/admin seams); ReturnRequest lifecycle/state transitions; idempotency; refund destination
selection (wallet vs PSP, no double-credit); persistence semantics + schema `returns`; the 3 migration
IDs and Up/Down; outbox event translation (3 integration events, `EventTypeName`, `Version=1`); the 6
metric names; tenant/current-commerce context; localization **keys/semantics** (new keys only; no
published key renamed); CQRS request/response DTO semantics.

The typed-fault seam must preserve the **same** mapped codes for the same directory failures
(`returns.request.not_found`→`return.missing`, `fulfillment.status.transition_invalid`→`return.stale`,
etc.) while switching from message-text to `Code`-property matching.

---

## 16. Migration Order (W1)

1. Create the canonical typed-fault seam + destination parser; rewrite the 4 command handlers and
   `IReturnDirectory` to consume MediatR requests; delete duplicate `Models/*Command` records and the
   dead `ReturnSemanticMapper`.
2. Add `ReturnsErrorCodes` helpers (`IsKnown`, declared-code set) + 5 FluentValidation validators +
   `ReturnsValidationCodes`; move destination validation into the validator.
3. Localization: add `ReturnsErrorResourceSet` + bilingual `.resx`; register once; repair reachable FA
   text and the endpoint literal.
4. Cohesion splits (`AdminReturnWorkQueueModels`, `ReturnsExceptionMapper`) and
   `OpenReturnUseCaseGuard` extraction.
5. Capability-first physical move + namespace alignment (hand to W2 for the structural gate).
6. Focused build + Returns/Host guard tests.

---

## 17. Verification Plan

- `dotnet build` Contracts/Domain/Application/Infrastructure/Endpoints + Host.
- `Tooba.Returns.Tests` (14 baseline) + new AMSC guards.
- `Tooba.Host.Tests` filtered to Returns-referencing tests + `HostModuleEndpointOwnershipTests`.
- W1 guard: typed seam (no `ex.Message`), 5 validators + set-equality matrix, localization resource set
  registered + bilingual keys, single canonical `ReturnsErrorCodes`, zero foreign coupling, route/request
  inventory unchanged.
- W2 guard: capability-first shallow Application, no single-file use-case leaves, path↔namespace EXACT,
  root allowlists, slnx `/Modules/Returns/` grouping, manifest↔disk.
- W3: fresh `ARCH-COMPLETE-002` certification + manifest promotion + SoT closure.

---

## 18. Certification Blockers (for W3 to close)

1. `Validator-Coverage-State = GAPS` → must reach `EXHAUSTIVE` (5 required).
2. `Localization-State = HARDCODED_TEXT` → must reach `CANONICAL`.
3. `Stable-Error-Code-State` incomplete → resource set must own the `return.`/`refund.` keyspace.
4. `File-Cohesion-State = MULTI_RESPONSIBILITY_COHESION_VIOLATION` → must reach `COHESIVE`.
5. `Folder-Granularity-State = TECHNICAL_AXIS_FIRST` + single-file leaves → must reach
   `PROFESSIONAL_SHALLOW` (W2).
6. Duplicate CQRS command shapes → must be removed.
7. Message-text fault classification → must be replaced by the typed `Code` seam.
8. Endpoint hardcoded `"customer.actor.missing"` → canonical foundation code.

---

## 19. Final Disposition

**`READY_TO_MIGRATE`** — ownership is correct and boundaries are Contracts-only; W1 performs the
behavior-preserving canonical migration (typed seam, validators, localization, cohesion, dedup,
capability-first foldering), W2 owns the structural gate, W3 certifies.
