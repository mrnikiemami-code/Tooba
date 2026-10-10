# TB-TMAR-WALLET-AMSC-001-W0 — Analyze (`tooba-architecture-analyze`)

```text
TASK:          TB-TMAR-WALLET-AMSC-001-W0
MODE:          ARCHITECT_DIRECT_AMSC
SKILL:         tooba-architecture-analyze
TARGET:        src/backend/Modules/Wallet/Tooba.Wallet.*
STARTING HEAD: 4f875042
STATE:         ANALYZE_COMPLETE
VERDICT:       READY_TO_MIGRATE
LOCK VERSION:  ARCH-COMPLETE-002
```

Read-only wave. No production file was changed. This document is the applicability gate, the
request/validator provenance matrix, the coupling/persistence audit, the localization + API-result
audit and the exact target foldering that W1/W2/W3 must realize.

---

## 0. Applicability Gate (MANDATORY BEFORE TARGET SHAPE)

**Classification: `HTTP_OWNING`.**

Evidence — the module owns a real `Tooba.Wallet.Endpoints` project with a non-empty route group and
two capability endpoint classes, mapped from Host composition:

```text
src/backend/Modules/Wallet/Tooba.Wallet.Endpoints/
  WalletEndpointModule.cs                 (MapWalletEndpoints -> Customer + Admin)
  Customer/WalletCustomerEndpoints.cs     (3 routes)
  Admin/WalletAdminEndpoints.cs           (8 routes)
src/backend/Host/Tooba.Host/Program.cs
  line  60: using Tooba.Wallet.Endpoints;
  line  97: builder.Services.AddWalletEndpointPresentation();
  line 437: app.MapWalletEndpoints();
```

Therefore endpoint ownership, CQRS/MediatR and the exhaustive validator matrix are **applicable** and
must be certified rather than excused. This is *not* an `INTERNAL_ONLY` module: no ceremonial
Endpoints project exists, the route group is non-empty (11 mapped operations), and the Host owns
**zero** Wallet routes.

---

## 1. Target analyzed

```text
projects = Tooba.Wallet.Application, Tooba.Wallet.Contracts, Tooba.Wallet.Domain,
           Tooba.Wallet.Endpoints, Tooba.Wallet.Infrastructure, Tooba.Wallet.Tests
per-project .cs = {Application:17, Contracts:3, Domain:8, Endpoints:9, Infrastructure:7, Tests:5}
total .cs = 49   path<->namespace mismatches = 0
```

| Project | Assembly | References (csproj) |
| --- | --- | --- |
| `Tooba.Wallet.Domain` | `Tooba.Wallet.Domain` | BuildingBlocks **only** |
| `Tooba.Wallet.Contracts` | `Tooba.Wallet.Contracts` | *(none)* |
| `Tooba.Wallet.Application` | `Tooba.Wallet.Application` | BuildingBlocks, Wallet.Domain, Wallet.Contracts |
| `Tooba.Wallet.Infrastructure` | `Tooba.Wallet.Infrastructure` | Wallet.Application, Notification.Contracts, ModuleContracts, Persistence |
| `Tooba.Wallet.Endpoints` | `Tooba.Wallet.Endpoints` | Wallet.Application, BuildingBlocks, `Microsoft.AspNetCore.App` |
| `Tooba.Wallet.Tests` | `Tooba.Wallet.Tests` | Domain, Application, Contracts, Infrastructure, Endpoints, Notification.Contracts, BuildingBlocks |

Note: `Tooba.Wallet.Domain` does **not** reference its own `Tooba.Wallet.Contracts` today, which is why
the stable codes currently live in `Tooba.Wallet.Application.Errors` instead of the canonical
Contracts home. W1 must fix this.

---

## 2. Responsibility map

