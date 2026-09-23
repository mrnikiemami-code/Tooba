# Recovery SoT — TB-TMAR-ORDER-GOLDEN-001-R10

## Claim

- Task-ID: `TB-TMAR-ORDER-GOLDEN-001-R10`
- Claim-Id: `034fe351-f31d-4562-b978-df9874507fd4`
- Channel: `tooba-main`
- HEAD at start: `936b92c22ba01240976df557745b17102af66ddb` (== `origin/main`)

## Outcome slice

- `SELLER_PANEL_ORDER_CQRS_R10`
- Seller Order list/detail routes moved Host → Order.Endpoints (ISender)
- Dashboard Order open/paid via `GetSellerOrderDashboardSummaryQuery` only
- `SellerPanelComposer` has **no** `OrderDbContext`
- SellerOrder* DTOs in Order.Application; dashboard/catalog models remain Host
- Foreign Contracts only (Catalog/Party); AccessControl via Host `ISellerOrderViewAccessReader` adapter
- R7 inventory refreshed for R10; historical R7 audit markdown + R8/R9 inventory notes preserved

## Module state

- Order: `INCOMPLETE_REFERENCE_REPAIR`
- Checkout: `PAUSED_AT_SAFE_W5_CHECKPOINT`
- nextTask: `TB-TMAR-ORDER-GOLDEN-001-R11` (not started)
- Gate: `ORDER_GOLDEN_REPAIR_REQUIRED`

## Durable docs synced

- `docs/architecture/tmar-current-state.json`
- `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md`
- `docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md`
- R7 `host-order-reference-inventory.json` updated for R10 (r7/r8/r9 history intact)
