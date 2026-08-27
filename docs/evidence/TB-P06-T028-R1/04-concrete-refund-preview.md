# 04 — Concrete refund preview URLs

## Customer Order / Return entry

```text
http://localhost:3000/customer-panel/orders/01a0451c-f1f8-7000-b291-db065075733f
```

(Also browser-paid order: `…/orders/01a0451c-fabf-7000-99f0-30d410a58638`)

## Seller Return / Refund operation

```text
http://localhost:3000/vendor-panel/returns/e848ec42-0a0f-4b99-a85e-78444033b21e?sellerPartyId=01a030d1-40cb-7000-8abe-6d31739956c5
```

Dev actor: pick «اپراتور آرمان» or set `tooba.sellerActorUserId=01a03628-3f68-7000-844d-99f1cadb54b0`.

## Customer Wallet after refund

```text
http://localhost:3000/customer-panel/wallet
```
