PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-STOREFRONT-PERF-SEED-AUTH-SCALE-181
Parent-Task: TB-STOREFRONT-PERF-SCALE-CLOSURE-180
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: b5af49751de3e03ebe3777fde94b58bcbb3365a5
Program: TOOBA-STOREFRONT-PERFORMANCE-RELEASE-CLOSURE
Mode: BOUNDED_OWNER_SEED_AUTHORITY_AND_TWO_SIZE_SCALE_PROOF
Hard-Timebox: 45 minutes HARD; stop and report PARTIAL if not proven by then.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Resolve the precise authorization blocker that prevented TB-179/180 from invoking the EXISTING Catalog-owned Development seed-only route in a disposable environment, then obtain reproducible two-size storefront Home measurements (size B >=4x size A) ONLY if the legitimate owner-authorized fixture can produce them. Do not repeat prolonged blind seeding; do not fake actor permissions, manufacture auth/session claims or hand-clone DB rows. This is a bounded investigation plus the smallest legitimate repair if an actual defect is found—not permission to weaken access control or rewrite the seeder.

MANDATORY FIRST CHECK

Verify HEAD == origin/main == Expected-Base-SHA and preserve all unrelated worktree/stashes. Read docs/planning/MUST-FIX-BEFORE-RELEASE.md, docs/architecture/evidence/TB-STOREFRONT-PERF-SCALE-CLOSURE-180/RESULT.md, previous TB-179 result, and relevant Catalog seed endpoint/handler, Admin access-policy checks, tenant/store actor bootstrap, and existing dev authorization contracts.
Trace the exact POST /v1/admin/catalog/demo/seed-only request to its owner handler and authorization policy. Explain the verified 403 admin.authorization.denied (and earlier 400) using real code, actor/store/tenant ownership and recorded response. Identify whether there is an approved Development-only fixture invocation that does not bypass security. Do not assume X-Tooba-Dev-Actor-User-Id alone grants store management.
Check whether seed-only adds >=4x useful products/offers/stock or is intentionally idempotent/fixed-size; inspect before running. If incapable of required B, report this blocker promptly rather than retrying seven minutes.

BOUNDED IMPLEMENTATION AND PROOF

If the scratch fixture bootstrap omits an ALREADY APPROVED store-membership/access grant, use the existing authorized provisioning seam to establish it in the disposable scratch only. Never manually insert identity/access-control role data or add global permissions. If production authorization truly rejects an authorized actor due to a defect, make only a minimal owner-correct fix with narrowly scoped denial/allowance tests; no Host business authority, no blanket dev bypass or security weakening.
Verify one owner seed-only request with genuine authorized Admin identity in the disposable Host; log bounded status/duration/row deltas. Explicit per-attempt timeout <=60 seconds, at most 2 controlled seed attempts; STOP if it cannot legitimately advance or if required data growth is structurally unavailable. Do not reset/reseed the same rows repeatedly, do not forge a 4x result.
Only if TWO legitimate nonempty sizes exist with B >=4x A by product rows (and report offer/price/stock counts), collect GET /v1/storefront/home?locale=fa under same Host/binary/settings, one warm-up + three measured requests per size. Use EF=Information diagnostics in disposable instance only to count request-scoped Database.Command and separate startup activity; committed default EF>=Warning unchanged. Report per-size median/p95-ish observed wall (do not extrapolate), bytes, command counts, growth ratio, and field-level checks for product IDs/slugs/names, seller, offer, price/currency, stock/availability, media references. Capture tenant isolation only if genuinely exercised; no invented claim.
If this cannot be safely completed within 45 minutes, deliver a reproducible source-backed root cause and the smallest authorized next action as PARTIAL/BLOCKED. Do NOT mark PERF-CRIT-001 CLOSED. Reuse prior evidence/scratch recipes and avoid fresh comprehensive harnesses or unrelated code changes.

ACCEPTANCE / SAFETY

PASS only with legitimate authorization (no bypass), real >=4x product-size A/B, request-scoped command accounting, full product/price/stock/seller parity validation, focused tests if code changed, and exact evidence. Otherwise PARTIAL/BLOCKED with precise remaining dependency.
Keep shared :3000/:5088/:5432, tooba_alpha, store-alpha, existing Host/Next, shared Redis/RabbitMQ and .next caches untouched. Only guarded disposable loopback Host/DB. Do not reset/stash/rebase/force-push, modify financial policy or shipping, change production feature flags, fake data provenance, suppress failing tests, or expand modules across Contracts-only boundaries.
Keep artifacts at docs/ai/tasks/<Task-ID>.task.md and docs/architecture/evidence/<Task-ID>/, exact naming/path/namespace conventions and architecture guards; no baseline rebaseline. Sanitize logs and remove private resources by exact PID/container before reporting. Push fast-forward only. STOP_FOR_ARCHITECT, no automatic next task.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-STOREFRONT-PERF-SEED-AUTH-SCALE-181
Parent-Task: TB-STOREFRONT-PERF-SCALE-CLOSURE-180
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-STOREFRONT-PERFORMANCE-RELEASE-CLOSURE
Status: PASS|PARTIAL|BLOCKED
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
Authorization-Root-Cause: ...
Legitimate-Seed-Provenance: ...
Size-A-And-B: ...
Request-Scoped-Performance: ...
Payload-And-Tenant-Parity: ...
Focused-Tests-And-Architecture-Guards: ...
Remaining-Release-Gates: ...
Isolation-And-Cleanup: ...
Elapsed-Minutes: ...
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
