TB-SHIPPING-ADMIN-MULTILINGUAL-ACTIVATION-169

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-MULTILINGUAL-ACTIVATION-169
Parent-Task: TB-SHIPPING-ADMIN-FLAT-METHOD-CREATE-ACCEPTANCE-168
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 814ae785618b45512f51a661d40a7ef1047db367
Program: TOOBA-SHIPPING-FLAT-METHOD-COMMERCIAL-READINESS
Mode: BOUNDED_REAL_ADMIN_MULTILINGUAL_AND_ACTIVATION_ACCEPTANCE
Hard-Timebox: 45 minutes, STOP at boundary; no overruns, no retry loops

Mission

Close the remaining TB-148 Admin multilingual save→GET→reopen proof and verify that method activation/deactivation is genuinely independent, especially for a newly created flat method (TB-167/168), in a real authorized disposable scratch Host+PostgreSQL+private Next browser. This is an acceptance/targeted-repair task, NOT a redesign or new policy implementation. Keep the four non-nested tabs and three list-row actions.

Mandatory starting inspection
Verify HEAD == origin/main == Expected-Base-SHA; if remote advanced, STOP and report, never rebase/force push.
Read docs/architecture/shipping/SHIPPING-ADMIN-FLAT-METHOD-CONTRACT.md, docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-16.md and TB-148, TB-167, TB-168 source and evidence. Inspect the actual canonical Fulfillment method translation and activation endpoints, authorization and Admin frontend handlers before testing.
Reuse the TB-168 disposable Admin harness and TB-157 permission/bootstrap recipe. Use actual Localization registry language IDs (not hardcoded), server-assigned binding, and real authorized Development actor. If necessary apply existing canonical migrations ONLY to disposable database.
Single bounded acceptance journey
Start private disposable PostgreSQL, isolated Host, and isolated frontend with fail-closed environment guard; use task-specific ports and names. Explicitly exclude shared :3000/:5088/:5432, tooba_alpha, shared store-alpha data, shared RabbitMQ/Redis and other workers. Capture clean baseline and cleanup evidence.
In real Admin browser create or select an independent tipax:<unique scratch-only option> via existing canonical Add Service flow; verify initially inactive, correct binding and four tabs. Populate names for at least fa-IR and en-US only when actually returned by live language registry; if ru-RU is returned, exercise missing-translation and valid save there too. No fabricated registry entries or translation data.
Edit each available target language independently using the actual General tab; save using canonical method+language endpoint, verify success and authoritative HTTP/SQL read-back, close/reopen modal and full document reload. Switch languages while one draft is unsaved and confirm typed draft preservation and that it never contaminates saved names of other languages. On failure the unsaved draft must survive.
Verify sibling isolation: tipax:standard and tipax:express existing translations and activation states unchanged by the newly created method's edits. Verify invalid language/empty name/unauthorized writes fail closed and change no persisted name; no silent success toast.
Exercise ACTUAL method activation and deactivation through the Admin UI against scratch data, with server read-back and reload. IMPORTANT: a newly created method has no legacy shipping_services/shipping_service_options binding nor pricing. If the existing activation endpoint intentionally refuses it, record the exact failure and leave it inactive; do not synthesize provider/option entities, bypass validation or claim the method is customer-selectable. Also test permitted independent activation/deactivation on an existing fully bound method only if safe in scratch. Activation must never imply financial readiness, FREE, or quote-ready.
If a source defect prevents a required journey, make ONE narrowly scoped owner-correct fix with focused tests; no broad refactor, schema guesswork, invented price, extra endpoint or permission bypass. If blocked, classify PARTIAL with exact root cause and next minimal fix.
Acceptance and evidence
PASS only if real authorized browser plus HTTP/SQL prove persisted per-language names across GET/reopen/reload, clean sibling isolation and truthful activation/deactivation behavior without fake readiness. If existing canonical binding makes activation of new methods impossible, treat that limitation as an OPEN GAP and do not claim full new-method operational readiness; use PARTIAL when it prevents acceptance.
Capture sanitized actual screenshot(s), HTTP status/response contract, registry language identity mapping, Admin DOM assertions, SQL persistence/isolation matrix, authorization negatives, and teardown proof in docs/architecture/evidence/TB-SHIPPING-ADMIN-MULTILINGUAL-ACTIVATION-169/.
Focused tests ONLY for changed code; TypeScript check when frontend code changes. No endless full suite. No secrets, OTP, tokens or connection strings in committed evidence.
Explicitly state any pre-existing failures separately. Preserve unrelated dirty/stashed files and historical evidence logs. No .next purge, no shared process restart, no PSP/SMS/payout, no checkout mutation, no auto-next-task.
Commit and fast-forward push only scoped changes + task artifact and honest evidence. Report Base-SHA, Code-SHA (NONE if acceptance-only), Evidence-SHA, Origin parity, exact PASS/PARTIAL/BLOCKED, elapsed time, cleanup, open gaps.

STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE
END_TOOBA_TASK