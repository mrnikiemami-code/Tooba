# TB-P0-CHECKOUT-PAYMENT-BOUNDARY-ANALYZE-010 — Analysis

Mode: `AMSC_ANALYZE_ONLY — READ_ONLY — STOP_FOR_ARCHITECT`
Repo SHA at start: `207dae32fd899584d8057405901593aecf222e45` (== `origin/main`)
Production code / test code / DB / migration / runtime / Host changes: **0**
Order / Cart / Payment mutations: **0** (all SQL used was `SELECT`; no HTTP mutation issued)
Evidence class tags used below: `FACT`, `RISK`, `UNKNOWN`, `RECOMMENDATION`.

Live environment read (read-only): tenant `tenant-alpha` / database `tooba_alpha`
(`inventory`,`order`,`cart`,`payment`,`catalog` schemas), Worker `tooba-worker-01`, channel `tooba-main`.

---

## (a) Verified current call graph (source file:line)

### a.1 Frontend — one click performs draft-save AND full commit

`src/frontend/app/storefront/storefront-shipping.tsx`

- `:707` the single button «ادامه» → `onClick={() => void persistAndContinue()}`
- `:225-251` `persistAndContinue()`:
  - `:231` `await saveShippingSelection(projection.cartId, projection.cartVersion, {…address…, shippingMethodCode, selectedDeliveryDate, selectedDeliveryTimeWindow, customerNote})`
  - `:246` `await commitShippingToPayment(projection.cartId, projection.cartVersion)`
  - `:250` `router.push(localizePath('/payment?checkoutId=' + encodeURIComponent(committed.checkoutId)))`
- `:115` `loadStorefrontCart()` used for the projection; no separate "view payment without order" step exists.

`src/frontend/app/storefront/storefront-shipping-api.ts`

- `:301-320` `saveShippingSelection(...)` → `PUT /v1/storefront/shipping/selection` (fetch at `:306`, body `:314-315`)
- `:363-398` `commitShippingToPayment(cartId, expectedCartVersion)` → `POST /v1/storefront/shipping/commit` (fetch `:368`), body `{cartId, expectedCartVersion, idempotencyKey, couponCode}` (`:376-380`)
- `:384-390` `if (!response.ok) await throwShippingError(response)`; **only after** success:
  `:391` `writeStoredCheckoutId(page.checkoutId)`, `:392` `persistCommittedCheckoutAndDetachActiveCart(page.checkoutId, page.cartId)`, `:394` clear shipping idempotency key
  → `FACT`: cart detachment happens strictly after a proven-OK commit response.
- `:359-361` `readStoredCheckoutId()` (sessionStorage fallback)

`src/frontend/app/storefront/storefront-cart-api.ts`

- `:76-83` `clearCartSession()` removes `cartId` + `guestSecret` and notifies listeners (pointer-only detach; committed proof is retained)

`src/frontend/app/payment/storefront-payment-handoff.tsx` (the `/payment` surface)

- `:42` `const checkoutId = params.get("checkoutId") || readStoredCheckoutId();`
- `:43-46` if none → `setError("سفارش ارسالشده پیدا نشد. از مرحلهٔ ارسال ادامه دهید.")` and stop
  → `FACT`: the payment page **cannot render at all without a committed `checkoutId`**.
- `:51-55` `Promise.all([loadStorefrontCheckout, loadStorefrontPaymentMethods, loadStorefrontWalletQuote])`
- `:81` `const paid = page?.paymentState === "Paid" || page?.canInitiatePayment === false;`
- `:85-100` method-specific guards; `:101-126` `pay()` → `startStorefrontPayment(page.checkoutId, {providerCode})`
- `:242` `disabled={paying || !method}` (intra-component double-click guard)

`src/frontend/app/storefront/storefront-payment-api.ts`

- `:20` `tooba.storefront.paymentIdempotency`; `:318-324` `paymentIdempotencyKey(checkoutId, providerCode)` (per checkout+provider, sessionStorage); `:414` used in initiate body; `:311` cleared on resets

`src/frontend/app/storefront/storefront-payment-methods.tsx`

- `:32` `walletEligible = Boolean(quote?.canPayFullyWithWallet)`; the picker only reflects server capabilities.

### a.2 Backend endpoints

`src/backend/Modules/Order/Tooba.Order.Endpoints/Storefront/StorefrontOrderEndpoints.cs`

- `:37` `MapPut("/shipping/selection", SaveShippingSelectionAsync)`
- `:38` `MapPost("/shipping/commit", CommitShippingAsync)`
- `:161-173` `SaveShippingSelectionAsync` → `SaveStorefrontShippingSelectionCommand`
- `:175-190` `CommitShippingAsync` → `CommitStorefrontShippingCommand(cartId, guestSecret, expectedCartVersion, idempotencyKey, …)`

