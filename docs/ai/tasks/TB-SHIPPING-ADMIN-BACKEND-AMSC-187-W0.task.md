PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W0
Parent-Task: TB-SHIPPING-ADMIN-UI-PREVIEW-186-R2
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 68d1aeb3525e8b2d43c719578b433874d26e09d4
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_AMSC_ANALYZE_CONTRACTS_AND_LEGACY_MAPPING_ONLY
Hard-Timebox: 45 minutes HARD; STOP and report PARTIAL if exceeded.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Prepare the first concrete backend implementation slice for the visually accepted Shipping Admin target, without creating fresh coupling. Execute AMSC Analyze (W0) on the bounded Shipping Admin authoring capability and its direct authoritative dependencies. Establish current ownership, lawful contracts, single-store versus marketplace authority, real database-backed language configuration, target aggregate/data mapping, and a safe legacy transition. Produce a source-backed implementable handoff, not another visual redesign or a speculative replacement shipping framework.
This task permits analysis and scoped documentation ONLY. Do not modify production code, schemas, tests, configuration or runtime data. Later implementation, Structure and Certify require separate Architect-issued tasks after this result is reviewed. Do not treat the existing Shipping module as absent or automatically reopen certified modules.

MANDATORY FIRST CHECK
Fetch origin/main, inspect HEAD/branch/dirty files/stashes and preserve all unrelated work. A strictly behind safe main may fast-forward only to the exact expected base. Before analysis require HEAD == origin/main == Expected-Base-SHA; mismatch/divergence/conflicting dirty files => STOP_FOR_ARCHITECT with RECOVERY_CONFLICT. No reset/rebase/stash/force-push. Execute only this received Bridge task and persist the receipt; do not replay previous tasks.

MANDATORY SKILLS AND SOURCE OF TRUTH
Read fully:

AGENTS.md and docs/ai/AGENT-SKILLS.md;
.cursor/skills/tooba-architecture-analyze/SKILL.md;
.cursor/skills/tooba-architecture-structure/SKILL.md (layout/handoff authority; inspection only here);
.cursor/skills/tooba-backend-feature/SKILL.md;
Migrate and Certify skill contracts for handoff awareness, without executing those waves.
Read applicable current architecture sources: TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md, TMAR-HOST-EVACUATION-PROTOCOL.md, TMAR-architecture-locks.md, TOOBA-REFERENCE-MODULE-PATTERN.md, relevant entries of tmar-current-state.json and tmar-module-structure-manifests.json, and actual dependency/guard evidence.
Read docs/architecture/shipping/SHIPPING-ADMIN-RULES-DECISION.md, especially sections 13–15 committed at the expected base, SHIPPING-STOREFRONT-UX-REFERENCE.md with its image, the Shipping architect recovery and current relevant financial/flat-method/delivery-commitment contracts. Reconcile superseded four-tab/pending-visual statements against the dated owner decisions; preserve unrelated locks. Do not read or update the opt-in findings register.

AUTHORITATIVE PRODUCT SCOPE
The owner accepted the TB-186-R2 visual baseline and now authorizes sequential clean backend delivery. This is not acceptance of production behavior or permission to deploy.

One Shipping implementation for single-store and marketplace. Single-store hides marketplace/seller owner selection and rejects marketplace-only server operations; uses the actual store's canonical merchant ownership. Marketplace preserves independently authorized marketplace/seller scopes.
Mode is authoritative platform edition/capability/context, not a Shipping-owned parallel switch, browser field, hostname guess or seller-count heuristic. Never hardcode a store owner or infer single-store from absent context.
Languages/default language come from the actual store's authoritative database-backed configuration via existing APIs/Contracts. One, two and several languages must work. The preview fa/en dictionary is sample data, not a production language inventory. Distinguish known/active/store-enabled languages and interface locale/content language.
Type above method name; localized names/descriptions/type names/group titles; payment prepaid/collect independent of manual/carrier/free computation; one non-overlapping multi-region selection per region group and multiple alternative AND-condition rows beneath it (weight/count/group merchandise amount), freight, packaging, delivery duration.
Weight grams, explicit half-open bounds; all-axis overlap detection, no first/cheapest-row fallback; unmatched input unavailable. No duplicated geography for packaging or weekly schedule.
Independent weekday working/closed and hours, shared across method regions; actual timezone/cutoff/closed-versus-absent semantics must be mapped, not guessed. Preparation, dispatch schedule and customer delivery windows remain separate.
Free base method versus Promotion offer remain separate. Composite shipping costs and financial PendingAllocation remain as approved. No tax, payout, carrier support or customer slot promise may be invented.

BOUNDED ANALYSIS

