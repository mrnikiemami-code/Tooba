# TB-TMAR-SETTLEMENT-AMSC-001 — Wave 0 (Analyze)

Skill: `tooba-architecture-analyze` (V2) · **ANALYSIS-ONLY** (no production change in this wave)
Module: `src/backend/Modules/Settlement`
Baseline: `main` @ `26942519d2ffda8043ca55590de4bdbefce44a54` (`HEAD == origin/main`, clean tracked tree)
Reference (read-only): `src/backend/Modules/Offer` (certified) + `src/backend/Modules/Returns`
(AMSC-001 W0→W3-R1 precedent, `f5c5a6db`/`0a573864`/`6cab1b87`/`fa535eca`/`26942519`)

Final objective (per the requesting Architect): Settlement must be liftable into an independent
microservice — zero cross-module Application/Infrastructure/Domain coupling, Contracts-only
boundaries, module-owned schema/outbox/localization.

---

## 1. Structured State Fields

| # | Field | Value |
|---|-------|-------|
| 1 | Foundation-State | `FOUNDATION_READY` (structureCertified: true in `tmar-module-structure-manifests.json`) |
| 2 | Ownership-State | `correct` (all responsibilities Settlement-owned; see §3) |
| 3 | File-Cohesion-State | `MULTI_RESPONSIBILITY_COHESION_VIOLATION` (2 files; see §13) |
| 4 | Oversized/God-File-State | `SettlementDirectory.cs` 701 LOC (`OVERSIZED_ONLY`, cohesive orchestration, no baseline entry); `SettlementContracts.cs` 398 LOC (`MULTI_RESPONSIBILITY_COHESION_VIOLATION`, mixed boundary+Application bundle) |
| 5 | Localization-State | `MISSING_INFRASTRUCTURE_USE` (no `IErrorResourceSet`, no `.resx`; `LocalizationKey = code` cannot resolve; plus hard-coded Persian display text) |
| 6 | API-Result-Pattern-State | `CANONICAL` at the endpoint layer (`ApiResponseFactory` only, zero raw `Results.*`) |
| 7 | Stable-Error-Code-State | `CATALOGUED` but **misplaced + incomplete** (codes live in `Application/Errors`; 3 codes registered as raw literals; no declared-code set) |
| 8 | Logging-State | `CANONICAL` (`ILogger<T>`/scope; no `Console`/`Debug`; no second pipeline) |
| 9 | Sensitive-Logging-State | `NONE` |
| 10 | OpenTelemetry-State | `BYPASSED` (Settlement `Observability/SettlementInstrumentation.cs` is a no-op stub with no `ActivitySource`/`Meter`; see §12) |
| 11 | Correlation-Trace-State | `CANONICAL` (no parallel correlation, no manual `traceparent`) |
| 12 | CQRS-State | `COMPLIANT` (10 `IRequest<Result<…>>` + 10 `IRequestHandler<,>` + `ISender`) |
| 13 | Validator-Coverage-State | `EXHAUSTIVE` (10 reachable = 4 `VALIDATOR_REQUIRED` + 6 `NO_VALIDATOR_REQUIRED`; see §8a) |
| 14 | Contracts-Boundary-State | `VIOLATION` (3 integration events declared in `Application/Ports`; see §5/§7) |
| 15 | Cross-Module-Coupling-State | `LEGAL_CONTRACTS_ONLY` (Infrastructure → 4 foreign `*.Contracts` + platform; zero foreign Application/Infrastructure/Domain) |
| 16 | Cross-Module-Join-State | `NONE` |
| 17 | Persistence-Ownership-State | `CORRECT` (own schema `settlement`, own DbContext, own outbox/inbox) |
| 18 | Endpoint-Ownership-State | `MODULE_OWNED` (10 routes, Host residue = composition + 2 authorizers) |
| 19 | Host-Residue-State | `ALLOWED_COMPOSITION_ROOT` + `ALLOWED_SECURITY_ADAPTER` only (see §13-Host) |
| 20 | Schema-Migration-State | `UNCHANGED` (1 migration `20260827030000_InitialSettlement` + snapshot) |
| 21 | Behavior-Preservation-Risk | `MEDIUM` (fault-mapping mechanism + localization surface + event type home must be preserved exactly) |
| 22 | Canonical-Reference-Used | API/error/localization/typed-seam/Events-home → `Returns` + `Payment` (certified); CQRS → `ToobaCqrsRegistration.AddToobaCqrsFoundation`; structure → `Returns` W2 guard; observability → `ReturnsInstrumentation` (`ToobaTelemetry.Meter`) |
| 23 | Final-Disposition | `READY_TO_MIGRATE` |

`Structure-Handoff-State = REQUIRED` (W1 changes physical layout; W2 owns the final physical gate).

---

## 2. Target Analyzed

`src/backend/Modules/Settlement` — 6 projects (5 production + Tests), 67 production `.cs` files:

