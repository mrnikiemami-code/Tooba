# 12 — Shopeiva Access Control source map

Task: TB-P06-T024  
Reference root (inventory): `SarvNewVerRequirment/reference/shopeiva`  
No dedicated Shopeiva Access Control page — closest patterns used.

## Primary sources

| Shopeiva source | Pattern borrowed |
|-----------------|------------------|
| `src/components/vendor/panel/customers/customersList.jsx` | Searchable list, dense table/rows, card shell, action density |
| `src/components/vendor/panel/settings/settings.jsx` | Section cards, form controls, tabs/sections, save affordances |

Documented inventory: `docs/evidence/TB-P05-T023/01-shopeiva-seller-panel-inventory.md`

## Tooba mapping

| Tooba UI | Mapped from |
|----------|-------------|
| `AccessControlCenter` shell (`rounded-2xl`, `border-gray-200`, `shadow-sm`, search) | settings section cards + customersList search/list |
| Role list + counts | customersList row density |
| Permission groups / accordion | settings grouped sections |
| Sticky unsaved banner / primary `#2563EB` CTA | panel save patterns (accent = accepted P05 deviation vs Shopeiva `#E53935`) |
| Admin/Seller shells | existing `admin-shell` / `vendor-shell` (not foreign ACL template) |

## Comment in code

`access-control-center.tsx` header documents: visual language = Shopeiva settings + customersList.
