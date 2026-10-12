# TB-CHECKOUT-OPEN-UNPAID-DISTINCT-001 — Bounded repair evidence

Mode: `IMPLEMENT_BOUNDED_REPAIR` · Owner: Order · Target: `main` @ `e8abc83787b7f16bed9e3c89d741346b4374f84e`

## Verified root cause

`CheckoutAbuseGate.CountOpenUnpaidAsync` projected the join of unpaid `SellerOrders` to
`Checkouts` **without `Distinct()`**, then used the same duplicate-bearing list for the
final count:

```csharp
.Select(checkout => checkout.CheckoutId)
.ToListAsync(...);                 // one entry per unpaid seller order
...
var distinctCheckoutIds = openUnpaidCheckoutIds.Distinct().ToArray();  // projections only
...
return openUnpaidCheckoutIds.Count(id => !unpayable.Contains(id));     // NON-distinct count
```

`Distinct()` was applied only to the reservation-cycle projection lookup. The customer-facing
count therefore multiplied by the number of unpaid seller orders per checkout: a single
multi-seller checkout consumed N slots of `MaxOpenUnpaidOrdersPerCustomer`, blocking a
legitimate customer with one open multi-seller checkout.

## Repair (single file, minimal)

`src/backend/Modules/Order/Tooba.Order.Infrastructure/Checkout/Abuse/CheckoutAbuseGate.cs`

- push `Distinct()` into the projection (`.Select(...).Distinct().ToListAsync(...)`);
- drop the now-redundant `distinctCheckoutIds` local and use the de-duplicated list for
  projections, payability filter and final count.

4 insertions / 3 deletions. Behaviour preserved: `ReservationCyclePayabilityPolicy.HasReachedRetryLimit`
remains the single source of unpayability, zero-cycle/null-projection rows still count,
tenant/customer isolation (`PlacedByUserId == customerId`), cap values and error codes unchanged.
No duplicated payability policy, no new dependency edge, no schema/query-contract/frontend change.

## Behavioral regression (real seam, not source-text)

`src/backend/Host/Tooba.Host.Tests/CheckoutAbuseOpenUnpaidDistinctTests.cs` drives the real
`CheckoutAbuseGate` over a real EF `OrderDbContext` and the real `ReservationCycleDirectory`.
Assertions go through the public `EnsureCanStartInitialReservationAsync` seam and read the
actual `CheckoutAbuseLimitException.CurrentCount`.

| Scenario | Expectation | Pre-fix |
| --- | --- | --- |
| One checkout with 3 unpaid seller orders, cap 2 | not blocked; with cap 1 reports `CurrentCount == 1` | blocked (count 3) |
| Two distinct checkouts, cap 3 then cap 2 | allowed, then `CurrentCount == 2` | same |
| Retry-exhausted checkout (3/3 cycles closed) + active retryable checkout | only the retryable one counts; cap 1 reports `CurrentCount == 1` | dead one not excluded as a slot |
| Cancelled (non-open) seller order, cap 1 | not blocked | same |

## Commands and results

```text
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj -c Release \
  --filter "FullyQualifiedName~CheckoutAbuseOpenUnpaidDistinctTests"
  → Passed! Failed: 0, Passed: 4, Total: 4

# discriminating proof: revert ONLY CheckoutAbuseGate.cs to HEAD
  → Failed! Failed: 1, Passed: 3 — CheckoutAbuseOpenUnpaidDistinctTests.Multi_seller_checkout_consumes_exactly_one_open_unpaid_slot [FAIL]
# restore fix
  → Passed! Failed: 0, Passed: 4, Total: 4

dotnet test src/backend/Modules/Order/Tooba.Order.Tests/Tooba.Order.Tests.csproj -c Release
  → Passed! Failed: 0, Passed: 152, Total: 152   (module suite incl. architecture guards)

dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj -c Release \
  --filter "CheckoutAbuse|CheckoutProcess|ReservationCycleFoundation|ReservationLifecycleIntegration|OrderSupplyUx|AdminReservationCycleAudit|StorefrontPendingPayment|HostAdminAmcCheckoutAbuse"
  → Failed: 1, Passed: 62, Total: 63
```

## Baseline distinction (pre-existing, NOT introduced here)

`Tooba.Host.Tests.Architecture.HostAdminAmcCheckoutAbuseGuardTests.Host_Admin_count_15_StoreAppearance_evacuated`
asserts exactly 15 `.cs` files under `src/backend/Host/Tooba.Host/Admin`; the committed tree
already contains **17** (`git ls-files … | *.cs` → 17, and `git status` reports 0 files touched
in that directory). This guard therefore fails on the untouched `HEAD` tree as well and is
reported, not fixed — it is outside this task's scope and no file in that directory was modified.
No other failure.

## Environment note

The Debug build could not be produced because the running `Tooba.Host` (PID 15068, live dev
server) locks `src/backend/Host/Tooba.Host/bin/Debug/net8.0`. All gates above were run with
`-c Release`, which leaves the live process untouched. No process was killed and no Debug
artifact was overwritten.

## Remaining risk / unproven

- The in-memory provider executes the join in-process; it reproduces the duplicate-row shape
  (proven by the pre-fix failure) but does not prove the PostgreSQL `SELECT DISTINCT` plan.
  A relational guarantee would need an integration run against a real provider, which is
  outside this bounded repair.
- `MaxOpenUnpaidOrdersPerCustomer` semantics across *different* customers in one checkout were
  not exercised (single-owner checkouts are the current contract).

## Scope discipline

No AMSC wave, no baseline reset, no schema/migration, no frontend, no new dependency edge, and
no weakened guard. `Worker PASS != Architect ACCEPT`.
