# admin-api shrink — TB-TMAR-FE-ADMIN-W5

| Metric | Before | After |
| --- | --- | --- |
| admin-api LOC | 1069 | 1022 |
| FE-FOLDER-002 exports | 41 | 38 |

Removed: AdminReceiptRow, mapAdminReceipt, queryAdminReceiptsGrid.

Remaining CAPABILITY_SPECIFIC_DEBT: orders + dashboard (+ order-detail types).
SHARED_TECHNICAL: formatters, headers, AdminResult re-exports.