### a.3 Draft path — CONTRACT-COMPLIANT, no side effects

`src/backend/Modules/Order/Tooba.Order.Application/Storefront/Services/StorefrontShippingService.cs`

- `:139-143` cart load + `cart.Version != request.ExpectedCartVersion` → `ShippingCartStale`
- `:155-166` `_checkouts.PrepareShippingAsync(...)`
- `:168-190` eligible shipping methods + delivery-slot validation
- `:199` `// کلاینت نمیتواند قیمت را جعل کند` → `amount = method.PriceAmount` (server-authoritative)
- `:204-235` writes **only** `CartShippingDraft` (`CartShippingDraft.Create` / `.Replace`, `_drafts.SaveAsync` at `:230`)

`FACT`: the only durable effect of `/shipping/selection` is a row in `order.cart_shipping_drafts`
(live index `pk_cart_shipping_drafts` UNIQUE on `(cart_id)`). No `order.checkouts`, no
`inventory.reservations`, no cart status change, no quota consumption.

### a.4 Commit path — the eager commit (single ambient TransactionScope)

`src/backend/Modules/Order/Tooba.Order.Application/Checkout/Process/CheckoutProcessManager.cs`

- `:62` `_host.LoadActiveCartAsync(command, ct)` → `CheckoutSubmitHost.cs:78-92`: rejects
  `checkout.cart.missing` / `checkout.cart.expired` (status≠Active) / `checkout.version.conflict` / `checkout.cart.empty`
- `:95-99` `using var scope = new TransactionScope(Required, new TransactionOptions { IsolationLevel = ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled)`
- `:100` `EnsureCanStartInitialReservationAsync(command.PlacedByUserId, abuseSettings, ct)`
- `:106` `ResolveInitialReservationExpiresAtAsync(cart, now, ct)`
- `:109-119` `CheckoutInventoryReservationRequest(cart.CartId, processId, correlationId, now, expiresAt, lines…)`
- `:119` `_inventory.ReserveForCheckoutAsync(reserveRequest, ct)`
- `:123-131` `PersistCheckoutAsync(...)` → CheckoutGroup + per-seller `SellerOrder`s (`PendingPayment`)
- `:141-158` `CartConversionIntent.OnlinePurchase` → `_cartConversion.ConvertForCheckoutAsync(...)`
- `:158-173` cart-conversion race branch (`TryResolveConvertedCartWinnerAsync`)
- `:179-182` `MarkPaymentPending` + `scope.Complete()`
- `:186-205` recovery for `CheckoutConflictException` and `ContractOperationException("inventory.reservation.conflict")`

`src/backend/Modules/Order/Tooba.Order.Infrastructure/Checkout/Persistence/CheckoutDirectory.Reservations.cs`

- `:41-55` `ReserveCartLinesForOrderAsync` (builds the reserve request)
- `:114-131` `ResolveInitialCycleExpiresAtAsync` → `now.AddMinutes(policy.InitialHoldMinutes)`
- `:84-110` `PrepareInitialCycleAsync` → `order.reservation_cycles` cycle #1, reason `InitialPayment`

### a.5 Payment path — no payability gate on money entry

`src/backend/Modules/Payment/Tooba.Payment.Application/Storefront/Orchestration/StorefrontPaymentOrchestrator.cs`

- `:110-121` `InitiateAsync(...)` first line: `_actorPolicy.EnsureCheckoutActorAsync`  then
  `_checkouts.GetForMutationAsync(checkoutId, cartId, guestSecret, authenticatedUserId, ct)`
- `:125-137` wallet branch (quote must fully cover) → `providerCode = Wallet`
- `:139-153` manual / gateway branches
- `:159-163` `_payments.InitiateAsync(new InitiatePaymentCommand(checkoutId, actor, null, idempotencyKey, providerCode), ct)`
- `:167-198` wallet: immediate `VerifyAsync` → `Succeeded` inside the same request
- `:201-213` manual: `RedirectUrl …awaitingManual=1`
- `:348-391` `SubmitManualEvidenceAsync` → `_orderPayments.PromoteReservationsForManualPaymentReviewAsync(checkoutId, reviewExpiresAt, ct)` (`:383-387`)

`src/backend/Modules/Order/Tooba.Order.Infrastructure/Integrations/Payment/CheckoutPaymentAccessBridge.cs`

- `:22-27` `GetForMutationAsync` = `OwnedAsync` + optional `cartId == snapshot.CartId`
- `:43-51` `OwnedAsync` — auth user passes; guest requires `guestSecret` matching `snapshot.CartId`
- `:55-59` `Map` → `(CheckoutId, PayableAmount = Σ GrandTotalSnapshot + ShippingAmount, Currency, OrderNumber)`

`src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Directories/PaymentDirectory.cs`

