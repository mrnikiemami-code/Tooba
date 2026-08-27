# 10 — Checkout / order confirmation wallet UI

Task: TB-P06-T028 (frontend)

## Surfaces

| Route | Role |
|-------|------|
| `/checkout` | Shipping + note that wallet/gateway choice happens after submit; mixed tender marked DEFERRED |
| `/order/confirmation?checkoutId=` | Payment method picker (Shopeiva geometry) + pay CTA |

## Host contracts consumed (backend adds)

- `GET /v1/storefront/checkout/{checkoutId}/wallet-quote?cartId=`
  - maps: `balance`, `maxUsableAmount`, `selectedWalletAmount`, `remainingPayable`, `canPayFullyWithWallet`, `mixedTenderAvailable`
- `POST /v1/storefront/checkout/{checkoutId}/payments`
  - body may include `providerCode: "wallet"` for full-wallet pay
  - wallet success: empty/`Succeeded` → **no sandbox redirect** → `/payment/result`

## UI rules

- Wallet method shown **only** when `canPayFullyWithWallet`
- Displays balance / max usable / remaining (0 for full wallet)
- Mixed tender: **not claimed** — dashed deferred note (`payment-mixed-deferred`)
- No fake card / deposit forms
- Accent `#2563EB` on panels (Tooba); wallet summary uses violet panel like Shopeiva wallet method

## Files

- `app/storefront/storefront-payment-api.ts` (+ tests)
- `app/storefront/storefront-payment-methods.tsx`
- `app/order/confirmation/storefront-order-confirmation.tsx`
- `app/storefront/storefront-checkout.tsx`

## Preview actor

Customer Dev actor: `aaaaaaaa-aaaa-4aaa-8aaa-000000000009`  
(`X-Tooba-Dev-Actor-User-Id` / `tooba.customerActorUserId` / BFF default)
