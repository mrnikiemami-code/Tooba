# TB-P09-T003 — Navigation rule

## Locked rule
- Canonical detail entry: **View** (`مشاهده`) only.
- Order reference/number must be non-navigation readable text.

## Change
In `src/frontend/app/admin/admin-screens.tsx` `orderColumns` `reference` cell:
- Removed `<Link href=/admin/orders/...>` duplicate navigation
- Now `truncatedCell(row.reference, row.reference)`

`orderRowActions` View + `AdminOrderOperationsMenu` unchanged.

## Scope
Orders grid only — receipt/other grids untouched.

## Test
`admin-order-operations.test.ts` asserts reference cell has no Link href and View remains canonical.
