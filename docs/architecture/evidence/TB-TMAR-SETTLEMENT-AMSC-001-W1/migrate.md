# TB-TMAR-SETTLEMENT-AMSC-001 — Wave 1 (Migrate)

Skill: `tooba-architecture-migrate` (V2) · Wave 1 of the AMSC re-standardization of
`src/backend/Modules/Settlement`.
Baseline: `main` @ `bac4dbe3` (`HEAD == origin/main` at wave start; W0 analyze commit).
W0 verdict consumed: `READY_TO_MIGRATE`
(`docs/architecture/evidence/TB-TMAR-SETTLEMENT-AMSC-001-W0/analyze.md`).

Behavior preservation: routes, HTTP verbs, status codes, response bodies, DTO semantics, business
rules, state transitions, ordering, idempotency, transactions, persistence, schema, outbox event
names/versions and telemetry names are unchanged. Only the fault-mapping mechanism, the stable-code
home, the integration-event home, the localization surface, file cohesion, CQRS shape and physical
layout change.

Final objective: Settlement must be extractable as an independent microservice — zero cross-module
Application/Infrastructure/Domain coupling, Contracts-only boundaries.

---

## 1. Responsibility map (post-migration)

| Responsibility | Class | Owner | Location |
|---|---|---|---|
| `SettlementEntry` accrual/credit/debit/neutralize rules | DOMAIN_RULE | Settlement | `Domain/Aggregates` |
| `PayoutRequest` lifecycle (`Pending→Processing→Succeeded/Failed`) | DOMAIN_RULE | Settlement | `Domain/Aggregates` |
| `PayoutAttempt`, `SettlementAccount`, `SellerPayoutProfile`, `SettlementStatement` | DOMAIN_RULE | Settlement | `Domain/Aggregates` |
| `CommissionPolicy` + snapshot | DOMAIN_RULE | Settlement | `Domain/Entities`, `Domain/ValueObjects` |
| 3 domain events | DOMAIN_RULE | Settlement | `Domain/Events` |
| Seller balance/entries/statements/payout-request use cases | APPLICATION_USE_CASE | Settlement | `Application/Payouts/Queries` |
| Admin balances/queue/process/retry/grid use cases | APPLICATION_USE_CASE | Settlement | `Application/Payouts/{Commands,Queries}` |
| Orchestration + persistence (`settlement` schema) | PERSISTENCE | Settlement | `Infrastructure/Directories` |
| Accrual/refund/void/neutralize/restore orchestration | APPLICATION_USE_CASE | Settlement | `Infrastructure/Directories` |
| Admin grid DB-native query engine | APPLICATION_USE_CASE | Settlement | `Infrastructure/Queries` |
| Payout gateway port + fail-closed/fake impls | INTEGRATION_ADAPTER | Settlement | `Application/Payouts/Ports` + `Infrastructure/Gateways` |
| Order/Payment/Returns snapshot bridges | INTEGRATION_ADAPTER | Settlement | `Infrastructure/Bridges` |
| History/AdminOrderDetail/OrderAccrual contract adapters | INTEGRATION_ADAPTER | Settlement | `Infrastructure/Adapters` |
| Outbox registration + payment/refund inbox handlers | INTEGRATION_ADAPTER | Settlement | `Infrastructure/Messaging`, `Infrastructure/Handlers` |
| Metrics | OBSERVABILITY | Settlement | `Infrastructure/Observability` |
| Stable error codes + resource set | CONTRACT | Settlement | `Contracts/Errors` |
| Published integration events (3) | CONTRACT | Settlement | `Contracts/Events` |
| History/Operations contracts | CONTRACT | Settlement | `Contracts/{History,Operations}` |
| 10 HTTP routes | HTTP_ENDPOINT | Settlement | `Endpoints/{Seller,Admin}` |
| Seller/Admin authorizer seams | AUTHORIZATION_ADAPTER | Settlement | `Endpoints/{Seller,Admin}` |
| Host `MapSettlementEndpoints()` + 2 authorizer impls + module/migration registration | HOST_COMPOSITION_ROOT / SECURITY_ADAPTER | Host | Host (ALLOWED) |