CURRENT CAPABILITY AND APPLICABILITY MAP
Start from actual Shipping Endpoints/Application/Domain/Infrastructure and current Fulfillment-owned method catalog/activation/translation routes consumed by the live Admin. Trace only direct dependencies necessary for this authoring workflow; no whole-Host sweep or whole-repository re-audit.
Read actual handlers/validators, aggregates, persistence configuration/migrations, DI registration, public contracts, relevant tests/guards and direct live Admin clients. Inspect the preview as UX evidence only; do not copy preview models into production.
Classify applicability HTTP_OWNING / INTERNAL_ONLY / MIXED_OR_UNKNOWN from real routes and callers for each relevant owner. Produce a responsibility-to-owner table and current dependency graph. Shipping rates/coverage/packaging/schedule, Fulfillment catalog/method identity, Geography ancestry, Localization language authority, platform edition/store context, authorization and monetary context must each have one proved owner. Do not assume all belong in Shipping.

EDITION, STORE AND OWNER PROVENANCE
Locate ToobaEdition, ICurrentCommerceContext and the actual control-plane resolver and persistence/config source. Inspect StoreContext.Contracts/Current/StoreCommerceContext.cs and its assignment path; Market/DefaultCurrency/SalesChannel alone do not prove owner/mode authority.
Trace canonical tenant/store/seller/merchant IDs from authentication and context to existing endpoint authorization, handlers and database filters. Specify a single-store/marketplace matrix for permitted operations, visible owner choices, server-derived scope and forged/missing/foreign owner rejection. Record unknown mappings explicitly. No authorization by UI concealment and no silent owner reassignment/data conversion. Identify any current cache key that needs edition/tenant/store/locale scope; do not invent a new cache.

DATABASE-BACKED LANGUAGE AUTHORITY
Start at Localization.Contracts/Ports/LanguageLookupContracts.cs (ILanguageLookup and LanguageLookupSnapshot), real Localization endpoints, LanguageDirectory/LanguageLookupBridge and language persistence/activation. Trace the actual frontend configured-language reader and per-store enablement/default/fallback authority.
ListAsync's comment says known languages: prove whether the returned set includes active/store-enabled semantics before proposing reuse. Snapshot currently does not expose every metadata field a UI might need; do not fabricate fields or bypass Contracts.
Document the minimal existing or proposed lawful read seam, owning module, authentication, language identity/code/culture/direction/default/fallback semantics, empty/error behavior and authoritative write validation. Reuse canonical resource/localizer/resolver mechanisms; no Shipping language table, duplicate language resolver, hardcoded fa/en API, foreign DbContext or cross-schema read. Verify one-language and 3+-language requirements in the planned acceptance matrix.

TARGET MODEL AND LEGACY TRANSITION
Map every approved editor field to existing persisted owner/identity/contract or an explicit gap. Include owner-scoped method identity, taxonomy and translations, payment/calculation modes, one region group/multiple areas, multiple rate rows, group amount basis, currency, packaging, weekly hours and timezone/cutoff.
Existing Shipping has serviceCode/optionCode policies/coverage/packaging/schedules and Fulfillment owns catalog identity. Explain how the target reuses these authorities without a second catalog or a parallel pricing engine. Identify exact conflicts with Unrestricted coverage, separate policy geography, rate thresholds/preparation fields, existing multi-window schedules and financial write/activation guards.
Provide additive/change/deprecate/keep decisions, target ownership and exact candidate paths/namespaces. Do not propose destructive collapse of distinct legacy rates/schedules/owners or remove support for old multi-window data just because the preview edits one interval per day. Explain unsupported legacy edit handling.
Design the smallest coherent save/read operation and transaction/concurrency boundary for method + region groups + rows, with failure/retry/idempotency implications. If a logical edit crosses module persistence, state an explicit orchestration/consistency design through Contracts, never a shared DbContext/transaction/foreign FK/SQL join or N independent browser writes falsely presented as atomic.
Identify needed schema/data migration and API compatibility decisions without generating/applying migrations. Identify existing-data ambiguities needing an Architect decision; do not guess group amount discount/tax basis, packaging payer, owners, schedule absence or financial allocation.

