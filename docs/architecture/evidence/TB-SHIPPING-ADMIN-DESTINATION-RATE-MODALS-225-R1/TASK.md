PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-ADMIN-DESTINATION-RATE-MODALS-225-R1
Parent-Task: TB-SHIPPING-ADMIN-DESTINATION-RATE-MODALS-225
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Reviewed-Source: 2e6aed0951a5e1ef75d5206d77c4df9f433f55b1
Mode: BOUNDED_MONEY_EDIT_PROVENANCE_AND_MODAL_VALIDATION_REPAIR
HARD20 minutes / implementation12 / final8 focused proof+recovery+commit. One correction maximum.
225 NOT ACCEPTED yet. User waits Architect acceptance BEFORE testing; no new user trial call.
Preserve225 destination/rate modals, four-stage cascade, one-path chips, MessageBox, shared range/mobile.
No frontend redesign, schema, backend, Storefront, seller-admin, catalog/data/runtime write.

READ TOP recovery/AGENTS, applicable UI/architecture skills, persist task first, fetch safe fast-forward.
Preserve unrelated local work. No full188 suite/solution/Bridge/reset/stash/rebase/git add-A.

SOURCE-CONFIRMED BLOCKER1 — INVALID TEXT CAN BE HEALED INTO DIFFERENT MONEY
MoneyField.onChange selects interpretMoneyTyping for EVERY non-paste event.
interpretMoneyTyping removes ALL commas without knowing previous raw/display state.
Reproduction: paste "12,34" -> intentionally INVALID raw "12,34"; then type "5" at end -> "12,345" ->
typing removes comma -> valid12345. Another: invalid "1,2,3" then delete1 comma can silently accept remaining
digits. Those separators were invalid USER input, not field-generated display separators.
Fix logical input provenance: inspect prior accepted raw/display and actual selection/input operation.
Only remove grouping separators known to be generated for a valid prior display. Never strip commas of a
prior-invalid token into accepted money merely because next event isn't a paste. Preserve invalid text
until explicit correction/replacement makes the visible input valid; no catch-to-number/sanitize repair.
Use existing input component primitives if available, one coherent field implementation, not another framework.
Handle paste inputType differences/drop/IME honestly; use onPaste/beforeinput where necessary, not assume
every browser change event provides "insertFromPaste". No negative/exponent/letter/multidot transformation.
Do not block normal digit typing5million from valid grouped display.0 remains lawful. IRR integer UI and
untouched .00 baseline exact wire preserved; no forced decimal scale/rounding.

BLOCKER2 — CARET OFFSET APPLIED TO WRONG TEXT
interpretMoneyTyping(nextText,caret) removes commas then calls caretAnchorAt(withoutGrouping, ORIGINALcaret).
Original offset includes commas, so middle edits drift.
Example grouped1,234: insert9 after2 => browser text1,2934 with caret4. Desired normalized digits before
caret are129 (3digits); current helper anchors4digits and jumps after3 in displayed12,934.
Count mapped prefix/separators before caret (including localized digits) against original displayed text;
compute exact logical edit offset. Don't clamp original offset on a shorter stripped string.
Focused actual test middle insertion, deletion, selection replace, decimal-side for scaled currency,
Persian digits and comma-adjacent Backspace/Delete. End-only typing is not proof of middle behavior.

ADJACENT SAME-MODAL VALIDATION DEFECT — STORED FRACTION TOLERANCE NEVER RECHECKED
AuthoringRateModal receives tolerated fields computed ON OPEN from original row; confirm passes that
same list after edits. Stored freight100.50 tolerated -> operator edits200.75 -> Confirm accepts due to
field name still in list, though parent later refuses. Modal must not falsely confirm invalid edited IRR.
Compute tolerance on CURRENT row against exact original stored token for that field, not field name alone.
Unchanged legacy stored fraction preserved; altered nonzero fraction refused in modal MessageBox.
Parent remains final validator. No weakening of precision or removal of preserved stored values.
Recheck editable synchronously at confirm handler, not just disabled button; handler must not mutate if
view/busy changed while modal open. Keep exact row identity/self-exclusion/duplicate scope.

FOCUSED PROOF ONLY
Write red-first regressions on SAME logical edit/state path component uses (not source-regex):
- invalid paste12,34 then type5 never silently becomes12345; user must explicitly correct invalid text;
- valid previous1,234 middle insertion -> raw12934, displayed12,934 and caret after9;
- normal sequential5million,0, replacing entire selection, paste/localdigit/caret paths preserved;
- stored100.50 unchanged allowed, edited200.75 refused before modal commit, no parent row mutation;
- busy/view confirm no-op; current exact self edit and other duplicate behavior kept.
ONE typecheck + exact new/affected tests, no188/166/whole guards/backend sweep unless directly required.
Real isolated browser on current FE3000/Host5088, no /v1 interception:
paste invalid then next key, middle insertion+caret, valid0 and5million; local modal confirm rejection and
Cancel -> unchanged parent, no new row/dirty on canceled modal. Existing stored-fraction case may use a
clearly labelled focused component fixture if no live record; don't alter real user data to create one.
No Apply/PUT/publication/catalog/migration/restart, user's tab untouched, normal discard local trial.
Report exact DOM results and limits. Do not spend remaining time expanding screenshot/harness matrices.

FINISH
docs/architecture/evidence/TB-SHIPPING-ADMIN-DESTINATION-RATE-MODALS-225-R1/{TASK.md,RESULT.md};
append correction to225, all3 TOP recovery updates (AGENTS/new-chat/canonical).
State225 awaits Architect review, no user trial yet. No Storefront/visual acceptance inferred.
Exact-path commit/push/fetch authorized, actual elapsed, STOP_FOR_ARCHITECT, Automatic-Next-Task NONE.