```text
Tooba.Settlement.Contracts        3 .cs  (History x1, Operations x2)
Tooba.Settlement.Domain          14 .cs  (Aggregates x6, Entities x1, Events x3, ValueObjects x4)
Tooba.Settlement.Application     23 .cs  (Commands x3, Queries x8, Errors x2, Models x1, Ports x2,
                                          Validators x5, GlobalUsings x2)
Tooba.Settlement.Infrastructure  22 .cs  (Adapters x3, Bridges x3, Directories x1, Errors x1,
                                          Gateways x2, Handlers x1, Messaging x1, Observability x1,
                                          Queries x1, DependencyInjection x1, GlobalUsings x2,
                                          Persistence x2 + Migrations x2 + snapshot x1)
Tooba.Settlement.Endpoints        5 .cs  (Admin x2, Seller x2, SettlementEndpointModule)
```

Applicability Gate: **`HTTP_OWNING`** — 10 real module-owned routes dispatched via `ISender`
(`SettlementEndpointModule.MapSettlementEndpoints`), and Host performs only composition
(`app.MapSettlementEndpoints()`, module registration, migration registration) plus two thin
security adapters. The `Endpoints` project is **not** ceremony.

---

## 3. Responsibility Map / Ownership Map

| Responsibility | Class | True Owner | Current location | Verdict |
|---|---|---|---|---|
| `SettlementEntry` accrual/credit/debit/neutralize rules | DOMAIN_RULE | Settlement | Domain/Aggregates | correct |
| `PayoutRequest` lifecycle (`Pending→Processing→Succeeded/Failed`) | DOMAIN_RULE | Settlement | Domain/Aggregates | correct |
| `PayoutAttempt`, `SettlementAccount`, `SellerPayoutProfile`, `SettlementStatement` | DOMAIN_RULE | Settlement | Domain/Aggregates | correct |
| `CommissionPolicy` + snapshot | DOMAIN_RULE | Settlement | Domain/Entities, ValueObjects | correct |
| 3 domain events | DOMAIN_RULE | Settlement | Domain/Events | correct |
| Seller balance/entries/statements/payout-request use cases | APPLICATION_USE_CASE | Settlement | Application/Queries, Commands | correct |
| Admin balances/queue/process/retry/grid use cases | APPLICATION_USE_CASE | Settlement | Application/Queries, Commands | correct |
| Orchestration + persistence (`settlement` schema) | PERSISTENCE | Settlement | Infrastructure/Directories | correct |
| Accrual/refund/void/neutralize/restore orchestration | APPLICATION_USE_CASE | Settlement | Infrastructure/Directories | correct |
| Admin grid DB-native query engine | APPLICATION_USE_CASE | Settlement | Infrastructure/Queries | correct |
| Payout gateway port + fail-closed/fake impls | INTEGRATION_ADAPTER | Settlement | Application/Ports + Infrastructure/Gateways | correct |
| Order/Payment/Returns snapshot bridges | INTEGRATION_ADAPTER | Settlement | Infrastructure/Bridges | correct |
| History/AdminOrderDetail/OrderAccrual contract adapters | INTEGRATION_ADAPTER | Settlement | Infrastructure/Adapters | correct |
| Outbox registration + payment/refund inbox handlers | INTEGRATION_ADAPTER | Settlement | Infrastructure/Messaging, Handlers | correct |
| Telemetry | OBSERVABILITY | Settlement | Infrastructure/Observability | **owner correct, implementation is a no-op stub** |
| Stable error codes | CONTRACT | Settlement | **Application/Errors** | **misplaced (see §7)** |
| Integration events (3) | CONTRACT | Settlement | **Application/Ports** | **misplaced (see §5)** |
| History/Operations contracts | CONTRACT | Settlement | Contracts/{History,Operations} | correct |
| 10 HTTP routes | HTTP_ENDPOINT | Settlement | Endpoints/{Seller,Admin} | correct |
| Seller/Admin authorizer seams | AUTHORIZATION_ADAPTER | Settlement (seam) | Endpoints/{Seller,Admin} | correct |
| Host `MapSettlementEndpoints()` + 2 authorizer impls + module/migration registration | HOST_COMPOSITION_ROOT / SECURITY_ADAPTER | Host | Host | ALLOWED |
| `SettlementExceptionMapper` (message-text classification) | APPLICATION_USE_CASE (presentation seam) | Settlement | Application/Errors | correct owner, **non-canonical mechanism** |
| Persian display fallbacks (`"فروشنده"`, `"دسترسی به تسویه…"`) | PRESENTATION (localization) | Settlement | Application/Queries, Application/Ports | **hardcoded FA** |
| `AdminPayoutGridQueryPolicy` (grid whitelist/normalize) | APPLICATION_USE_CASE | Settlement | Application/Queries | correct |
| `ISettlementUseCaseGuard`/`OpenSettlementUseCaseGuard` | APPLICATION_USE_CASE | Settlement | Application/Ports + Infrastructure/Directories | correct |

