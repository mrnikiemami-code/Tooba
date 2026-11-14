# TB-266 — payment-gated supply, readable queue, resolvable locator, clean trial set

Task: `TB-SHIPPING-PAYMENT-GATED-DISPATCH-READABLE-QUEUE-AND-CLEAN-TRIAL-DATA-266`
Parent: `TB-SHIPPING-COURIER-ORIGIN-RETURNS-LINKAGE-AND-OBLIGATION-CLOSURE-265-R2`
Channel: `tooba-main` · Worker: `tooba-worker-01` · Mode: `REPAIR_USER_REJECTED_QUEUE_AND_PAYMENT_AUTHORITY_WITH_AUTHORIZED_BACKED_UP_ORDER_CLEANUP_AND_SMALL_COHERENT_TRIAL_SET`

Status: **PASS** within the issued repair scope.
Decision: `AUTHORITATIVE_PAYMENT_GATED_PHYSICAL_SUPPLY_READABLE_64PX_QUEUE_RESOLVING_FULFILLMENT_LOCATOR_AND_BACKED_UP_OWNER_AWARE_ORDER_CLEANUP_LEAVING_FOUR_COHERENT_TRIAL_ORDERS`.

This is **not** Architect acceptance, **not** user visual/functional acceptance, **not** a paid end-to-end proof with a real provider, and **not** a claim that shipping work is complete.

---

## 1. Payment / supply authority (defect 1)

Source-confirmed starting point (unchanged claim, audited): the TB-265 relational helpers built a `Paid` seller order by direct SQL and then constructed `FulfillmentUnit.CreateFromPaidOrder` by hand. That proved the aggregate, **not** the paid path. The audit correction for those earlier claims is recorded in `audit-correction-265.md` and appended to the 265-R2 evidence.

What this task closed:

| Seam | Guard |
| --- | --- |
| `FulfillmentDirectory.MarkProcessingAsync` | `EnsureSupplyPaymentAuthorizedAsync` before any quantity/state write |
| `ProcessSelectionsAsync` | same guard |
| `MarkPackedAsync` / `PackSelectionsAsync` | same guard |
| `CreateShipmentAsync` (individual + consolidated package members) | same guard |
| `DispatchShipmentCoreAsync` (single + consolidated dispatch) | same guard |
| resend / replacement parcel creation paths | same guard |

The guard reads the owning projection `IOrderFulfillmentReader.GetHandoffAsync(...).IsPaid` — one existing narrow Contracts policy, no second payment authority, no cross-module DbContext. A refusal raises `fulfillment.order.payment_not_authorized` **before** any inventory consume, state transition, event or parcel row.

The Order-side projection (`AdminOrderOperationsOrchestrator.Projection.cs`) now emits mutable physical actions only when the seller order is `SellerOrderStatus.Paid`, so the UI does not offer an action the owning command will refuse.

Live negative observation on the canonical DB: the unpaid trial order `01a1daaf-…` has **no fulfillment at all** (`fulfillments: [null]`), projects only `cancel`, and a public `mark_processing` command returns `404 order.operation.invalid` with zero parcels — an unpaid checkout cannot obtain supply through any owning path. The guard's own proof for an *existing* unit is the four focused command tests (below).

Focused tests — `Tooba.Fulfillment.Tests/Behavior/PaymentAuthorizedSupplyGate266Tests.cs` **4/4 pass** (processing, packing, shipment creation, dispatch each refuse an unpaid order with the typed code and zero effects).

## 2. Queue readability (defect 2)

The queue is the same shared `AppDataGrid`; nothing was replaced or duplicated.

- `AppDataGrid` gained a supported `rowHeight` prop; the queue renders **64px** rows (header 48px).
- The operational-status cell keeps badge + scope on one non-wrapping line, scope truncated with a `title` tooltip (no vertical clipping, no 3–4 line wrap at 390).
- The scope label no longer repeats «(بدون گروه پذیرش)» beside a badge that already says it.
- The order column keeps the human reference (`TB-20261114151709-01-2d8281`), not a UUID tail.

Measured on the served production build (`queue-readability.json`):

| Viewport | Data rows | Cells overflowing vertically | Badges clipped | Document h-overflow |
| --- | --- | --- | --- | --- |
| 1440 | 64,64,64 | 0 | 0 | 0 |
| 390 | 64,64,64 | 0 | 0 | 0 |

At 390 the grid keeps its own scoped horizontal scroll for the later columns (explicitly allowed by the task) instead of crushing the desktop columns.

## 3. Exact locator (defect 3)

Queue rows emit `workUnitKey = group:{fulfillmentId}` and the row link carries that exact `fulfillmentId` plus `from=queue`:

```
/admin/orders/{checkoutId}/operations?fulfillmentId={fulfillmentId}&from=queue
```