| Capability | Surface | Files |
| --- | --- | --- |
| Customer wallet summary + ledger | Application/Endpoints | `GetCustomerWalletSummaryQuery`, `ListCustomerWalletLedgerQuery`, `WalletCustomerEndpoints` |
| Customer gift-card redemption | Application/Endpoints/Domain/Infrastructure | `RedeemCustomerGiftCardCommand`, `GiftCard`, `GiftCardRedemption`, `WalletDirectory.RedeemGiftCardForCustomerAsync` |
| Admin gift-card issue/list/get/revoke | Application/Endpoints/Domain/Infrastructure | `IssueAdminGiftCardCommand`, `ListAdminGiftCardsQuery`, `GetAdminGiftCardQuery`, `RevokeAdminGiftCardCommand` |
| Admin wallet inspect/ledger/adjust | Application/Endpoints/Domain/Infrastructure | `GetAdminWalletQuery`, `ListAdminWalletLedgerQuery`, `AdjustAdminWalletCommand` |
| Admin development demo-preview | Application/Endpoints/Infrastructure | `GetWalletDemoPreviewQuery`, `IWalletDemoPreviewPort`, `WalletDemoPreviewAdapter` |
| Order-payment debit (cross-module port) | Contracts/Infrastructure | `IWalletOrderPaymentPort`, `WalletDirectory.SpendForOrderPaymentAsync` |
| Refund credit (cross-module port) | Contracts/Infrastructure | `IWalletRefundCreditPort`, `WalletDirectory.CreditRefundAsync` |
| Checkout quote (cross-module port) | Contracts/Infrastructure | `IWalletOrderPaymentPort.QuoteForPayableAsync` |
| Development seed | Infrastructure | `WalletDevelopmentSeed`, `WalletDevelopmentSeedBootstrap`, `WalletDemoSnapshot` |

---

## 3. Ownership map

| Concern | Current owner | Canonical target |
| --- | --- | --- |
| Module HTTP routes | `Tooba.Wallet.Endpoints` (correct) | unchanged |
| CQRS requests/handlers | `Tooba.Wallet.Application` (correct) | unchanged |
| Domain aggregates/invariants | `Tooba.Wallet.Domain` (correct) | unchanged |
| Persistence + schema `wallet` | `Tooba.Wallet.Infrastructure` (correct) | unchanged |
| Cross-module ports | `Tooba.Wallet.Contracts.{Payments,Refunds}` (correct) | unchanged |
| **Stable error codes** | `Tooba.Wallet.Application.Errors.WalletErrorCodes` (**wrong**) | `Tooba.Wallet.Contracts.Errors.WalletErrorCodes` |
| **Error catalog + resource set** | `Tooba.Wallet.Endpoints.{Errors,Resources}` (correct — Support precedent) | unchanged |
| **Admin authorization codes** | `Tooba.Wallet.Endpoints.Admin.WalletAdminAuthorizationCodes` (correct — Support precedent) | unchanged |

---

## 4. Current illegal dependencies

Machine scan over all six projects (`inspect.cjs`):

```text
foreign Application/Infrastructure/Domain/Endpoints edges (production) = 0
```

The only matches are inside `Tooba.Wallet.Tests/Architecture/WalletArchitectureGuardTests.cs`, where
the strings `Tooba.Notification.Application|Domain|Infrastructure` are **negative assertions**
(`Assert.DoesNotContain`), not dependencies. **Zero production foreign-module coupling.**

```text
Cross-Module-Coupling-State = NONE
```

The single legal foreign edge is `Tooba.Wallet.Infrastructure -> Tooba.Notification.Contracts`
(the canonical `INotificationCreationPort` used for wallet notifications), which is a Contracts-only
edge and therefore legal.

---

## 5. Cross-module join inventory

```text
Cross-Module-Join-State = NONE
```

- One `WalletDbContext` owning schema `wallet`; four `DbSet`s (`Accounts`, `LedgerEntries`,
  `GiftCards`, `Redemptions`) plus the module outbox.
- Zero `DbContext`/`DbSet` access outside `Tooba.Wallet.Infrastructure`.
- Zero cross-module FK, zero shared table, zero EF join across module boundaries.
- `Tooba.Wallet.Infrastructure/Directories/WalletDirectory.cs` touches only `wallet.*` tables.
- `WalletLedgerEntry.SourceId`/`SourceType` are deliberately FK-free references (documented), so no
  cross-module referential coupling exists.

---

## 6. Contracts-only replacement map

