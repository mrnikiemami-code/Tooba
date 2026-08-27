# 15 — Browser proof

Task: TB-P06-T024-R1

## Runtime ports (dev)

| Service | URL |
|---------|-----|
| Tooba Frontend | `http://localhost:3000` |
| Original Shopeiva reference | `http://localhost:3001` |
| Bridge | `http://localhost:17321` |
| Tooba Host (API) | `http://localhost:5088` — **restart Host after backend changes** |

Health: `GET http://localhost:5088/health/live` and `/health/ready` → 200 when running.

## Intended verification URLs

### Admin Access Control

- Platform ACC: `http://localhost:3000/admin/access-control`
- Real Category scope selector in role editor + effective preview tab
- Search categories, select, save role permissions

### Admin Seller Access (ceiling + delegation)

- Seller list: `http://localhost:3000/admin/sellers`
- Per-seller ACC: `http://localhost:3000/admin/sellers/{sellerId}/access-control`
- Ceiling tab with Category ScopeEditor

### Seller Access Control

- `http://localhost:3000/vendor-panel/access-control`
- Roles tab + ScopeEditor + Users assignment + Effective preview

### Restricted Seller Orders

- `http://localhost:3000/vendor-panel/orders` — scoped employee context (dev actor headers / seller context switcher in vendor shell)
- Mobile order visible; Books absent
- `http://localhost:3000/vendor-panel/orders/{mobileOrderId}` — detail OK
- `http://localhost:3000/vendor-panel/orders/{booksOrderId}` — expect error/403 from API

### Shopeiva comparison

- Access Control visual reference: Shopeiva vendor settings / customers patterns on `:3001`
- Seller Orders list geometry unchanged on Tooba — compare side-by-side

## Capture status

| Capture | Status |
|---------|--------|
| Desktop screenshots | **Not captured in this evidence pass** |
| Mobile viewport ACC | **Deferred** |
| Side-by-side Shopeiva diff | **Deferred** |

## Proof substitute

- API + integration tests (`AccessControlRuntimeScopeTests`, foundation tests)
- UI wiring verified in source: `scope-editor.tsx` → `access-control-center.tsx`
- Nav capability fetch in `admin-shell.tsx` / `vendor-shell.tsx`

Browser capture to be completed during `19-final-runtime.md` USER-PREVIEW session.