Before this task such a **lawful** ungrouped unit resolved to an empty target list, so a valid queue row dead-ended on «گروه، مرسوله یا بستهٔ انتخاب‌شده … پیدا نشد».

Repair, in the shared operational route only:

- `targetSellerOrderIds` matches either the seller order's `fulfillmentId` or the seller order id (older links).
- When a **coarse** fulfillment-only locator matches a seller order but the order has no captured `operationalDeliveryGroups`, the target frame now renders that one seller fulfillment unit — `groupKey = fulfillment:{fulfillmentId}` — with its real method, accepted delivery commitment (or `—`), parcel summary and readiness. Nothing is invented.
- A **precise** locator (`shippingGroupId` / `shipmentId` / `packageId`) that no longer resolves keeps the honest not-found message and adds no card, so a stale link can never be presented as another group.

Live on the served build: clicking «عملیات ارسال» on the `ReadyToFulfill` row lands on
`/admin/orders/01a1daa2-430d-7000-87db-c4f586104050/operations?fulfillmentId=01a1daa2-48be-7000-baa8-53a8d4d6872a&from=queue`
and the target frame reads «ارسال پیش‌فرض فروشگاه · آماده پردازش · موعد ارسال مشخص نشده · تحویل مشتری — · مرسوله‌ها بدون مرسولهٔ ساخته‌شده» with `admin-order-operations-target-missing` absent, `بازگشت به صف کار` → `/admin/fulfillments`, and zero horizontal overflow. The finance-free shared panel below is the same component, no new page.

Mounted regression: `admin-order-operations-route.mount.test.ts` **5/5** (4 retained + the new TB-266 coarse-locator case; the stale precise-locator case still asserts the honest not-found).

## 4. Authorized cleanup (defect 4)

Same canonical target verified before any destructive step: `tooba_alpha` @ 127.0.0.1:5432, SingleStore, tenant `store-alpha`, Development. No second database, no Docker/disposable DB, no edition/connection/port change.

Mandatory private backups (custom format, private directory `D:\temp\tb266-backup`):

| Backup | Bytes | SHA256 |
| --- | --- | --- |
| `tooba_alpha-pre-tb266.dump` (pre-cleanup) | 3,791,764 | `07AD46F5B6CBDAD16D152F23407645A0C81197DE942E96AEFCAB251E2395C8C1` |
| `tooba_alpha-pre-tb266-seed.dump` | 2,897,993 | `7865F86196CFA47968784E6A1632D28C2629C4FBA3ACB366D636A04A5B37905D` |
| `tooba_alpha-pre-tb266-repair.dump` | 2,902,992 | `CCF0279DA9A59556350AA2BF230813CAC049C3E25BE2F3DB0CB34F1CCC36A5BA` |
| `tooba_alpha-pre-tb266-final-cleanup.dump` | 2,908,545 | `FDFD25BFBBC853CADD9FE050725690BE93C8D4E9AA7C04BCBF31513B0391F1DE` |

Restoration command (documented, **not** executed):

```
docker exec -i postgres-db pg_restore -h host.docker.internal -p 5432 -U admin -d tooba_alpha \
  --clean --if-exists --no-owner < /path/to/tooba_alpha-pre-tb266.dump
```

Cleanup was scoped to order-owned developmental data (checkout / seller order / order line / fulfillment / shipment / receipt / package / return / claim / decision / operation history and their synthetic fake-provider payment rows), in dependency order inside one transaction with `ON_ERROR_STOP`, following the owning tables recorded in `cleanup.sql`. No `TRUNCATE CASCADE`, no disabled FK checks, no database drop/recreate, no blanket reseed, no unknown-table hand deletion.

Inventory conservation: order-bound reservations were removed through their owning journal tables *after* the orders they served, never by zeroing `Reserved` for live orders; the final `Reserved` counter equals the sum of the surviving `Held` rows (see counts).

After-state on the canonical DB (`cleanup-counts.json`):

| Table | Count |
| --- | --- |
| checkouts / seller_orders / order_lines | 4 / 4 / 4 |
| fulfillments | 3 |
| shipments / receipts / claims / packages / damaged resolutions | 0 / 0 / 0 / 0 / 0 |
| return_requests | 0 |
| reservations | 6 (3 `Held` for the paid orders + 3 `Released` from the unpaid reacquire cycle) |
| `inventory.stock_positions.total_reserved` | 3 |
| payments | 3 |
| carts | 8 |
| settlement_entries | 0 |
| **protected:** stock_positions / offers / parties / identity users / shipping methods / shipping drafts | **3577 / 3223 / 10 / 86 / 7 / 4** |

A final scoped transaction removed this task's own cancelled repair fixture (its first unpaid attempt auto-settled by the fake gateway, then cancelled): one checkout, one seller order, one line, one fulfillment, one fake refunded payment, one cart. Zero other effect — `total_reserved` stayed 3 and every protected count stayed identical. All rows the database still holds are the four trial orders.

