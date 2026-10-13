# TB-P0-CHECKOUT-START-PAYMENT-COMPOSER-019 — bounded implementation evidence

Task: Order-owned, non-HTTP composition seam for the final "pay" click — canonical commit/replay →
018 hold preparation → one Payment.Contracts initiation call. Composer deliberately **inert**
(no HTTP endpoint, no Host wiring), so the orchestration is verifiable in module tests first.

Verdict: **WORKER_COMPLETE — IMPLEMENTED + FOCUSED TESTS PASSED — STOP FOR ARCHITECT ACCEPTANCE**
(not `BLOCKED_ARCHITECT_DECISION`: no forbidden edit and no unapproved new contract was required).

## 1. What was built

One new production service + one DI line (2 production files touched, within the `<=3` budget):

| File | Change |
| --- | --- |
| `src/backend/Modules/Order/Tooba.Order.Application/Storefront/Services/StorefrontStartPaymentComposer.cs` | NEW (401 lines): request/result contracts + `StorefrontStartPaymentComposer` |
| `src/backend/Modules/Order/Tooba.Order.Infrastructure/OrderModule.cs` | `+1` line at `:119` — `services.AddScoped<Application.Storefront.Services.StorefrontStartPaymentComposer>();` |

Test files (module test project only):

| File | Change |
| --- | --- |
| `src/backend/Modules/Order/Tooba.Order.Tests/Storefront/StorefrontStartPaymentComposerTests.cs` | NEW (747 lines, 22 tests) |
| `src/backend/Modules/Order/Tooba.Order.Tests/Architecture/StorefrontStartPaymentComposerGuardTests.cs` | NEW (9 source guards) |

No Host file, no `Program.cs`, no endpoint, no frontend, no schema/migration/settings, no
Inventory/015/017/018 production file, and no unrelated file was touched.

### 1.1 Fixed five-step sequence (source evidence)

```
 48: IdempotencyKeyTooLong = 3,                       (typed request fault, no invented error code)
155: public const string CommitKeyPrefix = "start-payment:commit:";
158: public const string PaymentKeyPrefix = "start-payment:payment:";
164: private const int SharedIdempotencyKeyMaxLength = 128;
198: public async Task<StorefrontStartPaymentResult> StartPaymentAsync(
218:     return Reject(StorefrontStartPaymentFault.IdempotencyKeyTooLong);
232:     : await _processes.FindSucceededBySubmissionKeyAsync(commitKey, cancellationToken)
237:     .GetOwnedForPaymentResultAsync(priorCheckoutId, request.GuestSecret, cancellationToken)
252:     page = await _shipping.CommitAsync(                     ← canonical Order commit path
264:     catch (ContractOperationException exception)            ← producer code passed through untouched
283: var preparation = await _preparation.PrepareAsync(          ← 018 seam
291: if (!preparation.Prepared)                                 ← only Prepared may continue
317: var initiated = await _payments.InitiateForCheckoutAsync(  ← 012R seam, once
```

1. syntactic validation (cart identity + caller key + explicit namespace/length fit);
2. durable replay detection: `ICheckoutProcessTracker.FindSucceededBySubmissionKeyAsync(commitKey)` —
   a succeeded `PaymentPending` process points at the already-persisted checkout, and only the
   **existing** ownership rule (`StorefrontCheckoutService.GetOwnedForPaymentResultAsync`) is
   re-checked. **No second commit, no cart touch, no quota, no reservation.**
3. otherwise `StorefrontShippingService.CommitAsync` → `StorefrontCheckoutService.SubmitAsync` →
   `ICheckoutDirectory.SubmitAsync` (canonical `CheckoutProcessManager`) exactly once. The composer
   re-implements none of it: no `TransactionScope`, no `ReserveForCheckout`, no
   `ConvertForCheckout`, no quota accounting, no cycle state (guard-asserted, §3).
4. `IOrderPaymentHoldPreparationPort.PrepareAsync` (018) with server-derived reservations/policy;
   only `Prepared` continues.
5. `IPaymentInitiationPort.InitiateForCheckoutAsync` (012R) exactly once — wallet / gateway / manual
   all take this single canonical path (no side channel).

