PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-RETURNS-SHOPKEEPER-PLAIN-LANGUAGE-WORKFLOW-AND-REVIEW-280
Parent-Task: TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Mode: RETAIL_OPERATOR_PLAIN_LANGUAGE_AND_CONTEXTUAL_UI
Expected-Base-SHA: 9aaf5bcd486ed63271ef431be20b7e24e8bb1951
automaticNextImplementationTask: NONE

## Goal and fixed boundary

The owner rejects a developer-oriented product experience: the software is sold to shopkeepers who should need no technical knowledge or developer explanation. Deliver a comprehensible Returns workspace in ONE task. Backend correctness was accepted in 279-R3; keep that acceptance and all write/financial/quantity/custody/replay boundaries unchanged.

Read docs/product/OPERATOR-UX-ACCEPTANCE.md (TOOBA-UX-001), AGENTS CURRENT and the mandatory repository frontend feature skill/architecture standard. Read the backend feature skill ONLY if the bounded read-only capability correction below is necessary.
The exact full task is already committed to docs/ai/tasks/TB-RETURNS-SHOPKEEPER-PLAIN-LANGUAGE-WORKFLOW-AND-REVIEW-280/TASK.md and docs/architecture/evidence/TB-RETURNS-SHOPKEEPER-PLAIN-LANGUAGE-WORKFLOW-AND-REVIEW-280/TASK.md; issuance-receipt.json pins identical bytes. Use it if Bridge is empty/formatted differently; never reconstruct or overwrite the canonical envelope.
Expected base must exist/be ancestor. Following docs-only issuance is permitted after verifying no source/config/schema delta. Record ACTUAL claim HEAD.
One Cursor worker/task/result, no delegation/screenshots/polling loop, no rebase/amend/force/reset/stash, explicit-path normal commits/push.

## Source-confirmed UX defects / user reference

User-provided screenshot: 127.0.0.1_3000_admin_returns_01a1eefb-ea96-7000-838d-9a0b37661c8b.png.
Observed case: requested1, received0; UI still offers "ثبت برنامهٔ تفکیک اقلام", opens a modal with maximum0 in both condition fields, immediately displays red errors, calls required line reason "اختیاری", and shows "تصمیم بازبینی‌شده"، "فقط افزودنی"، "برنامهٔ تفکیک (نسخهٔ0) — ناقص" plus empty technical sections.
Source audit:
- admin-return-workspace.tsx: action label103; resolutionOffered758 only checks actionCodes; technical section1028 onward; modal1647 onward; optional reason1747 despite the owning writer requiring a meaningful line reason.
- ReturnWorkPolicy canPublishResolution checks Receiving.Lines.Count>0 (requested summary lines exist even with zero received), and !effectiveResolution.IsComplete. Thus the UI offers an irrelevant zero-ceiling form, while the default fully received baseline can hide the route to an otherwise lawful partial review.
This is an OPERATOR UX increment, not a new domain architecture or re-opening the accepted financial repairs.

Primary review URL:
http://127.0.0.1:3000/fa/admin/returns/01a1eefb-ea96-7000-838d-9a0b37661c8b
Read its current state first; the user may have progressed it since the screenshot. Never reset it to reproduce the picture.

## Architecture/product decision

PLAIN_BUSINESS_LANGUAGE_ONE_CLEAR_NEXT_ACTION_CONTEXTUAL_REVIEW_OF_REAL_RECEIVED_GOODS_AND_PROGRESSIVE_HISTORY_ON_THE_EXISTING_RETURNS_WORKSPACE_WITH_ACCEPTED_OWNER_GUARDS_PRESERVED

Simplify what the operator sees; retain exact stored facts and authoritative wire commands. Do not rename public tokens/enums/routes/schema or remove backend guards to make a page look easy. No speculative new functionality.

## A. Everyday language and clear main page

1. A shopkeeper must answer without a manual: what happened, what remains, which button to press, and whether pressing it moves money.
2. Use short everyday Persian labels consistently across workspace, action dialogs, queue-facing labels, notices and relevant Returns error mapping. Prefer:
   - "دریافت کالا"
   - "بررسی مرجوعی" / "ثبت بررسی کالا"
   - "تأیید مرجوعی" / "رد مرجوعی"
   - "بازگشت وجه"
   - "تحویل کالای ردشده به مشتری"
   - "سابقهٔ بررسی" for meaningful history