`Ownership-State = correct`.

## 2. Repair 1 — Stable-code identity moved to the module boundary (W0 §7, §18.1/18.2)

- `SettlementErrorCodes` now lives in `Tooba.Settlement.Contracts/Errors/` — the single canonical home
  for the `settlement.*` / `payout.*` keyspace.
- The **declared-code surface** is complete: 17 `HttpReachable` codes + 1 `PlatformFaults` code
  (`settlement.outbox.unmapped_event`) = 18 declared, with `IsKnown` / `IsHttpReachable` /
  `IsPlatformFault` guards.
- The three codes that were previously **raw string literals** in `SettlementDirectory` and the error
  catalog (`settlement.unconfirm.payout_completed`, `settlement.cancel.payout_completed`,
  `settlement.restore.payout_completed`) are now declared `SettlementErrorCodes` members referenced by
  constant. No code value changed.
- `Tooba.Settlement.Domain` now raises its invariants by the canonical constants
  (`SettlementErrorCodes.AmountInvalid`, `.IdempotencyRequired`, `.PayoutInvalidState`) instead of
  inlining literal strings — the Domain therefore takes exactly one legal self-module `*.Contracts`
  edge (the established Cart / Payment / BulkInquiry precedent). No foreign Contracts edge is added.
- `SettlementErrorCatalogContributor` still registers exactly one descriptor per HTTP-reachable code;
  the platform fault is deliberately never catalogued. `ErrorCatalogUniqueCodeGuardTests` passes.

## 3. Repair 2 — Published integration events moved to Contracts (W0 §7, §18.1)

- The three published events (`SettlementEntryPostedIntegrationEvent`,
  `PayoutSucceededIntegrationEvent`, `PayoutFailedIntegrationEvent`) moved from
  `Application/Ports/SettlementContracts.cs` to `Contracts/Events/` — the certified
  Returns/Payment/Fulfillment convention, so a microservice extraction that keeps `Contracts` as the
  only inbound boundary no longer leaves the published events behind.
- Dependency direction is unchanged (`Infrastructure` already referenced `Contracts`); the move is
  dependency-neutral.
- Wire contract preserved byte-identically: `EventTypeName` (`settlement.entry.posted.v1`,
  `payout.succeeded.v1`, `payout.failed.v1`) and `Version = 1` are untouched.

## 4. Repair 3 — Canonical typed-fault seam (W0 §10, §18.4)

- New `Application/Composition/SettlementOperation.cs` — the module's single typed-fault → `Result`
  seam, mirroring the certified Returns/Payment/Inventory/Media/Party `*Operation` precedent:
  - `ExecuteAsync<T>(Func<Task<T>>)` + value-less `ExecuteAsync(Func<Task>)`;
  - `catch (ContractOperationException ex) when (SettlementErrorCodes.IsKnown(ex.Code))` →
    `Result.Failure(new SemanticError(ex.Code))`;
  - `catch (SemanticException ex)` → `Result.Failure(ex.Error)`;
  - `ToSemanticError(ContractOperationException)` rethrows unknown codes.
- Classification is by **typed code only**. Unknown codes and unknown exceptions propagate untouched
  to the canonical global exception boundary — a genuine defect is never silently converted into a
  business failure.
- `Application/Errors/SettlementExceptionMapper.cs` (message-text exact-match classification) and the
  whole `Application/Errors/` folder are **retired**.
- The three Application command handlers (`RequestSellerPayoutCommand`, `ProcessAdminPayoutCommand`,
  `RetryAdminPayoutCommand`) now dispatch through the seam. `FailClosedPayoutGateway` and
  `SettlementOutboxRegistration` already threw typed `ContractOperationException(code)`; they now
  reference the constants.
