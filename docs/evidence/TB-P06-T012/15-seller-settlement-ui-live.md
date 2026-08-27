# 16 — Vendor wallet UI (TB-P06-T012)

## Files

- `src/frontend/app/vendor-panel/vendor-wallet-ui.tsx` — main component
- `src/frontend/app/vendor-panel/wallet/page.tsx` — route wrapper

## Shopeiva port

Source reference: `../SarvNewVerRequirment/reference/shopeiva/src/components/vendor/panel/wallet/wallet.jsx`

Structural fidelity:

- Balance hero card with wallet icon
- Transaction list with credit/debit indicators
- Withdraw / payout modal flow
- Persian digit formatting (`toPersianDigits`)
- Status chips for payout requests

## Tooba adaptations (intentional)

| Shopeiva feature | Tooba behavior |
|---|---|
| Deposit / top-up | **Removed** — no fake deposit |
| Bank card management | **Removed** — no placeholder cards |
| Accent color | Tooba blue `#2563EB` / dark `#1D4ED8` |
| Data source | Live `settlement-api.ts` Host calls |

## Component API

`VendorWalletUi({ sellerPartyId })` — loads balance, entries, payouts on mount; refresh after payout submit.

`VendorWalletPageClient` reads seller party from `readSellerPartyId()`.

## States

Loading spinner, empty ledger message, API error message — all honest; no seeded demo transactions.
