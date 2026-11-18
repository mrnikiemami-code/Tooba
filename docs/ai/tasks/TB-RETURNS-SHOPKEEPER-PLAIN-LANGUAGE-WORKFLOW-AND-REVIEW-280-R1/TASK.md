# TB-RETURNS-SHOPKEEPER-PLAIN-LANGUAGE-WORKFLOW-AND-REVIEW-280-R1

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-RETURNS-SHOPKEEPER-PLAIN-LANGUAGE-WORKFLOW-AND-REVIEW-280-R1
Parent-Task: TB-RETURNS-SHOPKEEPER-PLAIN-LANGUAGE-WORKFLOW-AND-REVIEW-280
Channel: tooba-main
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Mode: ONE_CONSOLIDATED_FRONTEND_UX1_UX2_UX3_CLOSURE_AND_OWNER_TEST_HANDOFF
Expected-Base-SHA: 9075e2fa4325a2de60cbd17ebfe908d9d9ee74ed
Issued-Date: 2026-11-19 (Asia/Tehran)
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW
automaticNextImplementationTask: NONE

## Owner instruction and exact objective

«پس تسک بده جمع بشه من امتحان کنم ... کلی توسعه داریم»

Close the THREE concrete TB-280 Architect findings together in this single bounded repair. Deliver the existing merchant workspace ready for the owner's next test. Do not reopen the accepted backend or turn this into a redesign, architecture campaign, or chain of micro-tasks.

The customer of this product is an ordinary shopkeeper, not a developer. Read docs/product/OPERATOR-UX-ACCEPTANCE.md (TOOBA-UX-001). Plain Persian, RTL, obvious next step, accurate amounts/units and calm forms are delivery requirements. Build success is not user acceptance.

Decision:
ONE_AUTHORITATIVE_REVIEWED_MONEY_DISPLAY_CONDITION_BOUND_RECEIVED_GOODS_REVIEW_AND_PER_PRODUCT_UNIT_SUMMARY_ON_THE_EXISTING_RETURNS_WORKSPACE_WITH_EXACT_COMMAND_SAFETY_PRESERVED_ZERO_BACKEND_CHANGE_AND_ZERO_LIVE_BUSINESS_WRITE

## 0 — Claim, provenance and working rules

- Read AGENTS.md and applicable frontend feature instructions, this exact issued envelope, the parent TASK.md, and docs/architecture/evidence/TB-RETURNS-SHOPKEEPER-PLAIN-LANGUAGE-WORKFLOW-AND-REVIEW-280/ARCHITECT-REVIEW.md.
- Expected base is the reviewed main head above. A direct docs-only issuance descendant is allowed: verify ancestry and incoming diff, record actual claim HEAD, and stop for source/config/schema divergence rather than overwriting work.
- Execute the exact delivered task bytes. Persist them unchanged at docs/ai/tasks/TB-RETURNS-SHOPKEEPER-PLAIN-LANGUAGE-WORKFLOW-AND-REVIEW-280-R1/TASK.md and docs/architecture/evidence/TB-RETURNS-SHOPKEEPER-PLAIN-LANGUAGE-WORKFLOW-AND-REVIEW-280-R1/TASK.md; use the issuance receipt's SHA256 and byte count. If Bridge payload is missing but these exact issued files exist, use the issued files and disclose that transport condition; do not reconstruct a different scope.
- One Cursor worker, one task. No delegation, subagents, historical-agent resumption, screenshots, polling loop, or automatic next implementation.
- Normal commits and fast-forward push only. No force, amend, rebase, reset, stash, history rewrite or unrelated cleanup. Preserve unrelated local and untracked work. Do not stage keys, App_Data, scratch scripts or posting files.
- 279-R3 fixed-scope Architect ACCEPT remains preserved. Parent 280 remains PARTIAL until this repair is reviewed. Historical evidence is immutable.

## A — UX1: the confirmation must show the money the owning command actually uses

Source-confirmed defect: admin-return-workspace.tsx renders snapshot.refundAmount (requested money) in financial dialogs although approvedAmount and the bound attempt have separate authoritative stored values.

1. First-refund confirmation shows the stored approved payable amount and currency. A requested 3000 / approved 2000 case must say 2000 in the confirm dialog, while the main card may still distinguish requested 3000 from approved 2000.
2. An approved zero is zero, never replaced by requested money through || or a truthiness fallback. A no-money case must not be described as refunded. Preserve the server-published lawful action gate.
3. Resume and retry confirmation show the EXACT attempt selected by the server-published pending/failed id, with that attempt's amount and currency, not the requested amount and not first/latest fallback. Missing, foreign or ambiguous binding must not produce a fabricated monetary review or send.
4. For a legacy null approvedAmount, use a fallback only if the EXISTING owning read contract positively establishes the same complete money basis; otherwise show a short honest message and refuse to send from an unreviewable amount. Never infer payment amount from browser arithmetic, product prices or an unrelated attempt.
5. Keep command transport unchanged: frozen route/body/query, expected decision/resolution versions, bound attempt ids, occurrence identities, storage read-back, exact compare-and-clear, epoch fencing and refusal classification. Do not add client-supplied money or change backend calculation.
6. Keep «تأیید مرجوعی» decision-only. «بازگشت وجه» is the separate deliberate confirmation. Display copy describes only the action actually being sent. No new chained write or implicit refund.

Use one small feature-local display authority where it prevents divergent dialogs; do not create a generic financial framework.

## B — UX2: received conditions must stay separate, and the dialog must open clean

Source-confirmed defect: beginResolution fills acceptedUsable with line.accepted (aggregate) and leaves acceptedDamaged empty. The genuine implicit complete-receipt resolution is always returned by MapSnapshotAsync; damaged-only 0 usable + 1 damaged therefore opens usable=1 with ceiling 0, and mixed 2 usable + 1 damaged opens usable=3 with ceiling 2.

1. Initialize from the exact acceptedUsable / acceptedDamaged facts OR leave fields empty for deliberate input. Do not put aggregate accepted into usable. Do not pre-approve by a hidden new default.
2. Only render an editable accepted-quantity field for a condition whose actually received ceiling is positive. A damaged-only item shows its damaged field, not an unusable healthy input capped at zero. Mixed goods show both with their separate actual unit and ceiling.
3. A zero-received item within a multi-item claim is clearly described as not received; do not ask for fake received quantity. Still account for it in the complete, unchanged five-portion command and preserve the owning per-line reason requirement. If the reason is required, ask the operator plainly; do not invent a generic reason or silently omit the item.
4. Keep accepted usable/damaged bounded by their OWN received condition and owning unit quantum. Derive rejected received complements and rejectedUnreceived exactly using the existing integer-millionth authority, never rounding, clamping or adding unlike units.
5. On opening: no red errors on untouched fields and no impossible prefilled value. After interaction/submit: short corrective Persian beside the field, such as «بیشتر از ... عدد وارد نکنید» or the valid unit increment, rather than a lesson in decimal precision or backend portions.
6. Meaningful required reason remains clearly required and validated before storing/sending. Keep exact five wire quantities and frozen reviewed versions. No weakening of server guards.
7. Preserve partial receiving and full-receipt partial review, shared Dialog, cancel/focus behaviour and retained uncertainty. No automatic publish+approve or approve+refund sequence.

## C — UX3: a receipt summary cannot add pieces to grams

Source-confirmed defect: receivedUnitsOf/sumUnitsOf are used to display a single received X of Y across all case items. One piece + 500 grams appears as 501 of 501.

1. Replace the headline with either per-product received/requested quantity plus that product's unit, OR a clearly labelled count of fully received product lines with a separate partial-lines indication.
2. No addition of unlike units and no unlabeled aggregate physical quantity. Keep the accurate per-item table.
3. A numerical sum may remain only for an internal positive-presence predicate if safe; it must not become an operator quantity or a permission authority. Prefer a clear any-received predicate when practical.
4. Product names, Persian digits, decimals and units remain readable at 390 and 1440 widths. No raw GUID, enum, ISO timestamp, revision number or technical explanation in routine work copy.

## D — focused proof that closes the actual counterexamples

Mount the REAL AdminReturnWorkspace with controlled HTTP/storage using the repository-supported runner, not source-regex assertions or a replacement component. Use genuine mapped response shapes, especially the implicit full-receipt baseline with FromReviewedPlan=false and per-condition accepted values.

Required scenario matrix (can share fixtures/assertions; do not duplicate whole suites):
- M1: requested 3000, approved 2000 -> first-refund review displays 2000 and correct currency; decision approval still sends no refund. Include approved zero (no truthy fallback) and unavailable authoritative amount (honest refusal, no POST).
- M2: server-published exact pending/failed attempt ids -> resume/retry dialogs show bound attempt amount/currency even when requested differs; missing/mismatched binding cannot send; exact frozen identity is unchanged.
- M3: fully received damaged-only implicit baseline (usable0, damaged1) -> clean initial dialog, damaged quantity only, no prefilled usable1/zero-ceiling healthy input, lawful five-field body.
- M4: fully received mixed implicit baseline (usable2, damaged1) -> clean open, per-condition legal values, local over-ceiling refusal writes/sends nothing, intended accepted usable/damaged yields exact rejected complements. Include a fractional-unit input and Persian digits through the retained exact arithmetic.
- M5: multi-item claim with one zero-received item -> no pointless quantity input for that item, full requested remainder and meaningful reasons are accounted for in the immutable command; no implicit approval/refund.
- M6: different products, pcs1 + grams500 -> truthful per-product/unit display or labelled line-count summary, never “501 of 501”.
- M7: zero-received case still offers only receipt as primary; full receiving review stays reachable; settled case stays read-only; cancel sends/stores nothing. Retained possibly-sent intent survives close/remount/action disappearance and exact replay remains unchanged.

