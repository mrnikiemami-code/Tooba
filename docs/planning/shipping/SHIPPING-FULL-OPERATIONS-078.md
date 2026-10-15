# TB-SHIPPING-FULL-OPERATIONS-078 — planning

## Mission

Turn the already-authorized 076 diagnostic seam into something a merchant can actually read on the
existing `/admin/shipping-services` screen: one bounded dialog that explains how service/option,
destination, measured weight/count, carrier schedule, freight, packaging and the prepaid /
collect-on-delivery split add up to the final quote.

## Mission A — admin inspector (done)

- One per-service row action «بررسی هزینه و شرایط ارسال» on `/admin/shipping-services`, opening one
  bounded RTL dialog. No new route, no new page, no mega-form, no backend change, no new permission.
- Service code always originates from the clicked grid row.
- Destination scope reuses the real Geography reference picker; option identity reuses the real
  Fulfillment service/option tree.
- Explicit inputs only: currency (default `IRR`), order subtotal in IRR, measured shipment weight in
  kg converted **exactly** to integer grams through the existing 074 model, physical shipment item
  count (not cart lines), and an **optional** carrier pickup instant.
- Timezone is explicit and never guessed: the wall time is interpreted in a named IANA zone and
  converted to UTC, with ambiguous and nonexistent local times refused rather than silently chosen.
  No instant supplied ⇒ `NotEvaluated`, which is never rendered as available or bookable.
- The only backend call is the existing authorized read-only
  `POST /v1/admin/shipping-services/{serviceCode}/operational-quote`.
- Output lines stay separate: carrier freight, packaging, total service expense, payable at
  checkout, collect on delivery, payer mode, matched tier/rate scope, schedule availability with the
  matched weekday/window, and the explicit capacity disclosure
  «ظرفیت: فقط تنظیم شده؛ رزرو/اجرای ظرفیت وجود ندارد.».
- Nullable monetary parts render as «اعلام نشده», never as `0`; a genuine quoted `0` stays `0`.
- Nothing here is price authority: the dialog states that today's Checkout still runs under the
  legacy quote authority and that no amount is written to any order.

## Mission B — real Development matrix + integrity (done)

Executed against positively verified Development provenance (`dev-singlestore` → `tenant-alpha` →
`tooba_alpha`) using pre-existing approved-for-test fixtures without mutating them. Full matrix,
wire envelopes and the no-write proof are in `log-live-operational-quote.txt`, with the narrative in
`browser-ui-proof.md` and the fixture inventory in `fixture-ledger.md`.

## Deliberately not claimed

- No Checkout cutover, no customer price authority, no Order/Payment write.
- No capacity enforcement or reservation.
- No carrier pickup window described as a customer delivery promise.
- No live 1999/2000 tier boundary claim (no active weight tier exists in Development today).
- No live charged-packaging claim (no active packaging policy exists in Development today).

## Deferred to 079/080

Actual safe integration gating and final readiness remain out of scope here; this task only makes
the diagnostic readable and proves it honestly against real Development data.
