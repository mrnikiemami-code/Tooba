PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-MVP-SHADOW-PARITY-068
Parent-Task: TB-SHIPPING-MVP-DESTINATION-LIVE-067
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-MVP-OPERATIONAL-CLOSURE
Mode: BOUNDED_EXISTING_DEV_SHIPPING_POLICY_PARITY_PROOF
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 2bca9e0e602c7a025108215edf0986a1a31b7346
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 minutes HARD

ARCHITECT RULING
067 ACCEPT only for city-id propagation + authenticated old/new projection equivalence with shadow OFF. Its elapsed >45 minutes is NOT accepted as a new practice. Exactly three numbered tasks remain in the agreed 8-task 063–070 budget: 068, 069, 070. This task must make measurable progress toward live Shipping authority while preserving legacy Checkout. User confirmed current tooba_alpha database is disposable Development/test data; targeted edits to test data are allowed, but staging/production and unidentified processes are not. Do not create scratch/parallel databases, repeatedly rebuild Host, repeat 063–067 test matrices, or incur waiting loops.

PREFLIGHT
On BRIDGE-WAKE claim exactly one pending task for tooba-main; preserve exact received .task.md receipt unstaged. Read AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, 057 and 063–067 Shipping reports, current Order shadow evaluator/policy, Shipping rates/coverage lookup and active Fulfillment shipping-service codes. Fetch origin/main. Expected base may be followed only by path-by-path proven documentation/result-only commits; overlapping source changes => RECOVERY_CONFLICT/STOP. Preserve all unrelated dirty/untracked files; never reset, stash, rebase, force push.
Identify current Development Host/DB by process + /__platform-commerce + environment and configuration, not old PID assumptions. DB must be positively proven to be test tooba_alpha, no production configuration. Do not shut down an otherwise working host merely to run Host.Tests.

MISSION — ONE OUTCOME: REPRODUCIBLE, HONEST SHADOW PARITY AND POLICY READINESS
A. From current source, document EXACT actual storefront shipping method codes -> Fulfillment service codes, their legacy quote amounts/currency/free thresholds/preparation and lead semantics, and the Shipping-owned rate/coverage resolution precedence. Identify which active methods/services and representative city/area destinations are comparable. Do not equate methodCode with serviceCode. For any missing mapping or unrepresentable legacy semantic, record a specific gap, not a guessed match.
B. Existing Development tooba_alpha ONLY: record baseline Shipping rates/grants (including six inactive 065 fixtures); use real Geography IDs and real active Fulfillment service codes. Configure the smallest EXPLICITLY LABELED SYNTHETIC DEVELOPMENT-ONLY set of active rate policies + grants to exercise representative destinations and mapped methods, using existing authorized Shipping admin HTTP routes where practical. The values are test fixtures chosen to reproduce measured legacy behavior for comparison, NOT user-approved commercial rates. Avoid inserting broad IR grants unless strictly needed for a representative case, document exact boundaries. Do not touch existing rows except clearly identified prior 065 test fixtures where needed. Provide exact fixture IDs, safe reversible cleanup/deactivation strategy, and before/after counts. No manually fabricated roles, no SQL bypass of business rules. If the intended data shape cannot express a legacy method variant or delivery semantics, record as blocking mismatch instead of forcing parity by fake scope/rules.
C. Enable BOTH ShadowEnabled and PrerequisitesConfirmed ONLY in a temporary Development-scoped runtime override (never production config, never default flags). Reuse the live authenticated 067 Development cart/session if safely available; otherwise create ONE new ordinary dev cart through canonical auth/cart routes, not a new database. Run a SMALL matrix of genuine storefront projection requests with valid selected province/city Geography identities and relevant method codes: report legacy amount/eligibility/promise, shadow outcome and evaluator amount/coverage, whether genuinely comparable, mismatch category and cause. Provide a precise diagnostic of whether shadow actually ran (not inferred from unchanged projection). Legacy returned projection MUST remain identical with shadow ON/OFF; no Checkout mutation/cutover. If a restart is unavoidable for temporary override, exactly one controlled stop/relaunch of identified Dev Host with a recorded rollback and restore original flags at end; no build while host locks DLLs. Prevent capture of passwords/tokens or giant host logs.
D. Fix only a directly observed, narrowly scoped defect preventing reliable parity inside Shipping/Order-owned files if there is a clear low-risk change with tests and enough time. Otherwise mark PARTIAL with specific blocker; never silently rewrite pricing, service catalog, customer quote or snapshots. In particular do NOT create a cutover switch in this task. No shipping data should be represented as approved production tariffs.
E. Focused regression ONLY: 1–3 representative live projections; only targeted Order/Shipping tests for changed source; build/typecheck only if touched. No broad Host guard suite, no second scratch environment, no repeated test loops. Record elapsed, head SHA, HTTP response comparison, codes, fixture IDs, gating state and process restore. End with Host healthy and shadow flags returned to OFF unless the pre-existing baseline was different; never leave test observation enabled accidentally.

ALLOWED PATHS

docs/planning/shipping/SHIPPING-MVP-SHADOW-PARITY-068.md
docs/architecture/evidence/TB-SHIPPING-MVP-SHADOW-PARITY-068/**
docs/ai/tasks/TB-SHIPPING-MVP-SHADOW-PARITY-068.task.md (receipt only, never stage)
src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/RateAuthority/** (ONLY if narrow evidenced defect)
src/backend/Modules/Order/Tooba.Order.Tests/Storefront/** (ONLY focused tests for approved defect)
src/backend/Modules/Shipping/Tooba.Shipping.Application/** (ONLY if narrow evidenced defect)
src/backend/Modules/Shipping/Tooba.Shipping.Tests/** (ONLY focused tests for approved defect)
No other source files, no Host source/config, no checkout/Payment/Order snapshot changes, no new frontend page.

ACCEPTANCE
PASS ONLY IF: real Development target proven; mapped real service codes and real Geography IDs; explicit clearly scoped synthetic DEV fixtures with reversibility; shadow actually enabled temporarily and produces recorded comparable evidence over authorized real-cart HTTP; amounts/eligibility/delivery semantics assessed and all mismatch causes identified; returned legacy projection unchanged; runtime override restored to OFF and host healthy. PARTIAL if a gate is not proven or observed mismatches remain; do not call mismatch parity success. If a true commercial decision is needed, report specific values/coverage requiring approval and continue only noncommercial checks. Never assert full Shipping or Checkout production readiness from synthetic parity.

RESULT CONTRACT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-MVP-SHADOW-PARITY-068
Parent-Task: TB-SHIPPING-MVP-DESTINATION-LIVE-067
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-MVP-OPERATIONAL-CLOSURE
Mode: BOUNDED_EXISTING_DEV_SHIPPING_POLICY_PARITY_PROOF
Bridge-Task-Row: <actual row>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 2bca9e0e602c7a025108215edf0986a1a31b7346
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE
<exact target provenance; service-method mapping; policy/grant fixture ledger; explicit shadow gate proof; live cart parity/mismatch matrix; unchanged legacy projection; restore/cleanup; focused tests; actual elapsed; paths; commit/push; blockers>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