| Consumer | Current boundary | Verdict |
| --- | --- | --- |
| `Tooba.Payment.Infrastructure.Providers.WalletPaymentGateway` | `Tooba.Wallet.Contracts.Payments.IWalletOrderPaymentPort` + `Tooba.Wallet.Contracts.Dtos.WalletCurrency` | **LEGAL** |
| `Tooba.Payment` refund path | `Tooba.Wallet.Contracts.Refunds.IWalletRefundCreditPort` | **LEGAL** |
| `Tooba.Wallet.Infrastructure` → Notification | `Tooba.Notification.Contracts.Ports.INotificationCreationPort` | **LEGAL** |
| Host admin auth adapter | `Tooba.Wallet.Endpoints.Admin.{IWalletAdminAuthorizer,WalletAdminAuthorizationCodes}` | **LEGAL** |

`Microservice-Extractable` is blocked only by the mislocated stable-code home (§10), not by coupling.

---

## 7. CQRS / MediatR gaps

All 11 endpoint-reachable requests are real `IRequest<Result<…>>` records with exactly one
`IRequestHandler<…>` each, dispatched through `ISender` from thin endpoints. No endpoint calls a
directory or a `DbContext` directly. MediatR is registered through the canonical
`AddToobaCqrsFoundation` (MediatR **12.5.0**, FluentValidation + `TracingBehavior` →
`ValidationBehavior` → `LoggingBehavior`), and the Wallet Application assembly is passed at
`Program.cs:178`.

```text
CQRS-State = COMPLIANT_11_OF_11_REAL_IREQUEST_AND_HANDLER_ISENDER
```

**Gap (W1):** five handlers classify faults through the legacy `WalletExceptionMapper` message-code
set instead of a typed-fault seam, and the mapper's `KnownCodes` set is a *second* stable-code
declaration that lives in Application rather than Contracts.

---

## 8. Validation classification matrix

Derived from the actual endpoint request construction and route templates on disk.

**11 shipped routes == 11 endpoint-reachable requests == 4 VALIDATOR_REQUIRED + 7 NO_VALIDATOR_REQUIRED.**

| # | Route + verb | Request | Class | Reason |
| --- | --- | --- | --- | --- |
| 1 | `GET /v1/customer/wallet` | `GetCustomerWalletSummaryQuery` | NO_VALIDATOR_REQUIRED | actor server-derived by `IWalletCustomerAuthorizer`; no caller-controlled shape |
| 2 | `GET /v1/customer/wallet/ledger` | `ListCustomerWalletLedgerQuery` | NO_VALIDATOR_REQUIRED | actor server-derived; `page`/`pageSize` bounded by the executed canonical directory clamp (`Math.Max(1, page)` / `Math.Clamp(pageSize, 1, 100)`) |
| 3 | `POST /v1/customer/wallet/gift-cards/redeem` | `RedeemCustomerGiftCardCommand` | **VALIDATOR_REQUIRED** | caller body: `Code` (unbounded string), `IdempotencyKey` (unbounded string) |
| 4 | `GET /v1/admin/gift-cards` | `ListAdminGiftCardsQuery` | **VALIDATOR_REQUIRED** | caller query: `status` (free-text enum), `q` (unbounded search) |
| 5 | `POST /v1/admin/gift-cards` | `IssueAdminGiftCardCommand` | **VALIDATOR_REQUIRED** | caller body: `InitialAmount`, `Currency`, `IdempotencyKey` |
| 6 | `GET /v1/admin/gift-cards/{cardId:guid}` | `GetAdminGiftCardQuery` | NO_VALIDATOR_REQUIRED | `:guid` route constraint only |
| 7 | `POST /v1/admin/gift-cards/{cardId:guid}/revoke` | `RevokeAdminGiftCardCommand` | NO_VALIDATOR_REQUIRED | `:guid` route constraint only |
| 8 | `GET /v1/admin/wallets/{customerActorUserId:guid}` | `GetAdminWalletQuery` | NO_VALIDATOR_REQUIRED | `:guid` route constraint only |
| 9 | `GET /v1/admin/wallets/{customerActorUserId:guid}/ledger` | `ListAdminWalletLedgerQuery` | NO_VALIDATOR_REQUIRED | `:guid` route constraint; `page`/`pageSize` bounded by the canonical directory clamp |
| 10 | `POST /v1/admin/wallets/{customerActorUserId:guid}/adjustments` | `AdjustAdminWalletCommand` | **VALIDATOR_REQUIRED** | caller body: `Direction` (free-text enum), `Reason` (unbounded), `Amount`, `IdempotencyKey` |
| 11 | `GET /v1/admin/wallet/demo-preview` | `GetWalletDemoPreviewQuery` | NO_VALIDATOR_REQUIRED | parameterless; `Development`-gated at the endpoint (`Results.NotFound()` outside Development) |

