# RESULT — TB-SHIPPING-STOREFRONT-READ-FAILURE-INTEGRITY-230-R1

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-STOREFRONT-READ-FAILURE-INTEGRITY-230-R1
Parent: TB-SHIPPING-STOREFRONT-DESTINATION-CONFIGURED-TRIAL-230
Branch: main · Base = issue commit `6d0a506b2` (fast-forward only)
Status: **EXECUTED — one real failed-refresh defect repaired + two display fixes**, not accepted, no user
Visual ACCEPT. STOP_FOR_ARCHITECT · Automatic-Next-Task NONE.

## A — the defect and its repair (same existing refresh path, no new fetch framework)

The measured defect, exactly as the task described it: `refreshProjection` kept the last successful projection
in state, hid it only through one `capturedDestinationPending` boolean, cleared that boolean in `finally` on
BOTH success and exception, let the outer `catch` only set `error`, and advanced `appliedDestinationKey`
BEFORE the response — so a failed refresh for a new destination re-showed the previous destination's money and
could not be retried at all.

The repair keeps ONE coordinator and makes the identity explicit. In
`src/frontend/app/storefront/storefront-shipping-destination-refresh.ts`:

```
DestinationRefreshSnapshot { confirmedKey, requestedKey, failedKey, inFlight }
createDestinationRefreshSnapshot()
beginDestinationRefresh(snapshot, key)      // requested only; never confirmed
completeDestinationRefresh(snapshot, key)   // ignored unless requestedKey === key; clears failedKey of THAT key
failDestinationRefresh(snapshot, key)       // ignored unless requestedKey === key; records the failed key
canRetryDestinationRefresh(snapshot, key)   // !inFlight && failedKey === key
resolveDestinationPreviewMode(snapshot, currentKey) → "current" | "pending" | "failed"
planDestinationRefresh({capturedActive, snapshot, destinationKey, destination})
  → skip when legacy OFF, when in flight, when the key is already confirmed, or when the key already failed
  → request { key, destination (null when the identity was cleared), preserveTypedAddress: true }
```

`storefront-shipping.tsx` now owns `destinationRefreshRef` (synchronous decisions) plus
`destinationRefresh` state (render) committed through one helper; the old `appliedDestinationKey` ref and the
`capturedDestinationPending` boolean are gone. `refreshProjection` takes `options.destinationKey`: a tracked
call begins the identity, the SAME generation gate that guards `setProjection` also guards the completion, and
failure commits the failed latch instead of only writing `error`. Every outcome is keyed, so a stale success
**or** a stale error cannot mutate the current request (`complete/fail` return the input snapshot untouched
when `requestedKey !== key`). The mount call is tracked for the empty identity, so the first honest
`destination_required` answer is "current" for "no destination" instead of looking pending forever.

Presentation of that state stays inside the EXISTING card: `capturedPreviewForCards` returns the real preview
only when the mode is `current`; otherwise it returns the same preview with `groups: []` and one reason —
`storefront.captured_preview.destination_pending` while in flight,
`storefront.captured_preview.destination_failed` after a failure. The failed state renders one compact
existing-style line plus ONE explicit `تلاش دوباره` button
(`data-testid="shipping-captured-destination-retry"`); nothing auto-retries, and the button is gated by
`canRetryDestinationRefresh` on the synchronous ref, so a second click in the same tick cannot issue a second
request (measured below: two programmatic clicks → ONE request). Typed recipient/address is untouched:
destination refreshes pass `preserveTypedAddress: true` → `rehydrateDraft:false`, and a destination refresh
never calls `setAddress`. Legacy OFF still short-circuits: `planDestinationRefresh` returns `skip` and the
legacy card path is byte-for-byte unchanged.

## B — two presentation fixes

1. `formatExactAmountText` (`storefront-shipping-format.ts`): for `IRR`, a fraction that is **entirely zero**
   (`120000.00`, `0.00`) is dropped for DISPLAY only — `۱۲۰,۰۰۰ ریال`, `۰ ریال` — while the raw token is never
   rewritten, grouping/local digits are preserved, and an unexpected NON-zero fraction is shown exactly
   (`120000.50` → `۱۲۰,۰۰۰.۵۰ ریال`, `1234.000001` → `۱,۲۳۴.۰۰۰۰۰۱ ریال`, never rounded/truncated). Other
   currencies keep their previous decimal text (`۱۲۰,۰۰۰.۰۰ USD`), `null` stays unknown, malformed stays
   invalid. No `Number`/`parseFloat`, no FX, no business mutation.
2. The captured interval line wraps the server's own interval text in `<bdi dir="ltr">`, so `09:00–17:00` reads
   in the correct order inside RTL. The stored hours are not reversed and no calendar/slot design changed.
   Live DOM: `intervalBdiDir "ltr"`, computed `direction: ltr`, text `بازهٔ همین روز: 09:00–17:00`.