- `:61-65` idempotency lookup `FirstOrDefaultAsync(x => x.IdempotencyKey == key)` (non-atomic; `FACT` of shape, `RISK` of concurrency — see §c/vii)
- `:139-150` `GetPayableAsync` + `PendingPayment` + currency checks; `:148-150` `payment.initiate.reservation_forbidden` when mode ≠ `OnlinePurchase`
- `:186-200` create payment + `_timeoutAssigner.Assign(payment, now)` → `UnpaidTimeoutAt`
- `:213-261` `VerifyAsync`: `:228-231` already-Succeeded early return; `:250-255` duplicate provider-txn guard

`src/backend/Modules/Order/Tooba.Order.Infrastructure/Integrations/Payment/OrderPaymentBridge.cs`

- `:84-127` `ApplyVerifiedSuccessAsync` → `RecordVerifiedPayment` (`:110`) then `:113-124`
  `EnsurePaidDurableOrKeepPaidAsync` + `CommitPaidCycleAsync` inside `try { … } catch (Exception) { /* money stays Paid, SupplyStatus Unavailable */ }`

`src/backend/Modules/Order/Tooba.Order.Application/Storefront/Checkout/Queries/GetStorefrontCheckout/GetStorefrontCheckoutQuery.cs`

- `:21-28` the **only** capability overlay: `if (HasSucceededPaymentForCheckoutAsync(id)) return page with { PaymentState="Paid", CanInitiatePayment=false };`
- default is `CanInitiatePayment = true` (`StorefrontOrderModels.cs:86`, `:208`)

### a.6 Cycle-aware payability exists in Order but is not consumed by the payment surface

- `src/backend/Modules/Order/Tooba.Order.Application/ReservationCycle/Policies/ReservationCyclePayabilityPolicy.cs:12-25` (`IsActive`, `HasReachedRetryLimit`)
- `src/backend/Modules/Order/Tooba.Order.Application/Storefront/Services/StorefrontPendingPaymentProjector.cs:98-99`
  `var canInitiate = !awaitingReview && !retryLimit && cycleActive; var canRetry = !awaitingReview && !retryLimit && cycleEnded;`
- Consumed only by the pending-payments card list (`ListStorefrontPendingPaymentsQuery` → `StorefrontPendingPaymentService.ListAsync:69-117`).

### a.7 Live policy limits (read-only `SELECT`)

| Source (live row) | Value |
|---|---|
| `catalog.store_hold_policy_settings` `initial_reservation_hold_minutes` | **1** |
| `catalog.store_hold_policy_settings` `retry_reservation_hold_minutes` | 1 |
| `catalog.store_hold_policy_settings` `max_reservation_cycles` | 2 |
| `catalog.store_hold_policy_settings` `online/manual/cart_persistence` hours | **NULL** (inherit platform) |
| `catalog.store_checkout_abuse_settings` | `max_open_unpaid=2`, `window=30 min`, `max_commits=3` |
| `payment.payment_method_hold_overrides` | rows for `manual`,`fake` — all hour columns **NULL** |
| effective platform fallbacks | `Payment:Gateway:OnlinePaymentHoldHours = 2` (`appsettings.json:111`, `PaymentGatewayOptions.cs:93,98`), `ReservationCycle:InitialReservationHoldMinutes = 120` (`appsettings.json:126-129`), `Cart:PersistenceHours = 168` (`:103-104`), workers `CartExpiry` 5 s (`:37-40`), `UnpaidOrderExpiry` 15 s (`UnpaidOrderExpiryWorkerOptions.cs:10-14`) |

Live proof of the 60 s effective reservation hold (not 120 min):

```
reservation 01a13309-f5f5-7000-9c0b-5ed9813dea28  qty=2  status=Released
  created_at = 2026-10-13 02:14:13.233565+00
  expires_at = 2026-10-13 02:15:13.127436+00   (= created + 60.000 s)
  updated_at = 2026-10-13 02:15:17.431506+00   (released ~4.3 s after expiry, CartExpiry poll 5 s)
```

---

## (b) Exact violations of the desired contract

