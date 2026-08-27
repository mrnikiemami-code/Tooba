# 03 — Customer side-by-side (TB-P06-T011-R2)

CDP script: `scripts/capture-t011-r2-visual-evidence.mjs`

## Original Shopeiva (runtime mock orders)

| Capture | File |
| --- | --- |
| Orders list desktop | `captures/01-shopeiva-customer-orders-desktop.png` |
| Return request modal desktop | `captures/02-shopeiva-customer-return-modal-desktop.png` |
| Return modal mobile 390×844 | `captures/11-shopeiva-customer-return-modal-mobile.png` |

Trigger: click **مرجوع** on delivered order (`/user-panel/orders`).

## Tooba (live Host data)

| Capture | File |
| --- | --- |
| Order detail desktop (live checkout) | `captures/05-tooba-customer-order-detail-desktop.png` |
| Orders list desktop (live Host) | `captures/06-tooba-customer-orders-desktop.png` |
| Order detail mobile | `captures/15-tooba-customer-order-mobile.png` |

Live checkout example: `01a03ef2-4d7c-7000-a47a-deee181523cd` — reference `TB-20260826164102-01-05ee1a`, Paid, real line titles from Host.

## Modal parity

| Element | Shopeiva | Tooba | Status |
| --- | --- | --- | --- |
| Overlay blur + z-index | `bg-black/60 backdrop-blur-sm z-[9999]` | same pattern in `ReturnFormModal` | MATCH (accent differs) |
| Sticky header + Package tile | yes | yes (`#2563EB` tile) | MATCH structure |
| Amber eligibility banner | yes | yes | MATCH |
| Reason dropdown | `returnReasons` | `RETURN_REASONS` | MATCH labels |
| Description min 10 | validated | validated | MATCH |
| Success step | yes | yes | MATCH |

**Eligibility gate (live):** Tooba modal opens only when fulfillment status is **Delivered** — no fake modal without eligibility. Dev DB had zero fulfillments for sample checkout at capture time; modal structure verified against Shopeiva runtime PNG + source map (R1/R2).

## Repaired deviations

None required in this Task (evidence-only).
