# 02 — Dev scenario proof (TB-P06-T011-R3)

## Creation method

Legitimate **Development HTTP APIs** only (`scripts/t011-r3-return-scenario.mjs`):

1. Storefront guest cart → checkout → sandbox payment → **Paid**
2. Auto fulfillment handoff (in-process outbox)
3. Seller fulfillment lifecycle: Processing → Packed → Shipment → Tracking → Dispatch → **Deliver**
4. Customer return POST (after customer modal capture) → **Requested**

**No cross-module SQL. No direct DB mutation. No forced React modal state.**

## Recorded IDs

| Entity | ID |
| --- | --- |
| Customer actor | `aaaaaaaa-aaaa-4aaa-8aaa-000000000009` |
| Seller party (فروشگاه آرمان) | `01a030d1-40cb-7000-8abe-6d31739956c5` |
| Seller actor | `01a03628-3f68-7000-844d-99f1cadb54b0` |
| Checkout / paid order | `01a0408a-be00-7000-94a1-db0d82532d27` |
| Seller order | `01a0408a-be05-7000-a0c3-6d03a887e6b7` |
| Fulfillment | `3667d5ba-d9e2-4f81-bc28-1354419288c5` |
| Shipment (Delivered) | `ab52a945-70dd-40cb-a206-040a4d9ccdf9` |
| Order line | `01a0408a-be2f-7000-837e-cb64ab2fdce5` |
| Return request | `72528d83-a924-4ce4-8d25-8fe9bba88af5` |

Artifact: `dev-scenario.json`

## Chain verified

- Paid order reference: `TB-20260827000709-01-307c5a`
- Fulfillment status Delivered (Host enum 5)
- Return status Requested (Host enum 0)
- Seller returns list contains live row before modal capture
