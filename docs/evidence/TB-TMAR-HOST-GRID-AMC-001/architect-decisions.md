# TB-TMAR-HOST-GRID-AMC-001 — Architect decisions

Locked by Architect (user-authorized AMC: «پوشه Grid را رسیدگی کن» with three AMC skills).

## Decisions

1. **No Host/Grid business residue** — admin query engines and domain policies leave Host.
2. **No new Grid BFF module** — own engines live in Story / Reviews / Party (FOUNDATION_PARTIAL OK).
3. **BuildingBlocks.Grid remains the only shared grid platform** — do not invent a parallel Host grid stack; delete Host InMemory/Bounded/AdminListGridQueryPolicy when unused after domain moves (do not sink into Host/Development).
4. **Reviews Catalog coupling** must become Catalog Contracts before Reviews engine leaves Host.
5. **Multi-wave** R1→R5 sequential in this chat; Host/Grid ABSENT is the program goal.
6. **Behavior preservation** mandatory: Normalize validation codes/HTTP mapping, filter/sort/page semantics, Sellers Contracts-only metrics sort compromise.

## Wave plan

| Wave | Scope |
| --- | --- |
| **R1** | Delete Host `AdminListGridPolicies.Orders` residue; assert Order `AdminOrdersGridPolicy` sole owner; refresh stale Host write-file baselines for already-evacuated engines; durable `HostGridAmcR1GuardTests`. |
| **R2** | Story policy + `AdminStoryGridQueryEngine` → Story.Infrastructure/Grid; Host Story composer → module port/DI; remove Host Story grid DI/files. |
| **R3** | Catalog Contracts product-title lookup for admin review grid filter; Reviews policy + engine + `AdminReviewItem` → Reviews; remove Host CatalogDbContext from Reviews path. |
| **R4** | Sellers policy + engine + `AdminSellerListItem` → Party; update AdminPanelComposer / Canon guards. |
| **R5** | Delete remaining Host Grid primitives if unused; Host/Grid ABSENT; SoT CLOSED_HOST_ZERO; certify guards. |

## R1 start authorization

`READY_TO_MIGRATE` for R1 Orders residue + baseline hygiene.
