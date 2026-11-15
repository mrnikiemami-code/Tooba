PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-PARCEL-TABS-AND-QUIET-OPERATOR-VIEW-268
Parent-Task: TB-SHIPPING-OPERATIONAL-ORDER-PROFESSIONAL-LAYOUT-AND-PERSIAN-COPY-267
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 6f99323caa8cc341e13f7581dc8552234479bfd4
Mode: PRESENTATION_ONLY_GROUP_SCOPED_PARCEL_TABS_WITH_COUNTS_AND_PRESERVED_ACTION_RECOVERY
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## User feedback
2026-11-15: «بهتر شد حالا اگر بخش مرسوله ها تبدیل به تب بشه مثلا ارسال مجدد(1) تحویل در مبدا(1) و...دقت کن کارمند با دیدین این فرم خیلی شلوغه براش».
267 is improved per user; do not restart layout redesign. Implement ONLY tabs in shared parcel section to reduce simultaneously displayed cards. Functional user feedback remains positive, not universal/provider acceptance. One worker/task, no delegation/resumed agents, no next-task loop.

Review current read-only exercised Order B:
http://127.0.0.1:3000/fa/admin/orders/01a1db2f-e36e-7000-b7d3-afb2fe86e81a/operations?fulfillmentId=01a1db2f-eb79-7000-b084-3d6b4add0fbb&shippingGroupId=01a1db2f-e351-7000-a516-71ab5b815d1c&from=queue
Source parcel Returned + Delivered resend. KEEP facts/order state/data exactly as now.

## 1 One group-scoped parcel tab strip, existing tab appearance
In existing shared operational group's «مرسوله‌ها» subsection render one real underline tablist (like accepted authoring tabs, not blue exclusive-choice pills) and only its matching cards. Preserve267 card typography/compact timeline/recipient/financeFree layout.
Tabs are filters of projected parcels in THIS group only; counts are parcel count (not quantities/items), Persian digits:
- همه (N): every parcel in current group including cancelled history.
- نیازمند اقدام (N): parcels with current existing server-projected available actions or a retained unresolved attempt requiring reconciliation; never invent permission from status. Package-locked child gets no duplicate child action.
- در مسیر (N): Dispatched/InTransit.
- در حال برگشت (N): Returning.
- دریافت‌شده در مبدأ (N): Returned (carrier origin, not customer RMA).
- ارسال مجدد (N): durable resendOfShipmentId AND not IsReplacement. Include delivered resend, so current sample has1.
- تحویل‌شده (N): Delivered.
- لغوشده (N): Cancelled.
Created/non-delivered/unknown statuses remain accessible through All and existing NeedsAction where projected; no raw enum labels. Do not expand status taxonomy/backend.
To keep strip quiet, show All + nonzero categories, not a wall of zero-count tabs. If currently selected tab reaches0 on authoritative refresh, keep it selected with its clear empty state until user changes it; no surprise jump while reviewing. Default initial tab: NeedsAction if nonzero; else Resend if nonzero; else InTransit if nonzero; else ReceivedAtOrigin if nonzero; else Delivered if nonzero; else All. Apply initial choice once per group, not every render. Restore selected tab through ordinary parent rerenders/refetch for same group, no persistent browser storage required.
Resend is lineage, other tabs are statuses: filters deliberately overlap, e.g. Delivered resend belongs to Resend and Delivered. Counts from exact same full group snapshot, not currently filtered list. No summing tab counts into total, no duplicating parcel within one panel. Do not print a technical explanation of overlapping filters to operators.