| # | Desired contract clause | Observed | Evidence | Class |
|---|---|---|---|---|
| **V1** | Shipping / viewing payment / choosing method "MUST NOT create an Order/CheckoutGroup, convert/detach Cart" | One click on Shipping «ادامه» performs the **entire** commit: Order + CheckoutGroup + convert | `storefront-shipping.tsx:225-251`; `CheckoutProcessManager.cs:95-182` | FACT |
| **V2** | "MUST NOT … reserve/decrement stock" before the final pay action | Reservation is created at that same click | `CheckoutProcessManager.cs:119`; `CheckoutDirectory.Reservations.cs:41-55` | FACT |
| **V3** | "MUST NOT … consume unpaid-order/churn quotas" | `CheckoutAbuseGate.EnsureCanStartInitialReservationAsync` runs inside the same scope and `PrepareInitialCommit` writes `checkout_reservation_commits` | `CheckoutProcessManager.cs:100`; `CheckoutAbuseGate.cs:57-100`, `:102-123` | FACT |
| **V4** | "Only the final user action explicitly starting payment … starts one checkout commit" | Commit precedes payment; payment initiation has no commit role | `storefront-shipping.tsx:246,250`; `StorefrontPaymentOrchestrator.cs:110-121` | FACT |
| **V5** | "If payment initiation/transaction fails after successful commit, keep Order with safe retry/expiry policy" | No attempt-level retry on the *initiation* path; failure surfaces as an error while the Order+reservation exist | `StorefrontPaymentOrchestrator.cs:110-163`; `storefront-payment-handoff.tsx:124-126` | FACT |
| **V6** | Payment surface must be reachable for an Active cart ("without fake checkoutId") | `/payment` hard-fails without `checkoutId` | `storefront-payment-handoff.tsx:42-46` | FACT |
| **V7** | Method selection must not depend on a pre-existing Order | Method list + wallet quote are loaded *after* the order exists; `/payment` cannot be entered earlier | `storefront-payment-handoff.tsx:51-55` | FACT |
| **V8** | Docs must match code | `docs/architecture/47-checkout-order-foundation.md` still states step 5 "Require each cart line to already carry a reservation id (Cart hold). Do not consume stock on Order insert." and "No distributed transaction. `IdempotencyKey` uniqueness is the duplicate-submit seam." — current code reserves at commit and **does** use a cross-module `TransactionScope` | doc lines in §"Checkout orchestration" vs `CheckoutProcessManager.cs:95-99,119` | FACT (doc drift) |

Note on scope honesty: the outstanding Task explicitly labels the desired behaviour a
*product contract* and instructs to "distinguish old documented behavior from the new product contract".
V1–V5 are therefore **contract violations**, not regressions or bugs; nothing in the repository claims
the current eager-commit behaviour is new or wrong.

---

## (c) Inventory check+reserve atomicity and cross-module transaction boundaries

### c.i Single atomic, concurrency-safe reserve — SATISFIED

`src/backend/Modules/Inventory/Tooba.Inventory.Infrastructure/Directories/InventoryDirectory.cs:165-210`

- `:174-183` idempotency replay: prior `Held` row for the same key returns the same receipt (no second reservation)
- `:186-193` **conditional set-based update**, the real oversell guard:
  `.Where(x => x.StockItemId == stockItemId && x.OnHand - x.Reserved >= quantity && quantity > 0).ExecuteUpdateAsync(SetProperty(Reserved, Reserved + quantity))`
- `:193-196` `if (reserved != 1) throw ContractOperationException(InventoryErrorCodes.SupplyUnavailable)`
- `:199-200` `StockReservation.Hold(...)` + `position.RecordReserved(...)`
- `:202-209` single `SaveChangesAsync`; unique-index conflict → `InventoryErrorCodes.ReservationConflict`

`FACT`: availability arithmetic is `OnHand - Reserved` evaluated **inside** the SQL `UPDATE … WHERE`,
so two concurrent commits cannot both pass the predicate for the last unit. The preceding
`GetAvailabilityBatchAsync` in `CheckoutInventoryReservationAdapter.cs:34-37,60-66` is used only to
**choose a location**; it is not the guard. → **The clause "not separate query-then-command" is met.**

`FACT` (multi-line atomicity): `CheckoutInventoryReservationAdapter.cs:37-92` acquires per line and on
any failure releases already-acquired ids (`:87-92`) and rethrows; the enclosing `TransactionScope`
then rolls back. `LOCK-SF-050` ("no payable Order and no leftover reservations from a failed commit")
is therefore structurally enforced.

### c.ii Cross-module transaction — real single-DB ACID, not distributed

- Every module `DbContext` resolves its connection from the **same** reference:
  `ToobaNpgsql.ResolveForContext` (`src/backend/BuildingBlocks/Tooba.Persistence/ToobaNpgsql.cs:55-64`)
  `→ resolver.Resolve(commerce.Current.DatabaseConnectionReference)`, e.g. Cart:
  `CartModule.cs:53-65`; the `SingleStore`/`tenant-alpha` deployment maps one tenant to one database.
- Therefore Cart / Order / Inventory / Catalog connections in `CheckoutProcessManager.SubmitAsync:95-99`
  all enlist in one ambient `System.Transactions` transaction against one PostgreSQL database
  (`tooba_alpha`), which is a real ACID transaction over multiple schemas.
