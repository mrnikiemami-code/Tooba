# TB-TMAR-RETURNS-AMSC-001 — Wave 1 (Migrate)

Skill: `tooba-architecture-migrate` (V2) · Wave 1 of the AMSC re-standardization of
`src/backend/Modules/Returns`.
Baseline: `main` @ `f5c5a6db` (`HEAD == origin/main` at wave start; W0 analyze commit).
W0 verdict consumed: `READY_TO_MIGRATE`
(`docs/architecture/evidence/TB-TMAR-RETURNS-AMSC-001-W0/analyze.md`).

Behavior preservation: routes, HTTP verbs, status codes, response bodies, DTO semantics, business
rules, state transitions, ordering, idempotency, transactions, persistence, schema, outbox event
names, telemetry names and tenant/commerce scoping are unchanged. Only fault-mapping mechanism,
localization surface, file cohesion, CQRS shape and physical layout change.

Final objective: Returns must be extractable as an independent microservice — zero cross-module
Application/Infrastructure/Domain coupling, Contracts-only boundaries.

---

## 1. Responsibility map (post-migration)

| Responsibility | Class | Owner | Location |
|---|---|---|---|
| ReturnRequest / ReturnItem / RefundAttempt lifecycle | DOMAIN_RULE | Returns | `Domain/{Aggregates,ValueObjects,Events}` |
| Create/Approve/Reject/Retry use cases | APPLICATION_USE_CASE | Returns | `Application/ReturnRequests/Commands` |
| Get/List/QueryGrid use cases | APPLICATION_USE_CASE | Returns | `Application/ReturnRequests/Queries` |
| Orchestration + persistence | PERSISTENCE | Returns | `Infrastructure/Directories` |
| Eligibility evaluation (Order+Fulfillment via Contracts) | APPLICATION_USE_CASE | Returns | `Infrastructure/Evaluators` |
| Admin grid DB-native query | APPLICATION_USE_CASE | Returns | `Infrastructure/Queries` |
| Inventory restock adapter | INTEGRATION_ADAPTER | Returns | `Infrastructure/Gateways` |
| Settlement snapshot bridge | INTEGRATION_ADAPTER | Returns | `Infrastructure/Bridges` |
| History / AdminOperations contract adapters | INTEGRATION_ADAPTER | Returns | `Infrastructure/Adapters` |
| Outbox registration | INTEGRATION_ADAPTER | Returns | `Infrastructure/Messaging` |
| Metrics | OBSERVABILITY | Returns | `Infrastructure/Observability` |
| Stable error codes + resource set | CONTRACT | Returns | `Contracts/Errors` |
| History/Settlement/Operations/Events contracts | CONTRACT | Returns | `Contracts/{History,Settlement,Operations,Events}` |
| 11 HTTP routes | HTTP_ENDPOINT | Returns | `Endpoints/{Admin,Seller,Customer}` |
| Host `MapReturnEndpoints()` + 2 authorizer impls | HOST_COMPOSITION_ROOT / SECURITY_ADAPTER | Host | Host (ALLOWED) |

`Ownership-State = correct`.

## 2. Repair 1 — Canonical typed-fault seam (W0 §10, §4.2)

- New `Application/Composition/ReturnsOperation.cs` — the module's single typed-fault→`Result` seam,
  mirroring the certified Promotion/Inventory/Media/Party/Payment `*Operation` precedent:
  - `ExecuteAsync<T>(Func<Task<T>>)` + value-less `ExecuteAsync(Func<Task>)`;
  - `catch (ContractOperationException ex) when (ReturnsErrorCodes.IsKnown(ex.Code))` →
    `Result.Failure(new SemanticError(ex.Code))`;
  - `catch (SemanticException ex)` → `Result.Failure(ex.Error)`;
  - `ToSemanticError(ContractOperationException)` rethrows unknown codes.
- Classification is by **typed code only**. Unknown codes and unknown exceptions propagate untouched
  to the canonical global exception boundary — a genuine defect is never silently converted into a
  business failure.
