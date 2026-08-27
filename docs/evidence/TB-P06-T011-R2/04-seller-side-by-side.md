# 04 — Seller side-by-side (TB-P06-T011-R2)

## Original Shopeiva

| Capture | Route | File |
| --- | --- | --- |
| Order detail + return banner | `/vendor-panel/orders/1` | `captures/03-shopeiva-seller-order-detail-desktop.png` |
| Return review modal desktop | click **بررسی درخواست** | `captures/04-shopeiva-seller-return-review-modal-desktop.png` |
| Return review mobile | same | `captures/12-shopeiva-seller-return-review-mobile.png` |

Mock `returnRequest.status=pending` from `public/jsons/orders.json`.

## Tooba

| Capture | Route | File |
| --- | --- | --- |
| Returns grid desktop (live Host, empty honest state) | `/vendor-panel/returns` | `captures/07-tooba-seller-returns-list-desktop.png` |
| Returns grid mobile | same | `captures/13-tooba-seller-returns-mobile.png` |

Seller context: `sellerPartyId=01a030d1-40cb-7000-8abe-6d31739956c5`, actor `01a03628-3f68-7000-844d-99f1cadb54b0`.

## Workflow parity (structure)

| Step | Shopeiva `returnDetailModal` | Tooba `ReturnReviewModal` |
| --- | --- | --- |
| Approve two-click | yes | yes |
| Reject requires reason textarea | yes | yes |
| Status badge + date grid | yes | yes |
| Approve/reject button colors | emerald/red | emerald/red | MATCH |

Live Tooba review modal capture requires an existing **Requested** return row — dev DB returned 0 rows (no fake seed).
