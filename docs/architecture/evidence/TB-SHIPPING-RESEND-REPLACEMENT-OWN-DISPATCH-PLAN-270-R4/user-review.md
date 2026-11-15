# User review — TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R4

Status: **history-only review** — no eligible live parcel exists, so no new actionable example is invented.

## What this repair changes for an operator

Nothing visual: the modal copy, the 268 tab strip, the single action owner and the finance-free operations view are
byte-identical in intent. What changes is *what survives an interruption*:

1. **Closing/reloading the dialog (or the browser) while «تأیید زمان‌بندی» is in flight** no longer allows a new review to
   overwrite the one that may already have reached the server. Reopening shows the frozen intent and offers only
   «تلاش دوباره با همین درخواست» — the identical request.
2. **A conflict or an authorization loss** on confirm is no longer presented as "nothing happened"; the exact intent stays
   recoverable so the server can answer it with the stored plan instead of a second one.
3. **A parcel inside a live consolidated package, or an order that was cancelled**, can no longer receive a dispatch plan
   from a direct request (only from the package's own path / not at all after cancellation), while an already-confirmed
   plan remains readable as history.
4. **Create → plan handoff** only opens the plan dialog for the parcel that operation provably produced (exact source +
   reviewed key, or the exact damaged-goods decision and its own child). A look-alike child no longer opens a calendar.

## Review URL (read-only, completed history)

`http://127.0.0.1:3000/fa/admin/orders/01a1db2f-e36e-7000-b7d3-afb2fe86e81a/operations`

This order's linked resend is `Delivered`, so the page shows completed history and **offers no «زمان‌بندی ارسال» button**.
That is the correct truthful state and is exactly why no live plan row/click-through is claimed here. The page was only
read (GET 200) during this repair.

## What is NOT claimed

- No user visual/functional acceptance, no carrier/provider/waybill/consent/financial claim.
- The behavioural proof is the mounted/command evidence listed in `RESULT.md`, not a live operator session.
- Nothing was mutated to manufacture an actionable example; user rows and the completed linked history are read-only.