`Ownership-State = correct` — no `UNKNOWN_OWNER`. Two **misplacements** (stable codes and integration
events inside `Application`) and two **quality** defects (message-text fault classification,
no-op telemetry) are corrected in W1.

---

## 4. MUST_SPLIT Decisions

1. **`Application/Ports/SettlementContracts.cs` (398 LOC)** — a single mixed file bundling
   * Application-internal snapshots/ports (`SettlementOrderSnapshot`, `SettlementPaymentSnapshot`,
   `SettlementRefundSnapshot`, `ISettlementOrderReader`, `ISettlementPaymentReader`,
   `ISettlementReturnsReader`, `GatewayPayoutResult`, `IPayoutGateway`, `SettlementBalanceSnapshot`,
   `SettlementEntrySnapshot`, `SettlementStatementSnapshot`, `PayoutAttemptSnapshot`,
   `PayoutRequestSnapshot`, `ISettlementDirectory`, `ISettlementUseCaseGuard`, internal command
   records `RequestPayoutCommand`/`ProcessPayoutCommand`/`RetryPayoutCommand`,
   `RestoreSettlementLedgerSlice`, `SellerOrderRestoreSettlementPolicy`), **and**
   * module-boundary integration events (`SettlementEntryPostedIntegrationEvent`,
     `PayoutSucceededIntegrationEvent`, `PayoutFailedIntegrationEvent`).
   → split by responsibility and consumer boundary (see §7).
2. **`Application/Errors/SettlementExceptionMapper.cs` (97 LOC)** — replaces message-text
   classification with the typed `ContractOperationException.Code` seam
   (`Application/Composition/SettlementOperation.cs`), mirroring `ReturnsOperation`/`PaymentOperation`.
3. **Technical-axis-first Application tree** — `Application/Commands/<UseCase>` and
   `Application/Queries/<UseCase>` roots with **10 single-file leaf folders** → capability-first
   shallow `Application/Payouts/{Commands,Queries,Ports}` + shared `Application/{Composition,Errors,
   Models,Validation}` (see §14).
4. **`Infrastructure/Directories/SettlementDirectory.cs` (701 LOC)** — cohesive but oversized;
   `OpenSettlementUseCaseGuard` is co-located with the directory and is extracted to its own file.
   The directory itself is a single-responsibility orchestration and is **not** split (no cosmetic
   splitting).
5. **`Application/Errors/SettlementErrorCodes.cs`** — move to the module boundary
   (`Contracts/Errors/`) and add the declared-code surface (`IsKnown`/`IsHttpReachable`/
   `IsPlatformFault`) that the typed seam needs; add the 3 currently string-literal codes as
   declared members.

---

## 5. Current Illegal Dependencies

At the **project-reference** level: none illegal. Verified:

- `Tooba.Settlement.Contracts` → `BuildingBlocks` only.
- `Tooba.Settlement.Domain` → `BuildingBlocks` only.
- `Tooba.Settlement.Application` → `BuildingBlocks`, `Settlement.Domain`, `Party.Contracts`.
- `Tooba.Settlement.Infrastructure` → `BuildingBlocks`, `ModuleContracts`, `Persistence`,
  `Settlement.Application`, `Settlement.Contracts`, and **4 legal foreign `*.Contracts`**
  (Order, Payment, Returns, Party). Zero foreign `.Application`/`.Infrastructure`/`.Domain`.
- `Tooba.Settlement.Endpoints` → `BuildingBlocks`, `Settlement.Application`. Zero
  Infrastructure/Host.

**Non-reference boundary finding (must be repaired in W1):** the three module integration events
(`SettlementEntryPostedIntegrationEvent` = `settlement.entry.posted.v1`,
`PayoutSucceededIntegrationEvent` = `payout.succeeded.v1`, `PayoutFailedIntegrationEvent` =
`payout.failed.v1`) are declared inside `Tooba.Settlement.Application/Ports/SettlementContracts.cs`.
A microservice extraction that keeps `Settlement.Contracts` as the only inbound boundary would leave
these published events behind. They must live in `Tooba.Settlement.Contracts` (the certified
Returns/Payment/Fulfillment/Offer convention). They are also in the wrong dependency direction
today: `Settlement.Infrastructure` (csproj) already references `Settlement.Contracts`, so moving them
is dependency-neutral.

---

## 6. Cross-Module Join Inventory

`NONE`. Every persistence read/write goes through `SettlementDbContext` (schema `settlement`:
`commission_policies`, `settlement_accounts`, `settlement_entries`, `settlement_statements`,
`seller_payout_profiles`, `payout_requests`, `payout_attempts`, `payment_inbox`, `refund_inbox`,
`outbox_messages`). Foreign data is obtained only through **Contracts ports**:

- `IOrderReturnReader` (Order.Contracts)
- `IPaymentSettlementReader` (Payment.Contracts) + `PaymentSucceededIntegrationEvent` (Payment.Contracts.Events)
- `IReturnSettlementReader` (Returns.Contracts) + `RefundSucceededIntegrationEvent` (Returns.Contracts.Events)
- `IPartyLookup` (Party.Contracts)

