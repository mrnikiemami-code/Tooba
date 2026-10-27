# RESULT — TB-231-R2 — null-safe / canonical-shape database CHECKs

Parent: `TB-SHIPPING-COMPACT-GROUP-ORDER-EVIDENCE-231-R1` (value/transfer accepted; this task only corrects the
still-unshared, still-unaccepted migration's CHECK expressions and proves them directly against PostgreSQL).

Base: `a74fadd92` (the issue commit). Branches: `main`.

## What changed (source, exactly three things)

1. `src/backend/Modules/Order/Tooba.Order.Infrastructure/Persistence/Migrations/20261027150000_AddShippingGroupAcceptedEvidence.cs`
   — the SHAPE CHECK is now built by one helper `BuildShapeConstraint(table)` and, in place, closes the four
   confirmed holes **on both `order.cart_shipping_groups` and `order.order_shipping_groups`**:
   - optional delivery days: `(min IS NULL AND max IS NULL) OR (min IS NOT NULL AND max IS NOT NULL AND …)`.
     The explicit `IS NOT NULL` pair is what turns a half pair from `UNKNOWN` (which PostgreSQL **accepts**)
     into `FALSE`. The existing `0 ≤ min ≤ max ≤ 365` bounds are unchanged inside the second branch.
   - Store owner: `accepted_owner_tenant_key IS NOT NULL AND btrim(...) <> '' AND ... = btrim(...)` — blank,
     whitespace-only and non-canonical (leading/trailing-space) tenants are refused. The value is never
     lowercased and no tenant is ever guessed; `Store-Alpha` stays lawful as written.
   - required references: `accepted_snapshot_id`, `accepted_draft_id`, `accepted_area_group_id`,
     `accepted_rate_row_id` must not be the empty UUID; the optional-present `accepted_shipping_type_id` and a
     Seller owner's `accepted_owner_seller_party_id` must not be the empty UUID either.
   - `accepted_currency ~ '^[A-Z]{3}$'` replaces `char_length(accepted_currency) = 3`, so `123`, `A$!`, `irr`
     and `IRr` are refused while `IRR`/`USD` remain lawful. No currency default is introduced.
   Everything else in the migration is untouched: the same 22 plain `accepted_*` columns (no `IF NOT EXISTS`),
   the same `ck_*_accepted_evidence_complete`, the same single snapshot index, the same `Down`.
2. `src/backend/Modules/Order/Tooba.Order.Tests/Checkout/ShippingGroupAcceptedEvidenceStorageTests.cs` — five
   new direct-Postgres facts plus a private `EvidenceRowFixture` that writes **one valid row per group table**
   through the real domain constructors/materializer and then corrupts it with raw SQL (constructor bypass, as
   the task requires) on the existing task-owned target.
3. Nothing else. No Domain/DTO/mapping/column/table/index/transaction/amount/owner/time semantics change, no
   `OrderDbContextModelSnapshot` regeneration, no new column, no JSON, no cross-module FK.

## RED then GREEN (direct SQL, real constraints)

Target: one task-owned disposable `postgres:17-alpine`, `127.0.0.1:15433`, database `order_tb231`, user `tb231`.
Identity asserted before any DDL (`select current_database()` → `order_tb231`, `order` schema absent). No shared
`5432`, no reviewed-helper relaxation.

**RED** — with the reviewed (pre-correction) migration applied through the normal Order migration chain:

```
Failed!  - Failed: 4, Passed: 6, Total: 10
  Half_delivery_day_pairs_are_refused_by_the_database_not_left_unknown   No exception was thrown
  Blank_or_noncanonical_store_tenant_is_refused_by_the_database          No exception was thrown
  Empty_required_and_optional_present_uuids_are_refused_by_the_database  No exception was thrown
  Currency_must_be_exactly_three_ascii_uppercase_letters                 No exception was thrown
```

The four counterexamples are accepted by the reviewed CHECK, exactly as the Architect's review stated. The
fifth new fact (`Explicit_zero_amounts_and_a_fully_absent_row_stay_lawful`) is a preserving guard, not a
counterexample: it passes on the reviewed CHECK and must keep passing afterwards.

**GREEN** — after the correction, the same task-owned database was **recreated empty** (dropped and created
again; no already-applied history row was reused, no manual `ALTER`, no constraint adoption):

```
Passed!  - Failed: 0, Passed: 10, Total: 10
```

Applied definition read back from `pg_constraint` (both tables, identical body) contains
`(accepted_currency)::text ~ '^[A-Z]{3}$'`, `btrim(accepted_owner_tenant_key)` (twice: `<> ''` and `=`),
`accepted_minimum_delivery_days IS NOT NULL AND accepted_maximum_delivery_days IS NOT NULL`, and the four/five
`<> '00000000-0000-0000-0000-000000000000'::uuid` comparisons; the previous
`char_length(...) = 3` and the bare `min >= 0 OR …` half-pair form are gone from both tables.

Constraint inventory on the recreated database:

```
"order".cart_shipping_groups   | ck_cart_shipping_groups_accepted_evidence_complete
"order".cart_shipping_groups   | ck_cart_shipping_groups_accepted_evidence_shape
"order".order_shipping_groups  | ck_order_shipping_groups_accepted_evidence_complete
"order".order_shipping_groups  | ck_order_shipping_groups_accepted_evidence_shape
```

Every refusal is asserted as SQLSTATE `23514` **and** the expected constraint name
(`ck_<table>_accepted_evidence_shape`), and after each refusal the row is re-read and compared field-by-field
(snapshot id / owner kind / currency / freight / opening ticks) so a rejected write provably changed nothing.

Covered on both tables: `min NULL / max 3`, `min 2 / max NULL`, a repeated half pair after lawful edits, blank
tenant `''`, `'   '`, `' store-alpha'`, `'store-alpha '`, empty `snapshot_id` / `draft_id` / `area_group_id` /
`rate_row_id`, empty optional-present `shipping_type_id`, empty Seller id, and `123` / `A$!` / `irr` / `IrR` /
` IR` currencies. Still lawful and proven lawful: `min NULL / max NULL`, `min 0 / max 365`, `Store` with
`Store-Alpha`, `Marketplace`, `Seller` with the real seller id, absent optional type, `USD`, four explicit zero
amounts, and a complete `NULL` clear of all 22 columns (lawful legacy row).

Container removed afterwards; `127.0.0.1:15433` verified **closed** and no leftover container.

## Honest limits

- SQL precision policy is deliberately unchanged: `numeric(18,2)` coerces the value *before* the CHECK runs, so
  the database is not claimed to preserve an out-of-scale raw amount. Exact-amount representability stays the
  application's authority, as it already was.
- `btrim` trims spaces only; a tenant padded with a tab/newline is refused by the domain but is not separately
  asserted at SQL level.
- The EF model snapshot still lacks the group entities (pre-existing baseline gap); it was not regenerated and no
  future-scaffolding safety is claimed. Scoped runtime-model-vs-migrated-columns parity from 231-R1 is unchanged.
- Migration `20261027150000_AddShippingGroupAcceptedEvidence` is still **WRITTEN** and **NOT APPLIED** to shared
  `tooba_alpha`/store-alpha. No migration history was rewritten, no earlier migration edited.
- No live composer/writer, no parity, no shared migration, no Host/FE restart, no browser use, and the dedicated
  user review window was never enumerated, automated, reloaded or navigated. Live data, previewON/count7,
  `storefront.captured_preview.not_ready`, and TB185 paused/default-OFF/PendingAllocation are untouched.
- One correction round inside the module (a `SqlQuery` interpolation put a parameter where an identifier belongs;
  fixed to `SqlQueryRaw`). Counts reported above are the actual final RED/GREEN runs.

## NEXT (Architect)

Unchanged: trusted server composer (selected revision + cart/version + Offer/type/quote/date revalidation)
writing this compact evidence inside the existing atomic boundary, then the Order-storage-only historical read;
captured checkout stays `not_ready` until then. Storefront read-only UI, cross-owner label map / decoder and
Offer API gaps remain open and were not touched.

STOP_FOR_ARCHITECT; Automatic-Next-Task NONE. No user Visual ACCEPT is claimed.
