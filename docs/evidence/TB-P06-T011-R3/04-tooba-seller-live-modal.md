# 04 — Tooba seller live modal (TB-P06-T011-R3)

## List

Route: `http://127.0.0.1:3000/vendor-panel/returns?sellerPartyId=01a030d1-40cb-7000-8abe-6d31739956c5`

Capture: `05-tooba-seller-returns-list-with-row-desktop.png` — grid contains return `72528d83-a924-4ce4-8d25-8fe9bba88af5`

## Review modal

Route: `http://127.0.0.1:3000/vendor-panel/returns/72528d83-a924-4ce4-8d25-8fe9bba88af5?sellerPartyId=01a030d1-40cb-7000-8abe-6d31739956c5`

| File | Viewport | State |
| --- | --- | --- |
| `06-tooba-seller-return-review-modal-open-desktop.png` | 1440×900 | Review modal auto-open for Requested return |
| `07-tooba-seller-return-review-modal-hover-desktop.png` | 1440×900 | Approve button hover |
| `08-tooba-seller-return-review-modal-open-mobile.png` | 390×844 | Review modal mobile |

Live data from `POST /v1/customer/returns` — no mock return object.
