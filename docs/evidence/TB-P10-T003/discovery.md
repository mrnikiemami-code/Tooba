# Discovery — TB-P10-T003

## Classification

| Area | Class | Notes |
|---|---|---|
| `/payment` shell (red hero, steps) | real / preserved | Shopeiva structure |
| Checkout summary on `/payment` | real | Host `GET /checkout/{id}` |
| Payment methods | real (Store-enabled) | `GET /payment-methods`; gateway gated by Mode |
| Card number / CVV / expiry form | absent (correct) | Not reintroduced |
| Pay CTA + initiate | real | `POST .../payments` |
| Manual / card-to-card | real | config `ManualCardToCardEnabled` |
| Gateway Sandbox | real (Dev) | Mode=Sandbox → sandbox redirect |
| Gateway Production Webhook | partial | only when InitiateBaseUrl + secret set |
| Wallet full cover | real | quote-gated; not listed as fake option |
| COD | missing | not invented |
| Terms checkbox | missing | no legal contract — not faked |
| Order creation | already at shipping commit | payment initiates attempt only |

## Key files

- `src/frontend/app/payment/storefront-payment-handoff.tsx`
- `src/frontend/app/storefront/storefront-payment-methods.tsx`
- `src/frontend/app/storefront/storefront-payment-api.ts`
- `src/backend/Host/Tooba.Host/Storefront/StorefrontPaymentComposer.cs`
- `src/backend/Modules/Payment/Tooba.Payment.Infrastructure/PaymentDirectory.cs`
- `src/backend/Modules/Order/Tooba.Order.Infrastructure/OrderPaymentBridge.cs`