- `Application/Errors/ReturnsExceptionMapper.cs` (169 LOC, `TryMapExact` + ~18 `ex.Message` alias
  literals) is **retired**; the `Application/Errors/` folder is removed entirely.
- Zero `ex.Message` / `.Message.Contains(` / `.Message.StartsWith(` / `.Message ==` classification
  remains in Returns production (durable guard).
- Behavior preservation: the same directory/domain faults map to the same codes
  (`return.missing`→`return.missing`, `return.stale`, `return.quantity_exceeded`, `refund.*`) — the
  switch is from message text to the typed `Code` property.

## 3. Repair 2 — Transport destination parser split (W0 §4.2)

- New `Application/Composition/ReturnRefundDestinationParser.cs` — transport-shape parsing of the
  wire refund-destination token into the typed `RefundDestination`; absent → shipped default
  (`OriginalPayment`), unparseable → stable `refund.destination.invalid`.
- Consumed by the customer create route and the seller approve route instead of the retired mapper's
  `ParseDestination` (which threw Persian prose).
- Dead `Application/Ports/ReturnSemanticMapper.cs` (zero callers) is **retired**.

## 4. Repair 3 — Duplicate CQRS command shapes removed (W0 §4.1)

- The four duplicate command-shaped records in `Application/Models`
  (`ApproveReturnCommand`, `CreateReturnCommand`, `RejectReturnCommand`, `RetryRefundCommand`) are
  **deleted**. The authoritative MediatR requests are the single shape.
- `IReturnDirectory` and its implementation now consume the authoritative
  `ReturnRequests.Commands.*` requests directly.

## 5. Repair 4 — Capability-first shallow Application tree (W0 §14)

```text
Application/
  ReturnRequests/
    Commands/   CreateReturnCommand.cs, ApproveReturnCommand.cs, RejectReturnCommand.cs, RetryReturnRefundCommand.cs
    Queries/    GetAdminReturnQuery.cs, GetCustomerReturnQuery.cs, GetSellerReturnQuery.cs,
                ListAdminReturnsQuery.cs, ListCustomerReturnsQuery.cs, ListSellerReturnsQuery.cs,
                QueryAdminReturnsGridQuery.cs
    Models/     ReturnSnapshot.cs, ReturnItemSnapshot.cs, RefundAttemptSnapshot.cs, ReturnLineCommand.cs,
                ReturnEligibilityResult.cs, ReturnLineEligibility.cs, ReturnEligibilityReasonCodes.cs,
                AdminReturnWorkQueueRow.cs, AdminReturnQueueFilters.cs
    Ports/      IReturnDirectory.cs, IReturnEligibilityEvaluator.cs, IReturnInventoryGateway.cs,
                IReturnUseCaseGuard.cs
  Composition/  ReturnsOperation.cs, ReturnRefundDestinationParser.cs
  Validation/   ReturnsRequestValidators.cs, ReturnsValidationCodes.cs
```

- The technical-axis-first roots (`Commands/<UseCase>`, `Queries/<UseCase>`, `Models/`, `Ports/`,
  `Errors/`) and the 11 single-file use-case leaf folders are retired — zero per-use-case
  subfolders remain under `Commands/` / `Queries/`.
- Every moved file's namespace was updated to exactly match its new path.

## 6. Repair 5 — File cohesion (W0 §13)

| File | Before | After |
|---|---|---|
| `AdminReturnWorkQueueModels.cs` (163 LOC, read model + projection policy) | mixed | split into `AdminReturnWorkQueueRow.cs` + `AdminReturnQueueFilters.cs` |
| `ReturnsExceptionMapper.cs` (169 LOC, fault seam + destination parser) | mixed | split into `Composition/ReturnsOperation.cs` + `Composition/ReturnRefundDestinationParser.cs`; message heuristic dropped |
| `ReturnDirectory.cs` (426 LOC, guard co-located) | oversized-only | `OpenReturnUseCaseGuard` extracted to its own file |