- Behavior preservation: the same directory/domain faults map to the same stable codes as before
  (`settlement.account.missing`, `settlement.payout.invalid_amount`, `settlement.payout.missing`,
  `payout.gateway.unconfigured`, `settlement.restore.payout_completed`), verified by the rewritten
  behavior tests.

## 5. Repair 4 — Localization (W0 §9, §18.3)

- New `Contracts/Errors/SettlementErrorResourceSet.cs` (`IErrorResourceSet`) owning the `settlement.` /
  `payout.` keyspace.
- New bilingual `Contracts/Resources/SettlementErrors.resx` + `SettlementErrors.fa.resx` — one entry
  per declared code (18), wired as embedded resources.
- Registered exactly once: `SettlementEndpointModule.AddSettlementEndpointPresentation` →
  `AddSingleton<IErrorResourceSet, SettlementErrorResourceSet>()`, invoked from Host `Program.cs`.
- Before this wave **zero** Settlement keys could resolve and only the generic `SafeTitleFallback` was
  returned; now every declared code resolves to real EN/FA text.
- **Accepted and not repaired (recorded honestly):** the single Persian display fallback
  (`SettlementDisplayLabels.UnknownSeller = "فروشنده"`, consumed by `ListAdminSettlementBalancesQuery`
  and `AdminPayoutGridQueryEngine`) is **display-label composition** for optional Party display data,
  not error text, matching the accepted certified Returns / Fulfillment / Catalog / Promotion
  work-queue convention. It is deliberately not part of the stable error-code / localization contract.

## 6. Repair 5 — File cohesion (W0 §13, §18.5)

| File | Before | After |
|---|---|---|
| `Application/Ports/SettlementContracts.cs` (398 LOC, mixed Application bundle + boundary events) | `MULTI_RESPONSIBILITY_COHESION_VIOLATION` | split into `Application/Payouts/Ports/{SettlementDirectoryPorts,SettlementReaderPorts,SettlementGatewayPorts,SettlementRestorePolicy,IAdminPayoutGridQuery}.cs` + `Contracts/Events/*` |
| `Application/Errors/SettlementExceptionMapper.cs` (97 LOC, message-text classification) | non-canonical mechanism | retired → `Application/Composition/SettlementOperation.cs` (typed seam) |
| `Infrastructure/Directories/SettlementDirectory.cs` (701 LOC, guard co-located) | `OVERSIZED_ONLY` | `OpenSettlementUseCaseGuard` extracted to its own file; the directory stays cohesive and is not cosmetically split |
| `Application/Ports/SettlementAdminModels.cs` + display fallbacks | mixed | `Application/Payouts/Models/AdminPayoutModels.cs` + `Application/Payouts/Models/SettlementDisplayLabels.cs` |

No new god-file was created.

## 7. Repair 6 — Capability-first shallow Application tree (W0 §14, §18.6)

```text
Application/
  Payouts/
    Commands/   RequestSellerPayoutCommand.cs, ProcessAdminPayoutCommand.cs, RetryAdminPayoutCommand.cs
    Queries/    GetSellerSettlementBalanceQuery.cs, ListSellerSettlementEntriesQuery.cs,
                ListSellerSettlementStatementsQuery.cs, ListSellerPayoutRequestsQuery.cs,
                ListAdminSettlementBalancesQuery.cs, ListAdminPayoutQueueQuery.cs,
                QueryAdminPayoutGridQuery.cs, AdminPayoutGridQueryPolicy.cs
    Models/     AdminPayoutModels.cs, SettlementDisplayLabels.cs
    Ports/      SettlementDirectoryPorts.cs, SettlementReaderPorts.cs, SettlementGatewayPorts.cs,
                SettlementRestorePolicy.cs, IAdminPayoutGridQuery.cs
  Composition/  SettlementOperation.cs
  Validation/   SettlementRequestValidators.cs, SettlementValidationCodes.cs
```

- The technical-axis-first roots (`Commands/<UseCase>`, `Queries/<UseCase>`, `Models/`, `Ports/`,
  `Validators/{Admin,Seller}`, `Errors/`) and the 10 single-file use-case leaf folders are retired —
  zero per-use-case subfolders remain under `Commands/` / `Queries/`.
