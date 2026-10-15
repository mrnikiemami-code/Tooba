# SHIPPING-FULL-OPERATIONS-071 — Bounded Scheduling Vertical Slice

Task: `TB-SHIPPING-FULL-OPERATIONS-071` (task 1 of at most 10 in program `TOOBA-SHIPPING-FULL-OPERATIONS`)
Mode: `BOUNDED_SCHEDULING_VERTICAL_SLICE`
Expected-Base-SHA: `1c64b45731c59b16e36f6f4ed418219cef120800`

## What this slice delivers

A Shipping-owned, persistent, configurable **weekly operating schedule**, keyed to the existing
Fulfillment `serviceCode` and optional `optionCode`, with an authorized admin read/create/update/
deactivate API, a module-scoped migration and focused tests.

```text
GET   /v1/admin/shipping-services/{serviceCode}/operating-schedules
PUT   /v1/admin/shipping-services/{serviceCode}/operating-schedules
PATCH /v1/admin/shipping-services/{serviceCode}/operating-schedules/{scheduleId:guid}/active
```

Authorization reuses the existing platform permissions already declared by this module:
`fulfillment.view` (read) and `fulfillment.manage` (write). No new capability string, no
`AllowAnonymous`, no `Results.Json`, no custom privilege bypass.

## Semantics (locked by this slice)

| Aspect | Decision |
| --- | --- |
| Weekday | ISO 1..7 (1 = Monday, 7 = Sunday). Outside that range is refused. |
| Window range | Half-open `[start, end)`; `end > start` is a hard rule. |
| Overlap | Two windows on the same weekday may not overlap. Adjacent windows (`13:00–14:00`, `14:00–15:00`) do **not** overlap, because the boundary instant belongs to the later window exactly once. |
| Overnight | **Not supported.** A window that crosses midnight (`22:00–02:00`) is refused with the same stable range code. Supporting it means splitting across two weekdays, which is a separate explicit decision. |
| Timezone | Required and explicitly validated against what the runtime can resolve (IANA preferred, e.g. `Asia/Tehran`). An unresolvable id is refused. |
| Active flag | Present; deactivation never deletes the row, so historical references stay valid. |
| Capacity | Nullable, positive, per window. **Configuration only — not enforced.** See below. |
| Empty window set | Legal and meaningful: "no availability window declared". It is never rewritten into a default. |
| Absent schedule | Returns an empty list. Never a fabricated default, and never a checkout blocker. |

### Capacity is configured, not enforced

`configuredCapacityTotal` is the sum of the per-window `capacity` values that were configured. There
is **no** atomic reservation, counting or locking path in this slice, so a configured capacity does
**not** cap anything at runtime. It is a stored intention for a later task, and it is labelled as such
in the contract XML docs, the admin DTO and the tests.

### Schedule is not a promise

The schedule describes **carrier pickup/dispatch availability**. It is not, and does not create, a
customer delivery promise. The customer delivery promise and its snapshot remain Order-owned. The
compatibility guard `OperatingScheduleCompatibilityTests` pins that no promise/snapshot field leaks
into this aggregate.

### Fail-open with respect to today's Checkout

Nothing outside `src/backend/Modules/Shipping/**` references the schedule types, tables or routes
(asserted by `OperatingScheduleCompatibilityTests.No_module_outside_shipping_references_the_operating_schedule_types`).
No consumer reads the schedule yet, so an absent or inactive schedule cannot silently shut down the
legacy Checkout path.

## Ownership

- **Fulfillment** owns service and option identity. Shipping validates the submitted `(serviceCode, optionCode)`
  against the real Fulfillment catalog through the existing read-only Contracts seam
  (`IShippingCatalogReader` via `IShippingMethodOptionValidator`). An invented service or option is refused
  before any write, so it can never become a schedule key.
- **Shipping** owns schedules, coverage, rates and operating policy.
- **Order** owns the customer delivery promise/snapshot.
- **Payment/Settlement** own money handling. Nothing here touches money.

