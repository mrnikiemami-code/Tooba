Task-ID: TB-SHIPPING-GEOGRAPHY-DESIGN-035
Parent-Task: TB-SHIPPING-AUDIT-034
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ARCHITECTURE-PLANNING
Mode: DESIGN_ONLY_SOURCE_GROUNDED_ADR
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: bd382eb94106005c8692f7f46b03a0a2a3908616
Repository: mrnikiemami-code/Tooba
Branch: main
Timebox: 30 minutes HARD
Automatic-Next-Implementation-Task: NONE

ARCHITECT DECISION / PREREQUISITES

Architect ACCEPT granted for audit 034 only. This task authorizes ONE geography design document, not any product code or migration.
Source of evidence: docs/planning/shipping/SHIPPING-CURRENT-STATE-AUDIT.md at audited SHA; docs/architecture/SHIPPING-ARCHITECTURE.md is DRAFT, not certified.
Read AGENTS.md and existing module governance before work. Never override locked Host-only-composition, Contracts-only cross-module or existing architectural guard.

PREFLIGHT

Verify git branch main, fetched origin/main and Expected-Base-SHA, and record status. If HEAD differs, STOP and classify drift; do not merge/reset/rebase/stash/cherry-pick/force-push without explicit task authority. Pre-existing dirty files remain untouched. Never claim an unrelated dirty worktree is clean.
Verify output file does not already exist, else STOP rather than overwrite:
docs/planning/shipping/SHIPPING-GEOGRAPHY-DESIGN-035.md
Check no other active task. Do not restart services, query live DB, run migrations, touch Host, or modify runtime state.

OBJECTIVE — DESIGN GEOGRAPHIC IDENTITY BEFORE IMPLEMENTATION
Resolve B1 from audit 034 with a source-grounded proposal for stable destination identity and fail-closed delivery serviceability, suitable for multilingual, multi-seller Tooba and future cross-border shipping. Maintain user-facing Country → City → Sub-city/Service Area UX while keeping flexible administrative levels (Province/State) in the data model. This is a PROPOSAL subject to Architect acceptance, not an implementation order.

BOUNDED DESIGN SCOPE

Examine existing AddressBook address DTO/model, checkout snapshot, Order geography endpoint, StorefrontIranGeography constant and pinned architecture guard, service rate AllowedProvinces, Shipping catalog reader and any canonical Localization data/contracts. Cite real exact paths and line ranges; do not assume a reusable generic geography model exists.
Define entities/value identities and ownership candidates: Country, AdministrativeArea (optional state/province hierarchy), City, ServiceArea; globally stable IDs/codes; localized names; seller dispatch origin vs buyer destination; address reference and immutable Order destination snapshot. Address example Iran → Bandar Abbas (city) → Bandar Abbas (area) without treating identical names as identities.
Design coverage resolution contract (country/city/area, service provider, origin/destination), unknown/blank/ambiguous destination handling (FAIL CLOSED), and compatibility with existing province-based allow-lists. Identify how data creation, localization and versioning operate conceptually; no seeded nation-wide assumptions.
Propose an incremental backward-compatible migration/rollout plan only: no downtime assumptions; existing free-text addresses and historical Order snapshots remain interpretable; avoid rewriting existing committed orders. Show introduction of optional IDs, reconciliation states for legacy records, validation gates and staged cutover strategy, with explicit handling of the pinned StorefrontIranGeography guard. Do not remove or bypass guard.
Spell out intended module boundaries: address ownership versus geography reference catalog versus Shipping coverage policies; interfaces/contracts only across module boundaries; leave module extraction decision OPEN pending broader ADR.
Include at least six focused acceptance scenarios, e.g. exact match, duplicated localized names, blank province, incomplete legacy address, unsupported foreign destination, active coverage restricted to one sub-city, historical Order preservation. Scenarios are specifications, not executable tests.
Mark UNVERIFIED items, decision tradeoffs and precise next architectural choices; do not silently choose a schema location or data provider unsupported by current architecture.

REQUIRED DELIVERABLE — EXACTLY ONE FILE
Create docs/planning/shipping/SHIPPING-GEOGRAPHY-DESIGN-035.md with:

clear PROPOSED / NOT CERTIFIED header; audited base SHA and decision rationale;
existing evidence matrix with paths/lines and risks;
candidate domain/contract diagram (text or Mermaid) and sample identity/coverage rules;
old → new field mapping across AddressBook, Checkout/Order and Shipping projection;
rollout, backwards-compatibility and fail-closed matrix;
accepted vs pending decisions and six or more scenario specifications;
bounded recommendation for a future implementation task, explicitly NOT executed.

HARD GUARDS

Documentation ONLY. No C#/TS/frontend/backend/Host modifications, migrations, data writes, tests, config changes, seeds, endpoints, DTOs, service restarts, checkout/payment transactions, new Shipping module or wiring.
Do not edit docs/architecture/SHIPPING-ARCHITECTURE.md or audit 034; do not claim certification or ACCEPT.
Commit/push the sole new document only if preflight and permission checks pass, then STOP. If drift, conflict, file already exists or task overrun occurs: report BLOCKED/PARTIAL without unsafe actions. Maximum 30 minutes; no polling.

ACCEPTANCE

Every current-system claim is backed by source path/lines or marked unknown; target proposal is separated from source facts.
Coverage for blank/unknown destination is explicitly fail-closed; historical order promises remain intact.
Country/City/Sub-city UI works conceptually without hardcoding province absence or relying on translated labels as identity.
Only one target document appears in git diff and final commit. Include base, final SHA, elapsed time, all changed files and exact commands/checks.

REQUIRED WORKER RESULT — BRIDGE-WAKE-V1
Return BEGIN_TOOBA_WORKER_RESULT through END_TOOBA_WORKER_RESULT under established complete Result contract. Include Task-ID, Parent-Task, Pipeline-Protocol, Channel, Worker-Id, Agent-Type, Program, Mode, Stop-State, Expected/Base/Code/Evidence SHAs, verified evidence and paths, accepted/proposed/unknown separation, changed paths, commands, non-actions, elapsed minutes and actual push result. Finish STOP_FOR_ARCHITECT; automaticNextImplementationTask=NONE. Never submit dummy/probe Bridge result or start next task automatically.
