# 04 — Shared real scope editor

Task: TB-P06-T024-R1

## Component

**File:** `src/frontend/app/access-control/scope-editor.tsx`  
**Consumer:** `src/frontend/app/access-control/access-control-center.tsx` (Admin platform, Admin seller ceiling, Seller panel)

Single shared component for Admin and Seller — no duplicate selector implementations.

## UX surface (Shopeiva-derived)

| Feature | Implementation |
|---------|----------------|
| Scope type | `<select>` over `SCOPE_OPTIONS` (Global / Category / Product / Brand / deferred kinds disabled) |
| Search | Lucide `Search` + text input; triggers `loadResources(kind, q)` |
| Resource list | Scrollable `<ul>` max-h-36; click to select |
| Selected resource | Blue badge with display name; X to clear |
| Loading | «در حال بارگذاری…» |
| Empty | «موردی یافت نشد» |
| Error | Red inline message |
| Deferred kind | Amber «این نوع محدوده هنوز منبع زنده ندارد.» |
| Ceiling-disabled | `disabled` prop from parent when `disabledByCeiling` or system role |

Visual: white card, gray-50 panel, `#2563EB` accent — matches T024 Access Control Center (Shopeiva settings/customers pattern).

## Integration points in AccessControlCenter

1. **Role permissions tab** — per-permission `ScopeEditor` when `supportsScopedEditor(p)` and grant enabled.
2. **Seller ceiling tab (admin seller ACC)** — ceiling row scope editing with same component.
3. **`loadResources`** — `useCallback` → `api.listScopeResources(kind, q)` (Admin / Seller / AdminSeller APIs).

## Test hooks

- `data-testid="scope-editor"`
- `data-permission={permissionId}`

## Not used

- No foreign tree/select library
- No free-text UUID field for scope resource
