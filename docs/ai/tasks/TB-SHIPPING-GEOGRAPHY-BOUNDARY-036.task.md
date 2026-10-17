Task-ID: TB-SHIPPING-GEOGRAPHY-BOUNDARY-036
Parent-Task: TB-SHIPPING-GEOGRAPHY-DESIGN-035
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ARCHITECTURE-PLANNING
Mode: DESIGN_ONLY_SOURCE_GROUNDED_ADR
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: a814b8cb924e0c591b36cd99e561ad9bf2b020eb
Repository: mrnikiemami-code/Tooba
Branch: main
Timebox: 30 minutes HARD
Automatic-Next-Implementation-Task: NONE

ARCHITECT DECISION / PREREQUISITES

Architect DESIGN ACCEPT granted for 035 as planning evidence ONLY; no geography module, migration, endpoint, or change to checkout has been authorized.
Read AGENTS.md, docs/planning/shipping/SHIPPING-CURRENT-STATE-AUDIT.md, docs/planning/shipping/SHIPPING-GEOGRAPHY-DESIGN-035.md, and docs/architecture/SHIPPING-ARCHITECTURE.md (DRAFT, NOT CERTIFIED).
Respect locked architecture: Host composition only, Contracts-only cross-module, no duplicate authority, existing storefront geography architecture guard stays intact.

PREFLIGHT — STOP ON UNSAFE STATE

Confirm branch=main; fetch origin; record HEAD, origin/main and worktree status. Require HEAD == origin/main == Expected-Base-SHA. If not equal, STOP and report exact SHAs; NO merge, reset, rebase, stash, cherry-pick, force-push or silent baseline correction. Preserve unrelated tracked/untracked work.
Verify no other active Bridge task. Only execute this received task after authorized BRIDGE-WAKE; do not poll Bridge while idle.
Verify intended new output file does NOT exist. If it exists, STOP rather than overwrite:
docs/planning/shipping/SHIPPING-GEOGRAPHY-BOUNDARY-ADR-036.md
Inspect relevant existing Catalog, AddressBook, Localization, Order, Fulfillment and their Contracts, plus HostStorefrontAmcR3GuardTests; use exact paths/line numbers. Do not assume existing semantics from the architecture draft.

OBJECTIVE — BOUNDED GEOGRAPHY OWNER / CONTRACTS DESIGN ONLY
Resolve the ownership decision left OPEN in 035 before any schema, code or wiring. The target shipping selector is Country → City → Sub-city/Service Area, with optional Province/State/AdministrativeArea model for globally varying administrative hierarchies. Stable IDs and localized labels must be separate. This task delivers a source-backed decision candidate for Architect acceptance, NOT an implementation.

BOUNDED WORK

D3 Ownership: compare hosting a globally shared geography reference catalogue in an existing relevant module vs a dedicated neutral capability. Recommend ONE authoritative owner and explain responsibilities, dependency direction and Contracts-only read ports. AddressBook owns customer addresses; Order owns immutable committed destination snapshot; Shipping owns carrier origin/destination coverage; Host composes only. No parallel geography catalogues or direct DB reads across modules.
D9 Localization: identify existing LanguageId translation precedent and define catalogue display-label translation ownership/fallback while IDs/codes remain language-independent. Mark any unverified generic translation service OPEN instead of inventing it.
D8 Legacy compatibility: inventory GET /v1/storefront/geography/provinces, StorefrontIranGeography.Provinces symbol, StorefrontProvinceOption, frontend label-valued controls and pinned Host architecture test. Design additive/versioned contract and staged transition; do NOT remove, bypass or alter current route/guard/UI.
Correct rollout sequence: Phase 0 authoritative catalogue structure and minimal, explicit region data; separate service-coverage bootstrap; Phase 1 nullable IDs (future change, not in this task); Phase 2 legacy text resolution and reconciliation; Phase 3 fail-closed gate only after service coverage and compatibility have been verified. No blanket unrestricted grants inferred from empty AllowedProvinces defaults. Explicit safety checklist against denying all legitimate current Checkout shipping.
Preserve all existing order, payment and fulfillment semantics throughout this planning: shipping amount, selected service, time promise, historical snapshots, SellerOrder and shipments. No delivery-group implementation, carrier payable, pricing refactor or Shipping module extraction.
Provide a decision register with D3/D8/D9 plus pending D10; distinguish SOURCE-VERIFIED / RECOMMENDED / OPEN. Give exactly ONE narrow suggested next task for review only; do not create or execute it.

REQUIRED DELIVERABLE — EXACTLY ONE NEW DOCUMENT
Create docs/planning/shipping/SHIPPING-GEOGRAPHY-BOUNDARY-ADR-036.md with:

PROPOSED / NOT CERTIFIED header and audited base SHA;
source-backed ownership comparison and one recommended D3 owner, plus text dependency diagram;
Contracts-only boundary for geography lookup, Shipping serviceability, AddressBook and Order;
D8 guard-safe legacy API migration strategy, D9 translation fallback;
phased compatibility and fail-closed cutover gates with explicit operational safety preconditions;
decision register, unresolved facts/risks, and ONE bounded next-step recommendation;
exact source paths/line ranges and proof that the report alone was changed.

HARD GUARDS

Documentation ONLY. No C#/TS, frontend/backend/Host, endpoints/DTOs, tests, schema/migrations, DB query/write, seed, config, runtime deployment, service restarts, checkout/payment operations or Shipping module creation.
Do not edit AGENTS.md, architecture draft, audit 034 or design 035. No certification claims or implementation authority.
Preserve all pre-existing dirty/untracked files. Never stage or commit them.
No heavyweight tests; no hidden or placeholder Bridge POSTs; no idle polling.
Commit and push only the single authorized new document after verification, then STOP. Any drift, conflict, existing output or timebox overrun => STOP with accurate BLOCKED/PARTIAL report, not unsafe recovery.

ACCEPTANCE

Concrete, justified D3 owner recommendation grounded in actual source, with clear D8/D9 treatment and no invented certainty.
Explicit country/city/sub-city identity and optional administrative layers, while preserving historical addresses/orders.
Readable gate/checklist preventing checkout-wide shipping outages when coverage data is incomplete.
Existing geography endpoint and pinned guard remain unchanged; all code untouched.
Exactly one new document in git diff/commit; successful push verified by HEAD == origin/main; report actual elapsed time <= 30 min and exact checks.

REQUIRED WORKER RESULT — BRIDGE-WAKE-V1
Return the complete existing BEGIN_TOOBA_WORKER_RESULT ... END_TOOBA_WORKER_RESULT contract with Task-ID, Parent-Task, Pipeline-Protocol, Channel, Worker-Id, Agent-Type, Program, Mode, Bridge-Task-Row, Status, Stop-State, Expected-Base-SHA, Base-SHA, Code-SHA, Evidence-SHA, automaticNextImplementationTask=NONE; preflight including dirty status; audited evidence paths; D3/D8/D9 conclusions vs unknowns; exact changed path; git proof; elapsed minutes; non-actions; push status. Do not claim Architect ACCEPT or schedule next work.
Final state: STOP_FOR_ARCHITECT; automaticNextImplementationTask=NONE.
