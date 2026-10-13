# TB-P0-CHECKOUT-START-PAYMENT-012 — BLOCKED_ARCHITECT_DECISION

Program: TOOBA-CHECKOUT-PAYMENT-COMMIT-BOUNDARY
Parent: TB-P0-CHECKOUT-PAYMENT-BOUNDARY-ANALYZE-010 · TB-P0-CHECKOUT-PAYMENT-DRAFT-011
Mode: BOUNDED_IMPLEMENTATION — STOP_FOR_ARCHITECT
Base SHA: `19818212c1f33adaaeddd1aeb28b939594710072` (== origin/main, contains 011 `63b5b9a41`)
Verdict: **BLOCKED — MUTATION SEAM NOT SHIPPED (by task rule)**

Task rule used (quoted from the task text):

> "If this requires new cross-module port, schema migration, changing locked semantics, or > bounded scope,
> STOP_FOR_ARCHITECT with precise blocker and minimal proposed split rather than shipping a partial unsafe
> mutation endpoint."

> "If the complete safe final-click seam cannot fit 20 minutes, stop at ANALYSIS/BLOCKED
> (no half-functional endpoint)."

A new cross-module port is **strictly unavoidable** for a single backend "final-click" operation, and the
direction of that port decides who owns Order-creation authority. That is an Architect decision, not a Worker
decision. **zero production/test code was changed in this task.**

---

## 1. FACT — exact blocker

A single backend endpoint that does *commit (Order) → initiate (Payment)* needs BOTH operations in one call
path. Neither operation is reachable outside its own module, and no Contracts-level seam exists in either
direction. Verified by enumeration, not inference:

### 1.1 The two operations are module-internal only

| Operation | Interface | Declared in | Implemented in |
|---|---|---|---|
| commit (reserve + Order + Cart convert) | `ICheckoutDirectory.SubmitAsync` | `Tooba.Order.Application/Checkout/Contracts/CheckoutDirectoryPorts.cs:36,41` | `Tooba.Order.Infrastructure/Checkout/Persistence/CheckoutDirectory.cs:121` |
| commit orchestration internals | `CheckoutProcessManager.SubmitAsync` | `Tooba.Order.Application/Checkout/Process/CheckoutProcessManager.cs:51` | same file |
| storefront commit entry | `StorefrontShippingService.CommitAsync` | `Tooba.Order.Application/Storefront/Services/StorefrontShippingService.cs:309` | same file |
| initiate (provider resolve + attempt) | `StorefrontPaymentOrchestrator.InitiateAsync` | `Tooba.Payment.Application/Storefront/Orchestration/StorefrontPaymentOrchestrator.cs:110` | same file |

Both `ICheckoutDirectory` and `StorefrontPaymentOrchestrator` live in **Application** layers of different
modules. Module guards forbid the cross-reference in at least one direction:
`Tooba.Order.Tests/Architecture/OrderStorefrontArchitectureGuardTests.cs:128` asserts
`Assert.DoesNotContain("Tooba.Payment.Application", text)` for the whole `Tooba.Order.Application/Storefront`
subtree. The reverse direction is impossible by project reference as well:
`Tooba.Payment.Application/Tooba.Payment.Application.csproj` references only
`Tooba.Payment.Contracts`(:14), `Tooba.Payment.Domain`(:15), `Tooba.Wallet.Contracts`(:16),
`Tooba.Order.Contracts`(:17) — it has **no** `Tooba.Order.Application`/`Infrastructure` reference.

### 1.2 No existing Contracts seam for either step

- `Tooba.Order.Contracts` currently exposes only *read/projection* surfaces to other modules:
  `Payments/OrderPaymentContracts.cs:5` `ICheckoutPaymentAccessReader`, `:28` `IOrderUnpaidRetrySupplyPort`,
  `:31` `IOrderPaymentProjectionPort`, plus admin/customer/fulfillment/returns/notifications read contracts.
  **There is no commit/reserve-from-cart contract.**
