# User requirement — configurable receiving time slots — 2026-10-28

User corrected the interpretation explicitly: "نه منظورم اینه ساعت داشته باشد9تا10 10تا11 همینطور البته با تنظیمات".
This supersedes the earlier assistant interpretation of manually named morning/afternoon choices. No screenshot/visual-worker task is authorized or required; the user-provided image was inspected by root only.

## Required customer behaviour

Each shipping method/owner has receiving-day opening/closing hours and an explicitly configured slot duration.
Example: authored09:00–17:00 with60-minute slots =>09:00–10:00,10:00–11:00,...16:00–17:00.
After choosing an eligible date, customer chooses ONE slot from THAT method's own server-generated options.
A different slot duration in settings changes future offered slots; no hardcodedonehour/frontendsplit or cross-group sharedchoice.
Each group keeps its own date/slot, selected interval is sent and revalidated by server and retained in its immutable Order evidence.
Changing date/method/currentcapture/slotconfiguration invalidates only stale confirmations as appropriate; old Order facts remain unchanged.

## Source-confirmed gap

Reviewed source581c5f31aa949e92e410977dab8bab1bbefa01a8 (R7 issuance, existing app source).
Admin AuthoringWeekEditor has one start/end pair per day. ShippingAuthoringScheduleDayWrite + WeeklyScheduleReplaceRequest have no slot-duration field.
Current captured storefront displays date chips plus one intervalText line, not a selectable receiving slot.
Legacy ShippingOperatingSchedule Contracts allow windows but are a DIFFERENT operating configuration, not authority to silently reinterpret the selected immutable authoring capture.
Current final selection/evidence compares selected day opening/closing ticks; adding frontend buttons alone cannot deliver smaller lawful selected intervals end-to-end.

## Implementation integrity requirement, not an implementation claim

Duration must be explicit owner/method configuration, persisted/captured along with the SAME receiving schedule and interpreted by the SAME Shipping calendar authority.
No default60 inferred for old/unconfigured captures, no invented capacity/cutoff/preparation or fixed morning/afternoon labels.
Legacy captures/orders remain readable and their exact intervals are not silently rewritten or divided on read.
Calendar must retain available-date-count semantics (distinct dates, not count of slots), timezone and closed-day rules.
New selection needs exact server date+slot identity, amount/context echoes and final same-slot revalidation; compact Order accepted receiving date/open/close ticks remains the stored historical truth, no full scheduleJSON per order.
Validate duration and full slot containment; do not silently round hours, spill beyond close or invent a partial final slot. If a remainder policy is needed it must be deliberate, not assumed by a browser.

## Current execution state

This is a NEW clarified user requirement, NOT already implemented or accepted.
The already-issued240-R7 only fixes honest date selection, missing interval display and disabledContinue reasons; its no-backend-calendar scope is insufficient to claim configurable hourlyslots.
Do not broaden an already-running worker silently or fakehourlychoices to reportR7PASS. Preserve its current task and user-work safeguards.
At next Architect integration, treat configurable hourly receiving slots as OPEN product work; no automaticnewtask is issued by this documentation commit.
Original completion deadline remains MISSED; do not invent a new date or claim overallcompletion while this requirement is open.


## CORRECTION — user says this is a lost existing capability, 2026-10-28

User: "من قبلا داشتم انتخاب زمان تحویل الان ندارم". Classify missing delivery-time selection as a USER-REPORTED REGRESSION, not merely a new request or optional enhancement. The earlier section's wording "NEW clarified user requirement" was an inaccurate classification and is superseded here; the observed gap is not accepted delivery.

Root verified source4bce0a8b66490643b179180c176d9c2ac17264b3: existing GroupSchedulePicker in storefront-shipping-group-cards.tsx renders schedule.timeWindows as role=radio choices via onSelectWindow, while CapturedMethodRow offers dates and only intervalText. This proves the feature exists in the legacy UI and is missing in the new captured UI. It does NOT independently prove what exact old hourly-duration policy was configured; do not invent that history.

Restore equivalent customer ability in the captured authoritative path: choose eligible day then one server-authorized receiving slot (e.g.09–10,10–11) derived from explicit method settings. Do not revert to legacy mutable quote/price logic, merely show selectable9–17 as a substitute for configured hourlyslots, or add fake browser-only intervals rejected by finalacceptance. Duration configuration remains the user's clarified required behaviour.

No screenshot/visualworker task; user mention of Digikala is an analogy, not proof of this product's implementation. No new deadline allowance and no automatictask created by this correction.240-R7 remains the active issued task for datechoice/disabledreason; its limited repair must not be called completion of this regression unless actual date+configuredslot selection reaches persistence.