- `FACT`: no outbox/saga/compensation is needed *for the commit itself* in the current deployment.
- `RISK` (extraction): if any participant were moved to a different database/host, that same
  `TransactionScope` would silently require promotion to a distributed transaction (MSDTC); on
  Linux/PostgreSQL this is not supported and the commit would fail at runtime. The commit boundary is a
  **microservice-extraction blocker** and must be re-evaluated before any such extraction.

`UNKNOWN` (runtime, not proven here): no evidence was found that a *runtime concurrency test* proves
enlistment. `src/backend/Host/Tooba.Host.Tests/AtomicCheckoutCommitTests.cs:26-60` is a **source-text
assertion** test (`Assert.Contains("TransactionScope", checkout)`), not a behavioural/concurrency proof.
A documented runtime observation exists for the anti-abuse lock only
(`docs/evidence/TB-P10-T004/r23-concurrency.md`: two prepared carts committed with `Promise.all`
→ 1 winner `200`, 1 loser `409 checkout.open_unpaid_limit_reached`). Two concurrent commits racing on
the *same* stock item were not re-executed here (read-only mandate).

### c.iii Abuse gate is inside the same scope — correct but eager

`CheckoutAbuseGate.cs:145-164` acquires/updates a per-customer row (`checkout_abuse_customer_locks`)
with `SaveChangesAsync` **inside** the ambient scope, then counts open unpaid checkouts
(`:174-200`) and churn (`:81-99`). `FACT`: the quota is consumed at Shipping-continue time, i.e. before
any payment intent exists (violation V3).

### c.iv Failure / race matrix (commit boundary)

| Scenario | Behaviour | Class |
|---|---|---|
| Two concurrent commits, same cart | `ix_checkouts_cart_id` UNIQUE + `ux_checkout_processes_submission_key` UNIQUE + cart `Version` concurrency token (`CartDbContext.cs:58`) → exactly one CheckoutGroup; loser either recovers the winner (`CheckoutProcessManager.cs:158-173,186-205`) or fails with a typed error | FACT (constraints), documented runtime (r23) |
| Two concurrent commits, different carts, same last unit | conditional `ExecuteUpdateAsync` predicate → one `SupplyUnavailable` | FACT (mechanism) |
| Reserve succeeds, order persist fails | scope rolls back → no window; `LOCK-SF-050` | FACT |
| Reserve succeeds, cart convert fails (true race) | winner resolution or rethrow; scope not completed | FACT |
| Converted cart re-submitted later | `LoadActiveCartAsync` (`CheckoutSubmitHost.cs:78-82`) throws `checkout.cart.expired` **before** the try/catch, so the winner-resolution branch cannot run; frontend maps it to «سبد خرید منقضی شده است» (`storefront-checkout-api.ts:278-279`) though it is *Converted*, not expired | FACT (behaviour + copy mismatch) |
| Payment initiation fails after a successful commit | Order + reservation remain; reservation expires per policy; no automatic initiation retry | FACT (V5) |

### c.v Idempotency of the final pay click and of initiation

- Frontend: per-checkout+provider key in `sessionStorage` (`storefront-payment-api.ts:20,318-324,414`) and
  `disabled={paying}` (`storefront-payment-handoff.tsx:242`) → sequential double-click is idempotent.
  `FACT` (sequential).
- Backend: `PaymentDirectory.InitiateAsync:61-65` checks `IdempotencyKey` with a plain read, then inserts
  at `:186-200`; the only atomic guard is `ix_payments_idempotency_key`. Two **concurrent** requests can
  therefore race and surface as an unhandled unique-violation (5xx) rather than a clean replay.
  `RISK` (concurrency), proven at the code level, not reproduced here.
- `VerifyAsync` duplicate-callback handling is sequential-safe (`:228-231`, `:250-255`); `Payment` has no
  row version/concurrency token (`grep IsConcurrencyToken` finds only Cart/Story/PageComposition), so two
  concurrent verifies could double-emit `payment.succeeded.v1` and both call
  `EnsurePaidDurableSupplyAsync`, whose reacquire key embeds `_clock.UtcNow.UtcTicks`
  (`InventoryDirectory.OrderSupply.cs:143`) → two durable reservations for one order line.
  `RISK`, code-level.

### c.vi Reservation expiry ownership

`InventoryDirectory.Reclaimer.cs:14-63` selects `status='Held' AND expires_at IS NOT NULL AND expires_at <= now`
with `FOR UPDATE SKIP LOCKED`. Its **only production caller** is the Cart module's expiry sweep
(`CartExpiryScanner.cs:47`). `FACT`: order-bound hold release depends on the Cart worker being enabled
(`Tooba:CartExpiry:Enabled`, `appsettings.json:37-40`); `docs/operations/background-processing.md`
documents only Outbox + CartExpiry workers and omits `UnpaidOrderExpiry`. `RISK` (operational coupling).

