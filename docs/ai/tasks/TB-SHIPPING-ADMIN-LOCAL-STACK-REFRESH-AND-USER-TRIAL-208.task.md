TB-SHIPPING-ADMIN-LOCAL-STACK-REFRESH-AND-USER-TRIAL-208

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Channel: tooba-main
Worker-Id: tooba-worker-01
Repository: mrnikiemami-code/Tooba
Branch: main
Parent-Task: TB-SHIPPING-ADMIN-HOST-REFRESH-PREFLIGHT-207
Expected-Base-SHA: 822a8ea8de14630c84fb62a241b2c058bc27a4c5
Mode: AUTHORIZED_LOCAL_DEV_MIGRATIONS_HOST_REFRESH_AND_LIVE_EDITOR_HANDOFF
Hard-Timebox: 40 minutes
Stop-New-Implementation: minute30; final10 verification/evidence/commit/push/report
Automatic-Next-Task: NONE
Stop-State: STOP_FOR_ARCHITECT

User authorization — overrides earlier approval holds

The user explicitly confirms the database is DEVELOPMENT, NOT production, authorizes doing these changes now,
asks not to stop for further routine approval, prioritizes fast completion, requires updated recovery,
and wants to be called to test only when the real route is ready.
Authorized on the EXISTING LOCAL development target: pending checked-in migrations required by this stack,
Host-only refresh on5088, existing startup seed/backfill/background-worker effects, focused integration repair
and minimal real development draft preparation if no existing authorized draft is available.
Prior207/EXECUTION-PLAN "approval required"/nine-only holds and mandatory backup gate are SUPERSEDED
by this explicit instruction. Do NOT ask again for the same local actions.
This is not permission for external/production targets, database reset/drop, deleting unrelated work,
broad product expansion, or fabricated acceptance.

Objective / known evidence

Deliver the real existing-draft authoring page against the real configured local stack and keep it running for user trial.
206: old Host5088 routes404, FE3000 rewrite correct.207: current Host built successfully and retained
D:\temp\tb207-build\out; hashes/source listed in207/RESULT.md.
Seven Shipping and two Fulfillment migrations pending on local127.0.0.1:5432/tooba_alpha, alias tenant-alpha,
SingleStore/store-alpha; other modules' full pending set not yet established.
Shipping has NO startup migrator. Runner has NO module filter: apply iterates all registry descriptors.
Read207/ARCHITECT-REVIEW.md for corrections; do not repeat207 analysis/build.
Read AGENTS, TOP recovery and applicable repository feature skill BEFORE any actual production-code repair.
Persist task verbatim. Verify safe current main/origin; preserve unrelated dirt and docs-only descendants.

Execute through completion; no proof loop

Reconfirm target identity and trusted configured edition/tenant privately. Match LOCAL host127.0.0.1,
database tooba_alpha and canonical existing connection reference before writes.
Recheck exact current Host5088 PID/executable and FE3000 identity; never kill by name or stale PID.
Capture old executable/working-directory/config provenance for binary recovery; environment values UNKNOWN remain UNKNOWN.
Read current source/config/launch settings to recreate intended Development context; do not change edition/auth policy.
Avoid credential values in logs/commands/evidence.

Use current-source existing Tooba.MigrationRunner status/plan with explicit tenant store-alpha.
Inspect complete registry pending set, not just Shipping/Fulfillment.
The named nine from207 are expected. Additional pending CHECKED-IN migrations on THIS SAME local dev target
are authorized when reviewed as ordinary required schema progression with no unrelated data deletion/reset.
Record actual ids/Up effects and apply via the existing runner, tenant-scoped. Do not invent --module.
No additional approval merely because pending count differs from9. STOP only for target mismatch, destructive
unrelated operation, irreconcilable history/object drift or genuine execution blocker.
Existing SDK/packages only. Build Runner if needed in task-owned isolated output, no solution/suite.
Quick backup if existing tooling permits within60s; unavailable backup tooling is NOT a blocker on this user's disposable dev DB.
Do not install backup tooling, run restore, or turn backup into a separate task.
Respect runner migration lock. If same-name outbox index now exists, inspect actual shape/history;
do not silently adopt/drop objects or edit migration history to force success.
On failed apply, capture exact completed history/error and fix only a narrow source/configuration cause
within scope/timebox if lawful. Do not retry identical failures or edit previously accepted migrations blindly.
Verify actual applied migration ids/schema, no seed used to mask missing tables.