### 1.2 Idempotency-key ownership and namespace/length compatibility

The caller supplies one stable key; the composer derives two deterministic, namespace-separated keys:

* commit: `start-payment:commit:<key>` → `order.checkouts.idempotency_key` (128) and
  `order.checkout_processes.submission_idempotency_key` (128);
* Payment: `start-payment:payment:<key>` → `payment.payments.idempotency_key` (128).

Length compatibility is **explicitly checked** (21 + len ≤ 128 and 22 + len ≤ 128), so a key longer
than 106 characters is rejected *before* any order/money touch instead of being silently truncated by
a column. The 106-char boundary is asserted in both directions in the test suite
(127/128 chars accepted, 107 rejected). Dedupe of a repeated click is therefore: (a) checkout replay
by the stable commit key (canonical `FindExistingCheckoutAsync` key-or-cartId + the durable process
pointer), and (b) Payment's own idempotency replay on the stable payment key.

### 1.3 Typed failure policy (no invented codes, no generic 500)

* Pre-flight faults → `StorefrontStartPaymentOutcome.InvalidRequest` + `StorefrontStartPaymentFault`
  enum. **No** new error code string was invented, so the Order error catalog, its `.resx`
  resources, and the guarded Host/Host.Tests error-resource parity were not touched at all.
* commit/replay failure → `CheckoutCommitFailed` carrying the producer's own untouched typed code
  (`StorefrontOrderException.Code`, `ContractOperationException.Code`, `SemanticException.Error.Code`).
  A commit that yields no persisted checkout id is reported as `checkout.rejected` (existing Order code).
* 018 outcome ≠ `Prepared` → `HoldPreparationFailed` + the 018 `OrderPaymentHoldPreparationOutcome`
  plus its typed detail (reservation count, deadlines, unavailable ids). All eight non-`Prepared`
  outcomes are covered by tests.
* Payment faults are **not caught and not rewritten**: they propagate as Payment-owned typed faults,
  which is exactly why no `payment.*` code can be forged here (guard-asserted: the composer contains
  no `"payment.` / `"checkout.` / `"shipping.` string literal).

## 2. Verification (one focused project run, no broad suites)

```
dotnet test src/backend/Modules/Order/Tooba.Order.Tests/Tooba.Order.Tests.csproj
Passed! - Failed: 0, Passed: 214, Skipped: 0, Total: 214 — Tooba.Order.Tests.dll (net8.0)
```

New-file-only run: `Passed! - Failed: 0, Passed: 22, Total: 22` (`~StorefrontStartPaymentComposerTests`).

Behavioural coverage (`StorefrontStartPaymentComposerTests`, 22 cases):

| Gate | Test |
| --- | --- |
| successful one-shot composition | `Final_click_commits_once_prepares_the_hold_and_initiates_the_chosen_method_once` — 1 `SubmitAsync`, 1 `PrepareAsync`, 1 `InitiateForCheckoutAsync`, all on the same persisted checkout id, backend shipping price 150,000 (the client-saved 45,000 is overwritten authoritatively) |
| lost-response same-key replay | `Lost_response_with_the_same_key_replays_the_checkout_without_a_second_commit` — 2 clicks → `SubmitCalls == 0`, same checkout, identical payment key both times (`CheckoutReplayed == true`) |
| same chosen method flows through, second click does not duplicate payment | `Selected_method_is_forwarded_to_preparation_unchanged_and_wallet_is_mapped_to_its_stable_code` (wallet → `wallet`; `manual`/`gateway` passed verbatim, `UseWallet` unchanged) |
| commit failure leaves payment uncalled | `Commit_failure_keeps_the_cart_and_runs_no_money_path` (theory: `checkout.cart.expired`, `checkout.version.conflict`, `shipping.cart.stale`) + `Commit_that_returns_no_persisted_checkout_is_rejected_typed_without_money_movement` |
| post-commit preparation failure keeps the existing checkout | `Preparation_failure_keeps_the_existing_checkout_and_never_calls_payment` (theory: all 8 outcomes; 1 commit, 1 preparation, 0 payment, checkout id retained) |
| actor/guest mismatch fail-closed | `Guest_mismatch_on_a_replay_fails_closed_before_any_commit_or_payment`, `Preparation_access_denied_for_a_foreign_actor_never_starts_payment`, `Authenticated_caller_identity_is_forwarded_to_preparation_and_payment` |
| Payment code not forged, checkout retained on payment fault | `Payment_faults_propagate_untouched_so_no_payment_code_is_forged_and_the_checkout_stays` |
| request hygiene / key length | `Incomplete_request_is_rejected_before_any_order_or_money_touch`, `Key_length_compatibility_is_checked_explicitly_against_the_shared_column_limit`, `Composer_without_a_process_tracker_falls_back_to_the_canonical_commit_replay_only` |

