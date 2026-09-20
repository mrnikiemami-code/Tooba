# admin-api shrink — TB-TMAR-FE-ADMIN-W6

| Metric | Before | After |
| --- | --- | --- |
| admin-api LOC | 1022 | 1002 |
| FE-FOLDER-002 exports | 38 | 35 |

Removed: AdminDashboard, mapAdminDashboard, loadAdminDashboard.

Remaining CAPABILITY_SPECIFIC_DEBT: Orders (+ order-detail/supply types and loaders).
SHARED_TECHNICAL: formatters, headers, AdminResult/actor re-exports.
