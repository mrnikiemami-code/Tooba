# 09 — Restricted employee browser + API proof

Task: TB-P06-T024-R2

## Context

Actor: اپراتور سفارش موبایل (`01a04407-47ae-7000-a6b1-43f69a17cd1a`)  
Seller: فروشگاه آرمان (`01a030d1-40cb-7000-8abe-6d31739956c5`)

## Browser

| File | Result |
|------|--------|
| `captures/06-seller-orders-employee.png` | List shows Mobile + Mixed only; Books order absent; context switcher shows Mobile Order Operator |
| `captures/07-seller-order-mobile-detail.png` | Mobile order `TB-20260827162426-01-d8a7f2` detail with line گوشی دمو موبایل / category موبایل |

Owner contrast: `captures/05-seller-orders-owner.png` shows all three orders including Books.

## API (authoritative deny)

Evidence file: `api-employee-order-scope.json.txt`

| Call | Result |
|------|--------|
| `GET /v1/seller/orders` | 200 — Mobile + Mixed only (`lineCount` 1 for mixed) |
| `GET /v1/seller/orders/{mobile}` | 200 |
| `GET /v1/seller/orders/{books}` | **403** `seller.order.view.denied` |
| Mixed detail | 200 — only Mobile authorized line (Books line hidden) |

Headers: `X-Tooba-Dev-Actor-User-Id`, `X-Tooba-Seller-Party-Id`.

## Seed fix enabling this proof

Employee Party DB membership alone was insufficient; seed now also writes `user#member@party` so `SellerPanelAccess` party#view succeeds before category order filtering.
