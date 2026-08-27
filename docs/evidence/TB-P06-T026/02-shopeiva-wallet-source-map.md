# 02 — Shopeiva wallet/gift-card source map

Task: TB-P06-T026

## Source root

`D:\Users\User\source\repos\SarvNewVerRequirment\reference\shopeiva`

## Routes (`:3001`)

| Route | Role |
|-------|------|
| `/user-panel/wallet` | Balance + fake deposit + history |
| `/user-panel/gift-cards` | Balance card + redeem + list |
| `/gift-card` | Public buy/redeem (storefront) |

## Components (customer)

| Piece | Path |
|-------|------|
| Wallet | `src/components/dashboard/wallet/wallet.jsx` |
| Gift cards | `src/components/userGiftCards/*` |

## Contract

- Accent `#E53935`, gradient balance hero, `rounded-2xl` cards
- **Fake deposit/withdraw/cards MUST NOT be ported as live behavior** — hide unsupported controls
- Bind balance/history/redeem to real Tooba ledger APIs only