- `Validation/` is a single flat capability leaf (the certified Returns / Promotion / AddressBook
  precedent) instead of the audience-first `Validators/{Admin,Seller}` split, because Settlement is a
  single-capability module (W0 §14 explicitly handed this decision to W2 and recommended the flat
  leaf).
- Every moved file's namespace exactly matches its new physical path.
- `GlobalUsings.Domain.cs` and `GlobalUsings.Layout.cs` (Application + Infrastructure) are **retired**;
  every file now imports exactly what it uses, so no global alias can hide a wrong namespace.
  `tmar-module-structure-manifests.json` Settlement `rootAllowlist` entries were updated to `[]` and
  the two files added to `forbiddenRootFiles` so the change is durably locked.

## 8. Repair 7 — Transport validator coverage (W0 §8a, §18.6)

- `Application/Validation/SettlementValidationCodes.cs` (4 stable `settlement.validation.*` machine
  codes) and `Application/Validation/SettlementRequestValidators.cs` with **4** FluentValidation
  validators covering exactly the 4 `VALIDATOR_REQUIRED` requests:

| Request | Validator | Transport-shape rules |
|---|---|---|
| `RequestSellerPayoutCommand` | `RequestSellerPayoutCommandValidator` | `Amount > 0`; `IdempotencyKey` non-blank |
| `ProcessAdminPayoutCommand` | `ProcessAdminPayoutCommandValidator` | `PayoutRequestId != Guid.Empty` |
| `RetryAdminPayoutCommand` | `RetryAdminPayoutCommandValidator` | `PayoutRequestId != Guid.Empty` |
| `QueryAdminPayoutGridQuery` | `QueryAdminPayoutGridQueryValidator` | grid request body non-null |

- The remaining 6 requests stay `NO_VALIDATOR_REQUIRED` with durable provenance: 4
  `AUTH_SCOPED_QUERY` (`GetSellerSettlementBalanceQuery`, `ListSellerSettlementEntriesQuery`,
  `ListSellerSettlementStatementsQuery`, `ListSellerPayoutRequestsQuery` — seller party is
  server-derived by `ISettlementSellerAuthorizer`) and 2 `NO_INPUT`
  (`ListAdminSettlementBalancesQuery`, `ListAdminPayoutQueueQuery`).
- Set equality: 10 shipped routes = 10 reachable requests = 4 + 6 classified exactly once.
- Validators emit machine codes only (`WithMessage(` never appears) and own transport shape only — no
  ownership/balance/state/pricing/grid-policy rule is duplicated; `AdminPayoutGridQueryPolicy` keeps
  the grid whitelist/normalization authority.

## 9. Repair 8 — Observability (W0 §12, §18.7)

`SettlementInstrumentation` runs on the single `ToobaTelemetry.Meter` with the unchanged counter names
(`tooba.settlement.entry.posted`, `tooba.settlement.payout.succeeded`, `tooba.settlement.payout.failed`)
and unchanged method names. No second `Meter`/`ActivitySource` and no raw `StartActivity` exists in the
module (durable guard).

## 10. Boundary / coupling state

- `Cross-Module-Coupling-State = LEGAL_CONTRACTS_ONLY`: the only foreign references are the four legal
  `*.Contracts` edges (Order, Payment, Returns, Party) plus the platform BuildingBlocks/Persistence/
  ModuleContracts projects.
- Zero foreign `.Application` / `.Infrastructure` / `.Domain` reference; zero foreign `DbContext`/`DbSet`;
  zero cross-module EF/SQL join.
- `Endpoints` references only `Settlement.Application` + `Settlement.Contracts` — never
  `Settlement.Infrastructure` or Host.
- `Application` references `BuildingBlocks`, `Settlement.Contracts`, `Settlement.Domain`, `Party.Contracts`
  — never Infrastructure or Host.