No LINQ join, `FromSql`, navigation property or DbSet access touches a foreign schema.

---

## 7. Contracts-Only Replacement Map

Already Contracts-only for **inbound** integration. The W1 boundary repair is:

| Item | Today | Target |
|---|---|---|
| `SettlementEntryPostedIntegrationEvent` | `Application/Ports/SettlementContracts.cs` | `Contracts/Events/SettlementEntryPostedIntegrationEvent.cs` |
| `PayoutSucceededIntegrationEvent` | `Application/Ports/SettlementContracts.cs` | `Contracts/Events/PayoutSucceededIntegrationEvent.cs` |
| `PayoutFailedIntegrationEvent` | `Application/Ports/SettlementContracts.cs` | `Contracts/Events/PayoutFailedIntegrationEvent.cs` |
| `SettlementErrorCodes` | `Application/Errors/SettlementErrorCodes.cs` | `Contracts/Errors/SettlementErrorCodes.cs` (+ `SettlementErrorResourceSet`) |
| 3 raw literal codes (`settlement.unconfirm.payout_completed`, `settlement.cancel.payout_completed`, `settlement.restore.payout_completed`) | string literals in `SettlementDirectory` + catalog | declared members of `SettlementErrorCodes` |

`EventTypeName` constants and `Version = 1` must remain byte-identical (outbox wire contract).

Semantic contract audit:

- `Contracts/History/SettlementHistoryContracts.cs` (37 LOC) — boundary read port + DTO + stable
  source/entry type strings. Correct.
- `Contracts/Operations/SettlementOrderAccrualContracts.cs` (32 LOC) — boundary port + gate DTO.
  Correct.
- `Contracts/Operations/SettlementAdminOrderDetailContracts.cs` (25 LOC) — boundary read port + DTO.
  Correct.
- Application-internal snapshots/ports/`*Command` records stay in Application (correct) and must
  **not** move to Contracts.

---

## 8. CQRS / MediatR Gaps

`CQRS-State = COMPLIANT` at the shape level: 10 `IRequest<Result<…>>` + 10 real
`IRequestHandler<,>` + `ISender` in every endpoint; zero endpoint→DbContext/Directory/Infrastructure;
zero Host bypass; `AddToobaCqrsFoundation` registers the module Application assembly
(`Host/Program.cs:173`).

Minor cohesion notes (not CQRS gaps): `GetSellerSettlementBalanceQuery.cs` and
`ListAdminSettlementBalancesQuery.cs` carry an unused `using Tooba.BuildingBlocks;`, and
`QueryAdminPayoutGridQuery.cs`/`AdminPayoutGridQueryPolicy.cs`/`SettlementValidatorTests.cs` carry an
unused `using Tooba.BuildingBlocks.Grid;` where unused — tidied during the W1 move.

---

## 8a. Request Input-Provenance & Validator Matrix (MANDATORY)

10 shipped routes, 10 `ISender.Send` call sites, 10 distinct reachable request types.
Re-derived from the endpoint source on disk (not from the pre-existing guard manifest):

| # | Route+Verb | Request type | Caller-controlled inputs (source) | Transport-shape risk | Validator | Class |
|---|---|---|---|---|---|---|
| 1 | `GET /v1/seller/settlement/balance` | `GetSellerSettlementBalanceQuery(Guid)` | `sellerPartyId` (server-derived by `ISettlementSellerAuthorizer`) | none | — | `NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY` |
| 2 | `GET /v1/seller/settlement/entries` | `ListSellerSettlementEntriesQuery(Guid)` | server-derived seller | none | — | `NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY` |
| 3 | `GET /v1/seller/settlement/statements` | `ListSellerSettlementStatementsQuery(Guid)` | server-derived seller | none | — | `NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY` |
| 4 | `GET /v1/seller/settlement/payout-requests` | `ListSellerPayoutRequestsQuery(Guid)` | server-derived seller | none | — | `NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY` |
| 5 | `POST /v1/seller/settlement/payout-requests` | `RequestSellerPayoutCommand(Guid, Guid, decimal, string)` | body `RequestPayoutBody{Amount, IdempotencyKey}` (untrusted); seller/actor (server) | `Amount > 0`, `IdempotencyKey` non-blank | `RequestSellerPayoutCommandValidator` | `VALIDATOR_REQUIRED_PRESENT` |
| 6 | `GET /v1/admin/settlement/balances` | `ListAdminSettlementBalancesQuery` | none | none | — | `NO_VALIDATOR_REQUIRED_NO_INPUT` |
| 7 | `GET /v1/admin/settlement/payout-queue` | `ListAdminPayoutQueueQuery` | none | none | — | `NO_VALIDATOR_REQUIRED_NO_INPUT` |
| 8 | `POST /v1/admin/settlement/payout-queue/query` | `QueryAdminPayoutGridQuery(GridQueryRequest)` | body `GridQueryRequest` (page/pageSize/search/sort/filters/advanced) | envelope null | `QueryAdminPayoutGridQueryValidator` | `VALIDATOR_REQUIRED_PRESENT` |
| 9 | `POST /v1/admin/settlement/payout-requests/{payoutRequestId:guid}/process` | `ProcessAdminPayoutCommand(Guid, Guid)` | route `:guid`; actor (server) | route-constrained + empty-Guid guard | `ProcessAdminPayoutCommandValidator` | `VALIDATOR_REQUIRED_PRESENT` |
| 10 | `POST /v1/admin/settlement/payout-requests/{payoutRequestId:guid}/retry` | `RetryAdminPayoutCommand(Guid, Guid)` | route `:guid`; actor (server) | route-constrained + empty-Guid guard | `RetryAdminPayoutCommandValidator` | `VALIDATOR_REQUIRED_PRESENT` |