Remove routine operator copy containing "برنامهٔ تفکیک"، "نسخهٔ تصمیم"، "نسخهٔ بررسی‌شده"، "فقط افزودنی"، raw error/GUID tokens and storage/idempotency/lock implementation explanations. Keep numeric revisions/keys internally for safety, not as a concept the merchant must learn.
3. One visually primary NEXT action appropriate to the authoritative stored stage. Related secondary actions may remain quiet and understandable, never a competing wall of technical buttons. Do not invent an executable permission.
4. Show own product + requested/received quantity + unit and the useful remaining work. No first-item unit shortcut or false zero/unknown. Show requested/approved/refunded money accurately with business labels, separate from goods state.
5. Empty technical decision/resolution sections before any review must not dominate the page. Use concise empty state where useful; move real audit/attempt/receipt detail behind accessible "سابقه" disclosure. Keep important outstanding money/custody/blocker information visible. Never hide uncertain money as completed.

## B. Correct stage visibility, not a zero-ceiling form

1. For coherent zero receiving, render "هنوز کالایی دریافت نشده است." and the lawful primary "ثبت دریافت کالا". No resolution/review form with both condition ceilings0. An action disappearing must not erase its retained possibly-sent intent.
2. For positive recorded receiving, offer the appropriate review of the ACTUALLY received quantities, including lawful partial receiving plus unreceived rejected remainder.
3. For a fully received line, preserve the simple whole-case approve/reject path. The operator must also be able to review a LAWFUL partial acceptance of received goods without fighting the implicit all-accepted baseline.
4. A minimal change to ReturnWorkPolicy's READ-ONLY CanPublishResolution/action projection is authorized if needed to align these affordances: positive coherent receiving for meaningful review, and publish/edit only in the existing lawful Requested/no-effective-decision/no-attempt/no-disposition window. Both queue/detail must consume the same description. Do NOT change PublishResolutionAsync, approval/receipt/refund/stock/custody write rules or monetary authority.
5. If an unusual stored case needs a zero-received withdrawal/rejection route already lawful on the server, keep it clearly distinguished as rejecting a request; do not expose the zero-ceiling quantity review as the routine path. Do not create a new rejection permission/endpoint.

## C. Simple review dialogs with honest existing commands

1. Keep ONE existing shared DS Dialog for the selected action. No inline expanding action forms/private overlay/native prompt.
2. Receipt: own product/unit, received usable/damaged quantities, clear maximum. No guessed condition/quantity, no automatic filling to max, exact owning unit quantum and shared ceiling preserved.
3. Review: show only conditions with real received quantity. Ask in business terms how much of that condition is accepted for return; zero is a deliberate valid answer. Show the declined received remainder and actually unreceived remainder as a clear summary. Do not ask the user to understand five backend portion fields, BUT continue sending all five exact fields derived from deliberate accepted portions + own stored receipt facts. No usable-first accepted-condition inference.
4. Required reason must be labeled required and validated BEFORE storing/sending. The current "اختیاری" label is wrong for a resolution line. For actions with no required reason, do not add one arbitrarily. Do not fabricate a reason/receiving fact to bypass the owning validator.
5. No red validation errors on opening an untouched dialog. Show inline field feedback after relevant touch/blur or attempted confirmation; disable/refuse invalid sends. Use short corrective wording, e.g. "بیشتر از مقدار دریافت‌شده وارد نکنید." Display "حداکثر:۱ عدد" or the line's actual fractional unit, not explanations of decimal precision.
6. Keep approve decision-only and separate deliberate refund with actual amount/destination. Existing resolution publish stores review quantities; it must not silently approve or start money. Its success copy/next action must honestly explain any remaining approval step without exposing a technical "plan publication" concept.
7. Do NOT chain several server writes behind one friendly button or rebuild a new workflow engine. Keep existing reviewed intent/action ownership and frozen command versions/keys untouched. Never clear pending evidence on close or hide the unresolved action card because the new presentation gate changed.
8. Uncertain results: short honest merchant language ("نتیجهٔ اقدام قبلی هنوز مشخص نیست.") and exact existing lawful next action. State/version conflicts should explain that information changed and needs review, not teach revision numbers. Preserve all underlying typed fault classification and exact-body retry; no new no-effect tokens.
9. Template feedback uses the existing ONE react-toastify container; inline field errors remain inline. One toast per command outcome, never on GET/render.

## D. Retail-quality layout and focused acceptance

- 390 and1440 widths: no document horizontal overflow; own card/form scrolling; >=44px new controls; primary action visible without horizontal grid scrolling. RTL, Persian digits/Jalali dates, actual unit labels.
- Reduce needless vertical blank cards/repeated long instructions. Put the next step near case status. Short readable labels, adequate spacing; don't replace complexity with tiny text/tooltips.
- Source changes stay in Returns UI helpers/components and the minimal read-only capability projection if necessary; no global theme/navigation rewrite or new modal/toast library. Follow existing ownership standard; no giant unrelated extraction wave.