**W0 finding §8:** **zero** transport validators currently exist in the Wallet module
(`rg AbstractValidator` → no hits). The four `VALIDATOR_REQUIRED` requests are today protected only by
domain/directory invariants that surface as `wallet.*`-mapped 400s. W1 must add the four transport
validators emitting stable `wallet.validation.*` machine codes.

Optional input is **not** treated as safety: every `NO_VALIDATOR_REQUIRED` row rests on a `:guid`
route constraint, a server-derived actor, an executed canonical clamp policy, or a
Development-gated parameterless request.

---

## 9. Localization findings

### 9.1 Defect A — the stable-code home is in Application, not Contracts

`Tooba.Wallet.Application/Errors/WalletErrorCodes.cs` declares **11** `public const string` codes.
`Tooba.Wallet.Domain` does **not** reference `Tooba.Wallet.Contracts`, so the Domain and Infrastructure
cannot consume the module's stable codes at all. Consequence: the Domain throws raw
`InvalidOperationException("<literal>")` and the Infrastructure/Application layer must *re-parse those
literals* to classify the fault (§10). This is the root cause of the whole W1 defect set.

### 9.2 Defect B — a second, shadow stable-code declaration

`Tooba.Wallet.Application/Errors/WalletExceptionMapper.cs` holds a **43-entry** `KnownCodes` set —
a second declaration of Wallet stable-code identity, disjoint from `WalletErrorCodes`. Among those 43
entries are **13 legacy codes with base64-encoded Persian suffixes**:

| Legacy code | Decoded suffix | Real invariant |
| --- | --- | --- |
| `wallet.rejected.SWRlbXBv` | `Idempo` | idempotency key required |
| `wallet.rejected.2KjYp9iy` | `باز` | redemption replay belongs to another account |
| `wallet.rejected.2qnYryDa` | `کد …` | gift-card code not found |
| `wallet.rejected.2KfYsdiy` | `ارز` | currency mismatch |
| `wallet.rejected.2K3Ys9in` | `حسا` | account not mutable |
| `wallet.rejected.2qnYp9ix` | `کار` | gift card not found (revoke) |
| `wallet.rejected.2K_ZhNuM` | `دلی` | adjustment reason required/invalid |
| `wallet.rejected.2YXZiNis` | `موج` | insufficient balance |
| `wallet.rejected.2YfZiNuM` | `هوی` | ids required |
| `wallet.rejected.2YXYqNmE` | `مبل` | amount must be positive |
| `wallet.rejected.2qnZhNuM` | `کلی` | idempotency conflict / key mismatch |

These are the source of the W0 §10 "message parsing" defect: the classifier matches an exact
*message string* that is itself a base64 Persian word.

### 9.3 Defect C — the bilingual resource surface is nearly empty

`Tooba.Wallet.Endpoints/Resources/WalletErrors.resx` and `.fa.resx` each carry **exactly one** key
(`wallet.authorization.unavailable`). The other nine catalogued codes resolve only through the
descriptor `SafeTitleFallback`. The certified `Support`/`UserPreference` standard requires one EN +
one FA entry per module-owned declared code.

### 9.4 Defect D — implicit embedded-resource logical names

`Tooba.Wallet.Endpoints.csproj` has no explicit `<EmbeddedResource>` logical-name lock for
`WalletErrors.resx` / `WalletErrors.fa.resx`; resolution depends on
`EmbeddedResourceUseDependentUponConvention`. Same *narrower* finding as the UserPreference W0/W1
correction: the names are **implicit, not locked**. W1 must lock them explicitly (byte-identical
resource names, so behavior-preserving).

### 9.5 Confirmed clean

- Zero hard-coded Persian/English user-facing strings in Domain/Application/Endpoints.
- Zero `exception.Message` / `ex.Message` used as a localized or user-facing contract.
- Zero endpoint-level `Accept-Language` parsing.
- `WalletErrorResourceSet.Owns` is prefix-based on `wallet.` — the canonical shape used by 18+
  certified modules; the `wallet.` keyspace is collision-free repository-wide.

---

## 10. API result / error mapping findings