## C — focused proof

`node --experimental-strip-types --test` on the three affected storefront test files: **22/22 pass**, including
the state facts the task asked for, all driving the SAME coordinator the page calls:

- priced destination → new destination request → failure: `confirmedKey` keeps the old price's identity,
  `failedKey` is the new key, mode is `failed` (old money/dates cannot look current);
- same failed key: `canRetry` refuses any other key and refuses while in flight (the second click is a no-op),
  one retry → success moves `confirmedKey` to the new key and clears the failed latch → mode `current`;
- stale failure after a new success / stale success while another request is in flight: the snapshot is
  returned unchanged, so neither the new projection, the error nor the pending state is mutated;
- unrelated render (same confirmed key) issues no request, and legacy OFF issues none either;
- `preserveTypedAddress: true` for every destination refresh (typed recipient/address never re-hydrated);
- formatter: IRR `120000.00` → integer display with the raw token unchanged, IRR `0.00` → `۰`, IRR non-zero
  fraction preserved exactly, non-IRR decimals unchanged, `null`/malformed stay honest.

ONE `tsc --noEmit`: clean (exit 0). No backend build, no full/188/194 suite, no guard suite.

## D — mounted proof (task-owned profile, real traffic)

FE `127.0.0.1:3000` (PID 28288) and Host `127.0.0.1:5088` untouched — no rebuild, no restart, no migration, no
seed. Task-owned Chrome (CDP 9336) with the existing logged-in buyer/cart; every page a target created by this
task; no user tab read. Only frontend source changed, so the running UI already serves the new code.

1. Real destination through the existing selects (province تهران, city تهران) → priced card,
   `کرایه ۱۲۰,۰۰۰ ریال · بسته‌بندی ۵,۰۰۰ ریال` (fix B1 visible), goods `۹,۴۸۵,۷۷۱ ریال`, date strip present.
2. REAL failure: `Network.emulateNetworkConditions { offline: true }` on that target, then province changed to
   البرز through the already-loaded options. Result (DOM): `pricedCardVisible false`,
   `oldPriceVisible false`, `dateChipVisible false`, and one compact line
   «ارسال این مقصد محاسبه نشد؛ قیمت مقصد قبلی نمایش داده نمی‌شود.» with the `تلاش دوباره` button present.
   The old destination's price was NOT resurrected as current — the exact defect is closed live.
   Screenshot `screens/230r1-390-destination-failed.png` (390×844, card scrolled to `block:'center'`).
3. Network restored, ONE retry: the driver clicked the real button TWICE in the same tick and the network
   receipt shows exactly **1** projection POST carrying the exact new verified ids
   `{countryId e3451206-…, administrativeAreaId 1f8050a8-c325-59fa-959f-82d76b5b2bc5, cityId null}`,
   `provinceName "البرز"`; the priced card returned for THAT destination (country-rate applies), the retry
   control disappeared, `scrollWidth == clientWidth == 390`, card bounds `top 59 / bottom 786` in 844 with
   `intersects true`. Screenshot `screens/230r1-390-retry-success.png`.

HONEST limits of this mounted proof: with Chrome's offline emulation the blocked request emitted no
`Network.requestWillBeSent` (count 0 while offline), so the failure is proven by the resulting page state plus
the absence of any successful answer, not by a captured failing request record. Only one width (390) was
mounted, per the task. The pending state was exercised by the same code path but the screenshot pair documents
the failed and recovered states. While a destination refresh is pending or failed the card renders only the
compact notice — product lines and seller/type are not rendered during that window — but cart lines and typed
recipient/address are preserved and return unchanged with the confirmed answer (visible again in the retry
screenshot). No user Visual ACCEPT is claimed from these screenshots.

## E — untouched / not claimed

No auth, cart, draft, rate, week, pointer, assignment, publication or setup was touched. No selection/save/
commit/order/payment; the server `not_ready` guard remains. Checked-in captured-preview default stays OFF, the
local read preview stays ON with its date count, TB185 paused / default-OFF / PendingAllocation preserved.
Still unimplemented and NOT claimed: compact per-order-group immutable order evidence, server-selected revision
revalidation across selection/commit, no-historical-repricing. Known cross-owner label-map/decoder limits, the
227 mobile empty-note residual and the unrecovered 225-R1 user-tab local-edit loss remain recorded. The full
Storefront is NOT relabelled as delivered.

## F — git / time

Exact-path staging only (the three Shipping files in `git status` are pre-existing CRLF drift, never staged).
Elapsed ≈10 min (15:00 → 15:10 local) against HARD20 — inside the box in wall-clock; during the focused tests
ONE decision in the new module was corrected (an idle-snapshot comparison that would have skipped a lawful
clear-to-no-destination request) plus two test-file type/assertion adjustments. Recorded as one in-flight
correction, NOT a process PASS claim.
