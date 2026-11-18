# Architect review — TB-280

Date: 2026-11-19 (Asia/Tehran)
Verdict: PARTIAL — useful receiving-stage UX improvement; not final operator/product acceptance.
Reviewed worker head: 83f56842f139c8512974aa872b67b0d3b6e97d9e; implementation ebc46ec5f026612e5ea9c555d566a26ca0803ee5.
Repository authority: mrnikiemami-code/Tooba. The worker result's mrnikami-code spelling is a posting typo; source was inspected in the canonical repository.

## Review method and accepted improvements

Independent GitHub source/diff/test-assertion review against the issued docs-only base 2fb3791f0c1bd124ee9877f163918a25fdfa585a. Architect did NOT run the Windows builds/tests or access the localhost browser/database. Reported 40 focused backend and 76 frontend passes remain worker receipts, not independent execution.

The read-only work projection now refuses a zero-received review, requires positive coherent receiving, keeps reviewed-plan completion and no-effect prerequisites, and retains partial review after full receiving. The workspace has one next-step card, secondary actions, ordinary main titles, collapsed history, required review reasons, and no untouched empty-quantity red errors. No owning write implementation, schema, key operation or live business mutation is changed by this delta. 279-R3 ACCEPT is preserved.

## Remaining bounded product defects (same existing UI, no backend redesign)

### UX1 — the refund confirmation still displays the requested amount instead of the approved payable amount

Source: src/frontend/app/admin/admin-return-workspace.tsx, start_refund dialog around lines 1493–1500 (and the resume/retry amount displays).
The status card correctly distinguishes refundAmount and approvedAmount (lines 1116–1121), but the financial confirmation still renders formatRial(snapshot.refundAmount, snapshot.currency). return-api.ts maps these as distinct values without replacing refundAmount. For requested 3000 / approved 2000, the operator confirms a screen saying 3000 while the accepted owner sends 2000. This is a misleading monetary confirmation, not an unsafe backend amount calculation.
Required narrow correction: first-refund review shows the stored approved amount, including zero honestly; legacy fallback only under the existing authoritative contract. Resume/retry use the exact positively bound reviewed attempt amount/currency. Never use truthiness to replace an approved zero with requested money. Preserve frozen command bytes, identities, versions and no-effect classification. Mounted proof must assert visible amount with requested != approved and the bound attempt amount for resume/retry.

### UX2 — a fully received damaged/mixed case opens a wrongly prefilled review, and zero-condition inputs remain visible

Source: workspace beginResolution lines 825–838: acceptedUsable = formatQuantityDisplayFa(line.accepted), acceptedDamaged = empty. MapSnapshotAsync always supplies the implicit baseline resolution on full coherent receiving; ReturnResolutionPolicy.Describe (ReturnCorrectionModels.cs around lines 223–252) assigns accepted usable = received usable and accepted damaged = received damaged. Thus 0 usable + 1 damaged opens accepted usable 1 (ceiling 0), accepted damaged empty, and an immediate red error. A 2 usable + 1 damaged case opens usable 3 (ceiling 2). The newly offered full-receipt review exposes this directly. The four new mounted tests do not exercise that genuine server baseline shape; fixture withResolution uses an incomplete custom plan.
The dialog at lines 1831–1875 also always renders BOTH usable and damaged inputs, including a zero ceiling, and retains precision jargon in its error text. That misses the contextual-field requirement and the owner's original complaint.
Required narrow correction: initialize from each exact condition or leave deliberate fields empty; never assign aggregate accepted to usable. Render only received conditions with positive ceilings (and avoid asking the operator to fill a zero-received item; account for it automatically in the complete five-portion transport while preserving meaningful required reasons). Use short actionable unit/ceiling errors. Add genuine full-receipt baseline mounts for damaged-only and mixed goods: clean initial open, lawful per-condition values, exact five-field body, no new automatic approval/refund.

### UX3 — a new headline adds unlike units

Source: receivedUnitsOf/sumUnitsOf and the received-summary at lines 1128–1136. Quantities from every item are summed and presented as one received X of Y, without per-product units. One piece plus 500 grams becomes 501 of 501. Integer arithmetic does not make unlike units comparable.
Required narrow correction: show per-product quantity/unit, or a clearly labelled count of fully received lines with a partial-lines note; no numerical sum across unlike units. Keep the already accurate per-item table. Mount a multi-line pcs + gram case and assert understandable per-unit text rather than 501.

## Closure boundary

These are THREE bounded frontend product corrections plus their focused mounted counterexamples. No new financial algorithm, workflow engine, migration, live fixture, broad re-certification or ten-task chain is required. Do not reopen 279-R3. Do not silently alter accepted write guards, required reasons, retained envelopes or version binding to make UI tests pass. No new implementation task is issued by this review; automaticNextImplementationTask NONE. Preserve the user's active case and all history.

User can inspect the improved zero-received screen now; a full approval/refund/partial-resolution acceptance must wait for these monetary and condition-display corrections and the owner's actual review. Builds alone never establish sellable product quality. The live positive-received walkthrough remains unproven, as correctly disclosed by the worker.
