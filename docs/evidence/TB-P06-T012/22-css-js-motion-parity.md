# 22 — CSS / JS / Motion Fidelity

Task: `TB-P06-T012`

| Surface | Shopeiva source | Tooba | Notes |
|---------|-----------------|-------|-------|
| Seller wallet card | `wallet.jsx` gradient hero | `#2563EB` gradient | Brand accent only |
| Stats 3-col grid | `grid grid-cols-3` | preserved | Live totals |
| Transaction list | divide-y rows, icons | preserved | Live entries/payouts |
| Withdraw modal | fixed overlay blur | preserved | Maps to payout request |
| Deposit/cards | Shopeiva fake | **removed** | No fake financial UI |
| Admin grids | T024 DataGrid shell | `/admin/settlement`, `/admin/payouts` | No Shopeiva admin settlement source |

Hover/transition classes preserved from Shopeiva port; responsive breakpoints unchanged.