**Set-equality:** shipped routes {1..10} == endpoint-reachable requests {1..10}; each classified
exactly once. Split: **4 `VALIDATOR_REQUIRED`** + **6 `NO_VALIDATOR_REQUIRED`**.

**Exemption provenance proof (6 rows):** each exemption's only caller-controlled input is either
(a) a value derived server-side by the module-owned authorizer
(`ISettlementSellerAuthorizer.RequireAuthorizedAsync` → `ISellerPanelAccess`,
`ISettlementAdminAuthorizer.RequireAuthorizedAsync` → `IAdminPanelAccess`) or (b) absent. Optional
query/header values: none are read on these routes. `GridQueryRequest` field/operator/sort/connector
semantics stay owned by `AdminPayoutGridQueryPolicy` (module policy) and are deliberately **not**
duplicated in the validator; grid failures map through `GridQueryValidationException.ErrorCode` →
`SemanticError`.

**Current state:** `Validator-Coverage-State = EXHAUSTIVE` (4 concrete validators present and
resolvable through `AddToobaCqrsFoundation` → `AddValidatorsFromAssembly` + `ValidationBehavior<,>`).
W1/W3 must keep exact set equality and must not regress the guard.

---

## 9. Localization Findings (`MISSING_INFRASTRUCTURE_USE`)

Canonical mechanism exists and is used by certified modules (`Returns`/`Payment`): module
`IErrorResourceSet` + `*.Contracts.Errors.<Module>ErrorCodes` + `<Module>Errors.resx` /
`<Module>Errors.fa.resx` + `IErrorMessageLocalizer`. Settlement **does not participate**:

1. No `IErrorResourceSet` implementation anywhere in Settlement, and no `.resx` resource in any
   Settlement project.
2. `SettlementErrorCatalogContributor` registers `ErrorDescriptor(..., LocalizationKey = code, ...)`
   for every code, so `ResourceErrorMessageLocalizer` finds no owning resource set and falls back to
   the English `SafeTitleFallback` — **Persian can never resolve**.
3. Hard-coded Persian in production (non-comment) source:
   - `Application/Queries/ListAdminSettlementBalances/ListAdminSettlementBalancesQuery.cs`
     → `displayName ?? "فروشنده"` (reachable via `GET /v1/admin/settlement/balances`).
   - `Infrastructure/Queries/AdminPayoutGridQueryEngine.cs` → `sellerName ?? "فروشنده"` (reachable via
     `POST /v1/admin/settlement/payout-queue/query`).
   - `Application/Ports/SettlementContracts.cs` → the XML summaries are prose comments (not
     user-facing), no runtime text.
4. `FailClosedPayoutGateway` throws `new InvalidOperationException("payout.gateway.unconfigured")`
   and the outbox registration throws `new InvalidOperationException("settlement.outbox.unmapped_event")`
   — stable **codes**, not prose (correct); the *classification* mechanism is the defect (§10).

W1 must add `SettlementErrorResourceSet` + bilingual `SettlementErrors.resx`/`.fa.resx` (one entry per
declared code), register the resource set exactly once, and route the reachable Persian display
fallbacks through the canonical mechanism (or record them honestly as accepted display-label
composition if they are data-display, not error text — the certified Returns precedent).

---

## 10. API Result / Error Mapping Findings (`CANONICAL` at the endpoint layer)

- Every endpoint injects `ApiResponseFactory` and returns `api.From(...)`. Zero `Results.Json`,
  `Results.BadRequest`, `Results.Problem`, local `ProblemDetails` builder.
- **Finding (blocker for microservice quality):** `Application/Errors/SettlementExceptionMapper`
  classifies expected failures by **exact `InvalidOperationException.Message` / `ex.Code` text
  matching** (`TryMapExact`, `case SettlementErrorCodes.X:`). It is *exact-match only* (no `Contains`
  / `StartsWith` prose heuristics — better than the old Returns mapper), but it still treats
  `exception.Message` as the error contract and it cannot be used with `ContractOperationException.Code`
  uniformly for unknown codes. W1 replaces it with the canonical typed seam:
  `catch (ContractOperationException ex) when (SettlementErrorCodes.IsKnown(ex.Code))` +
  `catch (SemanticException ex)`, mirroring the certified `ReturnsOperation`/`PaymentOperation`
  precedent. Unknown codes rethrow to the canonical global exception boundary.
