PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-ADMIN-DESTINATION-RATE-MODALS-225-R2
Parent-Task: TB-SHIPPING-ADMIN-DESTINATION-RATE-MODALS-225-R1
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Reviewed-Main: 1d06252188a3278127401e804f53b263725c47e1
Mode: BOUNDED_PERSISTENT_MONEY_PROVENANCE_AND_ISOLATED_CHROME_PROOF
HARD20 minutes / implementation10 / proof+recovery+commit10. One correction round.
225/R1 NOT ACCEPTED. NO new user trial until Architect review.
Preserve existing modal/cascade/mobile UI and current corrected rateConfirmIssue. No new UX/backend/schema.

CRITICAL USER TAB INCIDENT
R1 reloaded an existing USER edit tab and discarded unsaved local work, contrary to instructions.
Do NOT call this preserved/user-tabs-untouched. Stored DB unchanged does NOT recover unsaved local edits.
Record incident in recovery. No claim recovery unless actual retained text can be verified read-only.
Do not manipulate/enumerate/select a user's Electron webview/session/tab again.
For proof launch an INSTALLED Chrome with FRESH task-owned profile and dedicated CDP identity/port after
verifying free; use only the targetId CREATED by this task. Never "first matching localhost tab".
Record browser profile + task-owned target identity. Stop/clean only task-owned processes.
No user profile/login/session assumption or shared-port process kill. Existing documented dev actor may be
used exactly as previous lawful real trial. All API traffic real, no /v1 fulfil/interception.

ONE SOURCE-CONFIRMED DEFECT — PROVENANCE LOST AFTER TWO KEYSTROKES
interpretMoneyTyping(previousRaw,nextText,caret) decides trusted=readMoneyText(previousRaw).valid.
But raw text can be structurally well-grouped while still explicitly untrusted from the previous event:
paste12,34 =>raw12,34 invalid; type5 =>raw12,345 valid:false; type6 =>
readMoneyText(previousRaw12,345).valid TRUE =>strip current12,3456 =>raw123456 accepted.
The previous event's valid:false is returned but never owned/persisted by MoneyField.
Fix provenance as real edit state tied to field/value identity, not re-inferred from text alone.
A prior-invalid comma-bearing token stays invalid through ANY number of appended keys until deliberate
operator correction removes/replaces offending grouping. Do not reinterpret it as field-generated grouping.
Keep valid normal grouping/middle caret, IRR integer entry,0, exact untouched .00 tokens, paste/drop fallback.
No field-local provenance leak across modal close/reopen/different row or authoritative external reseed.
Parent model raw invalid text must remain blocking; no laundering by blur/re-render/other-field change.
Use one cohesive existing money-field state mechanism. No extra ad-hoc sanitizer or copy of model authority.
If simplest reliable component already exists in design system, reuse it only with exact-value proof.
No change to accepted rate-shape/self-exclusion/legacy fraction/MessageBox semantics.

RED FIRST THEN FOCUSED GREEN
Executable sequence on actual helper/component state transitions (not one-call tests):
12,34 paste ->type5 ->type6 ->type7 and interleaved blur/re-render =>never accepted different money.
Explicit delete comma or replace whole value ->lawful. Different row opens clean state, canceled row doesn't
leak provenance; changing another field doesn't unblock old invalid token.
Valid initial1,234 ->insert9 after2 =>12,934 caret after9; valid sequential5000000 =>5,000,000;
0 valid; malformed paste/negative/exponent remain invalid; decimal side scaled currency retained.
Exact affected class+one final typecheck ONLY. No188/76/full feature/backend/solution/guard sweep.

MOUNTED GATE IS REQUIRED, NOT MODEL-ONLY
Use FRESH installed task Chrome and finite-timeout CDP Input.dispatchKeyEvent/Input.insertText, verify
document.activeElement exact testid before EVERY typing sequence and read actual DOM value+selection after.
Not DOM value setter alone; not Electron that cannot deliver keys. No blind retries if input doesn't land.
Real route/local actual draft: open row modal, invalid paste then TWO real appended digits ->invalid;
middle insert+caret; valid0+5million; failed explicitConfirm ->MessageBox non-empty reason and modal open;
Cancel ->main baseline unchanged, no false dirty. One new add can be local then canceled. No Apply/PUT.
One short screenshot of refusal + numeric DOM receipts sufficient. No matrix/harness redesign.
If real keys fail once after focus correction, report blocked proof, stop; do not invent PASS.
Tehran no-service-area gap unchanged and outside this correction; don't seed or claim service Add live.
Preserve current user draft/pointer, shared FE3000/Host5088, no restart/migration/catalog/publication write.

FINISH
docs/architecture/evidence/TB-SHIPPING-ADMIN-DESTINATION-RATE-MODALS-225-R2/{TASK.md,RESULT.md}
append R1 correction, update all3 TOP recovery with true tab incident/current code+proof limits.
Exact-path scoped commit/push/fetch authorized; no gitadd-A/reset/rebase/stash/force/Bridge/scripts.
Report ACTUAL elapsed. STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.
Do not tell user to test or claim final acceptance yourself.
