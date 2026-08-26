# 07 — Browser side-by-side proof (TB-P06-T010-R1)

CDP captures via `scripts/capture-t010-r1-fidelity-evidence.mjs` (headless Chrome).

| Surface | Shopeiva | Tooba | Files |
| --- | --- | --- | --- |
| Customer orders | `/user-panel/orders` | `/customer-panel/orders` | `11-original-*` / `14-tooba-*` |
| Seller list | `/vendor-panel/orders` | `/vendor-panel/fulfillments` | `12-original-*` / `15-tooba-*` |
| Seller detail basis | `/vendor-panel/orders/1` | detail route (live auth) | `13-original-*` |
| Admin list | n/a (no source) | `/admin/fulfillments` | `16-tooba-*` |
| Home regression | `/` | `/` | `17-tooba-home.png` |
| Mobile seller | n/a | `/vendor-panel/fulfillments` 390×844 | `19-tooba-*` |
| Mobile admin | n/a | `/admin/fulfillments` 390×844 | `20-tooba-*` |

Shopeiva origin during capture: `http://127.0.0.1:3001`

Tooba origin: `http://127.0.0.1:3000`

Static source inspection alone is insufficient — browser PNG evidence included above.
