# TB-SHIPPING-ONE-LEVEL-TABS-111 — summary

**Status: PARTIAL.** `STOP_FOR_ARCHITECT`. `Code-SHA: NONE` (zero production source changes).

## TB-109 barrier: RESOLVED at runtime
- `relation "party.parties" does not exist` occurrences: **0** (was the exact TB-109 blocker).
- All module schemas migrated before any post-migration step; 110 EF migration commands; 20 schemas.
- Catalog module-owned post-migration step COMPLETED: `schema-mobile-demo-phone` Published, 2 sparse
  variants, exactly 1 default (`PHONE-BLK-128`), no Cartesian growth.
- Causal proof: product/variant created 01:21:01.0–.19, then party 01:21:01.37, offers .41, prices .46,
  stock .56; all offer→party / price→offer / stock→offer joins have 0 broken rows.

## Real sellable readback (presence only — no readiness claim)
`SCHEMA-PHONE-PHONE-BLK-128` Active offer + Active base price 12,500,000 IRR + available 5.0;
`SCHEMA-PHONE-PHONE-BLU-256` Active offer + Active base price 13,000,000 IRR + available 5.0.
Totals: 3 offers (3 Active), 4 prices (2 Active Base), 3 stock positions (2 positive), 2 seller parties,
1 location, 0 carts, 0 orders, 0 shipping methods.

## FIRST NEW BLOCKER (startup not healthy)
`System.InvalidOperationException: catalog.attribute.code.duplicate` from
`WorkspaceDemoProductSeed.SeedProductAsync:103` → `CatalogDirectory.CreateAttributeDefinitionAsync:987`,
reached via `DevelopmentSchemaMigrator.ApplyCoreAsync:98/112` → `Program.cs:367`, i.e. AFTER
`MigrateOrderedAsync` completed. The legacy workspace demo seed re-creates attribute code `color`, already
created by `CatalogAttributeSchemaDevelopmentSeed:39`. A Development seed collision, not a migration or
Catalog/Shipping defect. No fix attempted.

## Isolation / cleanup / parity
1 tenant + scratch-only references, scratch port 55447, alpha bytes absent, Shipping switches OFF,
AllowResetAndSeed=false. ONE launch, watchdog 180s, elapsed 54s, natural exit, no kill, :5395 closed.
Container + all `.tmp-111-*` removed; raw logs dropped after a clean credential scan. `:5088` (PID 35624)
and `:3000` (PID 31580) untouched; `/health` ok; tooba_alpha 7/14/50/78/14/26/567/3223 identical
before/after.

See `docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-111.md` and the evidence folder.