Source guards (`StorefrontStartPaymentComposerGuardTests`, 9 cases): no `MapPost`/`MapGet`/
`IEndpointRouteBuilder`/`ISender`/`MediatR` in the composer; `start-payment` and
`StorefrontStartPaymentComposer` absent from `Tooba.Order.Endpoints` and the whole
`src/backend/Host/Tooba.Host` tree (**zero Host edits, zero endpoint publishing**); canonical seams
reused with no `TransactionScope`/`ReserveForCheckout`/`ConvertForCheckout`/quota/`SaveChanges`;
no invented error-code literals; request contract carries no `ReservationId`/`CheckoutId`/`Amount`/
`ExpiresAt`/`Deadline`; **no wall clock and no fixed hold duration** (`AddHours(`/`AddMinutes(`/
`AddSeconds(`/`TimeSpan.From` absent — deadlines come from 018 only); Contracts-only foreign
references; DI registration present; commit/payment namespaces stable.

The test wiring only fakes the real path's ports; the commit step runs the **real**
`StorefrontShippingService` + `StorefrontCheckoutService` over a faked `ICheckoutDirectory`, so the
canonical draft re-validation, version guard and command construction are exercised, not simulated.

## 3. Explicit limitations (reported, not hidden)

* **No ACID between extension and provider initiation.** 018's extension and Payment's initiation
  remain two separate steps; the composer only orders them. The window between them (and late
  PSP callbacks) is not closed and is not claimed closed. Real DB transaction enlistment is **not**
  proven here — the canonical commit's PostgreSQL/2PC evidence stays the separate 017 evidence.
* **Direct `/payment` initiation is still a separate guard concern.** This task publishes no route,
  so the pre-existing direct Payment endpoint (and 013's payability gate) is unchanged and still
  needs its own follow-up.
* **The default-gateway selector resolves its concrete provider inside Payment.** When the caller
  passes the catalog selector `gateway`, `StorefrontPaymentOrchestrator` picks the concrete provider
  code internally, so the hold policy resolved for the extension is the store/platform layer, not
  that provider's specific override. An explicitly chosen provider code (or `wallet`) is exact.
* **A request without a chosen method is not "guessed".** If neither `UseWallet` nor a provider code
  is supplied, preparation fails closed with `PolicyUnavailable` (no invented TTL, no default method
  invented in Order). Real method availability stays Payment's decision.
* **No HTTP-level verification.** The feature is intentionally non-HTTP: nothing to call. Endpoint
  binding is a later, separately authorized step (and would need the Architect's decision on how the
  eight preparation outcomes map to API error codes, which this task deliberately did not invent).
* Frontend was not touched; Shipping still commits and `/payment` still requires a checkout id (F4).
* The running Development Host was not built, stopped or restarted; Host output locks were not
  disturbed, and Host.Tests were not run.

## 4. Hygiene

* Edits confined to `Tooba.Order.Application` (1 new file), `Tooba.Order.Infrastructure` (1 line),
  the Order test project (2 new files), the received `.task.md` artifact and this evidence folder.
* Pre-existing unrelated working-tree entries (CRLF/index-stat artifacts in
  `CheckoutAbuseGate.cs`, `admin-order-detail-screen.tsx`, `storefront-landing-blocks.tsx`,
  `summary.md`, and untracked `bin/obj`) were **not** staged, committed, reverted or stashed.
* No historical task was replayed, no other worker was started, no DB/SQL/HTTP/payment/order
  mutation was performed.