- Domain aggregates (`PayoutRequest`, `SettlementEntry`) already throw
  `ContractOperationException(code)` — the typed source is available; only the Application mapper is
  non-canonical.
- `SettlementErrorCatalogContributor` registers 15 declared codes **plus 3 raw string literals**
  (`settlement.unconfirm.payout_completed`, `settlement.cancel.payout_completed`,
  `settlement.restore.payout_completed`). W1 declares those 3 as `SettlementErrorCodes` members and
  references the constant in both the directory and the contributor. No code value changes.
- No duplicate-suppression mechanism; `ErrorDefinitionCatalog` fail-fast duplicate detection is
  preserved (W1 must keep one descriptor per code; a W3 guard should prove composed-catalog
  uniqueness).

---

## 11. Logging / Sensitive-Data Findings (`CANONICAL` / `NONE`)

- No `Console.WriteLine`, `Debug.WriteLine`, custom logger framework or second telemetry pipeline in
  Settlement production. No logging of credentials, tokens, OTP/reset secrets, `Authorization`
  headers, cookies, session secrets, security stamps or payment payloads. `SettlementDirectory`
  logs nothing sensitive (it currently logs nothing at all).

---

## 12. OpenTelemetry / Correlation Findings (`BYPASSED`)

- No parallel correlation provider, no manual `traceparent`, no custom header. Correlation/trace
  identity comes from the canonical Host foundation. `ProblemDetails` trace/correlation is supplied by
  the canonical provider.
- **Finding:** `Infrastructure/Observability/SettlementInstrumentation.cs` (13 LOC) is a **no-op
  stub** — three empty methods (`RecordEntryPosted`, `RecordPayoutSucceeded`, `RecordPayoutFailed`)
  with no `Meter`/`ActivitySource`/counter. It is injected into `SettlementDirectory` and called on
  the accrual/payout paths, but emits nothing. The certified `Returns` precedent
  (`ReturnsInstrumentation`) uses `ToobaTelemetry.Meter` with `Counter<long>` instruments. W1 aligns
  Settlement to the canonical `ToobaTelemetry.Meter` mechanism using the same three call sites and
  preserving method names (behavior-preserving observability; no event-name change because none is
  published today).
- Cross-module calls go through Contracts ports (`IOrderReturnReader`, `IPaymentSettlementReader`,
  `IReturnSettlementReader`, `IPartyLookup`). Settlement does not decorate them with
  `IModuleCallTracer` (it never did); this is not a regression and is out of the bounded W1 scope
  unless the Architect requests trace-topology parity.

---

## 13. File Cohesion / Splitting Plan

| File | LOC | Classification | Action (W1) |
|---|---|---|---|
| `Application/Ports/SettlementContracts.cs` | 398 | `MULTI_RESPONSIBILITY_COHESION_VIOLATION` | split: internal snapshots/ports stay `Application/Payouts/Ports`; the 3 integration events move to `Contracts/Events` |
| `Application/Errors/SettlementExceptionMapper.cs` | 97 | `MULTI_RESPONSIBILITY_COHESION_VIOLATION` (mechanism) | retire → `Application/Composition/SettlementOperation.cs` (typed seam) |
| `Infrastructure/Directories/SettlementDirectory.cs` | 701 | `OVERSIZED_ONLY` (single cohesive orchestration) | extract `OpenSettlementUseCaseGuard` to its own file; keep the directory cohesive |
| `Application/Queries/QueryAdminPayoutGrid/AdminPayoutGridQueryPolicy.cs` | 100 | `COHESIVE` | keep; move under capability folder |

No new god-file may be created; splits must be by real responsibility, not cosmetic. Note: the
size baseline entry `src/backend/Modules/Settlement/Tooba.Settlement.Domain/SettlementDomain.cs`
(834 LOC, `OVERSIZED_LEGACY`) is **stale** — that file no longer exists on disk (Domain is already
split into Aggregates/Entities/Events/ValueObjects). W1/W3 should remove the stale baseline entry
honestly (it references a non-existent path); this is not widening a baseline.

### Host residue (W1 must preserve)

`src/backend/Host/Tooba.Host` + `src/backend/Host/Tooba.MigrationRunner`:

- `Program.cs` — `using Tooba.Settlement.Endpoints`, CQRS assembly registration (line 173),
  2 authorizer registrations (lines 233–234), `app.MapSettlementEndpoints()` (line 436)
  → `ALLOWED_COMPOSITION_ROOT`.
