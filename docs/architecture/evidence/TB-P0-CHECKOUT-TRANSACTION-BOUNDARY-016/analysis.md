# TB-P0-CHECKOUT-TRANSACTION-BOUNDARY-016 — Read-only architecture proof

Program: TOOBA-CHECKOUT-PAYMENT-COMMIT-BOUNDARY
Parent-Task: TB-P0-INVENTORY-PAYMENT-HOLD-GATE-015
Mode: READ_ONLY_ARCHITECTURE_PROOF — STOP_FOR_ARCHITECT
Base verified at claim: `HEAD == origin/main == 2d7597b778ce256f3c54c4c0989bd72915ba0675`
015 implementation SHA: `60ea9e8481a8e4dc2fb8afdd4486d8dab7647aa6`; evidence follow-up: `2d7597b77`.
READ_ONLY — NO_CHANGES: no production, test, governance, migration, configuration, database or
runtime value was written; no build, no test run, no HTTP mutation, no Host interaction.

## FACT

**F1 — Initiation interleaves a network call with the Payment write, without a transaction.**
`PaymentDirectory.InitiateAsync` resolves the gateway (`PaymentDirectory.cs:166`), calls the external
provider **before** persisting (`:194 var initiation = await gateway.InitiateAsync(...)`) and only then
opens the aggregate and saves (`:195-200`). No transaction wraps `:61-209`. So any design that puts
initiation inside a PostgreSQL transaction would freeze an external call under locks, and any rollback
after `:194` leaves a provider-side intent with no local row.

**F2 — The money-moving wallet debit happens in a different call, not in initiation.**
`StorefrontPaymentOrchestrator.InitiateAsync` calls `_payments.InitiateAsync` (`:159`) and, for the
wallet provider, immediately calls `_payments.VerifyAsync` (`:170-176`). The debit itself is
`WalletPaymentGateway.VerifyAsync` → `_wallets.SpendForOrderPaymentAsync` (`WalletPaymentGateway.cs:76`)
→ `WalletDirectory.SpendForOrderPaymentAsync`. Two module databases (Payment, Wallet) participate in
one storefront request with **no** transaction and **no** ambient scope.

**F3 — The only ambient TransactionScope in the system is at cart→checkout submit, and it spans
several module DbContexts.**
`CheckoutProcessManager.SubmitAsync` opens `TransactionScope(Required, ReadCommitted,
AsyncFlow)` (`CheckoutProcessManager.cs:95-98`) and inside it calls Inventory
(`_inventory.ReserveForCheckoutAsync`, `:119` → `InventoryDbContext`), the Order host
(`_host.PersistCheckoutAsync`, `:124` → `CheckoutSubmitHost` → `OrderDbContext.SaveChangesAsync`,
`CheckoutSubmitHost.cs:193`) and Cart conversion (`:146`). That is ≥2 physical connections inside one
ambient scope. Nothing in the repository sets `Enlist=false`, `max_prepared_transactions`, or any
ambient-transaction option (grep over `src/backend` for `Enlist|max_prepared_transactions|
AmbientTransaction` returns only the new 015 gate text), and `ToobaNpgsql.ConfigureModuleContext`
(`ToobaNpgsql.cs:33`) passes the connection string through unchanged.

