# TB-277-R1 — user review

## What to look at

One real case (read-only history — already exercised by you on 2026-11-16, **do not re-run the refund**):

- Queue: http://127.0.0.1:3000/fa/admin/returns
- Case: http://127.0.0.1:3000/fa/admin/returns/01a1e6b0-f33a-7000-80ee-beffb6344490

You will see a `Completed` case whose money is already recorded (`Succeeded` attempt, `2,407,839 IRR`), one line
«لنوو اولترابوک مدل 2» with unit «عدد» and `SaleableRestock` for the received piece. That is the honest stored history; the
case offers no new action because it is settled, and nothing on it was changed by this repair.

## What this repair actually changed (and cannot be seen on that settled case)

1. **A direct receipt request can no longer receive half a piece.** The server now reads the ordering unit of the case's own
   order line and refuses a quantity that does not land on that unit — `POST …/receipt` with `0.5` for a «عدد» line answers
   `return.receipt.unit_quantity_invalid` and writes nothing. A gram line with 3 decimals still accepts `0.125`, and `1`
   equals `1.000000`. A line whose unit precision the order does not answer is refused rather than assumed to accept six
   decimals.
2. **The receiving dialog no longer uses floating-point arithmetic.** `0.1 + 0.2` is now exactly `0.3`, so a legitimate
   split against a `0.3` remaining ceiling is accepted, while `0.2 + 0.2` against the same ceiling is refused before any
   request leaves. The field shows the real product, the localized unit and «باقی‌ماندهٔ قابل دریافت», and its `step`
   follows the unit (1 for pieces, 0.001 for grams).
3. **A retained uncertain action is no longer discarded by a later refusal.** Authorization denials, a missing actor, a
   permission loss, stale/already/conflict answers and every recovery/refund state code were removed from the
   "proven no effect" list: after an unknown outcome the exact reviewed intent stays visible and retryable, because a later
   refusal does not prove the earlier send did not write.

## How each was verified

- Backend unit gate: 10 focused facts through the real `ReturnDirectory` against in-memory EF plus controlled money/stock
  ports (whole pieces, 3-decimal grams, finer-than-unit refusal, unanswered metadata, exact replay after the ceiling is
  exhausted, value equality).
- Client arithmetic: 7 focused facts on `return-quantity.ts`, 26 queue-level facts and 8 mounted facts on the real
  workspace component.
- Two `ReturnsCharacterizationTests` that still assumed "approve = money" were updated to the accepted TB-277 behavior
  (approve records the decision; the separate first-refund command moves money). Declared in `RESULT.md`.
- Live look is HTTP/GET + served-chunk text only (no screenshots): Host `5088` healthy, Next `3000`
  `BUILD_ID 0RrDo7CGZ60FPsYrWxQ1a`, the served chunk carries both the unit-refusal copy and the `start-refund` route.

## Limits you should know

- No real PSP, Wallet or Inventory effect was produced; money/stock are proven with controlled ports.
- The mounted dialog suite is jsdom, not a browser; the case you can open is settled history, so no live modal is offered
  on it.
- Only the exercised «عدد» line exists live; other unit precisions are covered by focused tests.
- This is not Architect acceptance and not your functional/visual acceptance.