Also `FACT`: `CloseExpiredDueAsync` (`ReservationCycleDirectory.cs:165-183`) only closes `Active` cycles
and never cancels a seller order; unpaid order timeout requires a `payment.payments` row
(`PaymentExpiryDirectory.cs:62-80`). Live consequence: checkout `01a09b8b-3e28-7000-94ed-e37517c5cbf5`
has two `Expired` cycles (2026-09-13, 2026-10-11) and its seller orders are still `PendingPayment` today.

---

## (d) Smallest safe design + migration/compatibility risk

### d.1 Target shape (keeps every existing module and lock)

```
Cart (Active, owned)  →  Shipping (draft only)  →  /payment (read-only quote surface on CartId)
                                                          │  final click "پرداخت"
                                                          ▼
                            ONE orchestrated call, method-aware, idempotency-key-scoped to CartId:
                              1. commit  (Order/CheckoutGroup PendingPayment + atomic reserve + convert)
                              2. initiate (gateway | wallet | manual)
```

Ownership of transitions (unchanged module owners, per `.cursor/skills/*` and `docs/architecture/47-*`):

| Transition | Owner | Notes |
|---|---|---|
| Cart read for a pre-order payment surface | **Cart** (`ICartPresentationGateway`) | read-only; `LOCK-SF-048` (no hard hold on read) preserved |
| Shipping draft | **Order** (`order.cart_shipping_drafts`) | already compliant |
| Eligibility / method catalog / wallet quote | **Payment** (`IPaymentGatewayCatalogPort`, `IWalletOrderPaymentPort`) | already CartId-independent |
| Commit (reserve → order → convert) | **Order** coordinator, **Inventory** owner of the atomic reserve, **Cart** owner of `ConvertAsync` | unchanged mechanics |
| Payability verdict | **Order** (`ReservationCyclePayabilityPolicy`) | single authority, `LOCK-SF-074/076` |
| Reservation expiry | **Inventory** (mechanism) driven by **Order** cycle policy | see d.4 |
| Anti-abuse quota | **Order** (`CheckoutAbuseGate`) | must move to the commit step only |

### d.2 What must change (contracts / UI / API)

- **NEW read contract (no fake checkout):** a payment *draft/quote* projection addressed by `CartId`,
  not `checkoutId` — e.g. `GET /v1/storefront/cart/{cartId}/payment-draft` returning
  `{cartId, cartVersion, currency, merchandiseSubtotal, shippingSelectionSummary, shippingAmountEstimate,
   eligibleMethods[], walletQuote, requiresShipping, canStartPayment}` with **zero** writes.
  Compose it from existing ports only: `ICartPresentationGateway`, `order.cart_shipping_drafts` read,
  `IPaymentGatewayCatalogPort`, `IWalletOrderPaymentPort`, `ICheckoutReservationHoldPolicy` preview.
- **NEW single orchestrated command (final click):** `POST /v1/storefront/checkout/{cartId}/start-payment`
  with `{expectedCartVersion, shippingSelectionRef, providerCode|wallet|manual, idempotencyKey}` that
  performs commit-then-initiate and returns `{checkoutId, paymentId, redirectUrl|awaitingManual}`.
  Semantics: if a checkout already exists for `cartId` → reuse it (`ix_checkouts_cart_id`) instead of
  re-committing; if it exists **and** a Succeeded payment exists → `payment.already_succeeded`.
  The `idempotencyKey` must cover **both** steps so a retry cannot commit twice nor initiate twice.
- **`CanInitiatePayment` overlay** (`GetStorefrontCheckoutQuery.cs:21-28`) must additionally reflect the
  cycle verdict so the pay button cannot be enabled while the reservation is gone.
- **Frontend:** `persistAndContinue` (`storefront-shipping.tsx:225-251`) stops after
  `saveShippingSelection` and navigates to a cart-addressed payment surface; the commit moves into the
  final pay handler; `storefront-payment-handoff.tsx:42-46` accepts a `cartId` source.
- **Docs:** `docs/architecture/47-checkout-order-foundation.md` step 5 + "No distributed transaction"
  must be corrected to match code (V8) — documentation only.

### d.3 Backward compatibility

- Keep `/shipping/commit` and `checkoutId`-addressed surfaces temporarily so existing
  pending-payment card flows (`storefront-pending-payments.tsx:137,148`) and the payment-result/proof
  lifecycle (`LOCK-SF-066`, `LOCK-SF-071`) remain functional during migration.
- Existing Orders created by the old eager path are untouched: their cycles, holds and proofs stay valid.
- No schema change is strictly required for the draft projection; an optional
  `order.cart_payment_drafts` table would only be needed if method choice must survive a page reload.

### d.4 Expiry and quota timing under the new design

