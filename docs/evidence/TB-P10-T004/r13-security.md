# TB-P10-T004-R13 — Guest security

| Probe | Result |
| --- | --- |
| New Active Cart secret on old checkout GET | Host TryGet fails → 403 `checkout.access.denied` |
| Committed proof used as cart mutation | Proof is not written to active session; AddToCart uses new Active Cart only |
| Wrong checkout proof for Order B | Map keyed by checkoutId; A ≠ B |
| paymentId / checkoutId alone, no secret, guest | GetOwned null → access denied |
| Cross-store `storeKey` ≠ storefront | proof ignored |
| Auth path | Host session; guest proof not required; guest proof not leaked into auth composer branch |

payment/order id is not authorization.