Run the new targeted mounted suite plus the directly affected retained workspace suites and relevant quantity/API/error-map tests once on final code, then tsc --noEmit and one production Next build. Re-run a failed check only after a concrete fix; disclose intermediate defects rather than hiding them. No whole repository vitest sweep, whole Returns/Host/AMSC run, stress harness or backend build/publish: no backend source change is authorized or needed.

If a required counterexample reveals another defect in these same UI paths, fix it within this bounded task and keep the catching fact. Do not stop after changing labels. If an actual incompatible owning contract blocks this frontend-only repair, document the precise blocker instead of weakening safety or inventing permission.

## E — served handoff ready for the owner to test

Canonical environment: ONE tooba_alpha at 127.0.0.1:5432, SingleStore/store-alpha, Host5088, Next3000. Refresh ONLY the frontend artifact as necessary; preserve the Host, config and canonical data.

Existing review URL:
http://127.0.0.1:3000/fa/admin/returns/01a1eefb-ea96-7000-838d-9a0b37661c8b
Queue:
http://127.0.0.1:3000/fa/admin/returns

- Read current case state first. The owner may have progressed it; never reset it or require it to stay Requested.
- Read-only GET and opening/cancelling lawful dialogs are allowed. NO live POST/business mutation, new demo case/order, receipt, decision, resolution, custody, refund, stock/hold/provider/Wallet/PSP/notification operation.
- Zero migration/DDL/backfill/seed/cleanup/delete/restore/reset. No second database, changed connection/port/settings/key or security retirement. No screenshot.
- Check served BUILD_ID/chunk identity, case/queue reachability, and read-only DOM/layout at 390/1440: correct visible labels and stage, touch targets >=44px, no document overflow, no field errors on untouched open if the current case lawfully offers that dialog. Cancel sends nothing.
- If the current case cannot expose money/mixed-condition dialogs, prove them in mounted REAL-component fixtures and state that limit. Do not fabricate live walkthroughs or mutate data to manufacture one.
- Keep the template's existing single react-toastify container, one outcome toast per action and inline field errors. Preserve the prior clear next-step card and progressive history.
- user-review.md must be SHORT Persian for a shopkeeper: exact links, what changed, and a simple manual checklist: دریافت -> بررسی سالم/معیوب و قبول بخشی از مقدار -> تأیید بدون پول -> بازگشت وجه با مبلغ درست -> در صورت وجود تحویل کالای ردشده. State that submitting is a real business action; opening/cancelling is read-only. Do not replace it with technical prose. No user/financial/provider acceptance claimed by the worker.

## F — durable evidence and stop

Persist TASK.md (exact issued bytes), RESULT.md, RECEIPTS.md, result-contract.txt, three-ux-fixes-proof.md and user-review.md under docs/architecture/evidence/TB-RETURNS-SHOPKEEPER-PLAIN-LANGUAGE-WORKFLOW-AND-REVIEW-280-R1/. Proof maps UX1/UX2/UX3 to actual source changes and M1–M7 test assertions. Include actual command results, served identity, data-effect boundary and honest limits.

Update compact CURRENT pointers in AGENTS.md, docs/ai/TOOBA-RECOVERY-CONTEXT.md and docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md. Preserve prior reports/reviews and 279-R3 acceptance; do not rewrite the parent review as accepted.

Canonical worker result:
Task-ID: TB-RETURNS-SHOPKEEPER-PLAIN-LANGUAGE-WORKFLOW-AND-REVIEW-280-R1
Parent-Task: TB-RETURNS-SHOPKEEPER-PLAIN-LANGUAGE-WORKFLOW-AND-REVIEW-280
Status: PASS or PARTIAL with precise remaining blocker
UX1-Reviewed-Money-State:
UX2-Condition-Review-State:
UX3-Per-Unit-Summary-State:
Technical-Validation:
Operator-UX-Review-Readiness:
User-Acceptance: NOT_CLAIMED
Accepted-279-R3-State: PRESERVED
Wire-And-Intent-Safety-State: PRESERVED
Backend-Change-State: ZERO
Schema-State: ZERO
Live-Business-Write-State: ZERO
Security-Key-State: UNCHANGED_SEPARATE_PENDING_REVIEW
Expected-Base-SHA: 9075e2fa4325a2de60cbd17ebfe908d9d9ee74ed
Base-SHA:
Code-SHA:
Evidence-SHA:
Final-HEAD:
HEAD-Equals-Origin-Main:
Review-URL:
Evidence-Limits:
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW

Finish this single repair and STOP. Do not issue another task, claim user acceptance or reopen financial architecture.

