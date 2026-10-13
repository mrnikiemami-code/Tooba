# TB-P0-PAYMENT-ENTRY-RESERVATION-GATE-022 — analysis

Base: `8667e5bc6aa84ef02c95db26e4c3e5e41ba806ad` (== origin/main == Expected-Base-SHA, verified before any write).
Mode: BOUNDED_IMPLEMENTATION_OR_BLOCKED. Outcome: **BLOCKED_ARCHITECT_DECISION** — defect confirmed and localized,
but the sound fix is not implementable inside this task's constraints/timebox without an architect decision.
Production/Test files changed: **0**. Endpoint behaviour left **unchanged** (no partial gate published).

---

## 1. Actual money-entry paths (complete inventory)

### A. The route named by the task — the single publicly reachable fresh-initiation path

```
POST /v1/storefront/checkout/{checkoutId:guid}/payments
  PaymentStorefrontEndpoints.cs:22            (route)
  PaymentStorefrontEndpoints.cs:34-55         (handler -> InitiateStorefrontPaymentCommand, key from header/body)
  StorefrontPaymentOrchestrator.InitiateAsync StorefrontPaymentOrchestrator.cs:111-243
```

Exact order of operations (all line refs are the committed tree):

| # | Step | Location |
|---|------|----------|
| 1 | actor policy (`EnsureCheckoutActorAsync`) | Orchestrator:120 |
| 2 | cart-scoped ownership proof: `ICheckoutPaymentAccessReader.GetForMutationAsync(checkoutId, cartId, guestSecret, authenticatedUserId)`; null => `payment.missing` | Orchestrator:121-122 |
| 3 | **method selection** (no cycle/stock check anywhere here): wallet => wallet quote + require full coverage else `payment.wallet.mixed_deferred`; manual => `ManualCardToCardEnabled` else `payment.method.unavailable`; selector `gateway` => `IsOnlineGatewayOffered()` else `payment.method.unavailable`; concrete provider = `_catalog.DefaultProvider` | Orchestrator:125-156 |
| 4 | **existing-key replay / new attempt decision**: `_payments.InitiateAsync(InitiatePaymentCommand(checkoutId, actor, null, key, providerCode))` | Orchestrator:159-164 |
| 5 | immediately after a wallet initiation that is not `Succeeded`: `VerifyAsync` => wallet debit through `WalletPaymentGateway.VerifyAsync` -> `IWalletOrderPaymentPort.SpendForOrderPaymentAsync`; wallet is charged here, not in step 4 | Orchestrator:169-198 |

Inside step 4, `PaymentDirectory.InitiateAsync` (`PaymentDirectory.cs:61`):

| sub | Step | Location |
|-----|------|----------|
| 4a | load by key `x.IdempotencyKey == key`; if found: actor visibility (`PaymentActorAccess` -> `IPayableCheckoutReader.GetPayableAsync`) | :64-70 |
| 4b | **021 gate** (this wave): canonical requested provider vs persisted provider + `CheckoutId` equality, else `payment.idempotency.conflict` before any gateway call/retry/return | :70-84 |
| 4c | existing: `Succeeded` replay, or `Failed` prior -> **new provider initiation + new attempt + SaveChanges** | :112-149 |
| 4d | **fresh path**: `HasSucceededPaymentForCheckoutAsync` -> `_orders.GetPayableAsync(...)` -> mode must be `OnlinePurchase` else `payment.initiate.reservation_forbidden` -> pending seller orders -> currency parity -> amount -> **`gateway.InitiateAsync(...)`** -> `RecordInitiation` -> `SaveChangesAsync` | :151-222 |

**The only pre-money authority on the fresh path is `IPayableCheckoutReader.GetPayableAsync`, implemented by
`OrderPaymentBridge.GetPayableAsync` (`OrderPaymentBridge.cs:50-79`).** That method reads only:

* the checkout group + its seller orders,
* `group.CanBeViewedBy(buyerPartyId, actorUserId)` (throws an `InvalidOperationException` when unauthorized),
* and returns `(checkoutId, mode, currency, per-seller-order {GrandTotalSnapshot, Currency, PendingPayment}, ShippingAmount)`.

