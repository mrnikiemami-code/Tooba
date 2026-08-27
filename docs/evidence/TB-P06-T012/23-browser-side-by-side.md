# 23 — Browser Side-by-Side

Task: `TB-P06-T012`

Captures under `docs/evidence/TB-P06-T012/captures/` (Chrome CDP headless):

| File | Surface |
|------|---------|
| `01-shopeiva-vendor-wallet-desktop.png` | Original Shopeiva vendor wallet |
| `02-tooba-vendor-wallet-desktop.png` | Tooba `/vendor-panel/wallet` live |
| `03-tooba-admin-settlement-desktop.png` | Tooba `/admin/settlement` |
| `04-tooba-admin-payouts-desktop.png` | Tooba `/admin/payouts` |
| `05-tooba-vendor-wallet-mobile.png` | Tooba wallet mobile |
| `06-tooba-admin-settlement-mobile.png` | Tooba admin settlement mobile |

Script: `scripts/capture-t012-settlement-evidence.mjs`

Notes:
- Seller wallet may show honest empty/missing account until first marketplace payment accrual.
- Admin grids bind live Host APIs (empty arrays allowed; no fake KPI).
- Shopeiva wallet still shows demo deposit/cards; Tooba intentionally omits fake finance actions.