**F4 — Order already owns late-capture and re-acquisition compensation.**
`OrderPaymentBridge.RecordVerifiedPaymentAsync` keeps late captured money as Paid and deliberately
swallows a supply failure (`OrderPaymentBridge.cs:112-125`, comment "Late captured money stays Paid;
SupplyStatus remains Unavailable"). `OrderUnpaidRetrySupplyBridge.EnsureRetrySupplyAsync`
(`OrderUnpaidRetrySupplyBridge.cs:28-84`) re-acquires inventory (`EnsureUnpaidRetryHoldAsync`) and
opens a retry cycle. `OrderPaymentBridge.PromoteReservationsForManualPaymentReviewAsync` (`:149-177`)
promotes or re-acquires holds through `IOrderInventoryLifecyclePort.
PromoteOrReacquireForManualPaymentReviewAsync`. `StockReservation.CommitForPaidOrder` clears the TTL
(`ExpiresAt = null`) for a paid order (`StockReservation.cs:102-121`).

**F5 — The 015 gate requires an ambient transaction and deliberately leaves its connection open.**
`InventoryDirectory.PaymentHoldGate.cs:41-48` refuses without `Transaction.Current` or with an
already-open connection; `:56` opens the connection inside the scope; `:76` refuses unless
`txid_current_if_assigned()` confirms a transaction block. Locks live until the caller's transaction
ends (proved in 015 evidence: `H_COMPETING_FOR_UPDATE=BLOCKED_BY_HELD_ROW_LOCKS`,
`I_AFTER_SCOPE_FOR_UPDATE=NOT_BLOCKED_LOCKS_RELEASED`).

**F6 — Hold expiry and cycle expiry are independent, app-clock vs db-clock, and poll-driven.**
The reclaimer releases `Held` rows where `expires_at <= {utcNow}` using `FOR UPDATE SKIP LOCKED`
(`InventoryDirectory.Reclaimer.cs:36-46`) and takes the comparison time from the **application clock
parameter**; the 015 gate compares with `clock_timestamp()`. Order closes cycles only when its 15 s
poller sees `ExpiresAt <= now` (`ReservationCycleDirectory.CloseExpiredDueAsync:165-169`).

**F7 — A dedicated money-entry endpoint already exists.**
`POST /v1/storefront/checkout/{checkoutId}/payments` (`PaymentStorefrontEndpoints.cs:22`) is a separate
request from checkout submit, so the "composite endpoint" is a server-side gap inside Payment's own
route, not a new route.

## RISK

**R1 — A multi-DbContext ambient scope is not a safe boundary here.** 015 measured on this server
(PostgreSQL 17.10, `max_prepared_transactions = 0`) that a second enlisting connection inside one scope
promotes it and the commit then fails with `55000 prepared transactions are disabled`. F3 shows the
existing submit path already puts several module connections inside one scope. Whatever protects the
current submit path (volatile single-phase delisting when a pooled connection closes, or promotion not
actually occurring), it is **provider/pool behaviour that 015 proved fragile**, and it must not be
extended to the money path. No naive multi-DbContext `TransactionScope` may be proposed.

**R2 — Locks cannot cross the PSP call, so "keep the hold" must be achieved by state, not by locks.**
F1 means initiation includes a network round trip. A row lock spanning it is both a lock-duration
hazard and a transaction-duration hazard. The hold must be protected by an explicit durable fact
(TTL extension, promotion, or a claim row) written in a **short** transaction.

**R3 — Gate input has no ownership binding.** `IInventoryPaymentHoldGate` verifies only that the
supplied distinct ids are Held and unexpired. It cannot tell whether those reservations belong to the
caller's checkout; a caller passing foreign Held ids receives `Verified`. The caller must derive ids
from its own persisted order lines, and this must be asserted by a guard.

**R4 — The gate returns with a DbContext connection deliberately still open.** That is required for
lock lifetime but is invisible to the DI/scope owner; the next module DbContext used in the same scope
is what triggers R1's promotion. The contract needs either a returned disposable handle or an explicit
"one connection per scope" rule enforced by a guard.

**R5 — Inconsistent time authority.** The reclaimer compares `expires_at` against an application-clock
parameter (F6) while the gate compares against the database clock. With skew, a reservation can be
live for the gate and already release-eligible for the reclaimer in the same instant. One authority
must win.

**R6 — Async PSP success has no in-flight owner.** For the online provider the storefront returns a
redirect and success arrives later through `PaymentWebhookEndpoints`
(`POST /v1/payments/webhooks/{providerCode}`). Between initiation and callback nothing holds the
checkout; only the TTL keeps the stock, so the TTL must already cover the worst-case callback delay.

## UNKNOWN

**U1 — Why the existing multi-connection submit scope commits at all (F3 vs 015's measured failure).**
Cannot be settled by reading; it needs a runtime probe (Npgsql enlistment/delistment behaviour when a
pooled connection closes inside an ambient scope, and whether promotion actually happens in the Host).
This is the single most important missing evidence, because it decides whether the platform can rely on
ambient scopes anywhere on the money path. It is also exactly the evidence that must **not** be
gathered by adding more connections to the money path.

**U2 — Whether `PaymentDirectory.InitiateAsync` can fail after the provider call** (`:194`→`:200`)
leaving a provider intent without a local row; no reconciliation of that window was found by reading.

**U3 — Whether the online-gateway timeout (`_timeoutAssigner.Assign`, `PaymentDirectory.cs:197`) is
already configured to exceed the reservation TTL.** The two numbers live in different modules and no
single check ties them together.

## Repo review of 015 (subtle issues)

1. **Connection left open on the success path** (`PaymentHoldGate.cs:56`) — required, but undisclosed
   to the scope owner; see R4. A caller that then touches a second module inside the same scope walks
   straight into R1.
2. **Package-visible seal break for testability.** `EvaluatePaymentHoldGate` and
   `NormalizePaymentHoldGateIds` are `public static` on the infrastructure class purely so tests can
   call them without a database; the Infrastructure surface now exposes a decision function. A
   `private static` + `InternalsVisibleTo` (or a small internal verdict type) would keep the surface
   honest.
3. **Narrow fail-closed catch.** Only `PostgresException` is caught (`:86`, `:213`); other provider
   failures (connection-level `NpgsqlException`, `InvalidOperationException` from a disposed context)
   propagate as an untyped fault instead of `AmbientTransactionRequired`.
4. **`HeldCount` semantics.** `NotHeld` always reports `HeldCount = 0` even when some rows were live
   (`EvaluatePaymentHoldGate`), and `Verified` reports `HeldCount = RequestedCount`; the field is a
   verdict projection, not a count, which should be stated in the contract or renamed.
5. **`Guid.Empty` is reported as "unavailable" rather than "invalid"** — acceptable fail-closed, but
   the contract does not say so, so a caller cannot distinguish a caller bug from a released hold.
6. **No in-repo controlled lock-contention test.** The decisive evidence in 015 came from an external
   throwaway harness (recorded `probe/program.cs.txt`), not from a durable test, so a regression in the
   locking shape would not be caught by CI. It also remains `UNVERIFIED` end to end against a genuinely
   `Held` row (dev inventory has `Consumed=6`, `Released=25`, zero `Held`).
7. **Duplicate/missing/NULL-TTL handling is correct but only unit-tested at the pure-verdict level**;
   the SQL predicate `expires_at IS NULL OR expires_at > clock_timestamp()` and the `= ANY($1)` binding
   are not covered by a repository test.

## The three guarantees, separated

**(a) Authorizing an attempt on valid reserved stock at one linearization point.** Achievable. It needs
exactly one short transaction whose commit is the linearization point, and that transaction must touch
**one** module's data. The gate alone does not create it: the gate's verdict dies when its transaction
ends, so the authorization is only durable if the same transaction also writes the durable fact that
protects the stock (TTL extension / promotion / claim). Verified-but-not-persisted is worth nothing.

**(b) Keeping a hold during asynchronous PSP operation.** Not achievable with locks (R2). Achievable by
making the hold durable for a window that provably covers the PSP worst case (TTL extension or promotion
written in the transaction of (a)), followed by the async callback closing the loop. The window must be
longer than the provider timeout (U3) and longer than the reclaimer poll interval so a released-then-
reacquired flap cannot occur.

**(c) Reconciliation for late success.** Already owned by Order (F4): late capture stays Paid, the
supply failure is recorded rather than thrown, and unpaid-retry/manual-review re-acquire or promote the
hold. The missing piece is only that nothing today prevents (a) from authorizing an attempt against a
hold that (b) does not cover — i.e. the gap is before the charge, not after it.

## DECISION (one bounded next implementation task)

**Do not** implement a cross-module ambient scope, and **do not** put initiation inside a transaction.
Make the linearization point a **single-connection, single-module Inventory write** that both proves and
extends, and keep the PSP call outside any transaction.

Candidate task: *Inventory-owned atomic payability proof + hold extension.*

1. Add one Inventory Contracts member next to the 015 gate (same `Contracts/Orders` capability,
   no new error code) that atomically, inside a **single Inventory-owned transaction** (its own
   connection; **no** ambient scope required, so no enlistment and no promotion):
   proves that every supplied distinct reservation is `Held` and unexpired under `FOR UPDATE` in
   deterministic order, then raises `expires_at` to `max(existing, db_now + window)` for exactly those
   rows, and returns per-id proof (`Applied`/`Unavailable`) with the effective deadline.
2. Order (already the owner of the checkout and of the cycle) calls it **before**
   `POST /v1/storefront/checkout/{checkoutId}/payments` performs initiation, derives the ids from its own
   lines, and Payment initiates outside any transaction as today.
3. Keep the 015 gate as-is for callers that genuinely own an ambient scope; the new method is the
   supported path for the money entry.

Pros: no distributed transaction (one connection, one module, one `SaveChanges`); no migration and no
new status (reuses `expires_at` and the existing promote semantics); no network call under locks; the
failure mode of an unused extension is harmless (the reclaimer takes it back at the new deadline);
cycle-status lag (F6) stops mattering because the stock hold now outlives the cycle row; (c) is
unchanged and already safe.

Cons: the stock is protected by a *time window*, not by a lock — the window must be sized from the
provider timeout; the extension is applied before initiation, so a failed initiation costs one window
of availability; and Inventory must refuse rather than invent a deadline when the caller cannot supply
the policy window.

Three essential tests: (1) all-or-none — one released/consumed/missing/expired id ⇒ no row extended and
no `Applied` proof; (2) idempotent and monotonic — a second application never shortens `expires_at` and
never lowers the deadline; (3) NULL-TTL preservation — a durable hold (`expires_at IS NULL`) stays NULL
and is still `Applied`. Plus one source guard that extends task 015's guard file: the new method must
open no ambient scope, use one connection, and never be called inside a `TransactionScope`.

**Explicit refusal condition.** STOP and return to the Architect instead of implementing if: extending
`expires_at` requires a new column, a new status, a migration, or a new typed error code beyond the
pinned set; if any of the three tests cannot be written without a live Host or a running database; if
the caller must be a module other than Inventory (cross-module write); or if Host/Host.Tests would have
to change. In each case the correct next artefact is a durable **claim/lease** design (an explicitly
owned state), not a widened transaction.

## Next owner work

Architect decision needed on (i) U1 — whether the platform may rely on ambient scopes at all, and
(ii) whether the money path is protected by a time window (this proposal) or by a durable claim with
its own state and migration.

automaticNextImplementationTask=NONE.