- `Tooba.Payment.Contracts` exposes `Checkout/OrderPaymentProjectionContracts.cs:41` `IPayableCheckoutReader`
  (read-only payable snapshot), `Ports/ICheckoutActorPolicyPort.cs`, `Hold/PaymentHoldContracts.cs`,
  `Storefront/PendingPaymentContracts.cs`, `Storefront/PaymentProviderCodes.cs`.
  **There is no initiate-payment-for-payable contract.**
- No Contracts-level mediated request exists to reuse: searching `IRequest<|IMediator|ISender` across
  `Tooba.Order.Contracts` and `Tooba.Payment.Contracts` returns **zero matches** (verified). So there is no
  "send a command across modules" seam to piggyback on either.
- No Host composer exists to hide the gap: `OrderStorefrontArchitectureGuardTests.cs:20-27` asserts the Host
  storefront composer files are **absent**, and this task forbids Host edits anyway.

### 1.3 Consequence

Today the two steps are joined only by **the frontend** (two HTTP calls) — the very coupling this program is
trying to remove:

- `src/frontend/app/storefront/storefront-shipping-api.ts` → `POST /v1/storefront/shipping/commit`
- `src/frontend/app/storefront/storefront-payment-api.ts:424` → `POST /v1/storefront/checkout/{checkoutId}/payments`

Unifying them in the backend requires exactly one **new** Contracts port plus one adapter and one orchestration
endpoint — i.e. a new cross-module mutation seam.

---

## 2. FACT — the second blocker: payability has no Payment-facing read

The task requires: *"Payability should use one Order-owned reservation-cycle policy, before beginning charge."*

The canonical policy is `Tooba.Order.Application/ReservationCycle/Policies/ReservationCyclePayabilityPolicy.cs:13`
(`IsActive`) / `:20` (`HasReachedRetryLimit`). Its **only** consumers are Order-internal:

- `Tooba.Order.Infrastructure/Checkout/Abuse/CheckoutAbuseGate.cs:198`
- `Tooba.Order.Application/Storefront/Services/StorefrontPendingPaymentProjector.cs:79,82`

Payment never reads reservation-cycle state: `StorefrontPaymentOrchestrator.InitiateAsync`
(`StorefrontPaymentOrchestrator.cs:110-163`) validates actor policy, payable snapshot, wallet quote, and method
availability — **not** payability. `CheckoutPaymentAccessSnapshot`
(`Tooba.Order.Contracts/Payments/OrderPaymentContracts.cs:5`) carries only
`CheckoutId, PayableAmount, Currency, OrderNumber`.

So a Payment-owned final-click would need a **second** new cross-module read (or a widened existing contract) to
avoid duplicating or ignoring payability. An Order-owned final-click gets payability for free, because the same
module already owns it. This asymmetry is the main argument for the split proposed in §4.

---

## 3. FACT — what is already proven (no new certification needed)