### 10.1 Legacy parallel mapper (must be retired)

`WalletExceptionMapper` is a **parallel** error-mapping mechanism that predates the canonical
`*Operation` seam:

```text
WalletExceptionMapper.TryMapExact(message, publicErrorCode)  -> exact-message HashSet lookup
WalletExceptionMapper.ToSemanticError(ex, publicErrorCode)   -> message-based classification
WalletExceptionMapper.TryAsync(action, publicErrorCode)      -> catch InvalidOperationException + map
```

Its faults:

1. classification is by **message text** (`KnownCodes.Contains(message)`) — forbidden;
2. the message text is a **base64 Persian word** — doubly forbidden;
3. it catches the *generic* `InvalidOperationException` (no typed code), so an unrelated
   `InvalidOperationException` whose message happens to collide would be silently converted to a
   business failure;
4. it is a **second** declaration of stable-code identity, so the module has two competing owners.

### 10.2 Mixed typed mechanisms (partially migrated already)

`WalletDirectory.SpendForOrderPaymentAsync` and `CreditRefundAsync` already throw the canonical typed
`ContractOperationException` — but with the **legacy base64 codes**. The rest of the directory and
all of the Domain still throw raw `InvalidOperationException`. So the module is *half* migrated and
internally inconsistent: Payment's `WalletPaymentGateway` catches `ContractOperationException`
generically (fine), while the Wallet Application layer still needs the message mapper for everything
else.

### 10.3 Canonical target (W1)

Mirror the certified **Support** shape exactly (Support is AMSC-certified with this shape and Wallet
already shares its `Endpoints/Errors` + `Endpoints/Resources` + `Endpoints/Admin/*AuthorizationCodes`
layout):

```text
Tooba.Wallet.Contracts/Errors/WalletErrorCodes.cs
    HttpReachableCodes   (10 client-observable outcome codes)
    DomainInvariantCodes (38 typed invariant identities)
    IsKnown / IsHttpReachable / IsDomainInvariant / HttpReachable / DomainInvariants
Tooba.Wallet.Application/Composition/WalletOperation.cs
    ExecuteAsync<T>(action, publicOutcomeCode = null)
    ExecuteAsync(action, publicOutcomeCode = null)
    NotFoundIfNull<T>(value, missingCode)
    ToSemanticError(ex, publicOutcomeCode = null)
    ResolveOutcomeCode: HttpReachable -> typed code, else publicOutcomeCode ?? typedCode
```

`WalletExceptionMapper` and its `Errors/` folder are **retired**. Zero `Results.Json` /
`Results.BadRequest` / `Results.Problem` / `ProblemDetails` and zero `catch`-and-map blocks exist in
the endpoints today (confirmed), so the endpoint layer needs no API-result change.

---

## 11. Logging / sensitive-data findings

- Zero `Console.WriteLine` / `Debug.WriteLine`; zero second telemetry pipeline.
- Zero secrets or sensitive material logged. Notification payloads carry only
  `amount`/`currency`/`cardId`/`paymentId`/`returnRequestId` — no tokens, no `Authorization` headers.
- **Confirmed clean.** No W1 change required.

---

## 12. OpenTelemetry / correlation findings

- Zero direct `ActivitySource.StartActivity(` in Application/Endpoints.
- Zero manual `traceparent` parsing; zero competing correlation header.
- `Tooba.Payment`'s wallet gateway uses the canonical `IModuleCallTracer.Begin("payment","wallet",…)`
  decoration across the Wallet contract boundary, so trace continuity is preserved.
- **Confirmed clean.** No W1 change required.

---

## 13. File cohesion / splitting audit

Largest production files:

| LOC | File | Classification (`ARCH-SIZE-001`, cs ceiling 800) |
| --- | --- | --- |
| 682 | `Tooba.Wallet.Infrastructure/Directories/WalletDirectory.cs` | `WATCH` (>500) — under ceiling |
| 330 | `Tooba.Wallet.Infrastructure/Adapters/WalletDevelopmentSeed.cs` | `NORMAL` |
| 234 | `Tooba.Wallet.Domain/Aggregates/WalletLedgerEntry.cs` | `NORMAL` |
| 205 | `Tooba.Wallet.Domain/Aggregates/GiftCard.cs` | `NORMAL` |
| 157 | `Tooba.Wallet.Application/Models/WalletDtos.cs` | `NORMAL` |