No new god-file was created.

## 7. Repair 6 — Transport validator coverage (W0 §8a, HARD BLOCKER)

- New `Application/Validation/ReturnsValidationCodes.cs` (11 stable `returns.validation.*` machine
  codes) and `Application/Validation/ReturnsRequestValidators.cs` with **4** FluentValidation
  validators covering exactly the 4 `VALIDATOR_REQUIRED` requests:

| Request | Validator | Transport-shape rules |
|---|---|---|
| `CreateReturnCommand` | `CreateReturnCommandValidator` | `SellerOrderId != Guid.Empty`; `IdempotencyKey` required/≤128; `Reason` ≤512; `Items` non-empty; each `OrderLineId != Guid.Empty`; each `Quantity > 0`; `RefundDestination` in enum |
| `ApproveReturnCommand` | `ApproveReturnCommandValidator` | `ReturnRequestId != Guid.Empty`; `RefundDestination` in enum when supplied |
| `RejectReturnCommand` | `RejectReturnCommandValidator` | `ReturnRequestId != Guid.Empty`; `Reason` ≤512 |
| `QueryAdminReturnsGridQuery` | `QueryAdminReturnsGridQueryValidator` | grid request body non-null |

- The remaining 7 requests stay `NO_VALIDATOR_REQUIRED` with durable provenance (route `:guid`
  constraint and/or server-derived actor/seller party):
  `ListCustomerReturnsQuery`, `GetCustomerReturnQuery`, `ListSellerReturnsQuery`,
  `GetSellerReturnQuery`, `ListAdminReturnsQuery`, `GetAdminReturnQuery`, `RetryReturnRefundCommand`.
- Set equality: 11 shipped routes = 11 reachable requests = 4 + 7 classified exactly once.
- Validators emit machine codes only (`WithMessage(` never appears) and own transport shape only —
  no ownership/eligibility/state/pricing/quantity-remaining rule is duplicated.
- Discovery: `AddToobaCqrsFoundation` runs `AddValidatorsFromAssembly` over the Returns Application
  assembly registered in `Host/Program.cs` (updated to
  `Tooba.Returns.Application.ReturnRequests.Commands.CreateReturnCommand`).

## 8. Repair 7 — Localization (W0 §9, §18)

- New `Contracts/Errors/ReturnsErrorResourceSet.cs` (`IErrorResourceSet`) owning the `return.` /
  `refund.` keyspace.
- New bilingual `Contracts/Resources/ReturnsErrors.resx` + `ReturnsErrors.fa.resx` — one entry per
  declared code (21), wired as embedded resources with explicit `LogicalName` in the csproj.
- Registered exactly once: `ReturnEndpointModule.AddReturnEndpointPresentation` →
  `AddSingleton<IErrorResourceSet, ReturnsErrorResourceSet>()`.
- Before this wave **zero** Returns keys could resolve and only the generic `SafeTitleFallback` was
  returned; now every declared Returns code resolves to real EN/FA text.
- The dead hard-coded-Persian `ReturnEligibilityReasonCodes.ToFaMessage` (zero callers) is **removed**;
  the Application-local alias now delegates the stable vocabulary + reason→code mapping to the
  boundary contract `ReturnEligibilityReasons` instead of duplicating literals.
- Endpoint hard-coded English `"customer.actor.missing"` → canonical Foundation-owned
  `FoundationErrorCodes.CustomerSessionRequired` (`customer.session.required`); the Returns catalog
  deliberately does **not** re-register that descriptor.
