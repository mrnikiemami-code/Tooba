PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-081
Parent-Task: TB-SHIPPING-FULL-OPERATIONS-080
Channel: tooba-main
Worker-Id: tooba-worker-01
Program: TOOBA-SHIPPING-ONE-LEVEL-UI-AND-COMMERCIAL-CLOSURE
Mode: BOUNDED_READ_ONLY_ARCHITECTURE_AUDIT_AND_IMPLEMENTATION_DESIGN
Timebox: 30 minutes HARD
Stop-State: STOP_FOR_ARCHITECT

Objective

Prepare a source-grounded, safe implementation blueprint for converting Shipping management from nested service/option concepts and scattered row-action dialogs into one-level independently named shipping methods with a coherent tabbed management view. Examples: «پست معمولی», «پست پیشتاز», «تیپاکس معمولی», «تیپاکس اکسپرس». These names are examples only; do not create them as live data. Do NOT implement the conversion in this task.

Before working
Fetch current origin/main and report HEAD, working tree state and existing dirty/untracked paths; preserve them unchanged.
Read docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md (latest Shipping checkpoint), docs/planning/shipping/SHIPPING-FULL-OPERATIONS-080.md and the verified 079/080 evidence.
Locate and inspect actual sources for Fulfillment-owned shipping service/option catalog and storage; Shipping rate, coverage, schedule, packaging and operational quote policies; Order storefront shipping projection and legacy Checkout; current frontend src/frontend/app/admin/shipping-services-screen.tsx and dialogs in shipping-services/.
Required architecture decisions and evidence

A. Inventory all identifiers and references (service ID/code, option ID/code, foreign keys, read/write contracts, UI forms, current Development data). Show concrete file paths and line or symbol references. Distinguish the existing code's structure from the proposed structure.
B. Design ONE logical level in the merchant UI: each selectable/shown delivery method independently named and configured, without a nested service/option picker. Determine whether to preserve an internal provider/service grouping in the database for backwards compatibility; if so, it must be hidden from merchant configuration and must not be misrepresented as a second merchant level. Specify stable identity and safe transformation for existing post, tipax:standard, tipax:express, store_courier, in_person, snapp_courier. Never conflate options or erase historical order identities. Existing records are not to be migrated in 081.
C. Propose one coherent tabbed interface on /admin/shipping-services: «سرویس‌ها», «تعرفه و پوشش», «بسته‌بندی و کرایه», «برنامه کاری و ظرفیت», «بررسی هزینه». Describe selected-method context, list/empty/error/loading states, save/cancel, permission-denied behavior, RTL/responsive layout, and accessible tab navigation. Eliminate confusing icon-only scattered modal entry points, but preserve working functionality.
D. Trace effects of one-level identity on Shipping.Contracts, Geography, Fulfillment, Order, historical paid snapshots and legacy Checkout; document a migration/backward-compatibility/rollback strategy, including feature gating if needed.
E. Reconcile the unfinished capabilities: trusted packed weight/count provenance, actual capacity reservation/claim/release, COD collection/settlement, customer delivery promise, merchant-approved tariffs, and commercial Checkout cutover. Assign owners and dependencies; do NOT claim they can all be completed by UI work. Propose bounded delivery plan in at most 11 tasks TOTAL including 081, with clear evidence-based acceptance criteria and realistic dependencies. No implementation task may silently activate Checkout.
F. Record risks, blockers, minimum targeted tests, and an explicit recommended scope for task 082. If architectural uncertainty cannot be resolved from source, state STOP/BLOCK rather than guessing.

Non-negotiable guardrails
READ-ONLY audit + documentation inside this task. No production code edits, database/migration writes, seed changes, unauthorized service restart, or runtime process interruption.
No new Checkout price authority, paid order/snapshot mutation or Shipping shadow gate enabling.
Fulfillment owns delivery-method catalog; Shipping owns rates/coverage/schedule/operational quote; Geography owns place identity; Order owns customer promise/paid snapshots; Payment/Settlement own collections.
Host composition only; Contracts-only cross-module seams; MediatR 12.5 ISender; Result/ApiResponseFactory; no Results.Json and no cross-module DbContext.
Do not mark configured capacity as enforced or COD quote as settled; preserve legacy-compatible semantics until explicit user authorization.
Existing worktree modifications must be preserved. Do not run broad expensive test suites for a read-only audit.
Timebox 30 minutes HARD; stop and report PARTIAL/BLOCK if unresolved. No automatic 082.
Deliverables
A concise markdown architecture report in docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-081.md with grounded paths, impact map, one-level identity proposal, 5-tab interaction plan, cutover/rollback plan, task 082 scope and <=11-task roadmap.
If documentation mutation is safely allowed by the current worktree and branch, commit/push ONLY that report; otherwise provide the report as a task result without altering existing files. Never edit master recovery automatically.
Canonical result envelope:
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-081
Parent-Task: TB-SHIPPING-FULL-OPERATIONS-080
Channel: tooba-main
Worker-Id: tooba-worker-01
Status: PASS | PARTIAL | BLOCK
Base-SHA: <full SHA>
Code-SHA: <full SHA or UNCHANGED>
Evidence-SHA: <full SHA or NONE>
Elapsed-Minutes: <actual>
Changed-Files: <explicit list>
Findings: <source-grounded summary>
Blockers: <explicit list>
Recommended-Next-Task: 082 proposal ONLY, not started
Stop-State: STOP_FOR_ARCHITECT
END_TOOBA_WORKER_RESULT
END_TOOBA_WORKER_TASK