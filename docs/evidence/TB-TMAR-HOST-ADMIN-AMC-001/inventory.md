# Inventory — TB-TMAR-HOST-ADMIN-AMC-001 (W1)

## Scope

`src/backend/Host/Tooba.Host/Admin/` (+ `CatalogDemo/`)

| Metric | Value |
|--------|-------|
| Production `.cs` files (re-enumerated) | **59** |
| Approx LOC | ~18.8k |
| Analyze mode | ANALYSIS complete (prior turn); this wave = foundation only |
| Host files moved in W1 | **0** |

## Destination readiness (analyze)

| Module | Endpoints | CQRS | Notes |
|--------|-----------|------|-------|
| **Catalog** | **MISSING → created W1** | YES (~45 handlers) | `FOUNDATION_PARTIAL` → Endpoints foundation |
| Promotion | YES | YES | Merchandising still Host |
| Order / Payment / Settlement / Returns / Wallet / Support | YES | YES | Thin Host authorizers retained |
| Fulfillment | YES | YES | Authorizer pattern reference |

## Disposition summary (from analyze)

| Disposition | Count (approx) |
|-------------|----------------|
| KEEP_AS_HOST_PLATFORM_SEAM | 7 |
| KEEP_AS_THIN_HOST_SECURITY_ADAPTER | 8 |
| MOVE_TO_Catalog (incl. after SPLIT) | ~22 |
| MOVE_TO_Promotion (after SPLIT) | 1 (+ merch seed) |
| SPLIT (multi-owner) | HoldPolicy, ReservationPolicy*, CheckoutIdentity, CatalogAttribute, StoreLandingComposer, MerchAdmin, ProductWorkspaceComposer |
| DEVELOPMENT_RETAIN_OR_MOVE | ~18 |
| DEAD_ZERO_CONSUMER | **0** |

## Recommended waves (dependency-risk ascending)

| Wave | Goal |
|------|------|
| **W1 (this)** | Catalog.Endpoints foundation + Host/slnx wire; **no Admin evacuation** |
| W2 | Thin CQRS settings: QuantitySettings / StoreAppearance* |
| W3+ | UoM → Category/Tag/Facet/MegaMenu → checkout settings → menus/landing → attributes → reservation/hold SPLIT → merch → ProductWorkspace → demo/seeds |

## W1 non-goals (locked)

- Merchandising, ProductWorkspace, HoldPolicy, ReservationPolicy, CatalogDemo, industry seeds
- Changing `AdminPanelAccess` / `IAdminPanelAccess` / Host thin authorizers
- Schema/migrations / frontend
