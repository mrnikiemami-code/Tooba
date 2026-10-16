# TB-SHIPPING-GROUP-ID-CONTINUITY-153 — persisted draft ShippingGroupId continuity

Parent: `TB-SHIPPING-INDEPENDENT-GROUP-ACCEPTANCE-152`. Program: `TOOBA-SHIPPING-INDEPENDENT-SELLER-COMMITMENTS`.
Mode: `BOUNDED_FIX_PERSISTED_SHIPPING_GROUP_ID_CONTINUITY`. Base SHA: `9c581914c79fd1d55b60cf9d7361c7584ec48bdb`.
Verdict: **PASS** for this task's deliverable (exact-id continuity + join reconciliation), with one unrelated
scratch-fixture FAIL disclosed below (payment draft tax fixture), Stop-State `STOP_FOR_ARCHITECT`.

## Root cause (real code, not a design note)

`StorefrontShippingService.SaveSelectionAsync` replaced the whole group set on **every** accepted save:

```csharp
existing.ClearShippingGroups(now);
await _drafts.SaveAsync(existing, isNew: false, cancellationToken);
existing.AddShippingGroups(groups, now);   // groups came from Plan(..., UuidV7.New)
```

`CartShippingGroupPlanner.Plan` unconditionally called the `newGroupId` factory, so `UuidV7.New` re-issued a
fresh id for **both** groups on every accepted PUT — including a byte-identical retry and an edit that touched
only one group. TB-152 recorded this as `OPEN / MUST FIX` (gap D).

## Fix (narrow, behavior-preserving)

1. `src/backend/Modules/Order/Tooba.Order.Domain/Checkout/CartShippingGroupPlanner.cs`
   - `Plan(...)` takes an optional `IReadOnlyList<CartShippingDraftGroup>? reusableGroups`.
   - New pure rule `IsSameLogicalObligation(existing, cartId, seller, method, cartLineIds)`: same cart + same
     seller + same canonical `MethodCode` + exactly the same `ServiceCode`/`OptionCode` + empty origin + the
     **same authoritative line-membership set**.
   - `FindReusableGroupId` returns the first matching persisted id and marks it used, so one id is never
     assigned to two groups; otherwise the caller's `newGroupId()` is used.
   - Selected date/window are deliberately **not** part of identity: a pure schedule edit or an idempotent retry
     keeps the id, while a membership / seller / method / service / option change allocates a NEW id.
2. `src/backend/Modules/Order/Tooba.Order.Application/Storefront/Services/StorefrontShippingService.cs`
   - The trusted draft for the same cart is now read once **before** planning (same single read as before, just
     hoisted) and passed as `reusableGroups`; the later duplicate read was removed.
   - Nothing else changed: the existing two-phase delete/insert replacement, per-group validation, fail-closed
     date/window rules, legacy single-group scalar shorthand, price snapshots, seller eligibility, the unique
     `(cart, seller, method)` index, and Case D (same seller + same method + incompatible origin BLOCKED) are
     untouched. No schema change, no migration, no client-supplied id can become authoritative (the reuse set is
     derived only from the persisted draft).

## Focused tests — `ShippingGroupIdentityContinuityTests` (11 tests, all green)

Owner-correct, no Host and no shared DB; the service is driven through the real
`StorefrontShippingService.SaveSelectionAsync` with fake ports and an in-memory draft store that mirrors the
production two-phase replace semantics (`src/backend/Modules/Order/Tooba.Order.Tests/Storefront/`):

1. repeat plan for the same obligation reuses the persisted ids; two groups never share an id;
2. schedule-only change keeps both ids and moves only A;
3. changed membership allocates a new id (no data reuse);
4. another method for B never borrows A's id (and B's own id moves only because B's obligation changed);
5. a group belonging to another cart is never reused;
6. legacy plan without reuse candidates is unchanged;
7. first save creates two distinct ids and an identical save returns the exact same ids (zero submits);
8. editing only A keeps both ids and leaves B's whole promise byte-identical;
9. a genuine method switch re-ids only that group, with one-to-one line membership preserved;
10. fail-closed invalid date (`shipping.delivery.too_early`) / invalid window
    (`shipping.delivery.slot_unavailable`) never writes and never changes persisted ids/schedules;
11. legacy single-group shorthand still works and keeps its identity across a window change.

Full Order module suite: `Passed! Failed: 0, Passed: 365`.

## Persistence-backed proof (real EF + real Postgres + real HTTP)

Re-used the TB-152 recipe as task-153-owned copies (TB-152 evidence untouched): private Host on `:5414`,
disposable `tooba-tb153-pg` on `127.0.0.1:55472` with `tooba_153_scratch` / `tooba_153_messaging`, seller
preparation A=2 / B=3, `Edition=SingleStore` (no swap), one Active tenant, no `tooba_alpha`,
`tenant-alpha`, `Port=5432` or prior scratch port (`isolation-guard.json` = fail-closed PASS).

`journey-153.js` (28 checks: **27 PASS / 1 FAIL / 0 BLOCKED**) proves on a real two-seller, same-method cart:

- two independent promises: minimum `2026-10-21` (A) vs `2026-10-22` (B), one line each;
- **both `ShippingGroupId`s identical** across first save, edit-of-A save, identical repeat save and re-reads:
  `01a14632-9bff-7000-9ae5-32bfa328de90` (A) / `01a14632-9c00-7000-8839-7f0957fc95b4` (B);
- B's promise (id/fee/minimum/date/window/lines) byte-identical across the edit of A;
- rejected writes (past date, unknown window) never overwrite the accepted selection;
- `db-readback-153.txt`: same two ids in `order.cart_shipping_groups`, one-to-one join rows, and
  `duplicate_group_ids = 0`, `lines_in_two_groups = 0`, `orphan_join_rows = 0`;
- zero premature mutation: `checkouts = 0`, `seller_orders = 0`, `order_lines = 0`,
  `order_shipping_groups = 0`, `payments = 0`, `payment_attempts = 0`.

### Disclosed unrelated FAIL (not this task's deliverable)

`read-only payment-draft endpoint answers HTTP 200 (not 409)` → `409 checkout.tax.unavailable`. The fresh
scratch bootstrap mints new offer/category ids per seed; `seed-tax-153.sql` inserted the minimum active
percentage rule for the two current offer categories, yet the scratch tax resolution still refused. This is the
same scratch-fixture class of problem TB-152 closed with its own seed and the 152 evidence already proves the
read-only draft seam returns 200 with correct per-group fees. It is **not** masked or renamed: recorded as FAIL
in `assertion-matrix-153.txt`. No Order/Tax code was changed to work around it.

## Environment safety

Only task-owned disposable resources: private Host `:5414`, private scratch Postgres `127.0.0.1:55472`,
container `tooba-tb153-pg`. No OTP/payment/PSP/SMS/settlement/carrier API, no real token committed, no
`tooba_alpha`, no `:3000`/`:5088`/`:5432`, no schema change, no destructive migration, no rebase/stash/reset/
force-push, no unrelated file touched. Shared `:3000`, `:5088`, `:5432` and RabbitMQ remained healthy
throughout; private Host, container and private listeners were torn down afterwards.

## Evidence index

| File | Content |
| --- | --- |
| `isolation-guard.json` | fail-closed isolation guard for the private target |
| `host-launch-153.txt` | private Host launch (PID, health 200, watchdog) |
| `journey-153.js`, `journey-153-trace.txt`, `assertion-matrix-153.txt` | authenticated HTTP acceptance (28 checks) |
| `prepare-isolated-target-153.js`, `launch-isolated-host-153.ps1`, `derive-153-scripts.ps1` | task-owned copies of the proven 152 recipe + derivation |
| `identity-matrix-153.txt` | sanitized before/after id matrix and comparison rules |
| `selection-save-153.json`, `selection-edit-a-153.json`, `selection-repeat-save-153.json` | save / edit-A / repeat-save identity |
| `projection-before-save-153.json`, `projection-after-save-153.json` | independent per-seller horizons |
| `db-readback-153.txt`, `readback-153.sql` | persisted ids, joins, duplicate/orphan probes, zero premature rows |
| `payment-draft-153.json` | read-only payment-draft probe (disclosed 409) |
| `cart-after-flow-153.json` | cart integrity after the whole flow |
| `seed-tax-153.sql` | minimum scratch-only tax fixture attempt |

## Reproduction

```powershell
docker run -d --name tooba-tb153-pg -e POSTGRES_USER=scratch153 -e POSTGRES_PASSWORD=<env-only> `
  -e POSTGRES_DB=tooba_153_scratch -p 127.0.0.1:55472:5432 postgres:16-alpine
docker exec -e PGPASSWORD=$env:TOOBA_153_SCRATCH_PASSWORD tooba-tb153-pg psql -U scratch153 -d tooba_153_scratch -c "CREATE DATABASE tooba_153_messaging OWNER scratch153;"
dotnet publish src\backend\Host\Tooba.Host\Tooba.Host.csproj -c Debug -o .tmp-153-host-out
$env:TOOBA_153_SCRATCH_PASSWORD='<env-only>'; node docs\architecture\evidence\TB-SHIPPING-GROUP-ID-CONTINUITY-153\prepare-isolated-target-153.js
powershell -NoProfile -File docs\architecture\evidence\TB-SHIPPING-GROUP-ID-CONTINUITY-153\launch-isolated-host-153.ps1 -Port 5414
$env:TOOBA_153_HOST='http://127.0.0.1:5414'
# seller/offer ids are read back from the disposable scratch DB (fresh per seed) and injected:
$env:TOOBA_153_SELLER_A='<seller A>'; $env:TOOBA_153_SELLER_B='<seller B>'
$env:TOOBA_153_OFFER_A='<offer A>'; $env:TOOBA_153_OFFER_B='<offer B>'
node docs\architecture\evidence\TB-SHIPPING-GROUP-ID-CONTINUITY-153\journey-153.js
```