- `Composition/ToobaModuleComposition.cs` — `new SettlementModule()` → `ALLOWED_COMPOSITION_ROOT`.
- `Development/MarketplaceDevelopmentBootstrap.cs` — `SettlementDbContext` migration for dev bootstrap
  → `ALLOWED_COMPOSITION_ROOT` (already on the guard allowlist).
- `Security/Seller/HostSettlementSellerAuthorizer.cs`,
  `Admin/Access/Authorizers/HostSettlementAdminAuthorizer.cs` → `ALLOWED_SECURITY_ADAPTER`.
- `Tooba.Host.csproj` project references → `ALLOWED_COMPOSITION_ROOT`.
- No `Host/Settlement` folder; no Settlement business/DbContext/DbSet in Host.

---

## 14. Exact Target Paths / Namespaces (capability-first, shallow)

```text
Application/
  Payouts/
    Commands/   RequestSellerPayoutCommand.cs, ProcessAdminPayoutCommand.cs, RetryAdminPayoutCommand.cs
    Queries/    GetSellerSettlementBalanceQuery.cs, ListSellerSettlementEntriesQuery.cs,
                ListSellerSettlementStatementsQuery.cs, ListSellerPayoutRequestsQuery.cs,
                ListAdminSettlementBalancesQuery.cs, ListAdminPayoutQueueQuery.cs,
                QueryAdminPayoutGridQuery.cs, AdminPayoutGridQueryPolicy.cs
    Models/     AdminSettlementBalanceListItem, AdminPayoutListItem, RequestPayoutBody (SettlementAdminModels.cs)
    Ports/      SettlementDirectoryPorts.cs (ISettlementDirectory, ISettlementUseCaseGuard,
                internal command records, snapshots), SettlementReaderPorts.cs
                (ISettlementOrderReader/ISettlementPaymentReader/ISettlementReturnsReader + snapshots),
                SettlementGatewayPorts.cs (IPayoutGateway + GatewayPayoutResult),
                SettlementRestorePolicy.cs (RestoreSettlementLedgerSlice + policy),
                IAdminPayoutGridQuery.cs
  Composition/  SettlementOperation.cs (typed-fault seam)
  Errors/       SettlementErrorCodes.cs -> MOVES to Contracts/Errors
  Validation/   SettlementValidationCodes.cs, SettlementValidators.cs (or Validators/{Admin,Seller})
Contracts/
  Errors/       SettlementErrorCodes.cs, SettlementErrorResourceSet.cs
  Resources/    SettlementErrors.resx, SettlementErrors.fa.resx
  Events/       SettlementEntryPostedIntegrationEvent.cs, PayoutSucceededIntegrationEvent.cs,
                PayoutFailedIntegrationEvent.cs
  History/ Operations/   (unchanged)
Endpoints/
  Seller/ Admin/ SettlementEndpointModule.cs   (unchanged; Sales/Admin audience folders already canonical)
  Errors/       SettlementErrorCatalogContributor.cs (may stay Infrastructure/Errors per module precedent)
Infrastructure/
  Directories/  SettlementDirectory.cs, OpenSettlementUseCaseGuard.cs (extracted)
  Adapters/ Bridges/ Gateways/ Handlers/ Messaging/ Observability/ Persistence/ Persistence/Migrations/
  Errors/ Queries/ DependencyInjection/   (unchanged capability/integration folders)
```

Namespace must exactly match path. The 10 single-file use-case leaf folders are removed.

**Validator placement note (Structure decision):** the current `Validators/{Admin,Seller}` split is
audience-first inside a *single-capability* module. The certified module conventions differ
(`Payment` = `{Admin,Storefront,Webhooks}/Validators`; `Returns`/`Promotion`/`AddressBook` = flat
`Validation/`). W2 will decide the canonical placement for Settlement; the W0 recommendation is
`Application/Validation/{SettlementValidationCodes.cs, SettlementRequestValidators.cs}` (single
capability) OR keep `Validators/{Admin,Seller}` if the audience axis is judged a real capability axis
for Settlement. Either way path↔namespace must be exact and the validator files must remain
discoverable and guard-linked.

---

## 15. Behavior-Preservation Checklist

Must remain byte-for-byte behaviorally identical:

- 10 routes + verbs + audience groups (`/v1/seller`, `/v1/admin`);
- response shapes and status codes; the 18 stable codes and their HTTP classifications/statuses;
- authorization semantics (seller/admin panel access seams);
- `PayoutRequest` lifecycle/state transitions; idempotency keys
  (`payment-accrual:{paymentId}:{sellerOrderId}`, `refund-adjustment:{returnRequestId}`,
  `cancel-neutralize:…`, `cancel-restore:…`, `process:{id}`, `retry:{actor}:{ticks}`);
- FIFO restore/neutralize/void semantics (`SellerOrderRestoreSettlementPolicy`);
- commission math (10% default, `MidpointRounding.AwayFromZero`, 4 dp);
- persistence semantics + schema `settlement`; the 1 migration ID and Up/Down; snapshot semantics;
- outbox event translation (3 integration events, `EventTypeName`, `Version = 1`) — the **wire
  contract must not change** even though the CLR type moves project;
