# Architect review — 242 — 2026-10-29
Reviewed GitHub source3c42be960db4b33adee2cc2f0581f60f5b5a5d70 and latest evidence/main
a1fb2f3b7a2f9afe7cc27173ce2a5156e9b7c793. Root did NOT execute the Windows runtime, tests, browser or DB.

Decision: ACCEPTED WITHIN ORIGINAL-RUNTIME RESTORATION AND NARROW UI IMPLEMENTATION; NOT full completion of
the user's own-service hourly trial, NOT user visual acceptance. Retain worker PASS as its claim; do not use it
as unconditional delivery acceptance. Active implementation NONE, no automatic243.

Source observations:
- Existing DateChipStrip/GroupSchedulePicker vocabulary reused: rounded cards,44px minimum touch height,
  readable13–14px time text, responsive grid, native sr-only radios with focus-visible state, Clock/selected Check.
- Read-only presentational picker continues to use server slots; no invented client hours/calendar.
- Important actual transport repair: saveShippingSelection now sends selectedSlotStartTicks and
  selectedSlotEndTicks. Before242 the API adapter dropped those fields even when the intent builder had them.
  Do not retroactively call earlier API-created hourly orders a proof that the browser PUT forwarded slots.
- No backend quote/calendar/owner authority changed by242. Existing login-return-to let->const is build lint only.
- New/touched delivery labels remain hardcoded FA, time range uses server ASCII text, and compact summary still
  has separate group/method lines. Configured-locale requirement from242 is NOT closed; do not certify EN/i18n.
- Range element uses truncate and mobile computed geometry/keyboard receipts are absent from committed report.
  Source improvement is not a visual-quality verdict. No screenshots requested or required.

Worker runtime/storage receipts accepted as reported, not root verification:
primary3000->5088 restored to SingleStore/store-alpha/tooba_alpha; both production outputs rebuilt; old
Marketplace primary processes stopped while trial DB/profiles retained. Backup and forward migrations reported:
D:\temp\tooba_alpha_tb242_pre.bak, ShippingReceivingSlotDuration and OrderAcceptSlotDurationFormat.
User CDP9350 session/profile/cart preserved, but its Marketplace login does not map after DB switch. New profile
tb242-chrome/CDP9351 opened; committed handoff does not include an explicit auth/me identity or chosen-user cart id,
so do not promise that it is already signed in as the original buyer.

Actual original-data authority:
User method draft01a16eeb… post/NULL, type01a16f43…, selected format1, NULL slot duration, Monday09–17 only.
No active offer assigned to that type. Preserve it; do not reassign offers or edit/publish settings by inference.
The live positive instead uses existing DEV230 post/standard/type01a1785e… selected format1:
NULL duration and authored09–17 => ONE full-day slot. It correctly does not infer60 minutes.
Selection200 revision1, payment-draft200 same window, invented ticks400 slot_unavailable.
This proves settings authority and legacy-window preservation on original DB, NOT hourly original-service readiness.

Remaining exact configuration/acceptance facts:
1. The user's intended original service must have a real product/offer assignment through its owning normal flow
   before it can be the priced customer choice. Do not substitute DEV230 without naming it.
2. Hourly choices require an explicit duration on that service plus publication. NULL must stay full window.
   Do not silently modify user's settings to60 to make a demo green.
3. User must judge the new appearance. No Visual ACCEPT inferred; missing locale/auth/DOM facts remain disclosed.
No new task just to replay owner/coupon/history campaigns. No PSP/paid/final-order claim from this preview.
Tests24? NO: this slice reports typecheck clean, intent/continue27/27 and critical-storefront19/20 (existing landing
bg-white red); do not inherit previous24/12 counts as242 execution. Original deadline MISSED;45box overrun reported
without measured elapsed value. Checked-in gatesOFF, TB185 paused/defaultOFF/PendingAllocation.
