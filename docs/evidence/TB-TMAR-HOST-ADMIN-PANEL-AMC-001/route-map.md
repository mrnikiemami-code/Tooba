# Route map — TB-TMAR-HOST-ADMIN-PANEL-AMC-001

```text
ANALYSIS_ONLY_NOT_MIGRATED_NOT_CERTIFIED
Route count: 4
```

| Method | Path | Handler | Auth | Success body | Error path | Host ownership legal? | Final owner analysis |
|--------|------|---------|------|--------------|------------|----------------------|----------------------|
| GET | `/v1/admin/dashboard` | `GetDashboardAsync` → Composer | `AdminPanelAccess` | `AdminDashboardSummary` JSON | PlatformHttpException → `{title,errorCode}` | YES — cross-module composition | **KEEP Host Panel** (thin) |
| GET | `/v1/admin/sellers` | `ListSellersAsync` → Composer | same | `AdminSellerListItem[]` (Party.Contracts) | same | Temporary Host OK | **MOVE Party.Endpoints** |
| POST | `/v1/admin/sellers/query` | `AdminGridQueryEndpoint` → `QuerySellersGridAsync` | same (via Grid helper) | `GridPageResponse<AdminSellerListItem>` | Grid helper Json error | Temporary Host OK | **MOVE Party.Endpoints** |
| GET | `/v1/admin/dev-context` | `GetDevContext` | none (env gate only) | `{actorUserId,actorLabel,tenantId}` or 404 | hard-coded `"Not Found"` + `admin.dev.unavailable` | Weak — Development-only in Panel | **SPLIT**; destination **NEEDS_ARCHITECT_DECISION** |

## Public-contract preservation (migration must keep)

- Paths and HTTP methods above
- Success DTO field shapes (`AdminDashboardSummary` properties; `AdminSellerListItem` fields; grid page envelope; dev-context anonymous shape)
- Status codes: 200 success; 401/403/503 from Access; 404 for unavailable dev-context
- Stable error codes: `admin.*` including `admin.dev.unavailable`
- Authorization semantics: Bearer session preferred; Dev header only in Development
- Dev-only availability: non-Development always 404 unavailable
- Seller grid: server-side `GridQueryRequest` behavior via Party port
- Dashboard counting: published products, active offers, open/paid/pending orders, distinct offer sellers, customers — from existing Contracts ports
- CancellationToken propagation on async routes

## Duplicate ownership

No second Host or module map currently registers these four Panel routes (orders/customers already evacuated to Order.Endpoints per comments). Future Party sellers routes must **remove** Host maps in the same wave (no dual ownership).