The atomicity guarantee the task asks to prove ("multi-item reservation+Order+Cart atomic rollback on a forced
mid-commit failure") **already has a real-Postgres behavioural proof in this repo**, so a partial endpoint would
not be needed to "certify" anything:

`src/backend/Host/Tooba.Host.Tests/CheckoutOrderFoundationTests.cs` (`[Collection("PostgresSerial")]`, line 101;
Testcontainers `postgres:16-alpine`, real `Database.MigrateAsync()` for Cart/Catalog/Party/Offer/Pricing/
Inventory/Order/Tax/Promotion at :262-280; real `CheckoutDirectory` + real `CheckoutProcessManager` at :293):

- multi-seller / multi-line commit → 2 `SellerOrders`, all `PendingPayment`, every line has `ReservationId`
  (`:349-373`);
- forced mid-commit failure via `FailOnceCartDirectory` (`:463-474`) → `InvalidOperationException`, **Cart stays
  `Active`**, `0` checkout rows for that cart (`:474-476`) — i.e. reservation + Order + conversion rolled back;
- same-key replay after the fault commits once and reuses (`:477-479`); a **different** idempotency key after a
  successful commit returns the **same** `CheckoutId` with exactly `1` row (`:482-485`);
- concurrent double-submit with two independent `CheckoutDirectory`/DbContext sets → equal `CheckoutId`,
  `1` row, and no extra availability consumption (`:487-520`).

Commit ordering/kill-switch guards also already exist: `Host/Tooba.Host.Tests/AtomicCheckoutCommitTests.cs:42-47`
asserts `EnsureCanStartInitialReservationAsync` precedes `ReserveForCheckoutAsync` in the source.

**Before/after guarantees for THIS task: unchanged — no mutation seam was enabled, so there is nothing new to
guarantee.** No new atomicity claim is made here, and none is implied by the existing test above for any *new*
seam.

---

## 4. ARCHITECT DECISION REQUESTED — minimal proposed split

Both options add exactly **one** Contracts interface. Pick one; then the endpoint is a small, testable change.

### Option S2 (recommended) — Order-owned final-click; new **Payment.Contracts** initiation port

```
GET/POST  /v1/storefront/cart/{cartId:guid}/start-payment        (Order.Endpoints, own module, no Host change)
  body: { expectedCartVersion, idempotencyKey, providerCode?, useWallet? }
  identity: existing IOrderStorefrontCheckoutIdentityGate + guest secret (same as /shipping/commit)
```

1. **Order** (new): command + validator + handler; resolve draft + ownership (reuse
   `StorefrontShippingService.ProjectPaymentDraftAsync` / `EnsureDraftOwnership`), commit via the existing
   `StorefrontShippingService.CommitAsync()` → `ICheckoutDirectory.SubmitAsync` (**no new reserve logic**).
2. **New Order.Contracts-side call**: `IPaymentInitiationPort.InitiateForPayableAsync(InitiateForPayableRequest, ct)`
   in `Tooba.Payment.Contracts` (**+1 file**), adapter in `Tooba.Payment.Infrastructure` that forwards to the
   existing `StorefrontPaymentOrchestrator.InitiateAsync` (**+1 adapter**, DI line in `PaymentModule` — no Host
   change), returning a Contracts DTO mirror of `StorefrontPaymentInitiationDto`.
   Payment keeps all provider/wallet/manual routing decisions (`StorefrontPaymentOrchestrator.cs:124-158`) —
   no rule duplication.
3. **Payability** before charge: checked inside Order with `ReservationCyclePayabilityPolicy.IsActive`
   (`:13`) via the existing `IReservationCycleDirectory` — **no second new port**.
4. **Idempotency**: one cart-scoped key reused for both steps; the unique index `ix_checkouts_cart_id`
   (`Tooba.Order.Infrastructure/Persistence/Migrations/20260823140000_UniqueCheckoutCartId.cs:21-27`, EF
   `OrderDbContext.cs:126`) already makes a second commit for the same cart return the winner
   (`CheckoutProcessManager.cs:157-172`), and is already proven at `CheckoutOrderFoundationTests.cs:482-485`.
5. **Initiate after commit failed**: commit already succeeded, so the PendingPayment order persists and the
   existing `POST /checkout/{checkoutId}/payments` retry path remains valid — no re-commit, no orphan reserve.
   If commit throws, `TransactionScope` (`CheckoutProcessManager.cs:95`) leaves the Cart `Active` and releases
   holds (proven at `CheckoutOrderFoundationTests.cs:474-476`).

Why recommended: Order keeps "when an Order may exist" authority; Payment keeps "when/with which provider a
charge starts" authority; payability needs no extra port; the guest/version/draft policy is reused verbatim.

### Option S1 (alternative) — Payment-owned final-click; new **Order.Contracts** commit port

Payment gets `IStorefrontCheckoutCommitPort.CommitFromCartAsync(...)` in `Tooba.Order.Contracts` + an Order
adapter + DI in `OrderModule`. Smaller *code* delta, but it makes a Payment endpoint create Orders/reserve
stock/convertt carts, and it still needs a **second** new read for payability (§2) — so it is smaller in files
but larger in authority transfer.

### Explicitly out of scope for either option (unchanged by this task)

- No frontend/shipping cutover (task rule); the browser still performs two calls.
- No Host edits, no schema/DDL, no migration, no live mutation, no service restart.
- `/shipping/commit` stays untouched until the frontend cutover task.
- GET payment-draft (011) stays read-only and must never be read as a persisted Order.

---

## 5. RISK / UNKNOWN

**RISK**
- R1 An Order-owned endpoint that "starts a charge" and a Payment-owned endpoint that "creates Orders" are both
  authority inversions; choosing without the Architect would quietly redefine the commit boundary the Program
  exists to fix. *Mitigation: this BLOCKED.*
- R2 `HasSucceededPaymentForCheckoutAsync` / existing pending-payment policy for "different method after commit"
  (`StorefrontPaymentOrchestrator.cs:360-362,404-407`) must be reused, not re-invented; the chosen option must
  state which existing policy answers "second click with different method".
- R3 A new Contracts interface is a **contract change**; it needs Architect approval before implementation even
  though it adds no schema.

**UNKNOWN**
- U1 Whether the Architect wants the composite seam at all, or prefers the frontend to keep two calls and only
  needs the frontend cutover task. Not decidable from the task text alone.
- U2 Whether `IReservationCycleDirectory` may be exposed by adding fields to the existing
  `CheckoutPaymentAccessSnapshot` (`OrderPaymentContracts.cs:5`) instead of a new payability port — a contract-shape
  choice, not a code question.
- U3 Live HTTP behaviour of any new route (the running Host binary is pre-change; a rebuild/restart is not
  authorized here).

**FACT**
- F1 No cross-module commit/initiate seam exists in either direction (§1).
- F2 Module boundaries forbid both cross-Application references today (§1.1).
- F3 Payability is Order-internal and unreadable from Payment (§2).
- F4 Commit atomicity + idempotency + concurrency already have a real-Postgres proof (§3).
- F5 This task changed **0** production/test files; the repository is clean at the base SHA.

---

## 6. Commands, results, hygiene

- Base verification: `git rev-parse HEAD` = `git rev-parse origin/main` =
  `19818212c1f33adaaeddd1aeb28b939594710072`.
- Seam enumeration: `rg "IRequest<|IMediator|ISender" src/backend/Modules/{Order/Tooba.Order.Contracts,Payment/Tooba.Payment.Contracts}` → 0 matches;
  Contracts file listings → read surfaces only (§1.2).
- Payability consumers: `rg "ReservationCyclePayabilityPolicy\."` → 2 Order-internal call sites only (§2).
- Unique index: `rg "HasIndex\(x => x.CartId\).IsUnique"` → `OrderDbContext.cs:126`;
  migration `20260823140000_UniqueCheckoutCartId.cs:21-27`.
- Existing atomicity proof run: `dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter "FullyQualifiedName~CheckoutOrderFoundationTests"`
  → see §7 for the actual observed result on this machine.
- Changed files this task: `docs/architecture/evidence/TB-P0-CHECKOUT-START-PAYMENT-012/` (analysis + result) and
  `docs/ai/tasks/TB-P0-CHECKOUT-START-PAYMENT-012.task.md` (persisted artifact) **only**.
- No code, no test, no schema, no Host, no frontend, no live cart/order/payment/inventory mutation, no restart.

## 7. Focused test run result

See `RESULT.bridge.txt` §7 for the observed outcome of the `CheckoutOrderFoundationTests` run on this machine
(real Postgres via Testcontainers/Docker 29.4.2).

STOP for Architect decision. No follow-up task started. `automaticNextImplementationTask=NONE`.
