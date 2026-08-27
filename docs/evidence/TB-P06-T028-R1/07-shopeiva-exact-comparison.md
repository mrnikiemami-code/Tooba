# 07 — Exact Shopeiva comparison URLs

Original purchased Shopeiva (port **3001**). `/checkout` returns **404** on this build; closest payment/checkout surfaces:

| Surface | Exact URL | HTTP | Gate |
| --- | --- | --- | --- |
| Payment / methods | `http://127.0.0.1:3001/payment` | 200 | may need cart session |
| Cart | `http://127.0.0.1:3001/cart` | 200 | session |
| User panel | `http://127.0.0.1:3001/user-panel` | 200 | login link present |
| Wallet | `http://127.0.0.1:3001/user-panel/wallet` | 200 | login-gated content |
| Gift cards | `http://127.0.0.1:3001/user-panel/gift-cards` | 200 | login-gated |
| Orders | `http://127.0.0.1:3001/user-panel/orders` | 200 | login-gated |

No dedicated Returns/Refund operational route found under Shopeiva user-panel; closest is orders/wallet.

Tooba live wallet checkout/refund binds on `:3000`; Shopeiva `:3001` remains structural visual reference.
