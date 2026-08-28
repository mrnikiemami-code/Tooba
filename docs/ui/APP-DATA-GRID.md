# App Data Grid — راهنمای عملیاتی

`AppDataGrid` گرید reusable پروژه Tooba است. **Admin Products** (`/admin/products`) اولین adapter مرجع است.

## معماری

```text
AG Grid Community (presentation)
        ↓ adapter
AppDataGrid + GridServerQuery (frontend owned)
        ↓ toHostGridQuery
GridQueryRequest / GridPageResponse (BuildingBlocks)
        ↓ module policy + engine
Endpoint ماژول (مثلاً POST /v1/admin/products/query)
```

- مدل خام AG Grid به backend **نمی‌رسد**.
- CSS/تم گرید در `src/frontend/design-system/app-data-grid/theme.css` است.
- **صفحه نباید CSS گرید generic بنویسد** — فقط cell renderer دامنه‌ای.

## افزودن گرید جدید (چک‌لیست)

### Frontend

1. **ستون‌ها** — `ColDef[]` + `applyAppGridFilterHeader(col, spec)` از `app-grid-filter-header.ts`
2. **ماتریس فیلتر** — `Record<string, AppGridFilterSpec>` در همان صفحه/ماژول
3. **queryAdapter** — `GridQueryAdapter<T>` → API Host
4. **Saved Views** — `gridId` پایدار + `createHostSavedViewStore("grid.admin.orders")`
5. **عملیات سطر** — `AppGridRowAction[]` + `AppGridRowActionsCell` + `buildPinnedActionsColumnDef`
6. **سلول‌های generic** — `AppGridMediaCell`, `AppGridLinkSubtitleCell`, `AppGridBadgeCell`, `AppGridTruncatedCell`
7. **قابلیت‌ها** — `capabilities={{ search: true, ... }}` (پیش‌فرض = همه روشن)
8. **Export** — `getExportRow` + `exportHeaders` + `exportFilenameBase`

### Backend

1. **Policy** — `IGridQueryPolicy` با whitelist فیلد/عملگر (مثال: `AdminProductGridQueryPolicy`)
2. **Engine/Composer** — اجرای پرس‌وجو **داخل ماژول/Host workspace** (نه repository داینامیک universal)
3. **Endpoint** — bind `GridQueryRequest` → `policy.Normalize` → engine → `GridPageResponse<T>`
4. **اعتبارسنجی مشترک** — `GridQueryPolicyBase` در BuildingBlocks (page/search/connectors)

## APIهای کلیدی design-system

| API | مسیر |
|-----|------|
| `AppDataGrid` | `app-data-grid/AppDataGrid.tsx` |
| `AppGridCapabilities` | `app-grid-capabilities.ts` |
| `AppGridRowAction` | `app-grid-row-actions.tsx` |
| `applyAppGridFilterHeader` | `app-grid-filter-header.ts` |
| `buildPinnedActionsColumnDef` | `app-grid-pinned-actions.ts` |
| `useOverflowTooltip` | `use-overflow-tooltip.ts` |

## Admin Products (مرجع)

- صفحه: `src/frontend/app/admin/product-list.tsx`
- فیلتر matrix: `product-grid-filter-matrix.ts`
- Saved view key: `grid.admin.products` (`saved-view-store.ts`)
- Backend policy: `AdminProductGridQueryPolicy`
- Backend engine: `AdminProductGridQueryEngine`

## قوانین امنیت

- فیلدهای sort/filter باید whitelist شوند.
- `pageSize` حداکثر 1000 (policy).
- advanced filter connectors فقط `and` / `or`.
- **ممنوع:** generic SQL روی نام جدول/ستون از client.

## تست

```bash
cd src/frontend && npm run test:grid
cd src/backend && dotnet test Tooba.sln
```

## مهاجرت گریدهای legacy

گریدهای `DataGrid` قدیمی (P04) در Admin/Seller هنوز migrate نشده‌اند. برای migrate: این چک‌لیست + parity بصری با Products.
