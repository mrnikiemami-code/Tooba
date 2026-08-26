# 23 — Original vs Tooba Purchase Map

| Shopeiva | Route | Tooba | Binding |
| --- | --- | --- | --- |
| CartClient + Hero/Items/Summary/Coupon/Bottom | `/cart` | `storefront-cart.tsx` + `/cart` | Live Cart lines via OfferId |
| ShippingClient + Hero/Form/Summary | `/shipping` | Combined in `storefront-checkout.tsx` step «ارسال» | AddressBook + guest fields → Checkout snapshot |
| PaymentClient + Hero/Methods/Summary | `/payment` | Payment panel in checkout + `/order/confirmation` pay CTA | Host payment initiate only; no fake PSP |
| Payment success card | `/payment?payment=ok` | `storefront-order-confirmation.tsx` | `paymentState` from Host; Paid not fabricated |

Accent: Shopeiva `#E53935` → Tooba `#2563EB` (MINOR TECHNICAL DEVIATION, locked with T017–T020).

Flow sequence preserved: Cart → Shipping/Address → Payment handoff → Confirmation.