- Reserve/TTL starts **at the final pay click** (same call as order creation) — this removes the
  "user is deciding while stock drains" window entirely and makes `initial_reservation_hold_minutes`
  semantics honest. `initial_reservation_hold_minutes = 1` then becomes acceptable, though a floor
  relative to `online_payment_hold_hours` is still recommended as a guard.
- Anti-abuse counters (`checkout_reservation_commits`, open-unpaid count) move to the commit step, which
  is also where they belong semantically; their values/limits stay unchanged (`LOCK-SF-*, caps 2/30/3`).
- Do **not** merge the two timeouts: `LOCK-SF-047`, `SF-055`, `SF-039`, `SF-052`, `SF-080` deliberately
  keep reservation hold (minutes) and unpaid-payment timeout (hours, created at initiation) as separate
  lifecycles. The defect is the *absence of a payability gate* on the money entry point, not the
  existence of two durations.

### d.5 Wallet and offline paths

- Wallet (`StorefrontPaymentOrchestrator.cs:125-137,167-198`): fully synchronous inside the initiate
  request; under the new design this is the same single call → commit then immediate verified success.
  No extra boundary is needed; it stays inside the current try/catch so money cannot be lost silently.
- Manual/card-to-card (`:201-213`, `:348-391`): commit → `Pending` payment with
  `UnpaidTimeoutAt = ManualPaymentInitialHoldHours` → evidence submit promotes the reservation to the
  review hold and opens a `ManualReview` cycle (`OrderPaymentBridge.cs:148-215`). Unchanged.

### d.6 Migration / compatibility risk register

| Risk | Why | Mitigation |
|---|---|---|
| Duplicate commit after the move | double-click / retry now spans commit+initiate | one CartId-scoped idempotency key + `ix_checkouts_cart_id` reuse + `payment.already_succeeded` |
| Cart mutated between draft view and pay | user edits cart in another tab | `expectedCartVersion` re-check at commit (`CheckoutSubmitHost.cs:83-86`), already present |
| Guest ownership at the new endpoint | guest has no session; ownership today rides on the committed cart (`CheckoutPaymentAccessBridge.OwnedAsync:43-51`) | keep `CartAccess`/`GuestSecret` as the pre-order authority; do not invent a new token |
| Loss of the "empty payment page" expectation | some tests assert a hard error when `checkoutId` is absent | update those tests within the same bounded task |
| Abuse semantics shift | quota consumed later | keep counters/limits identical; only the timing moves |
| Doc/lock drift | `47-*.md`, `TOOBA-LOCKS.md` | doc-only correction; never rewrite locks unilaterally |

### d.7 Ordered bounded follow-up tasks (proposed, NOT started)

F1 (~15 min) — `GET /v1/storefront/cart/{cartId}/payment-draft` read-only projection + focused read-only test.
F2 (~20 min) — `POST /v1/storefront/checkout/{cartId}/start-payment` orchestration reusing the existing commit path; tests: idempotent double-call, existing-checkout reuse, zero orphan reservation on failure.
F3 (~15 min) — cycle-aware `CanInitiatePayment` overlay + error copy; tests: expired-cycle checkout cannot initiate.
F4 (~15 min) — frontend switch: Shipping stops committing; `/payment` reads the draft; final button calls start-payment; update the hard-error tests.
F5 (~15 min) — doc correction (47-*.md step 5, distributed-transaction sentence) + guard test.
F6 (optional, ~15 min) — concurrency hardening of `payment.payments` initiation/verify (conditional update) — separate authorization.

Each: single frontend/backend focused test run, PASS = zero new rows in
`order.checkouts` / `inventory.reservations` / `order.checkout_reservation_commits` for the draft steps,
rollback = revert the single commit (`git revert`), no data migration.

---

## (e) Next ONE recommended implementation task

**TB-P0-CHECKOUT-PAYMENT-DRAFT-011 — read-only pre-order payment draft addressed by CartId.**

- Mode: bounded implementation, read-only surface. Owner: **Order** (Application+Endpoints), consuming
  existing Cart/Payment/Inventory hold-preview contracts. No Host edit. No schema change.
- Scope: one new query + handler + endpoint `GET /v1/storefront/cart/{cartId}/payment-draft`
  returning `{cartId, cartVersion, currency, merchandiseSubtotal, shippingDraftSummary,
  shippingAmountEstimate, eligibleMethods[], walletQuote, canStartPayment, blockers[]}`.
  Compose from `ICartPresentationGateway`, the existing `order.cart_shipping_drafts` read,
  `IPaymentGatewayCatalogPort`, `IWalletOrderPaymentPort`, and the hold-policy preview port only.
  **Zero writes.**
- Files (expected, minimal): 1 query + validator + endpoint mapping (+1 endpoint registration line);
  no changes to `CheckoutProcessManager`, `InventoryDirectory`, `PaymentDirectory`, or the frontend.