## 2 Preserve lineage, stable parcel identities, actions and recovery
Use stable parcel IDs for keys; existing visible ordinal «مرسولهٔ۱/۲» must refer to its ordinal in full group, NEVER renumber filtered list. Counts/order/source membership stay scoped, no sibling borrowing.
Existing link «پیگیری ارسال مجدد: مرسولهٔ۲» or source-link must reach the actual counterpart even if hidden by current tab: select appropriate tab containing target, then scroll/focus existing card by stable id. No cloned card/action in source card and target list. Preserve child/source distinction from actual durable facts; replacements are not incorrectly labelled same-goods resend.
Presentation only: no API/backend changes, no source-derived action eligibility, no changed mutation payload/handler/quantity/capacity. Dialogs/retained envelope owned outside filtered card lifecycle, or remain safely mounted; switching tabs must NOT reset reviewed values, mint occurrence, dismiss pending/unknown request, retry automatically, clear attempt or hide all recovery paths. NeedsAction must include actual retained recovery obligation even after ordinary action disappears. Existing permission/viewer scope honored.
If an existing full order/orphan parcel list already has current/cancelled filtering, don't nest a second redundant tablist around it or duplicate action owner. Scope change to shared operational parcel area with same feature helper as useful; keep full order shared behavior compatible.

## 3 Readable compact RTL mobile behavior
Strip immediately under «مرسوله‌ها», count next to label, active underline/text, proper baseline. Keep required controls readable44px, no tiny badge text or wrapping tabs into a huge multi-row toolbar. At390 allow scoped tab-strip horizontal scroll with selected tab brought into view, no document horizontal overflow. Desktop1440/1024 same hierarchy. Do not add fullscreen dialog, new page/search/toolbar or redesign whole layout.
ARIA tablist/tab/tabpanel ids, aria-selected/controls/labelledby and roving tabIndex. RTL arrow navigation + Home/End through shown tabs, keyboard focus visible. Clicking tabs triggers ZERO business requests. Empty state «مرسوله‌ای در این بخش نیست.» short and clear.
If only one actual parcel category/one parcel exists, do not make operators navigate needless empty tabs; only useful nonzero filters/All, no blank panels.

## 4 Focused proof only
READ ONLY user/exercised data. No create/dispatch/deliver/return/refund/cancel/payment/save/publish or cleanup, no new order/draft/database, no screenshot artifact.
Real served browser on OrderB: All2, origin1, resend1, delivered1 (if live facts changed, use real counts and disclose). Selecting source filter shows only source, Resend only child; stable human numbers and working counterpart navigation. Snapshot content source/child/case/status unchanged, no repeated amber raw tracking paragraphs. Switch tab, refresh facts -> same tab; no new POST.390/1024/1440 no clipped label/control/document overflow, tab scoped scrolling usable.
Meaningful mounted cases: group scope and count overlap; filtered numbering/lineage cross-tab link; initial actionable priority; retained unknown attempt survives tab switch and remains discoverable; keyboardRTL; selected-empty after refresh. Use existing mount infrastructure, minimal affected tests once. No backend or business regression suite/publish if no backend changed. Typecheck/build once after final edit; FE restart/served BUILD_ID confirmed; repeated green suites not authorized.

## Delivery
Persist TASK verbatim docs/ai/tasks/TB-SHIPPING-PARCEL-TABS-AND-QUIET-OPERATOR-VIEW-268/TASK.md and evidence TASK/RESULT/RECEIPTS/result-contract/tab-counts-and-navigation.md/user-review.md under docs/architecture/evidence/TB-SHIPPING-PARCEL-TABS-AND-QUIET-OPERATOR-VIEW-268/. Update AGENTS/START-HERE/recovery:267 improved per user,268 parcel-only tabs, current user record read-only, no functional reopening. One active banner, no automatic next task.
ONE canonical tooba_alpha5432 SingleStore/store-alpha, FE3000->Host5088, keys/CapturedPreview unchanged, no schema/config/edition/port/data write.255 parked/liveTipax deferred/parent-package business gaps unchanged.
Commit AND push normally, exact code/evidence/finalHEAD/origin parity/runtime/build. One review URL and short visual checklist. WorkerPASS not Architect/userVISUAL_ACCEPT. STOP for review and user, no next task proposed.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK
