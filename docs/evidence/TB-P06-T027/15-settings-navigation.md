# 15 — Settings navigation integrity

| Surface | Href | Nav live |
|---|---|---|
| Customer profile | `/customer-panel/profile` | `live: true` in `customer-panel-shell.tsx` |
| Customer settings | `/customer-panel/settings` | `live: true` |
| Vendor settings | `/vendor-panel/settings` | `live: true` in `vendor-shell.tsx` |
| Admin settings | `/admin/settings` | `live: true` in `admin-shell.tsx` (was false) |

## Tests
- `app/customer-panel/panel-nav-integrity.test.ts`
- `app/vendor-panel/panel-nav-integrity.test.ts`
- `app/admin/admin-nav-integrity.test.ts` (new)

## Dead links
None for settings/profile surfaces in this scope.
