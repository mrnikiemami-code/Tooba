PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-STOREFRONT-GROUP-DELIVERY-CALENDAR-CLOSURE-251
Parent-Task: TB-STOREFRONT-CART-RIAL-TOTALS-AND-SELLER-GROUPING-250
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: Tooba Storefront — restore each selected group's configured delivery calendar
Mode: REPRODUCE_MISSING_GROUP_DATE_AND_SLOT_THEN_FIX_OWNING_PATH
Expected-Base-SHA: 7d916bc7a8154f77ff2ee734f0d78c6ca54459cd
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

USER STEERING
User waited for 250 result before this work. Actual report: for ONE shipping group, selecting a delivery method does not expose the date/hour choice. This is a SHIPPING commitment-group defect, separate from cart seller sections. Restore the configured server calendar for each lawful group; no invented times. Recovery must remain current.
250 corrected IRR cart totals/grouping; preserve those changes and carrier/physical-spec work. No parallel task.

SOURCE REVIEW / WHAT IS AND IS NOT PROVEN
250 root-total path now reads totalsByCurrency. IRR formatting and sellerPartyId sections landed with recorded canonical proof.
Existing shipping card CapturedMethodRow renders time slots only for dateChosen && activeSlots.length>0; selected date with empty slots has no visible explanation. decodeCapturedSlots maps absent, non-array and malformed slots all to []. This source-level silent state is confirmed; it is NOT yet proof of the user's exact root cause.
Do not mark the report fixed merely by adding a message. Trace the actual missing group end-to-end first.

A. ACTUAL REPRODUCTION / OWNERSHIP LEDGER
On canonical user's currently Active cart, identify authenticated/guest access normally (do not reuse the expired cart or replace the buyer). Read projection and sanitized DOM for ALL groups after selecting their own method. Record:

cart/version, SERVER groupKey, seller/owner/responsibility, actual line membership;
serviceCode/optionCode/type, assigned method binding;
Selected publication snapshot/source/selection/format version and its own weekly hours/timezone/slot duration/lead-day/region eligibility;
raw availableDates/slots/counts, decoded equivalents, chosen method/date/slot state and rendered controls.
Determine whether calendar missing upstream, dropped in contract/decoder, choice assigned to another group, mismatched option/null binding, stale/pending destination, or rendering/state defect. Show the measured root cause, not a guess.
No provider credentials or Tipax HTTP necessary for Manual/Free configured paths.

B. NARROW REPAIR
Fix the owning module/component/adapter seam according to evidence. For a configured eligible group, the user's explicit method/date selection must reveal that group's server-authored dates and slots. Confirmed group keys and method identities must remain independent even if two groups share method labels/service codes.
Never borrow another group's calendar, duplicate first method data, generate hourly times in frontend, silently switch owner/type, or loosen preview/selection revision guards.
NULL slot duration keeps the server-defined full open-close window; configured duration produces only server-authorized slots. Respect closed days/lead days/timezone. Do not assume 60 minutes.
Missing/unavailable/malformed calendar must produce an owning localized reason and block Continue for that group, never a silently selected method with no date/hour explanation. Distinguish pending, unavailable and invalid wire data; keep valid groups independent where architecture permits. No technical wall of text.
Preserve name/postal/recipient edits and compatible choices through destination revalidation; no full-page navigation, repeated projection loops or disappearing confirmed choice from unrelated fields.
If lawful configuration is actually missing, report exact source setting and use only authorized owning APIs for an explicitly justified minimal change; record before/after. No fake schedules, fabricated weights/prices or bulk merchant mutation.

C. SMALL 250 REVIEW RESIDUAL — CURRENCY IDENTITY
Do not expand money redesign. Fix these two introduced currency-identity hazards while retaining IRR result:

mapStorefrontCart currently falls back to the sole totalsByCurrency row when it does not match defaultCurrency, but keeps default currency label. Never label a USD total as IRR; exact currency match or unavailable/explicit supported multi-currency presentation.
groupCartLinesBySeller currently sums all line amounts then uses first line currency. Never add different currencies into one monetary subtotal. Keep seller section but separate totals per currency, or show subtotal unavailable for mixed currency if product contract disallows it. No conversion.
Keep one authoritative subtotal resolution path rather than a second test-only helper that diverges from the actual mapper.
Retain customer integer IRR display and no fake zero totals.

REQUIRED FOCUSED PROOF

Actual mounted shipping cards with two groups: select A method/date/slot and B method/date/slot; own calendar+money/ticks rendered, no state leakage; reproduce user's failure RED -> GREEN.
Server/raw/decoded/display parity on the real cart for the formerly failing group. At least one hourly and one null/full-window fixture; unavailable and malformed slots show specific owning blocker. No invented fallback calendar.
Recipient/postal edits preserve selections; province/city refresh has one owner and bounded calls; choose compatible vs genuinely invalidated slots correctly.
If a selection write is required to prove save, prefer task-owned isolated cart; never submit user's checkout/payment. Verify exact per-group selected ticks/revision reach existing PUT and accepted evidence; no whole-cart partial save.
Currency regressions above + adjacent affected 250 tests. Relevant typecheck/build and owning server tests only if server changed; avoid full-suite replay.

CANONICAL / PRESERVATION
Final runtime D:\Users\User\source\repos\SarvNewVer FE3000 -> Host5088, SingleStore/store-alpha/tooba_alpha, actual user's current cart. No alternate final port/database/source. Do not fabricate a second environment as delivery.
Do not enumerate/read/automate arbitrary user tabs or unsaved work. Use an explicitly task-owned normal session for authorized inspection; no credential/header bypass. No screenshots/images.
No checkout/payment/carrier-create/cancel/fulfill on user orders; no cart-line mutations for repro on actual cart. Use isolated fixtures for destructive cases. No seed/reset/reseed/manual business SQL. Preserve DataProtection, gates, user data.
Validate actual base/main and preserve concurrent changes; no force/reset/stash/rebase. No migration expected; if necessary justify, private backup, normal runner and explicit readback.
Live Tipax remains deferred by user; do not request test account. Label/PDF unsupported is not part of this task.

RECOVERY / HANDOFF
Update AGENTS.md, docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md AND docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md. Record user's missing-calendar report, measured group-specific cause, exact fix/proof and any remaining gap. Do not call 250 cart grouping proof of shipping calendar correctness.
Preserve TASK verbatim under docs/ai/tasks/TB-STOREFRONT-GROUP-DELIVERY-CALENDAR-CLOSURE-251/TASK.md and evidence TASK/RESULT/RECEIPTS.
Return raw-vs-decoded date/slot counts per group before/after, actual bindings/snapshot identities, focused test results, source/runtime SHA and deployment facts. No screenshots or secrets. Worker PASS != User ACCEPT.
STOP_FOR_ARCHITECT. automaticNextImplementationTask NONE.
END_TOOBA_TASK
