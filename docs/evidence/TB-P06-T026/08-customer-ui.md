# 08 — Customer Wallet / Gift Card UI

Task: TB-P06-T026

## Routes

| Route | Behavior |
|-------|----------|
| `/customer-panel/wallet` | Live balance hero `#E53935`, ledger list, gift-card redeem — **no** deposit/withdraw/bank cards |
| `/customer-panel/gift-cards` | Wallet-derived balance + redeem form + GiftCardCredit history |

## Source binding

- Shopeiva: `reference/shopeiva/src/components/dashboard/wallet/wallet.jsx`
- Shopeiva redeem: `reference/shopeiva/src/components/userGiftCards/giftCardRedeem.jsx`
- Tooba modules: `src/frontend/app/wallet/wallet-api.ts`, `wallet-ui.tsx`

## APIs (via BFF `/api/customer/...` → `/v1/customer/...`)

- `GET /api/customer/wallet`
- `GET /api/customer/wallet/ledger?page&pageSize`
- `POST /api/customer/wallet/gift-cards/redeem` `{ code, idempotencyKey }`

## Nav

- `CUSTOMER_DEFERRED_NAV_HREFS` no longer includes wallet / gift-cards
- Live menu items: کیف پول، کارت‌های هدیه

## Forbidden UI (not ported)

- Fake deposit / withdraw modals
- Bank card CRUD
- Fake gift-card purchase list