No Host business authority was added. `Program.cs` was **not** modified: the new directory is registered
in module-owned DI (`ShippingModule.AddServices`), and the HTTP layer is mapped through the existing
`ShippingEndpointModule.MapShippingEndpoints()` composition that the Host already calls.

## Persistence

New migration: `20261015045130_OperatingSchedule` (Shipping module only).

| Table | Notes |
| --- | --- |
| `shipping.operating_schedules` | One operating policy per `(service_code, option_code)`, unique with `NULLS NOT DISTINCT` so a second option-less row for the same service cannot exist. |
| `shipping.operating_windows` | Owned by the schedule through an intra-module cascade FK. |

Check constraints mirror the aggregate guards exactly, so raw SQL cannot store a row the domain refuses:

```text
ck_operating_schedules_service_code_canonical   char_length(service_code) > 0 AND service_code = btrim(service_code)
ck_operating_schedules_option_code_canonical    option_code IS NULL OR (...)
ck_operating_schedules_timezone_canonical       char_length(time_zone_id) > 0 AND time_zone_id = btrim(time_zone_id)
ck_operating_windows_weekday_iso_range          weekday >= 1 AND weekday <= 7
ck_operating_windows_range_half_open            end_time > start_time
ck_operating_windows_capacity_positive          capacity IS NULL OR (capacity > 0 AND capacity <= 1000000)
```

The only foreign key in the whole `shipping` schema is the intra-module
`operating_windows -> operating_schedules` cascade. No cross-module FK exists.

## Migration target and rollback

Applied **only** to the positively verified Development target:

```text
host          : existing Development process, 127.0.0.1:5088
GET /__platform-commerce => {"edition":"SingleStore","deploymentId":"dev-singlestore",
                             "tenantId":"store-alpha","connectionReference":"tenant-alpha",
                             "resolvedHost":"127.0.0.1"}
database      : tooba_alpha on localhost:5432 (container postgres-db, postgres:17)
```

No Production/staging connection, no scratch database, no all-module `MigrationRunner`, no parallel
database. Pre-existing `069` rate and coverage rows were preserved (9 rates, 7 grants before and after).

**Bounded rollback.** The change is purely additive:

```text
docker exec postgres-db psql -U admin -d tooba_alpha -c \
  "DROP TABLE IF EXISTS shipping.operating_windows; DROP TABLE IF EXISTS shipping.operating_schedules; \
   DELETE FROM shipping.__ef_migrations_history WHERE migration_id = '20261015045130_OperatingSchedule';"
```

or, equivalently, `dotnet ef database update OptionLevelRatePolicy` from
`src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure`. Dropping the two new tables cannot affect
`rate_policies`, `coverage_grants` or `outbox_messages`.

## Verification actually run

- Focused module suite: **154/154 pass** (`Tooba.Shipping.Tests`), including 34 new scheduling tests
  across domain invariants, admin persistence roundtrip, refusals, endpoint ownership and compatibility.
- One live HTTP roundtrip on the existing Development host against `tooba_alpha`: real Sunday-only
  `13:00–14:00` window plus a second distinct `16:30–18:00` window (capacity 25), persisted, read back,
  idempotent on replay, refusals at 400 with stable codes, `401`/`403` at the authorization boundary,
  deactivate/reactivate roundtrip, cross-service toggle refused.
- Storefront projection shape was not exercised as a diffed payload in this slice; instead the
  compatibility guards prove no non-Shipping source file references the schedule surface at all, which
  is the stronger statement for a module with no runtime consumer.

## Not done here (deliberately out of scope)

- No admin UI (task `072` in the Architect's budget plan owns the scheduling editor).
- No weight/quantity tiers, packaging fees, freight-payer modes, or tier/packing evaluation.
- No capacity enforcement (no reservation/counting path).
- No pricing/checkout cutover, no order/payment/snapshot mutation, no invented commercial tariff.