- **Accepted and not repaired (recorded honestly):** `AdminReturnQueueFilters.ComposeEligibilitySummary`
  and the `AdminReturnGridQueryEngine` display-label fallbacks (`"کالای سفارش"`, `"مرجوعی"`,
  `"مشتری"`, `"فروشنده"`, `"واحد"`, `"غیرقابل مرجوعی"`, `"منقضی"`) are **display-label composition**,
  not error messages, matching the accepted certified `Fulfillment`
  (`AdminFulfillmentWorkQueueQueryEngine` `"فروشنده"`), `Catalog`, `Promotion`
  (`MerchandisingCampaignAdminComposer` `"کالا"` / `"فروشنده"` / `"بدون عنوان"`) work-queue convention.
  They are not part of the stable error-code / localization contract and are not API error text; the
  remaining `eligibilitySummary` text is data-derived (snapshot policy label passed through unchanged).
  `AdminReturnWorkQueueTests.Eligibility_summary_uses_snapshot_not_current_offer` locks this
  behavior.

## 9. Repair 8 — Stable-code catalog (W0 §18.3)

- `Contracts/Errors/ReturnsErrorCodes.cs` is the single canonical home: 20 HTTP-reachable codes +
  1 platform-side outbox fault (`return.outbox.unmapped_event`), with `IsKnown` / `IsHttpReachable` /
  `IsPlatformFault` declared-set guards.
- `ReturnsErrorCatalogContributor` registers exactly the 20 `HttpReachable` descriptors, each exactly
  once; the platform fault is deliberately never catalogued. `ErrorCatalogUniqueCodeGuardTests` passes
  over the composed catalog.
- No code value changed; `customer.session.required` remains Foundation-owned and is consumed
  without re-registration.

## 10. Boundary / coupling state

- `Cross-Module-Coupling-State = LEGAL_CONTRACTS_ONLY`: the only foreign references are the seven
  legal `*.Contracts` edges (Order, Fulfillment, Payment, Wallet, Inventory, Party, Catalog).
- Zero foreign `.Application` / `.Infrastructure` / `.Domain` reference; zero foreign
  `DbContext`/`DbSet`; zero cross-module EF/SQL join.
- `Endpoints` references only `Returns.Application` — never `Returns.Infrastructure` or Host.
- Host residue: `ALLOWED_COMPOSITION_ROOT` (`Program.cs`: CQRS assembly + `AddReturnEndpointPresentation`
  + `MapReturnEndpoints()`) and `ALLOWED_SECURITY_ADAPTER`
  (`Security/Seller/HostReturnSellerAuthorizer.cs`, `Admin/Access/Authorizers/HostReturnAdminAuthorizer.cs`).
  No `Host/Returns`, no `Host/Grid`, no Returns DbContext in Host.

## 11. Persistence / schema state

`UNCHANGED`. The 3 migration IDs, their order, Up/Down and the model snapshot are untouched; the
Returns `returns` schema, DbContext and outbox registration are unchanged. No migration was
regenerated for the structural cleanup.

## 12. Focused validation

| Command | Result |
|---|---|
| `dotnet build src/backend/Tooba.slnx` | **0 errors** |
| `dotnet test Tooba.Returns.Tests` | **16 passed / 0 failed** |
| `dotnet test Tooba.Host.Tests --filter "Return\|ErrorCatalogUniqueCode"` | **107 passed / 5 skipped / 0 failed** |
| `dotnet test Tooba.Host.Tests --filter ReturnsModuleAmsc001W1MigrateGuardTests` | **12 passed / 0 failed** |

New durable guard: `src/backend/Host/Tooba.Host.Tests/Architecture/ReturnsModuleAmsc001W1MigrateGuardTests.cs`
locks the single stable-code home + reachability split, the typed seam (no message parsing, retired
mapper/`TryMapExact`), the destination parser, the resource set + bilingual coverage, the 4-validator
matrix, the capability-first Application layout with no technical-axis roots, the retired duplicate
command shapes and dead `ToFaMessage`, the Contracts-only boundary, the Foundation session code, and
the unchanged migration set.

## 13. Wave disposition

`READY_FOR_STRUCTURE` — ownership correct, behavior preserved, canonical mechanisms in place,
boundaries Contracts-only.
`Structure-Handoff-State = REQUIRED` (W2 owns the final physical/solution gate).
