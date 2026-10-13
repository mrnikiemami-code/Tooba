# TB-P0-CHECKOUT-PAYABILITY-GATE-013 — bounded implementation evidence

Parent: TB-P0-CHECKOUT-START-PAYMENT-S2-012R (S2 approved). Expectation: close the missing
reservation-cycle payability guard at the Payment money-entry boundary, without publishing
`POST /v1/storefront/cart/{cartId}/start-payment`.

Verdict: **BLOCKED_ARCHITECT_DECISION** — no production file was changed. The guard itself is
mechanically small (3 files), but the mandatory "typed, actionable error" cannot be produced inside
the authorized change set: the only certified places where a Payment-owned stable code may be added
are two **Host.Tests** guards that this task explicitly forbids editing.

## 1. Verified call graph — one choke point, no alternative money-entry path

```
StorefrontPaymentOrchestrator.InitiateAsync                      (Payment.Application/Storefront/Orchestration/StorefrontPaymentOrchestrator.cs:110-241)
  :120  _actorPolicy.EnsureCheckoutActorAsync(...)               (actor gate)
  :121  _checkouts.GetForMutationAsync(checkoutId, cartId, guestSecret, authUserId)  (ownership + payable amount)
  :125-158  method selection -> wallet | manual | gateway providerCode
  :159  _payments.InitiateAsync(new InitiatePaymentCommand(...))  <-- SINGLE money-entry call
          -> PaymentDirectory.InitiateAsync                     (Payment.Infrastructure/Directories/PaymentDirectory.cs:60-210)
             :61-135   idempotency-key replay branch (returns the EXISTING payment/attempt)
             :139      HasSucceededPaymentForCheckoutAsync check
             :143-149  _orders.GetPayableAsync(...) -> mode/currency checks
             :167      _gateways.Resolve(providerCode)          (wallet | manual | fail-closed | webhook | fake)
             :192      gateway.InitiateAsync(...)               (provider request for online/manual)
             :193-201  RecordInitiation + Assign(timeout) + SaveChanges
  :168-198  wallet: _payments.VerifyAsync(...) -> WalletPaymentGateway.VerifyAsync -> IWalletOrderPaymentPort.SpendForOrderPaymentAsync  (ACTUAL wallet debit)
```

- `IPaymentDirectory.InitiateAsync` has exactly one production caller (`StorefrontPaymentOrchestrator.cs:159`);
  every method (online, wallet, manual) reaches `PaymentDirectory.InitiateAsync` before any provider
  request or debit. Wallet's debit happens later, inside the immediate `VerifyAsync` at
  `StorefrontPaymentOrchestrator.cs:170`, which is only reachable after a successful initiate.
  => a gate placed in `PaymentDirectory.InitiateAsync` covers all three methods in one place.
- `WalletPaymentGateway.InitiateAsync` performs no debit; the debit is in `VerifyAsync`
  (`WalletPaymentGateway.cs:40` vs `:70-76`).

## 2. Constraint that rules out the "obvious" placement

`IPayableCheckoutReader.GetPayableAsync` (Order-implemented, `OrderPaymentBridge.cs:50-78`) is **not**
initiation-only. It is also the ownership/read seam:

- `PaymentDirectory.GetLatestForCheckoutAsync` — `PaymentDirectory.cs:288`
- `PaymentActorAccess.EnsureActorCanSeeAsync` — `Directories/Shared/PaymentActorAccess.cs:12-25`
  (used to authorize payment reads, and therefore the webhook/verify path)

Throwing a payability error inside `GetPayableAsync` would reject **verification of a payment that
actually took money** whenever the cycle had expired — a financial correctness regression. Therefore
payability must be *reported as data* and enforced only at the initiation call site.

## 3. The blocker: a new typed code cannot be declared within the authorized change set

