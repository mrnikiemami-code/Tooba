# 03 — Tooba customer live modal (TB-P06-T011-R3)

Route: `http://127.0.0.1:3000/customer-panel/orders/01a0408a-be00-7000-94a1-db0d82532d27`

## Captures (real live page — no mocked DOM)

| File | Viewport | State |
| --- | --- | --- |
| `01-tooba-customer-order-before-modal-desktop.png` | 1440×900 | Order detail before action; Delivered fulfillment visible |
| `02-tooba-customer-return-modal-open-desktop.png` | 1440×900 | **Return Request modal OPEN** after clicking درخواست مرجوعی |
| `03-tooba-customer-return-modal-hover-desktop.png` | 1440×900 | Primary button hover (CDP mouseover) |
| `04-tooba-customer-return-modal-open-mobile.png` | 390×844 | Modal open mobile |

## Live data

- Product line: **پیراهن مردانه لینن** (offer `01a030d1-40f1-7000-95f6-b8efc58e2619`)
- Eligibility gate: fulfillment `Delivered` from Host API
- Modal opened via real button click — not `setReturnModal` injection

## Fidelity repair (minimal)

Host serializes fulfillment enums as numbers; `fulfillment-api.ts` now normalizes numeric status to `Delivered` so eligibility gate matches live API data.
