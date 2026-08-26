# 20 — Original vs Tooba seller map (TB-P05-T023)

| Shopeiva | Tooba after T023 | Notes |
|---|---|---|
| Sticky 65px header + store badge | Yes | Accent blue `#2563EB` |
| Collapsible `w-64` sidebar | Yes | Desktop |
| Mobile `w-[280px]` drawer | Yes | `data-testid=vendor-panel-drawer` |
| Nav order (11 items) | Yes | Full list |
| Dashboard density + quick actions | Live cards + quick links | No fake charts/revenue |
| Products table | Offer DataGrid | Architecture preserved |
| Orders | Live seller orders | Isolation preserved |
| Settings/profile | Route present | Honest unavailable |
| Wallet/etc. | Routes present | Honest unavailable |

Runtime Shopeiva captures (`02`–`07`) may show login/storefront gate without vendor auth; source inventory remains authoritative for structure.