It contains **no** reservation-cycle read, **no** hold/expiry read, **no** stock/supply read; the bridge's
`_inventoryLifecycle` and `_cycles` fields are not touched in that method. Therefore a checkout whose reservation
cycle is expired/closed, or whose Inventory holds are `Released`/expired (the 014 case: Inventory Released while
Order shows Active), still passes 4d and reaches `gateway.InitiateAsync` — and for wallet, the debit in step 5.

### B. The canonical Payment initiation seam (same orchestrator, no second public path)

`IPaymentInitiationPort` (`PaymentInitiationContracts.cs:36`) is implemented by `PaymentInitiationAdapter`
(`PaymentInitiationAdapter.cs:19`) as `Func<StorefrontPaymentOrchestrator>` (`PaymentModule.cs:101`) — i.e. it
delegates to the *same* orchestrator, so any future gate inside the orchestrator would also cover it.
Its only consumer is Order's 019 `StorefrontStartPaymentComposer` (`:169,:182`), which is inert:
registered (`OrderModule.cs:119`) and exercised only by tests (`StorefrontStartPaymentComposerTests.cs`,
`StorefrontStartPaymentComposerGuardTests.cs:37` asserts no endpoint surface references it).
**No endpoint consumes the composer or the port today**, so no second publicly reachable initiation path exists.

### C. Other money-touching paths (not fresh initiations; out of this task's scope)

* `POST /payments/{paymentId}/sandbox/complete` -> `VerifyAsync` (verifies an **existing** attempt).
* `POST /unpaid-retry` -> `RetryUnpaidCoreAsync` (`Orchestrator.cs:438-463`): **already gated** — it calls the
  Order-owned authoritative `_supply.EnsureRetrySupplyAsync(checkoutId, ct)` (`:446`) *before*
  `ReopenExpiredForRetryAsync` (`:460`), and maps supply faults to `payment.unpaid.supply_unavailable`.
* manual evidence / manual retry / admin confirm-reject-reconcile: no new charge creation.

So the defect is exactly and only on path A/step 4d. Wallet is covered by the same gate because a wallet debit
requires a successful `InitiateAsync` first (step 5).

---

## 2. Why the fix cannot be completed inside this task (four independent blockers)

### B1 — The existing 018 proof cannot be called from the payment boundary (guest regression)

`IOrderPaymentHoldPreparationPort.PrepareAsync` (`OrderPaymentHoldPreparationContracts.cs:128`) is the only
existing cycle **and** Inventory proof. Its step [1] authorizes through
`ICheckoutPaymentAccessReader.GetForMutationAsync(checkoutId, request.CartId, request.GuestSecret,
request.AuthenticatedUserId)`. The payment boundary has neither input: guests are a fixed sentinel
(`StorefrontPaymentOrchestrator.cs:57-59`) and `InitiatePaymentCommand` carries no cart/secret, with
`buyerPartyId: null`. Calling 018 from `PaymentDirectory` would therefore return `AccessDenied` for **every guest
checkout** — a storefront outage. (018 also *extends* every hold, which is a behaviour change beyond a gate.)

### B2 — The gate must be inside `PaymentDirectory`'s fresh path, not in the orchestrator

The task requires same-key replay to keep working. An orchestrator-level pre-gate would deny a legitimate replay:
a paid wallet checkout has its cycle closed to `CommittedPaid` by `OrderPaymentBridge.CommitPaidCycleAsync`
(`:250-292`), so `ReservationCyclePayabilityPolicy.IsActive` is false immediately after a successful payment —
a duplicate click would return 409 instead of the succeeded payment. Only the branch after 4a/4b distinguishes
replay from new initiation.

### B3 — Error-mapping conflict the worker may not self-authorize