`WalletDirectory.cs` is **not** in `tmar-source-size-baseline.json` (zero Wallet baseline entries) and
is 682 LOC — below the 800 ceiling, so no size violation. However it **is** a multi-responsibility
god-file candidate: it implements three interfaces (`IWalletDirectory`,
`IWalletOrderPaymentPort`, `IWalletRefundCreditPort`) and holds customer + admin + payment + refund +
quote use cases plus mapping helpers in one file.

**W1 decision (bounded):** W1 owns the *typed-fault* migration inside `WalletDirectory` (literal →
`WalletErrorCodes.*` constant). **W2** owns splitting it by capability so no single file mixes
customer/admin/payment/refund responsibilities, which also keeps W1's diff behavior-reviewable.

Also flagged for W2: `Tooba.Wallet.Application/Models/WalletDtos.cs` (157 LOC) bundles public result
DTOs **and** internal command-shaped inputs (`RedeemGiftCardCommand`, `IssueGiftCardCommand`,
`AdminGiftCardListQuery`, `AdminWalletAdjustmentCommand`) — a mixed `*Dtos.cs` bundle that W2 must
split by capability.

---

## 14. Target foldering (exact paths / namespaces)

Capability-first shallow, mirroring the certified `Support`/`Payment` shapes.

### Contracts (`Tooba.Wallet.Contracts.*`)

```text
Errors/     WalletErrorCodes.cs                                  Tooba.Wallet.Contracts.Errors
Dtos/       WalletCurrency.cs                                    Tooba.Wallet.Contracts.Dtos
Payments/   WalletOrderPaymentPort.cs                            Tooba.Wallet.Contracts.Payments
Refunds/    WalletRefundCreditPort.cs                            Tooba.Wallet.Contracts.Refunds
```

### Domain (`Tooba.Wallet.Domain.*`)

```text
Aggregates/   WalletAccount.cs, WalletLedgerEntry.cs, GiftCard.cs, GiftCardRedemption.cs
ValueObjects/ GiftCardStatus.cs, LedgerDirection.cs, LedgerEntryType.cs, WalletAccountStatus.cs
```

### Application (`Tooba.Wallet.Application.*`) — capability-first

```text
Composition/      WalletOperation.cs
Customer/         Commands/  Queries/  Models/  Ports/
Admin/            Commands/  Queries/  Models/  Ports/
Payments/         (order-payment use-case seam, W2)
Refunds/          (refund-credit use-case seam, W2)
Models/           shared request/result models (capability-scoped after W2 split)
Ports/            IWalletDirectory.cs, IWalletDemoPreviewPort.cs
Validation/       WalletRequestValidators.cs, WalletValidationCodes.cs
```

### Endpoints (`Tooba.Wallet.Endpoints.*`)

```text
WalletEndpointModule.cs                       Tooba.Wallet.Endpoints            (root allowlist)
Customer/  WalletCustomerEndpoints.cs, IWalletCustomerAuthorizer.cs, WalletCustomerAuthorizer.cs
Admin/     WalletAdminEndpoints.cs, IWalletAdminAuthorizer.cs, WalletAdminAuthorizationCodes.cs
Errors/    WalletErrorCatalogContributor.cs
Resources/ WalletErrorResources.cs, WalletErrors.resx, WalletErrors.fa.resx
```

### Infrastructure (`Tooba.Wallet.Infrastructure.*`)

```text
DependencyInjection/ WalletModule.cs, WalletOutboxRegistration.cs   (root allowlist -> W2 move)
Persistence/         WalletDbContext.cs, Migrations/
Directories/         wallet capability directory implementations (split by capability in W2)
Adapters/            WalletDemoSnapshot.cs, WalletDevelopmentSeed.cs
Development/         WalletDevelopmentSeedBootstrap.cs
```

### Tests (`Tooba.Wallet.Tests.*`)

```text
Architecture/  WalletArchitectureGuardTests.cs, NotificationContractsArchitectureGuardTests.cs
Behavior/      WalletAccountFactoryBehaviorTests.cs, WalletCqrsAndHttpContractTests.cs,
               WalletFinancialCharacterizationTests.cs
```

---

## 15. Behavior-preservation baseline

