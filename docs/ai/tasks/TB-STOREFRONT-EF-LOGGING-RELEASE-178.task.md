PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-STOREFRONT-EF-LOGGING-RELEASE-178
Parent-Task: TB-SHIPPING-PACKAGING-CUSTOMER-CHARGE-177
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: fce505a2be1388aca1eca7b8e0b4f944923aad49
Program: TOOBA-STOREFRONT-PERFORMANCE-RELEASE-CLOSURE
Mode: BOUNDED_EF_LOG_VOLUME_REPAIR_AND_STOREFRONT_SCALE_PROOF
Hard-Timebox: 45 minutes HARD
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Close the outstanding EF Core Debug/stdout log-flood portion of PERF-CRIT-001 with a narrowly scoped production-safe change and measured evidence. Preserve the already delivered N+1 optimization and store appearance, offer/price/stock behavior, tenant isolation, shipping drafts and checkout. This is an implementation and runtime-evidence task, NOT a new architecture proposal. The unresolved TB-177 packaging payer/tax/settlement policy stays BLOCKED; do not charge packaging to customers or invent a financial allocation.

MANDATORY FIRST CHECK

Verify HEAD == origin/main == Expected-Base-SHA. If not, STOP with exact divergence; never reset/rebase/autostash/force push.
Read docs/planning/MUST-FIX-BEFORE-RELEASE.md (PERF-CRIT-001), docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-16.md, TB-177 result and evidence, and the actual performance improvement evidence for commit 73160e3. Locate EF logging setup and stdout consumers in the real composition/configuration owner; inspect the active Development/Production log levels, query logging and sensitive-data safeguards before editing.
Follow existing naming, capability-first folder/namespace and architecture guard conventions; no new generic Utilities/Helpers/Services root and no Host business logic. Source file path and namespace must match canonical module owner. Existing task naming/protocol fields are fixed by this envelope.

BOUNDED IMPLEMENTATION
A. Identify and prove the actual log-flood source: EF Core command/category Debug logging, host stdout sink, sensitive-data logging, duplicate providers or other cause. Do not assume every stdout line is a DB command. Measure a bounded, same-correlation-request baseline on a disposable loopback environment or use already reproducible sanitized baseline artifacts; record environment and limits.
B. Apply the SMALLEST production-appropriate logging configuration change at its legitimate composition/configuration owner: retain errors/warnings and useful structured diagnostics; prevent high-volume EF Debug command dumps by default, including Development unless deliberately opted into a bounded diagnostic session. Do not hide exceptions, SQL execution failures, app errors or security audit events; do not expose SQL parameters/secrets. Explicitly establish production-default and development-default behavior; opt-in diagnostics must be safe and traceable.
C. Run one bounded comparable repeat measurement (same data/route and request scope), including /v1/storefront/home and /fa only when private customer/frontend harness is available safely; shipping page only if safely executable. Capture wall latency, DB command count as a distinct metric, request-scoped stdout size and EF-category log lines, plus any remaining errors. Do one small, controlled scale-check comparing two data sizes if fixtures already permit it without extensive setup. Verify no storefront content/price/stock/seller projection change and no shared-state writes.
D. No cache purge/restart of shared Next/Host; no proxy timeout increase, no reduction of returned catalog items, no fake performance PASS. If the root cause requires larger structural work or the scale-check is unavailable within 45 minutes, ship only the bounded safe logging fix with focused tests and report PARTIAL with exact missing acceptance evidence. Do not turn PERF-CRIT-001 to CLOSED unless its complete official definition of done has been met AND Architect approval remains pending.

REQUIRED VERIFICATION

Minimum focused configuration/guard tests for Development/Production defaults, scoped diagnostic opt-in (if added), warning/error retention and sensitive-data logging disabled. Run only the essential build/test set and relevant pre-existing guards; do not rebaseline unrelated red tests.
Genuine before/after measurements from the same private setup, with exact commands, source SHA, dataset/request counts, units and raw sanitized evidence. Clearly distinguish query count, bytes, time and logging line count; no synthetic benchmark numbers.
Demonstrate unchanged Home product identifiers/names/prices/availability and unchanged authorized tenant data at the comparison boundary; if unable, explicitly mark unverified.
Record source paths and owner, tests, evidence, cleanup and HEAD/origin parity. Use sanitized task-owned evidence under docs/architecture/evidence/TB-STOREFRONT-EF-LOGGING-RELEASE-178/. Update release-blocker register only to describe actual measurements, never claim entire blocker CLOSED merely because logging improved.

HARD SAFETY / STOP
One active task only, HARD 45-minute cap. Never touch shared :3000/:5088/:5432, tooba_alpha/store-alpha, shared RabbitMQ/Redis, running Host/Next processes, shared .next caches, unrelated dirty Admin file, TB-075/079 logs, TB-173 files or any stash. Disposable loopback scratch resources only with fail-closed isolation guard, exact-PID cleanup and no leaked tokens/connection strings. No auth bypass, SMS, PSP, payout, migrations on shared data, Host business logic, cross-module DbContext, Results.Json, architecture guard suppression or blanket logging disablement. Fast-forward commit/push only. At deadline, stop, report PASS/PARTIAL/BLOCKED and STOP_FOR_ARCHITECT; never auto-claim another task.

PASS / PARTIAL / BLOCKED
PASS for this bounded task requires proven production-safe EF log reduction on repeatable representative requests with errors still logged, no PII exposure and no behavioral regression; a complete PERF-CRIT-001 closure additionally requires the release register's full performance, scale and user verification criteria and explicit architect acceptance. PARTIAL when safe changes ship but comparative measurement or scale/behavior evidence is missing. BLOCKED if no safe owner/configuration or baseline exists; deliver exact code/source blocker and do not invent outcomes.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-STOREFRONT-EF-LOGGING-RELEASE-178
Parent-Task: TB-SHIPPING-PACKAGING-CUSTOMER-CHARGE-177
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-STOREFRONT-PERFORMANCE-RELEASE-CLOSURE
Mode: BOUNDED_EF_LOG_VOLUME_REPAIR_AND_STOREFRONT_SCALE_PROOF
Status: PASS|PARTIAL|BLOCKED
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
Root-Cause-And-Owner: ...
Source-Changes: ...
Focused-Tests-And-Guards: ...
Before-After-Measurements: ...
Scale-And-Payload-Parity: ...
Security-Logging-Guard: ...
Remaining-Release-Blockers: ...
Isolation-And-Cleanup: ...
Elapsed-Minutes: ...
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
