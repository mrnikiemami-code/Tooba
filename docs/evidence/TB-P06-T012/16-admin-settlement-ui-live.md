# 17 — Admin settlement screens (TB-P06-T012)

## Route

`/admin/settlement` → `src/frontend/app/admin/settlement/page.tsx`

## Screen

`AdminSettlementScreen` in `admin-screens.tsx`

## UI pattern

Reuses existing admin DataGrid shell (`AdminDataGridScreen`) — same component family as fulfillments, returns, etc.

## Columns

| Column | Source field |
|---|---|
| Seller party | `sellerPartyId` |
| Available balance | `availableBalance` (formatted IRR) |
| Posted credits | `postedCredits` |
| Posted debits | `postedDebits` |
| Reserved payouts | `reservedPayouts` |

## Data loader

`loadAdminSettlementBalanceRows()` wraps `loadAdminSettlementBalances()` and adds grid `id` key.

## Navigation

Admin shell nav entry: **تسویه فروشندگان** → `/admin/settlement` (`live: true`)

## Behavior

- Live Host data only
- Standard admin loading / error / empty states
- No export of fake settlement analytics