| Surface | Baseline (must not change) |
| --- | --- |
| Routes / verbs / group prefixes / templates | 11 routes as §8 |
| Success response shapes | `WalletSummaryDto`, `WalletLedgerPageDto`, `GiftCardRedeemResultDto`, `GiftCardListPageDto`, `GiftCardDetailDto`, `GiftCardIssueResultDto`, `AdminWalletAdjustmentResultDto`, `WalletDemoPreviewDto`, `{}` |
| Wire-visible HTTP error codes | the 10 catalogued outcome codes (byte-identical) |
| Domain invariants | `fa`/`en` locale, length caps, `Guid.Empty`, positive amounts, idempotency |
| Authorization + actor resolution order | `TryResolveActor` then `ISender`; admin `RequireAuthorizedAsync` then `ISender` |
| Schema / migrations | `wallet` schema + `20260827180000_InitialWallet` + designer + snapshot |
| Outbox | `Translate => null`, `ResolveEventClrType => null`, `Schema`, `TableName` |
| DI lifetimes | `IWalletDirectory` scoped, ports scoped via cast, demo preview singleton, outbox singleton |
| Notification semantics | `WalletGiftCardRedeemed`, `WalletAdminAdjustment`, `WalletPaymentSucceeded`, `WalletRefundCredited` + source ids + target route |

The **only** intentional wire-visible delta is the *invalid-input* envelope for the four
`VALIDATOR_REQUIRED` requests, which moves from a mapped business 400 to the canonical
`validation.failed` 400 (the accepted AMSC transport-validation contract; success paths are
byte-identical).

---

## 16. Foundation / certified-module state

- `lastAcceptedTask = TB-TMAR-HOST-ROOT-FINAL-CERT-001`; `currentHostCheckpoint = HOST_ROOT_FINAL_CERTIFIED`.
- `structureLock.certifiedModules` currently holds **31** modules; `Wallet` is **absent**.
- `tmar-module-structure-manifests.json`: `uncertifiedHttpOwningModules = ["Wallet"]`; `Wallet` is
  absent from both `modules[]` and `preCertModules[]`. This is the module's structure-authority slot
  for W2/W3.
- Wallet is therefore the last outstanding `uncertifiedHttpOwningModules` entry.

---

## 17. Migration order (W1 → W2 → W3)

1. **W1 (migrate)** — create `Tooba.Wallet.Contracts/Errors/WalletErrorCodes.cs` with the
   `HttpReachable`/`DomainInvariants` split and `IsKnown`/`IsHttpReachable`/`IsDomainInvariant`;
   add `Tooba.Wallet.Domain -> Tooba.Wallet.Contracts`; replace every raw
   `InvalidOperationException("<literal>")` with `ContractOperationException(WalletErrorCodes.X)`
   (Domain) / `SemanticException(new SemanticError(...))` where the current mapper path requires it;
   create `Tooba.Wallet.Application/Composition/WalletOperation.cs`; retire `WalletExceptionMapper`
   and `Application/Errors/`; add the four transport validators + `WalletValidationCodes`; extend
   `WalletErrors.resx`/`.fa.resx` to one EN + one FA entry per declared code and lock the explicit
   embedded logical names; add the W1 durable guard.
2. **W2 (structure)** — capability-first Application (`Customer`, `Admin`, `Payments`, `Refunds`,
   `Composition`, `Ports`, `Models`, `Validation`), split `WalletDtos.cs`, split `WalletDirectory.cs`
   by capability, move `WalletModule.cs`/`WalletOutboxRegistration.cs` into
   `DependencyInjection/`, exact path↔namespace, root allowlists, `.slnx` grouping, manifest
   structure authority; add the W2 durable guard.
3. **W3 (certify)** — durable cert guard with the §6a set-equality gate, manifest promotion to
   `modules[]` with `structureCertified: true`, `structureLock.certifiedModules` (32), SoT lineage,
   Master Recovery checkpoint, zero-new-failure proof.

---

## 18. Verification plan

