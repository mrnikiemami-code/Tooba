PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-INDEPENDENT-GROUP-ACCEPTANCE-152
Parent-Task: TB-SHIPPING-INDEPENDENT-GROUP-LIVE-PROOF-151
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: e57ef0639244be1ddd2bb6151456faf3436d76e7
Program: TOOBA-SHIPPING-INDEPENDENT-SELLER-COMMITMENTS
Mode: BOUNDED_ISOLATED_PAYMENT_DRAFT_AND_REAL_BROWSER_GROUP_UI_ACCEPTANCE
Status-Target: PASS_OR_PARTIAL_OR_BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 MINUTES HARD — reserve last 10 minutes for cleanup, concise evidence, commit and report.

SOURCE OF TRUTH — READ FIRST

docs/architecture/shipping/SHIPPING-GROUP-INDEPENDENT-DELIVERY-COMMITMENT.md
docs/planning/shipping/SHIPPING-GROUP-SCHEDULE-WIREFRAME-145.md (user-approved)
docs/planning/shipping/SHIPPING-GROUP-SCHEDULE-IMPLEMENTATION-145.md
docs/architecture/evidence/TB-SHIPPING-INDEPENDENT-GROUP-LIVE-PROOF-151/RESULT.md
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-16.md
docs/planning/MUST-FIX-BEFORE-RELEASE.md

MISSION
Close only the two concrete TB-151 proof gaps using a PRIVATE, DISPOSABLE, AUTHORIZED Development environment: (A) multi-seller SAME-method shipping selection -> persisted distinct seller promises -> read-only payment draft with correct group totals, and (B) real authenticated browser screenshots + DOM assertions showing two separate Tipax cards with independent dates/windows. This is a RUNTIME ACCEPTANCE task, NOT another architecture investigation, visual redesign, refactor, or commercial policy change.

PRECONDITIONS / SAFETY

Check origin/main SHA before work; if changed, review diff and STOP on material conflict. Only one active worker task.
Reuse the task-owned scratch bootstrap/journey scripts from TB-151 as appropriate. Create ONLY isolated scratch Postgres, private Host, and private Next frontend on unused loopback ports. Guard DB connection string and container identity before ANY write. Never touch shared :3000, :5088, :5432, tooba_alpha, store-alpha or real PSP/SMS/RabbitMQ; do not purge .next or restart shared services.
No leaked secrets: never commit OTPs, tokens, connection credentials or browser cookies. Ensure isolated Host and frontend have a correct private backend URL; do NOT accidentally route writes to shared Host via fallback. Fail closed if private wiring cannot be proven.
Use authentic sanctioned Development test customer/session and seeded two seller offers; do not bypass auth. No invented carrier rates/slots/real origins; use existing seller preparation 2 vs 3 and tipax:standard LeadDays 3 using fixed authoritative IClock/reference, subject to existing fixture semantics.
Existing TB-151 tax failure was exact: payment draft returned 409 checkout.tax.unavailable because a seeded offer category lacks a matching active tax rule. In scratch only, inspect the existing tax seed shape and insert the MINIMUM valid category-specific tax rule(s) needed for BOTH seeded offers, consistent with existing tax design. Do not change production tax code or force a dummy zero quote. If tax setup remains invalid, report exact blocker rather than patching unrelated code.

EXECUTION / ACCEPTANCE
A. Re-run the unchanged TB-151 authenticated two-seller same-method HTTP journey (or only minimal test runner parameter adjustments for private ports): initial GET returns two seller-specific GroupKeys; save produces distinct ShippingGroupIds, seller A min +2 prep and B min +3 prep (both plus existing lead); independent selected dates/windows preserved on GET/reload; change A leaves B unchanged; invalid A schedule rejected without altering B; both groups' line memberships and fees independently persisted. No premature checkout/charge during Shipping navigation.
B. Read the PAYMENT DRAFT through the actual existing endpoint after shipping draft save. Require HTTP 200 (not 409), two groups with correct seller/method/line mapping, preserved requested schedules where that contract exposes them, shipping fee accounted for once per group, correct aggregate and tax calculation as determined by the authoritative pricing/tax owner. Do not assert non-existent fields or invent financial share allocations. If current payment-draft contract does not expose some schedule fields, establish their persisted provenance via Order draft / existing group mapping and report the limit.
C. Run a PRIVATE Next frontend bound to the PRIVATE Host, log in with an approved disposable customer and prepare a two-seller cart. Capture actual screenshots of /fa/shipping with TWO distinguishable 'تیپاکس معمولی' groups, each showing its own seller/items/fee/earliest date and separate date/time controls. Prove with DOM actions that choosing/changing A's slot leaves B intact, Continue is gated on both, and reload/back restores group selections. Screenshot/DOM from actual app only; no synthetic UI/mock images. Preserve current store RTL/styling. Do not confuse the old shared page with the private frontend.
D. Identify whether saved ShippingGroupId changes on repeat PUT as TB-151 observed. This is a known architectural continuity deficit against the locked 'stable ShippingGroupId' requirement. Document it as OPEN / MUST FIX; do not quietly certify stable identity. No schema rewrites or opportunistic id overhaul in this acceptance task. Verify downstream payment references remain internally consistent for the latest save.
E. Before cleanup, collect concise sanitized HTTP status/timing, two-seller projection and draft excerpts, DB readback of group IDs/schedules and payment summary, and actual PNG screenshots and DOM markers. No giant logs or personal data. Tear down every task-owned isolated service/container and verify shared processes unchanged.

PASS RULE
PASS only when BOTH real authenticated HTTP/payment-draft assertions AND real private browser per-group cards/independence/reload proof succeed. Mark PARTIAL/BLOCKED when either is unverified or unsafe. A PASS on this acceptance task does NOT close overall Shipping: repeated-save ShippingGroupId continuity, authoritative carrier calendar/capacity/timezone, same-seller same-method incompatible origins, Admin monetary modes/create-method and release blockers remain separately OPEN.

TESTS / CHANGE BOUNDARY

Environment fixture/evidence only by default; do not change production source without a narrowly proven defect, and if any source change is unavoidable STOP_FOR_ARCHITECT with the precise failure rather than broaden the task.
Only focused probes covering this real journey; do not run repeated broad suites or unnecessary rebuilds. No hidden stashes, resets, rebases, force pushes, or unrelated admin modifications.
Commit safe task scripts/evidence under docs/architecture/evidence/TB-SHIPPING-INDEPENDENT-GROUP-ACCEPTANCE-152/ and issue the exact Bridge result: Base-SHA, Code-SHA (NONE if no source), Evidence-SHA, HEAD/origin parity, PASS/PARTIAL/BLOCKED, precise observed state and known open issues. No automatic next task.
END_TOOBA_TASK