No genuine external payment/refund/wallet/settlement accounting was erased: `settlement_entries = 0`, no payout row, every removed payment was `provider_code = fake`. No provider was called. `DataProtection` keys and `CapturedPreview` were untouched. The user's shipping method `01a17d0d-e033-7000-af43-49a1daa28822` was never written.

## 5. Trial set

Four coherent orders, all created through owning checkout/selection/commit and the Development payment-confirmation path (no `UPDATE status='Paid'`, no faked payment reader in the canonical runtime):

| # | Reference | Checkout | State | What the user should see |
| --- | --- | --- | --- | --- |
| 1 | `TB-20261114153112-01-6aa876` | `01a1daaf-21bc-7000-b5d1-1dd42a194dcf` | `PendingPayment` | No preparation/dispatch action at all — only `cancel`; no fulfillment exists |
| 2 | `TB-20261114151709-01-2d8281` | `01a1daa2-430d-7000-87db-c4f586104050` | `Paid` · `ReadyToFulfill` | «شروع پردازش» in the queue row and the seller panel |
| 3 | `TB-20261114151711-01-b558d5` | `01a1daa2-4a2d-7000-9086-c0e309939070` | `Paid` · `Processing` | «بسته‌بندی» next step, `pack_selected` per line |
| 4 | `TB-20261114151713-01-cfbc30` | `01a1daa2-519d-7000-bb9c-c4d0150a1971` | `Paid` · `Packed` | «ایجاد مرسوله» next step, `unpack` per line |

Review links (queue → exact target, one entry per physical row):

- queue: `http://127.0.0.1:3000/fa/admin/fulfillments`
- 1 `http://127.0.0.1:3000/fa/admin/orders/01a1daaf-21bc-7000-b5d1-1dd42a194dcf/operations`
- 2 `http://127.0.0.1:3000/fa/admin/orders/01a1daa2-430d-7000-87db-c4f586104050/operations?fulfillmentId=01a1daa2-48be-7000-baa8-53a8d4d6872a&from=queue`
- 3 `http://127.0.0.1:3000/fa/admin/orders/01a1daa2-4a2d-7000-9086-c0e309939070/operations?fulfillmentId=01a1daa2-50e6-7000-a834-6c3151fc33c7&from=queue`
- 4 `http://127.0.0.1:3000/fa/admin/orders/01a1daa2-519d-7000-bb9c-c4d0150a1971/operations?fulfillmentId=01a1daa2-5929-7000-98a4-32f0a306553e&from=queue`

Deliberately **not** seeded: the two return scenarios (usable resend, damaged/mixed replacement) from the task's scenario list. Creating a returned parcel legitimately requires a dispatched parcel, a carrier non-delivery and a return receipt, and the only carrier path available here is a fake waybill; the task forbids fake waybills and provider calls. This is disclosed as a scope limit rather than manufactured.

## 6. Verification actually run

| Check | Result |
| --- | --- |
| `Fulfillment` `PaymentAuthorizedSupplyGate266Tests` | 4/4 |
| Mounted operations route (`admin-order-operations-route.mount.test.ts`) | 5/5 |
| Queue/return-operations client contract set | 17/17 |
| `tsc --noEmit` | 0 |
| `next build` (final) | 0 · `BUILD_ID yuIokX6A6PV337VQbzU1k` |
| served frontend | port 3000, PID 25852, `/fa` 200 |
| Host | 5088, `/health` 200, `/health/ready` 200 → SingleStore |
| browser geometry 390 / 1440 | 64px rows, 0 clipped cells, 0 document overflow |
| queue → operations navigation | exact target card, no not-found, back link preserved |

No migration was added or applied (no schema change in this task). No disposable test database, no 15433, no provider call, no fake waybill, no global reclaimer, no scheduling/Returns redesign; TB-263 feedback, TB-264 readiness/mobile and TB-265 quantity/claim/closure invariants were not reopened.

## 7. Limits (explicit)

- No live paid end-to-end order was created with a real provider; the paid trial orders come from the Development payment-confirmation path, and the payment **guard** is proven by the four command tests plus the live unpaid observation. Live approval of real money remains the user's own business action.
- The unpaid trial order has no fulfillment by construction, so the guard's refusal for an *existing* unpaid unit is proven at the command level, not on a live unpaid-with-fulfillment row (such a row cannot be produced by any owning path).
- Return/resend/damaged scenarios were not seeded (fake waybill forbidden).
- The `shippingGroups` count is 0: the trial orders were committed without a captured shipping group, which is why the queue unit is a seller fulfillment; the locator now names that unit honestly instead of inventing a group.
- No Architect or user acceptance is claimed. Next step is the user's own trial. `STOP_FOR_ARCHITECT`, `automaticNextImplementationTask: NONE`.