The required failure must be typed and actionable. `PaymentOperation.ExecuteAsync`
(`Payment.Application/Composition/PaymentOperation.cs:19-51`) maps a `ContractOperationException` to a
`Result` **only when `PaymentErrorCodes.IsKnown(ex.Code)`**; an undeclared code propagates to the global
boundary instead. The module's documented ownership split (`PaymentErrorCodes.cs:1-21`) says a consumed
cross-module code must still be declared in `PaymentErrorCodes` ("declared here so the module's
declared-code guard recognises it").

Both certification guards that pin the Payment code surface live in **Host.Tests**, and this task
forbids Host edits ("Only Order/Payment module and their Contracts/tests may change. NO Host/..."):

| Guard | Pinned thing | Location |
| --- | --- | --- |
| declared Payment code count | `Assert.Equal(28, declared.Length)` — currently exactly 28 members | `src/backend/Host/Tooba.Host.Tests/Architecture/PaymentModuleAmsc001W1MigrateGuardTests.cs:238` |
| bilingual key list | hard-coded list of every localized key in **both** `PaymentErrors.resx` and `PaymentErrors.fa.resx` | same file, `:89-127` |

Consequences (each verified against the listed lines):

- New **Payment-owned** code → needs `PaymentErrorCodes` member (28→29 → guard line 238 fails) plus a key in
  both resx (guard lines 89-127 fails) plus a `PaymentErrorCatalogContributor` descriptor.
- New **consumed Order/Inventory-owned** code → `PaymentErrorCodes` must still declare it (28→29), and the
  resx coverage guard only exempts keys whose owner localizes them, so a new `order.*`/`inventory.*` key
  would also need entries in Payment's resx pair. Same two Host guard failures.
- Reusing an already-declared code avoids the guards but is semantic repurposing: `payment.unpaid.supply_unavailable`
  is registered as "unpaid retry cannot be supplied", and `inventory.reservation.retry_limit_reached` is
  exact **only** for the retry-limit sub-case, not for a plain expired cycle. `PaymentErrorCodes.cs:5-8`
  forbids renaming/repurposing declared codes.
- `inventory.reservation.not_active` is registered by Inventory and localized by Inventory
  (`InventoryErrorCatalogContributor.cs:52`), but Payment cannot reference `InventoryErrorCodes` without
  adding a **new cross-module project reference** (Payment.Infrastructure references only
  Application/Catalog.Contracts/Wallet.Contracts) — a dependency-direction change outside this task, and
  a raw literal would contradict the declared-code doctrine above.

So a *correct, fully guarded* change is **7 production files** (see §4) and, in addition, requires
authorization to edit the two Host.Tests guards. This exceeds the task's "Max 3 production files
preferred / STOP and submit exact minimal proposed file set" limit, and the Host edits are explicitly
out of scope. No partial path was implemented, per instruction.

## 4. Exact minimal proposed file set (for Architect authorization)

Payability *decision* stays in Order (existing canonical policy, no rule duplication); Payment only
enforces; the error surface is the only extra work.

1. `Modules/Payment/Tooba.Payment.Contracts/Checkout/OrderPaymentProjectionContracts.cs`
   extend `PayableCheckoutSnapshot` with trailing optional members, e.g.
   `bool CanInitiatePayment = true, string? PayabilityBlocker = null` (additive, all existing positional
   constructions keep compiling).
2. `Modules/Order/Tooba.Order.Infrastructure/Integrations/Payment/OrderPaymentBridge.cs`
   compute the flag from the existing Order-owned policy — `IReservationCycleDirectory.GetProjectionAsync`
   (already injected as optional `_cycles`) + `ReservationCyclePayabilityPolicy.IsActive/HasReachedRetryLimit`
   — using the injected `IClock` (ctor gains `IClock`, Order DI registration only).
3. `Modules/Payment/Tooba.Payment.Infrastructure/Directories/PaymentDirectory.cs`
   between the mode check (`:148`) and `_gateways.Resolve` (`:167`):
   `if (!payable.CanInitiatePayment) throw new ContractOperationException(payable.PayabilityBlocker ?? <new code>);`
   — after the replay branch, so an existing initiation replays unchanged.
4. `Modules/Payment/Tooba.Payment.Contracts/Errors/PaymentErrorCodes.cs` — declare
   `payment.reservation.not_payable` (+ `KnownCodes`).
5. `Modules/Payment/Tooba.Payment.Endpoints/Errors/PaymentErrorCatalogContributor.cs` — descriptor
   (Conflict / 409).
6. `Modules/Payment/Tooba.Payment.Contracts/Resources/PaymentErrors.resx` — en text.
7. `Modules/Payment/Tooba.Payment.Contracts/Resources/PaymentErrors.fa.resx` — fa text.

Plus, **required Host.Tests edits not covered by this task's scope** (files 8-9):
8. `PaymentModuleAmsc001W1MigrateGuardTests.cs:238` — count 28 → 29.
9. same file `:89-127` — add the new key to the bilingual pinned list.

Tests to add with that change (one focused run, Payment.Tests): active cycle accepted; expired cycle
rejected before any provider request/attempt row (assert zero `payment.payments`/`payment_attempts`
writes); wallet and manual paths rejected identically; idempotent replay of an existing initiation still
returns the persisted payment; ownership failure still wins over payability.

## 5. Residual race — explicitly NOT an invariant proof

Even with the flag design, the sequence is *read payability → insert payment/attempt → call provider*.
The cycle can expire (alpha policy: `initial_reservation_hold_minutes = 1`) between the read and the
insert. That reduces, and does not eliminate, the window. It is **not** atomic and must not be described
as race-free; no single-transaction/lock/CAS proof exists.

Atomic coordination options (needed before the final-click endpoint can be considered safe):

- **CAS in Order, verified by Payment (preferred):** extend the existing
  `IReservationCycleDirectory.CorrelatePaymentAttemptAsync` into a conditional transition
  (`WHERE status = Active AND expires_at > now` → returns false instead of no-oping), and have Payment
  call it inside an ambient `TransactionScope` opened around its own `SaveChangesAsync`, so the cycle
  transition and the payment insert commit together (single physical database, same as the existing
  checkout commit scope). Requires an Order Contracts change and a Payment transaction boundary —
  a larger, separately authorized design.
- **Order-owned composite call:** an Order operation that performs commit + payability + correlation in
  the existing commit transaction and returns a token Payment must validate at initiation. Still needs
  the same typed error and the same Order Contracts change.

Until one of these is authorized, the delivered `IPaymentInitiationPort` (012R) has no production
caller and no endpoint exists, so the missing gate is not reachable.

## 6. Verification performed in this task

- Source inspection only (call graph, guards, error surfaces, localization, DI). Listed above with file:line.
- Production files changed: **0**. Schema/migrations: **0**. DB writes: **0**. Host/restart: **none**.
  Frontend/Cart/Inventory source: **untouched**.
- Tests executed: **none** — there is no code change to test, and per instruction a claim of passing
  tests without a change would be unsupported. The locked Host.Tests guards in §3 were read, not run
  (the running Development Host still locks Host.Tests output DLLs; not stopped, not retried).
- Configurable durations were not modified; no artificial floor between reservation and payment
  timeouts was added.
