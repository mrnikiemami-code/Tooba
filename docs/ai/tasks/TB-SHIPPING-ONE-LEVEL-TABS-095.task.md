PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-095
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-094
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_SAFE_INDEPENDENT_METHOD_ACTIVATION
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 1bbf2607cb21792282aa3b20c7d55feb66c85a5c
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD

ARCHITECT RULING
094 is ACCEPT for independent display-name editing and live browser proof, but actual duration ~205 minutes exceeded the hard stop. Do NOT repeat this. 095 is a single narrow incremental task: determine whether existing one-level method activation/deactivation can be safely managed independently without contradicting current legacy Fulfillment catalog and storefront behaviour. If a safe atomic ownership-preserving implementation and focused proof cannot be completed in 30 minutes, STOP with PARTIAL plus concrete blocker, not a speculative expansion. No method CREATE in this task. No Checkout cutover.

PREFLIGHT
Claim exactly one Pending Bridge task, preserve the received task file UNSTAGED. Read AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, latest master recovery, 081-094 records, 094 source changes, Fulfillment ShippingMethod and legacy ShippingService/ShippingServiceOption, ShippingServiceDirectory.UpdateAsync, store catalog reader, storefront eligibility, gate and auth. Fetch origin/main. Expected SHA may be an ancestor ONLY across positively proved result-bookkeeping-only drift; overlapping source drift => RECOVERY_CONFLICT/STOP. Preserve all pre-existing dirty/untracked files. No reset/stash/rebase/force push.

MISSION A — VERIFY ACTIVATION AUTHORITY FIRST (MANDATORY)

Map activation authority for active/inactive one-level methods and the legacy service/option catalog. Explicitly inspect the destructive delete-and-reinsert legacy option update path, the independent method row, service-level optionless methods, current storefront method projection and historical codes. Prove what activation of post:express vs post:standard would mean and whether a method toggle without synchronizing legacy state would mislead the merchant.
Decide whether a minimal transactionally consistent write is possible entirely in Fulfillment via module-owned interfaces, without rewriting option IDs, impacting sibling methods, or activating inactive parent services. A bare update of fulfillment.shipping_methods.is_active while the legacy storefront still uses old activation is FORBIDDEN as a misleading apparent activation. Optionless service handling must be correct. Never touch legacy option UpdateAsync destructive replacement.
If full consistency is not provable in the timebox, document an explicit BLOCKED_ACTIVATION_AUTHORITY decision, leave activation controls read-only, and STOP. This is preferable to a misleading green button.

MISSION B — ONLY IF A IS PROVEN AND TIME REMAINS
4. Implement the smallest module-owned CQRS/ISender + Result + ApiResponseFactory authorized admin toggle seam, behind the existing Fulfillment gate and permission convention. Method identity is stable immutable method_code (max 32). Server resolves legacy binding, never trusts client-provided serviceCode/optionCode, and fails closed for missing/inactive parent, unknown/inactive code, gate OFF, not-ready schema and authorization failures. Treat activation and deactivation distinctly; no mass updates. Preserve historical records.
5. Expose a simple enable/disable action in tab «سرویس‌ها» only when the server outcome can honestly represent customer-eligibility state. Require explicit user confirmation on deactivation; do not silently activate post:express or snapp_courier in Development. No arbitrary service creation, no new nested option editor, no provider level. Display any 'not yet reflected in storefront' limitation clearly if applicable, rather than claiming success.
6. Add the minimum focused tests for one method only, sibling preserved, inactive parent refusal, no destructive option rewrite, gate OFF zero DB calls, auth denied, identity immutability and old code safety. Run minimal build/tests in isolated OutDir if Host output is locked; no retry loops. Do not run full suites.

LIVE SAFETY
The shared Development :5088 Host is script-managed after 093. Do not restart it unless an executable change genuinely requires it and scripts/dev/host-stack.ps1 proves ownership, health/provenance and rollback. Keep Tooba:ShippingRateAuthority and Tooba:ShippingOperationalShadow OFF. Verify target dev-singlestore/store-alpha/tenant-alpha before even read-only probes. Do not modify actual business rates or active/inactive service state in Development to 'test' activation; use an isolated DB or task-owned disposable fixture and prove rollback. No staging/production contact. No payment/settlement/Checkout mutation, no capacity booking or delivery promise.

ALLOWED PATHS

src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/Shipping/**
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Shipping/**
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Endpoints/Shipping/ShippingServiceEndpoints.cs
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/DependencyInjection/FulfillmentModule.cs (minimum additive DI only)
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Behavior/ShippingOneLevel* (focused tests)
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Architecture/FulfillmentValidatorCoverageGuardTests.cs (count only if needed)
src/backend/Host/Tooba.Host.Tests/Architecture/FulfillmentModuleAmsc001W3CertGuardTests.cs (count only if needed)
src/frontend/app/admin/shipping-services/shipping-one-level-*.ts
src/frontend/app/admin/shipping-services/shipping-one-level-*.tsx
docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-095.md
docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-095/**
docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-095.task.md (receipt ONLY, never stage)
Any broader source path (e.g. legacy catalog persistence) needed for consistency is OUT OF SCOPE: stop and request scoped architect authorization rather than editing it silently.

FORBIDDEN
No fake independent activation, no loss of service-option integrity, no historical code rewrite, no broad migration, no uncontrolled Host restart, no frontend auth bypass, no anonymous admin elevation, no changing Checkout authority or pricing, no inventing packed measurements or COD settlement, no extra work beyond 30 minutes.

ACCEPTANCE
PASS only if activation semantics are genuinely consistent with the operational legacy catalog, minimal authorized implementation is committed and focused tests prove this. PARTIAL if root-cause and safe design documented but a source change requires broader ownership; BLOCKED if safety or access cannot be proven; RECOVERY_CONFLICT on overlapping drift. State exact elapsed and changed files. No auto 096.

RESULT CONTRACT — BRIDGE-WAKE-V1
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-095
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-094
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_SAFE_INDEPENDENT_METHOD_ACTIVATION
Bridge-Task-Row: <actual>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 1bbf2607cb21792282aa3b20c7d55feb66c85a5c
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE
<activation authority finding; legacy parity decision; actual implementation/proof if any; limitations; exact elapsed; touched paths; rollback>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