```text
dotnet build src/backend/Modules/Wallet/Tooba.Wallet.Infrastructure/Tooba.Wallet.Infrastructure.csproj
dotnet build src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj
dotnet test  --filter Wallet
                   | WalletModuleAmsc001W1MigrateGuardTests
                   | WalletModuleAmsc001W2StructureGuardTests
                   | WalletModuleAmsc001W3CertGuardTests
                   | ErrorCatalogUniqueCodeGuardTests
                   | HostAdminAccessAmcCertGuardTests
                   | HostAdminCanon003GuardTests | HostAdminCanon009GuardTests
                   | PaymentPrecertHygieneTests | WalletCurrencyContractsTests
                   | TmarSourceSizeAndInfraAppTests
```

**Hard constraint discovered in W0:** `HostAdminAccessAmcCertGuardTests` (a certified Host guard for a
different task) asserts the *existence and location* of `WalletErrorResourceSet`,
`WalletErrors.resx`, `WalletErrors.fa.resx` under `Tooba.Wallet.Endpoints/Resources/` and
`WalletAdminAuthorizationCodes` under `Tooba.Wallet.Endpoints/Admin/`. W1 must therefore **keep** the
catalog/resource-set/authorization-codes surface in Endpoints (the certified `Support` shape) and must
**not** relocate it to Contracts (the `UserPreference` shape). This is recorded as an architecture
constraint, not a preference.

---

## 19. Certification blockers

| # | Blocker | Owner |
| --- | --- | --- |
| B1 | Stable codes live in `Application.Errors`, not the canonical `Contracts.Errors` | W1 |
| B2 | `WalletExceptionMapper` message/base64 classification is a parallel mechanism | W1 |
| B3 | 67 raw `InvalidOperationException("<literal>")` faults (Domain/Infrastructure) | W1 |
| B4 | 13 legacy base64 Persian code suffixes | W1 |
| B5 | Zero transport validators for 4 caller-controlled requests | W1 |
| B6 | Bilingual resource surface has 1 of 49 keys | W1 |
| B7 | Implicit embedded-resource logical names | W1 |
| B8 | `WalletDirectory.cs` mixes customer/admin/payment/refund responsibilities | W2 |
| B9 | `WalletDtos.cs` bundles public results with internal command inputs | W2 |
| B10 | Wallet absent from `modules[]` / `structureLock.certifiedModules` | W2/W3 |

---

## 20. Structured state fields

```text
HttpApplicability              : HTTP_OWNING
ModuleOwnedRouteCount          : 11  (3 customer + 8 admin)
HostOwnedRouteCount            : 0
EndpointReachableRequests      : 11  (4 commands + 7 queries)
CqrsState                      : COMPLIANT_11_OF_11_REAL_IREQUEST_AND_HANDLER_ISENDER
ValidatorMatrixState           : GAP_4_VALIDATOR_REQUIRED_0_PRESENT_7_NO_VALIDATOR_REQUIRED
StableCodeHomeState            : MISLOCATED_IN_APPLICATION_11_DECLARED_10_CATALOGUED_1_LOCALIZED
DeclaredCodeGuardState         : ABSENT
TypedFaultSeamState            : ABSENT_PARALLEL_MESSAGE_MAPPER_43_KNOWN_CODES
RawFaultState                  : 67_RAW_INVALID_OPERATION_EXCEPTION_LITERALS
LegacyBase64CodeState          : 13
LocalizationState              : CANONICAL_MECHANISM_1_EN_1_FA_OF_49_IMPLICIT_LOGICAL_NAMES
ApiResultMappingState          : ENDPOINTS_CLEAN_APPLICATION_PARALLEL_MAPPER
LoggingState                   : CANONICAL
CorrelationTraceState          : CANONICAL
CrossModuleCouplingState       : NONE
CrossModuleJoinState           : NONE
PersistenceOwnershipState      : SINGLE_WALLET_SCHEMA
SchemaState                    : UNCHANGED
PathNamespaceState             : EXACT
GodFileState                   : NONE_UNDER_CEILING_682_LOC_WATCH
MicroserviceExtractable        : FALSE_PENDING_W1_ERROR_HOME
StructureAuthorityState        : UNCERTIFIED_HTTP_OWNING
StopGate                       : USER_REVIEW_WALLET_AMSC_001_W0
```

---

## 21. Destination-integrity check

No production file was created, modified or deleted by W0. The only artifacts are this evidence
document and its read-only `inspect.cjs` helper. `HEAD == origin/main == 4f875042` at the start of
W0; the working tree is clean apart from untracked pre-existing evidence from the previous
UserPreference task.