The not-payable outcome must be a registered/localized 4xx. `payment.checkout.not_payable` (`:155`) and
`payment.initiate.reservation_forbidden` (`:157`) exist only as literals and are **not** in `KnownCodes`/the
catalog, so they already degrade to `platform.unexpected` HTTP 500 (021 analysis, L2) — forbidden. The single
candidate existing registered code is `payment.unpaid.supply_unavailable` (409), whose declared purpose is
documented as the unpaid-**retry** supply failure. The mission says "fail closed with a meaningful existing
owner-defined result"; the delivery constraints say "never repurpose an existing code". Those two clauses
conflict. A new code additionally requires the certified guard pins
(`PaymentModuleAmsc001W1MigrateGuardTests.cs:237`, `PaymentModuleAmsc001W3CertGuardTests.cs:288/320`) and would
collide with the frozen `tmar-current-state.json` R2/R3 records still pinned at 28/27/24 — the very divergence
reported as follow-up in 021.

### B4 — Coverage/regression decision needs architect knowledge (live data)

Gating fresh initiation on an *Active* cycle denies payment for any checkout with no active cycle. Whether every
legitimate chargeable checkout in the live database has an active `InitialPayment` cycle (the cycle was added by a
later wave; open checkouts created before it, and any flow that never starts one) cannot be established from the
repository and would require live-DB inspection, which this task forbids. Shipping a gate with wrong coverage
would be a storefront outage; shipping it only for checkouts that *have* a cycle would leave the pre-cycle
population unprotected.

Timebox: with ~13 of 20 minutes consumed by the inventory above, implementing B1–B4 correctly (contract change +
Order service + Payment call site + DI + focused tests in two modules + two test runs) is not achievable.
Per the task: *"If it cannot be safely implemented in 20 minutes, deliver a precise blocker with smallest needed
follow-up and no unsafe partial endpoint."*

---

## 3. Smallest needed follow-up (ready-to-apply, 4 production files + tests)

**F1 — Order contract (additive, same file).**
`Modules/Order/Tooba.Order.Contracts/Payments/OrderPaymentHoldPreparationContracts.cs`
* `enum OrderPaymentPayabilityOutcome { Payable=0, ReservationSetIncomplete, CycleNotPayable, RetryLimitReached, HoldUnavailable, PolicyUnavailable }`
* `record OrderPaymentPayabilityRequest(Guid CheckoutId, string ProviderCode)`
* `record OrderPaymentPayabilityResult(Outcome, CheckoutId, ProviderCode, int? CycleNumber, DateTimeOffset? CycleExpiresAtUtc, IReadOnlyList<Guid> UnavailableReservationIds)`
* second method on the **same** port:
  `Task<OrderPaymentPayabilityResult> EnsurePayableAsync(OrderPaymentPayabilityRequest request, CancellationToken ct);`
  Documented as **read-only**: no Inventory write, no Order write, no extension, no TTL, and deliberately no
  ownership re-proof (it mutates nothing; the request is already authenticated at the storefront layer by
  `ICheckoutPaymentAccessReader`). This is what removes the B1 guest blocker.

**F2 — Order implementation.** `Modules/Order/Tooba.Order.Application/Checkout/Process/OrderPaymentHoldPreparationService.cs`
Implement `EnsurePayableAsync` by reusing the existing private steps of the same class: cycle projection +
`ReservationCyclePayabilityPolicy.IsActive` (`ReservationCyclePayabilityPolicy.cs:12`), map exhausted renewals with
`HasReachedRetryLimit` (`:23`) to `RetryLimitReached`, and prove each persisted line reservation **read-only** via
`IOrderInventoryLifecyclePort.FindReservationAsync` (`OrderInventoryLifecycleContracts.cs:136` — returns `Released`
too) requiring `Held` + unexpired. No extension, no writes, no duplicated stock logic, no new TTL constant.

**F3 — Payment call site.** `Modules/Payment/Tooba.Payment.Infrastructure/Directories/PaymentDirectory.cs`
Call `EnsurePayableAsync(checkoutId, canonicalProviderCode)` in the **fresh path only**, after the 021 conflict
gate and before `gateway.InitiateAsync`; fail closed on every non-`Payable` outcome. Pass the
**registry-resolved canonical provider** (`_gateways.Resolve(command.ProviderCode).ProviderCode`) so the effective
per-method hold/policy override is enforced, never the bare selector `gateway` (021 §2 finding).
Replay/retry branches (4a–4c) untouched.

