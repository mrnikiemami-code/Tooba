# Feature boundary — TB-TMAR-FE-ADMIN-W6

Public boundary: `features/admin-dashboard/index.ts` exports `AdminDashboardScreen`, `loadAdminDashboard`, `mapAdminDashboard`, `AdminDashboard`.

Route uses public boundary only. No deep imports. Guard updated with admin-dashboard feature id.
