# 02 — Admin ↔ Shopeiva visual source map (TB-P05-T024)

Shopeiva has **no dedicated Admin shell**. Visual authority:

| Admin pattern | Shopeiva source | Notes |
|---|---|---|
| Sticky 65px header + brand badge | `vendor-panel/layout.jsx` header | Shield icon + «مرکز عملیات توبا» |
| Collapsible `w-64` sidebar | same layout aside | Grouped Admin IA (ops/market/moderation/system) |
| Mobile `w-[280px]` drawer | vendor mobile drawer | Not a seller clone — Admin labels |
| KPI cards / quick actions | `vendor/panel/dashboard/dashboard.jsx` | Live Host counts only; no fake charts |
| Status badges / order density | vendor orders list | Admin orders Grid |
| Form / card language | vendor settings / product forms | Product workspace cards |
| Table density | Shopeiva listing/dashboard cards + Tooba Data Grid | Grid is Tooba foundation |
| Accent | Shopeiva `#E53935` | Tooba `#2563EB` (MINOR TECHNICAL DEVIATION) |

Do not invent a separate Admin design system beyond this map.
