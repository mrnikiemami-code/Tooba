# TB-TMAR-HOST-GRID-AMC-001-R1 — Migrate

## Scope

Remove Host Orders grid policy residue; assert Order `AdminOrdersGridPolicy` is sole owner; refresh stale Host write-file baseline for Grid reality.

## Disposition executed

| Item | Before | After |
| --- | --- | --- |
| `AdminListGridPolicies.Orders` | Host Grid Orders whitelist + Order `AdminOrderListItem` coupling | **Deleted** |
| Order usings in Host policies | `Tooba.Order.Application.Admin.OrdersGrid.Models` | **Removed** |
| `AdminListGridQueryEngineTests` Orders.Execute | Host in-memory `Orders.Execute` | Order `AdminOrdersGridPolicy.Normalize` + Host residue asserts |
| `tmar-host-write-files.json` Grid entries | 9 stale evacuated engines + 3 remaining | **Only** Review/Sellers/Story engines |

## Ownership notes

- Orders normalize/whitelist already owned by `Tooba.Order.Application.Admin.OrdersGrid.AdminOrdersGridPolicy`.
- Host `AdminListGridPolicies` retains Sellers / Reviews / Stories until R2–R4.
- Payment / Returns / Settlement / Fulfillment guards that read `AdminListGridPolicies.cs` remain valid (file stays).

## Behavior parity

- Order grid Normalize defaults (page/pageSize/search/default sort `created` desc) unchanged via Order policy.
- Unknown filter field still rejected with `grid.filter.field.invalid`.

## Guards

- New: `HostGridAmcR1GuardTests`
- Updated: `AdminListGridQueryEngineTests`

## Explicit non-goals

- Story / Reviews / Sellers moves (R2–R4)
- Host/Grid ABSENT (R5)
- Schema change
- Commit / push / Bridge POST