- `Domain` references `BuildingBlocks` + the one self-module `Settlement.Contracts` edge.
- Host residue: `ALLOWED_COMPOSITION_ROOT` (`Program.cs`: CQRS assembly +
  `AddSettlementEndpointPresentation` + `MapSettlementEndpoints()` + the two authorizer registrations)
  and `ALLOWED_SECURITY_ADAPTER` (`Security/Seller/HostSettlementSellerAuthorizer.cs`,
  `Admin/Access/Authorizers/HostSettlementAdminAuthorizer.cs`). No `Host/Settlement`, no `Host/Grid`, no
  Settlement DbContext in Host.
- No `GlobalUsings*.cs` alias file survives anywhere in the module; no `TypeForwardedTo`; no
  compatibility shim.

## 11. Persistence / schema state

`UNCHANGED`. The migration ID `20260827030000_InitialSettlement`, its Up/Down and the model snapshot are
untouched; the `settlement` schema, DbContext, inbox records and outbox registration are unchanged. No
migration was regenerated for the structural cleanup.

## 12. Focused validation

| Command | Result |
|---|---|
| `dotnet build Tooba.Settlement.Domain` | **0 errors** |
| `dotnet build Tooba.Host` | **0 errors** |
| `dotnet build Tooba.Settlement.Tests` | **0 errors** |
| `dotnet test Tooba.Settlement.Tests` | **30 passed / 0 failed** |
| `dotnet test Tooba.Settlement.Tests --filter Architecture` | **17 passed / 0 failed** |
| `dotnet test Tooba.Host.Tests --filter SettlementModuleAmsc001W1MigrateGuardTests` | **12 passed / 0 failed** |
| `dotnet test Tooba.Host.Tests --filter "Settlement\|Marketplace\|AdminOrder\|ErrorCatalog"` | **84 passed / 3 skipped / 1 failed** |

New durable guard:
`src/backend/Host/Tooba.Host.Tests/Architecture/SettlementModuleAmsc001W1MigrateGuardTests.cs` (12 tests)
locks the single stable-code home + declared/reachability split, the bilingual resource set registered
once, the Contracts home + unchanged wire contract of the three events, the typed seam (no message
parsing, retired mapper), the Domain's constant-based invariants and its single self-module Contracts
edge, the 4-validator matrix with the retired audience-first tree, the capability-first Application
layout with exact path↔namespace, the Contracts-only boundary, the zero-global-using rule, the Host
composition/security residue, the single-meter observability, and the unchanged migration set.

### Pre-existing failures (reproduced at the wave-start baseline, not caused by this wave)

Verified by running the same filters in a clean `git worktree` at `bac4dbe3`:

- `PaidProjectionFinancialTests` (3 of 5) — the test `Read(...)` helper expects Order/Inventory paths
  that do not exist at `HEAD`; identical failure count and names at baseline.
- `TmarSourceSizeAndInfraAppTests` (3 of 6) — stale repository-wide source-size baseline (13 baseline
  entries point at files that no longer exist, one of them Settlement's pre-split `SettlementDomain.cs`),
  a stale source-size inventory `fileCount`, and a stale Infrastructure→foreign-Application edge
  baseline; identical at baseline.
- `TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment`
  — the pre-existing `Tooba.Catalog.Contracts.Cart` namespace deviation (explicitly documented in that
  test's Inventory note as out of scope for a module-local certification); identical at baseline.

The Settlement `Application`/`Infrastructure` `rootAllowlist` entries in
`docs/architecture/tmar-module-structure-manifests.json` were updated in this wave because W1 itself
retired the two `GlobalUsings` files they listed; the manifest now truthfully records the new state and
the deleted files are added to `forbiddenRootFiles`.

## 13. Wave disposition

`READY_FOR_STRUCTURE` — ownership correct, behavior preserved, canonical mechanisms in place,
boundaries Contracts-only.
`Structure-Handoff-State = REQUIRED` (W2 owns the final physical/solution gate).