- inbox idempotency (`payment_inbox`, `refund_inbox`);
- tenant/current-commerce context (`ICurrentCommerceContext`, `ToobaNpgsql.ResolveForContext`);
- localization **keys/semantics** (new keys only; no published key renamed);
- CQRS request/response DTO semantics;
- telemetry method names (`RecordEntryPosted`, `RecordPayoutSucceeded`, `RecordPayoutFailed`).

The typed-fault seam must map the **same** codes for the same directory/domain failures while
switching from `Message` text to the typed `Code` property (e.g. `settlement.account.missing` →
`settlement.account.missing`; `settlement.amount.invalid` → `settlement.payout.invalid_amount` must be
preserved as today's alias behavior, verified by the existing behavior test).

---

## 16. Migration Order (W1)

1. Move `SettlementErrorCodes` → `Contracts/Errors/`; add `IsKnown`/`IsHttpReachable`/
   `IsPlatformFault` + declare the 3 raw-literal codes; add `SettlementErrorResourceSet` + bilingual
   `.resx`; register once; update the contributor and directory to use the constants.
2. Move the 3 integration events → `Contracts/Events/`; repoint the outbox registration and DI.
3. Add `Application/Composition/SettlementOperation.cs` (typed seam); rewrite the 7 handlers; retire
   `SettlementExceptionMapper`; replace `InvalidOperationException(code)` throws in
   `FailClosedPayoutGateway`/outbox with `ContractOperationException(code)`; preserve the existing
   behavior-test expectations.
4. Split `Application/Ports/SettlementContracts.cs` by capability/consumer; capability-first shallow
   Application tree with exact namespaces; remove the 10 single-file leaf folders.
5. Extract `OpenSettlementUseCaseGuard`; align `SettlementInstrumentation` to `ToobaTelemetry.Meter`.
6. Localization repair for the two reachable Persian fallbacks (or record accepted display-label
   composition honestly).
7. Focused build + Settlement/Host guard tests; add the W1 durable guard.
8. Hand to W2 for the physical/solution structure gate.

---

## 17. Verification Plan

- `dotnet build src/backend/Tooba.slnx` (0 errors).
- `Tooba.Settlement.Tests` (baseline 5 test classes) + new AMSC guards.
- `Tooba.Host.Tests` filtered to Settlement-referencing tests (`Settlement`, `Marketplace`,
  `AdminOrder`, `PaidProjection`, `ErrorCatalogUniqueCode`) + `HostModuleEndpointOwnershipTests` +
  `TmarCompleteReferenceStructureGateTests`.
- W1 guard: typed seam (no `ex.Message` classification), single canonical `SettlementErrorCodes`
  home with declared-code set, bilingual resource set registered + one resource per code, events in
  Contracts, capability-first Application with no single-file leaves, route/request inventory
  unchanged (10/10/4+6), Contracts-only boundary, unchanged migration set.
- W2 guard: capability-first shallow Application, no single-file use-case leaves, path↔namespace
  EXACT, root allowlists, slnx `/Modules/Settlement/` grouping, manifest↔disk.
- W3: fresh `ARCH-COMPLETE-002` certification + manifest honesty + SoT closure.

---

## 18. Certification Blockers (for W1–W3 to close)

1. `Contracts-Boundary-State = VIOLATION` → 3 integration events must move to `Contracts/Events`.
2. `Stable-Error-Code-State` → codes must live in `Contracts/Errors` with a declared-code surface; the
   3 raw literals must become declared members.
3. `Localization-State = MISSING_INFRASTRUCTURE_USE` → resource set + bilingual `.resx`; reachable
   Persian fallbacks repaired or honestly recorded.
4. Message-text fault classification → typed `Code` seam.
5. `File-Cohesion-State = MULTI_RESPONSIBILITY_COHESION_VIOLATION` → split
   `SettlementContracts.cs`; retire the mapper.
6. `Folder-Granularity-State = TECHNICAL_AXIS_FIRST` + 10 single-file leaves → `PROFESSIONAL_SHALLOW`
   (W2).
7. `OpenTelemetry-State = BYPASSED` → no-op instrumentation aligned to `ToobaTelemetry.Meter`.
8. Stale size-baseline entry for a non-existent `SettlementDomain.cs` → removed honestly.

---

## 19. Final Disposition

**`READY_TO_MIGRATE`** — ownership is correct, boundaries are Contracts-only for integration, and the
persistence/schema is module-owned. W1 performs the behavior-preserving canonical migration (typed
seam, contracts home for codes + events, localization, cohesion, capability-first foldering,
observability alignment), W2 owns the structural gate, W3 certifies and promotes the manifest/SoT.

`Structure-Handoff-State = REQUIRED`.
Stop gate: `USER_REVIEW_SETTLEMENT_AMSC_001_W0`; `automaticNextImplementationTask = NONE`.