Refresh only Host5088 using the retained current binary if source/hash/config provenance still matches.
No new Host build if unchanged. If source actually changed, one isolated targeted build only.
Stop exact verified old Host gracefully with a finite wait; leave frontend3000 running unchanged.
Launch new Host with current intended Development settings, existing SingleStore/store-alpha configuration
and port5088, correct content root/config and environment. Do not trust launchSettings to apply when executing EXE.
Explicitly reconstruct existing ShippingOneLevelCatalog gate if required by documented intended dev launch.
Normal existing startup migrations/seeds/backfill/workers are AUTHORIZED by the user for this local environment;
record actual outcomes, do not claim all downstream operations wrote merely because wrappers ran.
No Production environment trick, new auth bypass, extra Host business logic or new migration/seed framework.
Wait boundedly for health and routes. If startup fails, inspect first exact error; at most one bounded repair/relaunch,
not a churn loop. Preserve a working Host when possible; old binary restart does NOT undo schema/data changes.
Keep successful new Host running for the user; record PID/executable/source/port and how to restart it.
Do not clean its retained output after handoff. No need to rebuild/restart FE for a backend-only repair.

Read real dev-context and use the EXISTING frontend's adminRequestHeaders/dev-actor mechanism as documented,
not a guessed token/user/grant. Confirm actual shipping permission boundary; no auth guard weakening.
Real GET authoring-drafts and authoring-types through3000 and editor GET for the chosen authorized draft.
Match exactly serviceCode+nullable optionCode from the real existing method binding.
Empty active-type list is a valid unconfigured catalog, NOT an excuse to seed invented type ids.
If no draft exists for an existing authorized method, minimal LOCAL DEVELOPMENT draft creation via the EXISTING
authorized POST is explicitly authorized in this task: read real handler/DTO/validation, use the server's real binding/
registry/geography facts, create at most ONE user-trial draft with minimal valid sections (empty groups if lawful).
Do not implement whole-method creation, clone/delete existing drafts, invent registry facts, write raw SQL business rows,
or seed an unrelated catalog. Keep this real draft for user trial and record its id/binding.
If a draft already exists, reuse it and do NOT overwrite its authored content just to prove persistence.

Open actual route /admin/shipping-services/authoring/{realDraftId}?mode=view in installed browser with
real /v1 responses and normal documented dev authorization. NO Fetch fulfilment/mock/response replacement.
Do not treat fresh headless profile as the user's authenticated profile; existing authorized DEV actor mechanism
may be used as the real application already does. If interactive sign-in is genuinely required, report exact wall
and API outcomes; do not collect credentials.
Verify visible General, real configured content languages/type resolution, groups/rates/week/settings and revision,
truthful null/unconfigured/inactive states, no route/load blocker or directly relevant console exception.
Enter Edit and exit CLEAN to establish reachability; no need to replay syntheticA-E or run a visual matrix.
No mutation of existing authored content solely for test. New draft creation above is enough when needed.
If a narrowly caused live integration defect blocks the page, repair it within existing ownership/contracts after
reading applicable skill; one focused relevant check, not whole suites/refactor/architecture expansion.
Max two bounded source/harness correction iterations. If still blocked, report the exact blocker promptly.

Deliver / recovery

READY means a real mounted page with real backend/database reads, actual authorized draft id and usable URL,
not health200 or page-shell200 alone. Report live write acceptance separately; do not infer Apply persistence
if not exercised on existing real data. No runtime quote/activation/whole-method creation claim.
Update canonical recovery TOP with:

explicit user local-dev authorization (migration+Host startup effects+minimal trial draft), no future duplicate approval;
actual completed migrations vs pending; real runtime executable/PID/source/config and FE route;
real draft/binding and exact user-test URL, or first unresolved blocker and next concrete action;
accepted190–205 unchanged; NO replay; state READY_FOR_USER_TRIAL or exactBLOCKED;
retained runtime/output and any task-owned helper cleanup.
Update root AGENTS current checkpoint only if needed; historical review/evidence remains unchanged.
Evidence208/RESULT.md includes actual commands/exit codes, redacted target identity, migration ids/outcomes,
startup seed/worker observations, real API/mounted outcome, draft creation yes/no, business writes exact scope,
elapsed/deviations, changed paths, commit/push/fetch parity.
No dummy Bridge probe; no historical post-result.js execution. No secrets/build artifacts in git.
Commit only task-owned source/docs/evidence. Preserve unrelated untracked/bin/obj/.next work.
Leave working user runtimes up; stop only task-owned temporary browser/harness processes.
Return complete result, STOP_FOR_ARCHITECT. Architect will review promptly and call the user for trial when ready.