Required meaningful mounted/read-model cases:
T1 requested1/received0: clear waiting-for-goods copy + receive primary; no quantity-review button/modal; opening receipt has no untouched red errors; cancel sends/stores nothing.
T2 received3 usable / accept2: review reads "بررسی مرجوعی", bound quantity input, required reason, exact five-field wire body; no technical plan/revision vocabulary; review itself sends no refund.
T3 received usable2/damaged1: each condition's own ceiling; partial/fractional unit and Persian input remain exact. Over-ceiling/finer-than-unit refuses BEFORE intent/request; required reason omission refuses locally.
T4 complete receipt with lawful partial-review capability: review remains reachable through the shared description; forbidden/terminal cases never gain actions.
T5 approve then separate refund: labels and confirmation explain decision-only versus money; correct existing frozen versions/identity preserved. Custody uses ONLY rejected received quantity and explicit actual handback attestation.
T6 pending/unknown retained intent survives dialog close/remount/presentation-stage change; no duplicate send or silent clearing. Existing terminal history is read-only.
T7 history disclosure accessible, meaningful stage summary and no routine technical/empty-plan blocks; actual template toast only once.
Keep tests behavior-based; don't replace with source regex or blindly successful all-body mocks. Use real components with controlled ports/HTTP, and focused ReturnWorkPolicy facts if changed.

Run affected mounted/API/quantity suites, tsc and one FE production build after final code changes. If backend read-model changed: affected work-description facts, backend Release build and publish. No whole Returns/Host/AMSC/vitest sweep or repeated green loops. Record actual failures/corrections, no suppression to obtain green.

## E. Read-only live review, data and handoff

One canonical tooba_alpha127.0.0.1:5432 SingleStore/store-alpha, Host5088/FE3000, current config. Zero schema/migration/DDL/backfill/seed/reset/cleanup/restore, no second DB/Docker/port/settings/key/CapturedPreview change.
Do not mutate the active user case or completed history. No live POST/receipt/decision/review/refund/recovery/custody/provider/Wallet/Inventory/notification effect. No new demo case/order/hold. Live GET + DOM inspection and open/cancel dialogs are permitted, with ZERO command send; no screenshot capture.
Refresh changed served artifacts gracefully, verify BUILD_ID/chunks and health/platform/DLL if backend changed. Inspect current case and queue at390/1440 by read-only DOM/text: text/order/action visibility/errors/overflow. User decides final visual/functional acceptance; a build/mounted test is not "ready for sale" by itself.
The screenshot case may now have progressed: use its current read-only state and controlled T1 fixture; never reset it. If unavailable, report honestly and preserve the known review URL.

Persist concise TASK/issuance receipt unchanged, RESULT.md, RECEIPTS.md, ux-before-after.md, user-review.md and canonical result-contract.txt under docs/architecture/evidence/TB-RETURNS-SHOPKEEPER-PLAIN-LANGUAGE-WORKFLOW-AND-REVIEW-280/.
ux-before-after maps each user-visible defect to replacement wording/visibility, source and meaningful proof. State clearly:
Technical-Validation, Operator-UX-Review-Readiness, User-Acceptance.
No worker "user acceptance" or blanket sellable/full Marketplace readiness claim. Retain 279-R3 Architect ACCEPT and prior history unchanged; this task addresses the user-rejected presentation.
Update compact AGENTS CURRENT/recovery/START-HERE pointers to this task/result and TOOBA-UX-001. No automatic next task, no speculative new polish list.

Canonical result:
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-RETURNS-SHOPKEEPER-PLAIN-LANGUAGE-WORKFLOW-AND-REVIEW-280
Parent-Task: TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA
Status: PASS | PARTIAL | BLOCKED
Expected-Base-SHA: 9aaf5bcd486ed63271ef431be20b7e24e8bb1951
Base-SHA: <actual claimed HEAD, permitted docs-only issuance verified>
Code-SHA: <sha>
Evidence-SHA: <sha>
Final-HEAD: <sha>
HEAD-Equals-Origin-Main: <fresh fetch>
Technical-Validation: <actual focused results>
Operator-UX-Review-Readiness: <before/after T1-T7 evidence; no technical language/zero-ceiling form>
User-Acceptance: NOT_CLAIMED
Accepted-279-R3-State: PRESERVED
Wire-And-Intent-Safety-State: PRESERVED
Live-Business-Write-State: ZERO
Schema-State: ZERO
Security-Key-State: UNCHANGED_SEPARATE_PENDING_REVIEW
Review-URL: <actual current route>
Evidence-Limits: <mounted vs live/read-only geometry, current case state>
Recovery-Next-Task: NONE
Next-Recommended-Task: NONE
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW
END_TOOBA_WORKER_RESULT

Normal explicit commits/push, fresh parity, complete actual Result through Bridge only, finish claim lifecycle and STOP. No /api/results probe, next task, screenshots or loop.
STOP_FOR_ARCHITECT_AND_USER_REVIEW
END_TOOBA_TASK