COUPLING AND CANONICAL MECHANISM PROOF
Record exact project/type/file edges. Cross-module APIs consume only stable owning-module Contracts; internal Application ports/models stay internal. No foreign Application/Infrastructure/Domain/Endpoints implementation reference, foreign persistence, shared mutable aggregate, generic god-contract or dependency cycle. Host retains only permitted composition/platform authority and cannot become the business orchestrator.
Discover existing reusable policies before proposing new ones; no duplicate pricing/ancestry/ownership/language rules in UI, Host, seed or adapters.
For in-scope shipped Admin routes, enumerate verb/path -> actual ISender request/handler -> EACH input's binding/provenance -> validator/canonical policy -> Result/error/HTTP outcome. Prove exact reachable-request/classification set equality, not validator counts. No optional-input exemption by assumption. Inventory noncanonical success mapping including current status/body/Content-Type/Location and any explicit lock; proposed factory deltas require explicit review, never silent header/body changes.
Check stable typed code/catalog ownership, configured-language resources/fallback, API factory, DI lifetime/discovery, canonical logging/trace/correlation, cohesive file size, path/namespace and existing structural manifests for the bounded surface. Report violations honestly without repairing or broadening the audit. Offer is read-only reference, not a physical clone.
Carry all required Analyze state fields and Structure-Handoff-State. Proposed layout must be capability-first shallow, no one-folder-per-request, root dump, alias shim or new Host sink. No final Structure PASS or certification claim in W0.

CONCRETE NEXT IMPLEMENTATION HANDOFF
Produce the smallest safe first implementation slice: exact owner, files/paths, lawful contract additions if any, allowed behavior/API/schema delta, source-backed acceptance and focused test plan. List the remaining dependency-ordered slices briefly as planning only, not executable task envelopes. Explain why the first slice can be reviewed/committed independently without dead code, broken existing runtime or temporary illegal coupling.
Separate migration of misplaced authority from approved new behavior. Preserve current defaults until an explicit task authorizes a proven transition. If source authority or product semantics is unresolved, give the exact blocker/options and return NEEDS_ARCHITECT_DECISION rather than implement a guess.

EVIDENCE / ACCEPTANCE
Allowed writes ONLY:

docs/ai/tasks/TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W0.task.md (received artifact);
docs/architecture/evidence/TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W0/ with RESULT.md plus concise source map, input/validation matrix and model/legacy/handoff evidence as needed.
Include all Analyze required states/findings in these files; avoid duplicative reports. Cite exact source paths/types and inspected commit. Mark unproved facts UNKNOWN. A documented gap is useful analysis; do not mark it safe/complete.
No production/schema/test/manifest/SoT certification edits; no containers/servers/DB/seed/package install or browser setup. No broad test/build suite for documentation-only analysis; existing tests may be read, commands proposed, never reported as run. If a narrow check is genuinely necessary, state purpose and result. Never start a test/repair loop.
PASS only for complete, source-backed bounded analysis and an honest disposition. PARTIAL for incomplete required evidence/timebox; BLOCKED for unsafe source recovery or unavailable authority. PASS does NOT mean READY_TO_MIGRATE if blockers exist; report disposition separately.
Commit/push only scoped documentation, fetch and verify exact HEAD-origin parity and scoped tree state. Preserve shared listeners/apps, unrelated work and stashes. Post one complete Bridge result, no test/probe/dummy payload. Stop; no next task scheduling or automatic implementation. TB-185 remains paused; Storefront/checkout/payment/deployment and production flag activation are out of scope.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W0
Parent-Task: TB-SHIPPING-ADMIN-UI-PREVIEW-186-R2
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_AMSC_ANALYZE_CONTRACTS_AND_LEGACY_MAPPING_ONLY
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 68d1aeb3525e8b2d43c719578b433874d26e09d4
Base-SHA: ...
Code-SHA: UNCHANGED
Evidence-SHA: ...
automaticNextImplementationTask: NONE
HEAD-Origin-Parity: ...
Analyzed-Surface-And-Applicability: ...
Canonical-Owner-And-Dependency-Map: ...
Single-Store-Marketplace-Authority: ...
Database-Language-Authority: ...
Target-Model-And-Legacy-Transition: ...
Contracts-Boundary-State: ...
Cross-Module-Coupling-State: ...
Cross-Module-Join-State: ...
Validator-Coverage-State: ...
Canonical-API-Error-Localization-State: ...
Foundation-State: ...
Structure-Handoff-State: ...
Final-Disposition: READY_TO_MIGRATE|FOUNDATION_REQUIRES_SEPARATE_BOUNDED_TASK|NEEDS_ARCHITECT_DECISION|RECOVERY_CONFLICT|BLOCKED_BY_UNKNOWN_BEHAVIOR
First-Implementation-Slice: ...
Unresolved-Decisions: ...
Focused-Checks-Actually-Run: ...
Production-Changes: NONE
Schema-And-Runtime-Writes: NONE
Changed-Files: ...
Isolation-And-Cleanup: ...
Elapsed-Minutes: ...
Production-Functional-Acceptance: PENDING
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
