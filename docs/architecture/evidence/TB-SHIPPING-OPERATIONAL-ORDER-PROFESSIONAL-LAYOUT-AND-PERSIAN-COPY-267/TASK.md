PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-OPERATIONAL-ORDER-PROFESSIONAL-LAYOUT-AND-PERSIAN-COPY-267
Parent-Task: TB-SHIPPING-EXECUTABLE-PARCEL-CREATION-AND-REAL-OPERATOR-HANDOFF-266-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: bf9aa8fccad50d984c54f7bacd8cbcea3bcfc1a6
Mode: PRESENTATION_ONLY_PROFESSIONAL_OPERATIONAL_ORDER_LAYOUT_AND_OPERATOR_COPY
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## User decision and boundary
2026-11-15 user on Order B: «الان جا نمایی ها اصلا حرفه ای نیست ui افتضاح هست این رو درستش کن واقعا نوشته ها قابل تحویل نیست بقیه اش کار کرد».
Record FUNCTIONAL USER FEEDBACK: the exercised workflow worked for user, including the visible returned/resend/delivered sequence; do not infer universal shipping/provider/financial acceptance. UI REJECTED. ONLY presentation/copy is active now. One worker/task, no delegation, no old agent resumed. Do not reopen payment gate, quantity, scheduler, return/refund/replacement/reservation/idempotency architecture or re-run their suites.

Reference exact live user page:
http://127.0.0.1:3000/fa/admin/orders/01a1db2f-e36e-7000-b7d3-afb2fe86e81a/operations?fulfillmentId=01a1db2f-eb79-7000-b084-3d6b4add0fbb&from=queue
Screenshot supplied to architect on2026-11-15 shows: giant mostly empty full-width target and recipient panels; telephone stranded at far left; minuscule dense dates/deadlines; raw Asia/Tehran repeated amid RTL dates; one long line of mixed dates+zone+transport policy; duplicate group info in top target and cards; two narrow dense parcel cards with long TRK prose; bottom finance-heavy line grid despite operations-only intent. User completed changes since previous run: source Returned and linked resend Delivered. READ CURRENT facts, don't rewind demo back to Packed.

## Deliver ONE coherent professional surface
Use existing design system/type/space/tone patterns and SAME shared items/operational-group components used by full order. No new framework/page or second action owner. Identify feature-owned files before editing; presentation-only props/composition allowed, never duplicate business logic or build frontend lifecycle.

### Layout hierarchy
1 Compact page header: «عملیات ارسال» with order reference as secondary LTR text; existing back-to-queue and full-order buttons grouped,44px. No huge empty padding.
2 Compact overview for selected work target + recipient, desktop columns where appropriate; mobile stacks. Content must use space, no half-width tiny card inside full-width empty outer card. Recipient name/phone/address together, phone LTR beside its label rather than on opposite screen edge. Allow long address wrap.
3 Main operational group: readable method/status header, customer delivery and carrier handoff in clean labelled rows/columns. Preparation start / warehouse departure / carrier deadline each a structured label-value, not one technical sentence. Primary next action in consistent place, exactly one; terminal history has clear readable state.
4 Contents and parcels use deliberate proportion. A one-line product summary must not reserve half a huge empty area while parcel histories squeeze into tiny text. Prefer consistent full-width parcel cards / stacked content on mobile; keep item quantities close to product and relevant parcel. Preserve group/source/member distinctions.
5 Parcel card title / Persian status / key tracking / actions form clear hierarchy. Tracking on own LTR selectable/copyable line using existing clipboard pattern; don't embed a long TRK code twice into an amber paragraph. Source vs resend/replacement relationship expressed by concise linked human parcel label, technical identity available through explicit details. Preserve complete lineage, receipt condition, pending/recovery/case links and blockers.
6 The shared shipping-only route must respect its existing finance-free intent. Use existing view context/presentation option to show operational quantities/status and suppress price/totals/fees/payment/refund/settlement columns on this route only if currently leaked. Full order detail retains its required finance display. Do not fork the component or alter APIs/money. Product/quantity/physical actions remain available.