**F4 — DI.** `Modules/Payment/Tooba.Payment.Infrastructure/DependencyInjection/PaymentModule.cs:56-67` must inject the
port **as a required dependency** in the Host composition so the gate cannot silently fail open.

**F5 — Host.Tests consequence (needs architect authorization).** A required ctor argument breaks the eight
`new PaymentDirectory(` sites in the forbidden-to-edit Host.Tests
(`MarketplaceSettlementEventPathTests.cs:170`, `PaymentFoundationTests.cs:190/265/276`,
`R2LiveMarketplaceSmokeTests.cs:101`, `ReturnFoundationTests.cs:185`, `SettlementFoundationTests.cs:158`,
`WalletCheckoutRefundTests.cs:199`). Either the architect authorizes an **optional** parameter plus a
Host-composition guard test, or authorizes those eight test-file edits. This constraint alone prevents completing
F1–F4 inside this task.

**F6 — Focused tests once authorized.**
Order.Tests (truthful fakes): expired cycle; `Released` hold while the Order cycle is `Active` (the 014 case);
valid hold; renewal allowed vs retry-exhausted; no active cycle; wallet/manual/online provider selection.
Payment.Tests: proof failure => **no gateway invocation and no payment/attempt row**; identical-method replay and
the 021 conflict cases stay green. No PostgreSQL locking/cross-module ACID claim is made or faked.

**F7 — Error result.** Architect must pick one: (a) authorize reuse of `payment.unpaid.supply_unavailable` with an
updated declared-purpose comment, or (b) authorize a new Payment-owned code (then the 2 certified Host guards +
2 `.resx` + the R2/R3 SoT follow-up from 021 are in scope, and the conflict/retry codes must be fixed together).

---

## 4. Verification performed

Static only: direct reads of every file cited above (`PaymentStorefrontEndpoints`, `StorefrontPaymentOrchestrator`,
`PaymentDirectory`, `OrderPaymentBridge`, `OrderPaymentHoldPreparationContracts`/`Service`,
`ReservationCycleContracts`/`ReservationCyclePayabilityPolicy`, `OrderInventoryLifecycleContracts`,
`PaymentModule`, plus the consumer scans for `IOrderPaymentHoldPreparationPort`, `IPaymentInitiationPort`,
`StorefrontStartPaymentComposer`, `new PaymentDirectory(` and the `IPayableCheckoutReader` implementors).
No build and no test run were executed because **zero production and zero test files changed** — there is nothing
new to verify, and this task forbids building/running Host and Host.Tests.

Deliberately **not** added: a Payment-module characterization test asserting that a fresh initiation reaches the
gateway while no hold proof is consulted. It would pin the defective behaviour as a contract, and the available
contracts cannot express the expired/Released-hold scenarios truthfully on the Payment side (the
`IPayableCheckoutReader` snapshot carries no reservation/hold field at all). The Order-side proof belongs in
F6 after authorization.

## 5. Residual risks / not claimed

* R1 No runtime, live-DB, PSP or wallet proof was produced or claimed.
* R2 The defect remains **live** on the public route until F1–F4 (and the F5/F7 decisions) are authorized. This is
  a disclosed P0 blocker, not a mitigated one: the only production change shipped in this task is none.
* R3 `payment.checkout.not_payable` / `payment.initiate.reservation_forbidden` still surface as HTTP 500 on the
  fresh path (pre-existing, 021-L2), unchanged.
* R4 The 021 follow-up (frozen `tmar-current-state.json` R2/R3 counts 28/27/24 vs live 29/28/25) is recorded, not
  rewritten.
* R5 No hardcoded retry/deadline/TTL value, no policy override, no schema/migration/config, no frontend, no new
  endpoint, no Host production or Host.Tests edit, no 015/017 Inventory change.
