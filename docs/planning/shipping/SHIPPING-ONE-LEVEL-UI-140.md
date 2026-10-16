# TB-SHIPPING-ONE-LEVEL-TABS-140 — Paid-event Fulfillment root cause and repair (bounded)

Parent: `TB-SHIPPING-ONE-LEVEL-TABS-139`
Mode: `BOUNDED_FULFILLMENT_PAID_EVENT_ROOT_CAUSE_AND_REPAIR`
Base: `e5451961a3b0e21b59f81f721b5fb12ea65f9635`
Status: **PASS — STOP_FOR_ARCHITECT**

## 1. What TB-139 left open

TB-139 repaired the Host transport consumer so one failing module handler can no longer starve the other
module handlers of the same integration event, and made the real per-handler error observable. It did not
identify the fault **inside** the Fulfillment owner path and did not run a paid path at runtime, so the
FulfillmentUnit invariant stayed UNPROVEN.

## 2. Root cause (owner: Fulfillment EF mapping)

`FulfillmentUnit` exposes `public IReadOnlyList<FulfillmentShippingGroup> ShippingGroups => _shippingGroups;`
and `FulfillmentDbContext.OnModelCreating` ignored `DomainEvents`, `Items` and `Shipments` **but not
`ShippingGroups`**. `FulfillmentShippingGroup` is mapped separately with its own scalar `FulfillmentId`
and no navigation back to the unit, so EF Core inferred a 1-to-many navigation from the un-ignored
read-only collection and synthesised a shadow foreign key:

```
FulfillmentShippingGroup {'FulfillmentUnitFulfillmentId'} -> FulfillmentUnit {'FulfillmentId'} ClientSetNull ToDependent: ShippingGroups
```

The migrated schema (`AddFulfillmentShippingGroups`) creates `fulfillment.shipping_groups` with
`fulfillment_id` and **never** `fulfillment_unit_fulfillment_id`. The in-memory provider hid the
mismatch; PostgreSQL does not, so the single `SaveChangesAsync` in
`FulfillmentDirectory.CreateFromPaidSellerOrdersAsync` (unit + items + shipping groups + `PaymentInbox`
in one transaction) aborted with

```
Npgsql.PostgresException 42703: column "fulfillment_unit_fulfillment_id" of relation "shipping_groups" does not exist
```

That is exactly the TB-138/TB-139 fingerprint: `order.payment_inbox = 1`, order `Paid`,
`fulfillment.payment_inbox = 0`, `fulfillment.fulfillments = 0`.

## 3. Owner-correct change (smallest, bounded)

`src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Persistence/FulfillmentDbContext.cs`

```csharp
entity.Ignore(x => x.ShippingGroups);
```

The collection is intentionally read-only and is loaded/persisted exclusively through the module's own
`DbSet<FulfillmentShippingGroup>` (`PersistShippingGroups` / `AttachLoadedShippingGroups` in
`FulfillmentDirectory.ShippingGroups.cs`), so the un-mapped navigation must not participate in the model.
The real link stays the scalar `fulfillment_id` column. No migration, no schema change, no Contracts
change, no module-boundary change, no handler or domain behaviour change, no frontend/CSS/theme touch.
`order.*`, `payment.*` and `settlement` were not modified.

## 4. Focused proof (red then green on the real fault)

`src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Behavior/FulfillmentShippingGroupSchemaParityTests.cs`
(3 tests, model built with the real `ToobaNpgsql.ConfigureModuleContext` snake_case convention):

| state | result |
| --- | --- |
| `Ignore` disabled (pre-fix model) | `Failed: 2, Passed: 1` — `Collection: [ForeignKey: FulfillmentShippingGroup {'FulfillmentUnitFulfillmentId'} -> FulfillmentUnit {'FulfillmentId'} …]` and column list contains the extra `fulfillment_unit_fulfillment_id` |
| `Ignore` restored (shipped model) | `Passed: 3, Failed: 0` |

Logs: `focused-test-fail-before-fix.txt`, `focused-test-pass-after-fix.txt`.
Independent SQL proof that the migrated relation really lacks the column and that a write naming it fails
with the exact code: `shadow-fk-absence-proof-140.txt`.

## 5. Runtime acceptance (isolated, scratch, fake sandbox only)

- Isolated Host `PID 27584` on `http://127.0.0.1:5408`, `ASPNETCORE_ENVIRONMENT=Development`,
  scratch tenant `store-alpha` → `tenant-140-scratch`, Postgres `127.0.0.1:55461`
  (`tooba_140_scratch`, `tooba_140_messaging`), single container `tooba-tb140-pg`.
  `prepare-isolated-target-140.js` guard: `PREPARE=PASS`, no `tooba_alpha`, no `tenant-alpha`, no `:5432`,
  no `ShippingRateAuthority`/`ShippingOperationalShadow`.
