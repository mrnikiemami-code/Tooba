# 08 — Shopeiva comparison

Task: TB-P06-T026-R1

## Runtime

- Shopeiva kept at `http://127.0.0.1:3001` (HTTP 200)
- Comparison routes:
  - `http://127.0.0.1:3001/user-panel/wallet`
  - `http://127.0.0.1:3001/user-panel/gift-cards`

## Captures

- `captures/08-shopeiva-wallet.png`
- `captures/08b-shopeiva-gift-cards.png`

## Notes

Shopeiva user-panel routes render shell/skeleton without authenticated demo session (Login CTA). Source geometry contract remains locked from:

- `reference/shopeiva/.../dashboard/wallet/wallet.jsx`
- `reference/shopeiva/.../userGiftCards/*`

Tooba Customer Wallet/Gift UI preserves `#E53935` hero, rounded cards, redeem form geometry; **does not** port fake deposit/bank-card actions (hidden/replaced with honest ledger copy). No foreign finance dashboard introduced.
