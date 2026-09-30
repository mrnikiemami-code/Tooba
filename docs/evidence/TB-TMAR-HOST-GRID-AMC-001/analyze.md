# TB-TMAR-HOST-GRID-AMC-001 — Analyze

## Target

`src/backend/Host/Tooba.Host/Grid/` (8 files)

## Final-Disposition

**READY_TO_MIGRATE** — business engines leave Host; legacy InMemory/Bounded/Policy helpers leave Host (promote to BuildingBlocks only if still needed, else delete).

## Per-file disposition

| File | Disposition |
| --- | --- |
| `AdminListGridPolicies.cs` | MUST_SPLIT → Orders delete (Order owns); Sellers→Party; Reviews→Reviews; Stories→Story |
| `AdminStoryGridQueryEngine.cs` | REMOVE_TO_Story |
| `AdminReviewGridQueryEngine.cs` | REMOVE_TO_Reviews (+ Catalog Contracts for product title filter) |
| `AdminSellersGridQueryEngine.cs` | REMOVE_TO_Party (already Contracts-only) |
| `AdminListGridQueryPolicy.cs` | DELETE or RETAINED_PLATFORM → BuildingBlocks.Grid when unused |
| `BoundedListGridQueryEngine.cs` | DELETE or RETAINED_PLATFORM → BuildingBlocks.Grid when unused |
| `InMemoryGridField.cs` / `InMemoryGridFieldKind.cs` | Same |

## Coupling

- Story engine: Host → Story DbContext/Domain (**illegal**)
- Reviews engine: Host → ReviewsDbContext + **CatalogDbContext** (**illegal** / foreign persistence)
- Sellers engine: Offer/Party/Order **Contracts-only** (**legal**)

## Canonical references

- `BuildingBlocks.Grid` (`GridQueryPolicyBase`)
- Payment `PaymentAdminGridQueryNormalizer`
- Content `ContentAdminGridPolicies`
- Order `AdminOrdersGridPolicy`

## Wave plan (locked in architect-decisions)

R1 Orders residue + baseline → R2 Story → R3 Reviews (+ Catalog Contracts) → R4 Sellers/Party → R5 Host/Grid ABSENT + primitives cleanup.