- Payment path: pre-existing Development-only fake provider (`Mode=Sandbox`, `DefaultProvider=fake`,
  empty PSP credentials, sandbox simulator gated to non-Production). No live PSP, no injected success
  event, no direct DB write, no auth bypass.
- Verified checkout `01a143bb-c830-7000-913e-58cd322fddc4` (scoped readback `A1..A21`):
  1 checkout, 1 SellerOrder `Paid` `TB-20261016080222-01-f93671` (7,194,000 IRR),
  1 payment `01a143bb-c9d1-7000-847e-1204a1d13012` `Succeeded` (7,574,000 IRR),
  2 accepted `order.order_shipping_groups` (`post:express` 200,000 + `tipax:standard` 180,000, disjoint
  line membership), 3 allocations = 2 `StoreShippingGroup` + 1 `SellerOrder`.
- Fulfillment (whole scratch DB, `B1..B7`): `payment_inbox = 1` (`01a143bb-ca43-7000-ab7e-f3fcf41f41db`),
  **`fulfillments = 1`**, **`shipping_groups = 2`**, `items = 2`, `shipments = 0`,
  `groups_per_unit = 01a143bb-cb12-7000-853f-1745962d630d=>2`, shadow column present = 0.
  `A13..A21`: unit `01a143bb-cb12-7000-853f-1745962d630d` (`ReadyToFulfill`) owns both stable group ids
  `01a143bb-c813-7000-87cc-12ce90b5cab4` (`post:express`) and `01a143bb-c813-7000-af28-ca99fb0ab07c`
  (`tipax:standard`) — the **same** ids as the accepted order groups and the payment allocations — with
  item→line→group membership exactly matching `order.order_shipping_group_lines`
  (`A20_item_group_matches_order_group=true`).
- Host log: `payment.succeeded.v1` `EventId=01a143bb-ca43-7000-ab7e-f3fcf41f41db` `HandlersPresent=True`
  consumed at `08:02:23.231`, `fulfillment.created.v1` emitted at `08:02:25.060` (+1.83 s);
  `Integration handler failed` lines = 0, `42703` lines = 0, shadow-column lines = 0.
- Outbox: `order 0/8`, `fulfillment 0/1`, `payment 0/6` pending — no stuck publication.
- No duplicate charge (1 payment, 1 Succeeded for the verified checkout), no auto shipment
  (`fulfillment.shipments = 0`); grouped shipment booking remains an explicit authorized seller action.

Scratch-DB context (not part of the verified run): the same disposable database also holds the two
cancelled and one paid checkout from the TB-138/TB-139 reproduction attempts, so `C1_checkouts_total=4`
and `C4_payments_total=2`; the Fulfillment side of the database contains exactly the one unit produced by
the verified post-fix run, which is why the fulfillment invariant is exact DB-wide.

## 6. Residual transport observation (for the Architect)

The scratch transport still holds the **pre-fix** delivery
`message_id b85b0000-7a7c-d88c-cc48-08df2b5ae81f`, `eventType payment.succeeded.v1`,
`eventId 01a143b5-dbce-7000-8934-6a8e168f9491`, enqueued `2026-10-16 07:56:45`,
`delivery_count=1`, `consumer=none`, with `error_queues=0`. That is the durable trace of the TB-138/139
pre-repair attempt whose Fulfillment write failed: it was never acknowledged and never dead-lettered,
whereas the post-fix event (`01a143bb-ca43-…`) is gone from the transport because it was consumed and
acknowledged. This task neither relabels nor replays that historical message; the scratch container was
disposed, so the historical paid order it belongs to no longer exists anywhere.

## 7. Non-interference, cleanup, rollback

- Shared surfaces untouched: `:5088`, `:3000` and `tooba_alpha` were never started, stopped or written;
  probes confirm both listeners still alive after cleanup.
- Cleanup: isolated Host `PID 27584` stopped (`:5408` listeners = 0), container `tooba-tb140-pg` removed
  (`:55461` listeners = 0). No runtime left behind.
- One pre-run obstacle, scratch-only: the post-fix re-run was first refused by
  `CheckoutAbuseLimitException` because three commit attempts from the pre-fix runs of the same scratch
  customer were inside the default 30-minute window. Only the disposable scratch
  `order.checkout_reservation_commits` rows for that scratch customer were deleted; no abuse-gate code,
  threshold, shared database or shared environment was touched.
- Rollback: revert the fix commit — the un-ignored navigation reappears, the schema-parity tests fail
  again and PostgreSQL resumes rejecting every `SaveChangesAsync` that inserts a shipping group. No data,
  schema, migration or configuration is affected.

## 8. Next bounded step (for the Architect)

Keep Settlement `PendingAllocation` and the missing settlement schema as a separately tracked blocker, as
instructed — this task made no allocation or commercial-split decision. The paid→fulfillment seam is now
closed and runtime-proven; the remaining work is the one-level shipping UI consumption of the two stable
group ids on the fulfillment surface, plus the independently tracked settlement allocation.