- Single targeted test run: the Order storefront/endpoint test project, one new test asserting the
  draft response **and** that `order.checkouts`, `order.cart_shipping_drafts`,
  `inventory.reservations` and `order.checkout_reservation_commits` row counts are unchanged, and that
  `cart.carts.status` is still `Active`.
- PASS: response 200 for an Active cart with a shipping draft; all four counters unchanged; cart still
  Active; no `checkoutId` required anywhere in the request.
- FAIL: any write, any quota row, any reservation row, any cart status change, or a response that
  requires a pre-existing `checkoutId`.
- Rollback: revert the single commit; nothing durable was created.
- Why first: every later step (F2/F4) depends on a surface that can render payment without an Order;
  this step is provably side-effect-free and therefore the lowest-risk entry point.

---

## (f) UNKNOWN / not provable read-only

- **U1** — Whether two concurrent commits racing on the **same stock item** were ever exercised in a
  runtime test. Only the mechanism (conditional `ExecuteUpdateAsync`) is proven, plus a documented
  runtime proof for the abuse-lock race (`docs/evidence/TB-P10-T004/r23-concurrency.md`).
- **U2** — Whether `TransactionScope` enlistment across the module contexts is exercised by any
  *behavioural* runtime test. `AtomicCheckoutCommitTests` is source-text assertion only.
- **U3** — Whether a production tenant runs with platform defaults (120 min / 2 h) rather than the
  1-minute store override observed on `tenant-alpha`; only the alpha row was read.
- **U4** — Whether the `2026-10-12` `open_unpaid 2/2` blocks were caused by the zombie unpaid checkout;
  not established (the checkouts from that window are `Cancelled`).
- **U5** — Whether any automatic refund/compensation exists for the accepted
  "paid but supply unavailable" state (`LOCK-SF-060`); no such path was found, so it is unproven rather
  than disproven.
- **U6** — Behaviour of the `/payment` page when the reservation cycle is expired but no payment row
  exists; only static evidence was gathered (no runtime session was driven — read-only mandate).

Suggested tiny read-only verification (not performed): run only the two above-named test classes in
read-only mode to convert U1/U2 into facts.

---

## Appendix — evidence inventory (read-only)

Source reads: `storefront-shipping.tsx`, `storefront-shipping-api.ts`, `storefront-cart-api.ts`,
`storefront-checkout-api.ts`, `storefront-payment-api.ts`, `storefront-payment-methods.tsx`,
`storefront-payment-handoff.tsx`, `storefront-pending-payments.tsx`,
`StorefrontOrderEndpoints.cs`, `StorefrontShippingService.cs`, `CheckoutProcessManager.cs`,
`CheckoutSubmitHost.cs`, `CheckoutDirectory.Reservations.cs`, `CheckoutAbuseGate.cs`,
`CheckoutPaymentAccessBridge.cs`, `OrderPaymentBridge.cs`, `OrderPaymentSucceededHandler.cs`,
`CheckoutInventoryReservationAdapter.cs`, `InventoryDirectory.cs`, `InventoryDirectory.OrderSupply.cs`,
`InventoryDirectory.Reclaimer.cs`, `StockReservation.cs`, `StorefrontPaymentOrchestrator.cs`,
`PaymentDirectory.cs`, `PaymentExpiryDirectory.cs`, `CommerceHoldPolicySource.cs`,
`PaymentGatewayOptions.cs`, `GetStorefrontCheckoutQuery.cs`, `StorefrontPendingPaymentProjector.cs`,
`ReservationCyclePayabilityPolicy.cs`, `ReservationCycleDirectory.cs`, `UnpaidOrderExpiryReconciler.cs`,
`ReservationCyclePolicyResolver.cs`, `ReservationCycleHoldPolicyReader.cs`, `ToobaNpgsql.cs`,
`CartModule.cs`, `CartDbContext.cs`, `CartExpiryScanner.cs`, `ContractOperationException.cs`,
`SaveHoldPolicySettingsCommandValidator.cs`, `SaveReservationPolicyCommandValidators.cs`,
`appsettings.json`, `docs/architecture/47-checkout-order-foundation.md`, `docs/architecture/TOOBA-LOCKS.md`,
`docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/*`, `docs/evidence/TB-P10-T004/r23-concurrency.md`.

Live read-only SQL (`tooba_alpha`): `catalog.store_hold_policy_settings`,
`catalog.store_checkout_abuse_settings`, `catalog.reservation_cycle_policy_overrides`,
`payment.payment_method_hold_overrides`, `payment.payments`, `order.checkouts`,
`order.seller_orders`, `order.reservation_cycles`, `order.checkout_abuse_block_events`,
`inventory.reservations`, `pg_indexes` (order/cart/payment/inventory).
No write, DDL, migration, build, Host restart, or HTTP mutation was performed.
