# Slice selection — TB-TMAR-FE-ADMIN-W5

## Selected: admin-receipts

| Criterion | Evidence |
| --- | --- |
| Bounded ownership | `/admin/receipts` payment receipts grid |
| admin-api debt | `AdminReceiptRow`, `mapAdminReceipt`, `queryAdminReceiptsGrid` |
| Flat touch | large receipts columns/actions (~140 LOC) |
| Risk | Low — read-only list; deep-link to orders only |

## Rejected this wave

- orders — checkout/payment/fulfillment adjacency
- dashboard — deferred to W6 (smaller; residual after receipts)
