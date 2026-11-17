# Customer multi-seller partial selection — proof

Controlled owning proof (`ReturnsCustomerSelection278Tests`, 9/9) plus mounted real-component proof
(`return-request-modal-278.mount.test.ts`, 4/4). No live case, no fixture, no provider/money/stock effect.

## 1. Eligibility is the owning authority, not the order status

`GET /v1/customer/returns/eligibility/{sellerOrderId}` → `GetCustomerReturnEligibilityQuery` →
`CustomerReturnEligibilityQueryEngine`:

- the seller order's `PlacedByUserId` is compared with the authenticated customer **first**; a foreign scope is
  `return.not_owner` and an unknown scope `return.missing`, with **no** line/product fact read (facts 1–3);
- the quantity facts come from `IReturnEligibilityEvaluator` — the same evaluator the create command runs, so the read
  and the write cannot disagree;
- `ReturnableUntil` and the blocker are now per line: `window_expired` for an expired window, `nothing_returnable` for
  an exhausted ceiling, `not_delivered`/`non_returnable` from the line's own snapshot (fact 5);
- `ProductLabel`, `UnitLabel`, `QuantityDecimalPlacesSnapshot` come from Catalog/Order Contracts; an unanswered
  precision stays `null` → `unit_precision_unavailable` and the line is not selectable (fact 4).

Mounted: the modal prints «حداکثر قابل مرجوعی: 3» for a 3-piece socks line and «2» for a gram line, shows the depleted
shirt line with its own «مقدار قابل مرجوعی این قلم تمام شده است» blocker and a disabled checkbox, **nothing is checked**,
no quantity control is rendered before the customer selects a line, and no request is sent.

Partially delivered orders: the customer order page no longer requires the aggregate `Delivered` fulfillment state; it
loads eligibility per seller order and opens the modal from the eligible lines.

## 2. Creation quantum is enforced server-side before any row

`ReturnDirectory.Creation.cs` reads eligibility **after** the existing per-seller-order serialization, then per item:
remaining ceiling → `OwningUnitQuantity.IsCreationQuantityAcceptable(quantity, line.QuantityDecimalPlacesSnapshot)`.

| Input | Line precision | Result |
| --- | --- | --- |
| `0.5` | `0` (whole piece) | `400 return.quantity_unit_invalid`, **zero rows** (fact 6) |
| `0.125` | `3` (gram) | accepted (fact 7) |
| `0.0001` | `3` (gram) | `return.quantity_unit_invalid` (fact 7) |
| `1` | `null` (unanswered) | accepted — a whole piece is lawful under every unit (fact 8) |
| `0.25` | `null` | `return.quantity_unit_invalid` — no default six-decimal scale is invented (fact 8) |

The exact stored-occurrence replay is answered **before** that gate, so a committed child is never re-described as
invalid on retry (fact 9). `return.quantity_unit_invalid` is catalogued, HTTP-reachable and localized fa/en.

## 3. Two sellers, one checkout, correct goods untouched

Mounted fact 2 builds ONE checkout with two seller orders:

- seller A, «جوراب نخی», `pcs`, delivered 3, remaining 3, precision 0;
- seller A, «پیراهن آستین‌بلند», already fully returned (`nothing_returnable`);
- seller B, «چراغ مطالعه», `g`, delivered 2, remaining 2, precision 3.

The customer selects **2 of 3 socks** and the **damaged seller-B item** with the Persian input «۱٫۲۵۰». Result:

- exactly two POSTs, one per seller order (`so-a`, `so-b`), each a one-line body — `[{ol-socks, 2}]` and
  `[{ol-lamp, 1.25}]` — i.e. the exact stored decimal, not a rounded `1.3`;
- two **different** idempotency keys, so a child of seller A can never be replayed as seller B's case;
- the untouched shirt line appears in **no** request body (the correct/unselected goods stay untouched);
- one `toast.success` from the shared template, «درخواست مرجوعی ثبت شد», and the act is resolved so storage is empty.

Server side the same shape is enforced: one case is still created per `SellerOrderId` (the existing owning rule), so a
multi-seller act produces independent child cases, each independently decidable/refundable — no mixed-seller case and no
cross-seller transaction claim.

## 4. What this does NOT prove

No live customer HTTP session, no real checkout, no money/stock/provider call, no browser geometry/RTL pixel check (jsdom
mount), no seller-facing Returns surface (none exists in this app), and no legacy multi-item case behaviour — that stays
279.