### Typography, language, dates
Use existing Persian font/design tokens. Important values/body should be normally readable (roughly14px where current tokens allow), secondary metadata about12px; no critical instructions/status/date at9–10px. Clear contrast, normal line height; avoid all-bold microtext. Section titles restrained and consistent.
Use app's established Persian calendar/date/number format consistently; no top Gregorian dates vs bottom Jalali for same promise. Time range isolated LTR with clear start/end, never reversed by RTL. Show human timezone label once where operationally needed, not repeated raw Asia/Tehran next to every timestamp; derive label from existing zone data, don't guess timezone or change accepted instants. Customer zone and carrier zone may differ, preserve that fact when relevant.
Rewrite operator copy in plain concise Persian, without architectural jargon/source/revision/policy/test instructions. Examples of DIRECTION, only use if supported by actual facts:
- Used healthy return capacity + linked child: «ارسال مجدد با مرسولهٔ ۲ ثبت شد.» plus linked parcel, not a paragraph repeatingTRK.
- Returned source: «دریافت‌شده در مبدأ»; resend: «ارسال مجدد»; damaged: «نیازمند تعیین تکلیف». Never call carrier return customer RMA without owning case.
- Unknown old captured history: «اطلاعات ارسال این سفارش هنگام ثبت ذخیره نشده است.» with only useful existing action; no «آزمایش را روی سفارش دیگری…» in product.
- Replace crude «کار فیزیکی باقی نمانده» with a fact-appropriate summary; don't assert delivered/obligation complete merely from zero packed remainder.
No invented data or sanitizing away business-entered service names (TB fixture names in stored names are test data, not license to strip arbitrary names). Do not alter names/accounts/settings/data to beautify screenshot.
Success is existing transient Toast once on confirmed action; existing MessageBox for blocking/irreversible decisions; field errors adjacent; contextual status/real blockers inline. No duplicate toast+banner, no long diagnostics or forced "do not close" technical instructions. Retained uncertain attempts must stay recoverable.

## Functional preservation
Keep SAME server-projected permissions/capabilities, immutable retained envelopes, exact source capacity and lineage. Do not hide a required action to make layout look cleaner, no disabled placeholder action, no change to mutation requests/handlers/validation/order of business events. Preserve correct empty/pending/fail-closed semantics. Full order and finance-free route share exactly one action owner per physical unit. No global typography/sidebar/grid sweep.
Tailwind currently may omit features/**/* scanning. Verify classes exist in actual served CSS; reuse tokens/utilities or make narrowly justified emitting fix if needed, no broad purge/design change.

## Verification: visual layout work, not another business test cycle
Existing order B is READ-ONLY: no create/dispatch/deliver/return/refund/payment/cancel/save/publication on user's exercised trial records. No new order/draft/database or cleanup. Tab/detail/accordion toggles and browser reads allowed; no screenshots as deliverable/committed artifact.
Inspect real served route desktop1440/1024 and mobile390 (360 if actual layout differs):
- clear visual hierarchy, no empty-card filler, recipient fields together, no tiny key text;
- readable uncut labels/long references, time ranges correct, no horizontal document overflow; scoped line table scroll only if still needed;
- primary actions >=44px, modal/details controls accessible, no collisions;
- Returned source plus Delivered resend relationship clear, same actual API facts before/after;
- prices/payment/refund/settlement absent from shipping-only surface, full order still displays needed fields;
- exactly one action/case/recovery owner, no functional test mutation needed.
Exercise read-only open of shared details/review surface only where safe, close without submit. Verify mobile long address/code and any existing visible blocker.
Add/update only meaningful affected mounted presentation assertions (not snapshots duplicating CSS or a giant suite). Typecheck+production build once after final change; run affected mount suite once, no backend build/test if backend untouched, no broad parity/module/stress/green repeats. Rebuild/restart ONLY FE if backend unchanged; verify served BUILD_ID/chunk/CSS geometry. User can visually accept next.

## Environment, handoff and recovery
ONE tooba_alpha5432 SingleStore/store-alpha, FE3000->Host5088; no edition/connection/port/config/flag switch, no schema/migration/backend endpoint/data writes, no provider/real money/waybill. DataProtection/CapturedPreview preserved;255 parked, Tipax deferred; parent-package business gaps not reopened.
Persist TASK verbatim docs/ai/tasks/TB-SHIPPING-OPERATIONAL-ORDER-PROFESSIONAL-LAYOUT-AND-PERSIAN-COPY-267/TASK.md and evidence TASK/RESULT/RECEIPTS/result-contract/layout-before-after-metrics/copy-changes/user-review.md under docs/architecture/evidence/TB-SHIPPING-OPERATIONAL-ORDER-PROFESSIONAL-LAYOUT-AND-PERSIAN-COPY-267/. Document actual touched files and desktop/mobile evidence; no screenshot artifacts.
Update AGENTS/START-HERE/current architect recovery: user says functionality worked, UI rejected;267 presentation only; current exercised order is not Packed anymore; no data reset or renewed payment/Returns loop. Supersede old active banners clearly.
Commit AND push normally, exact base/code/evidence/final HEAD/origin parity, clean tree, FE build/runtime. One review URL and concise list of visual changes. No user VISUAL ACCEPT claimed, no all-shipping claim, no next task proposed.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK
