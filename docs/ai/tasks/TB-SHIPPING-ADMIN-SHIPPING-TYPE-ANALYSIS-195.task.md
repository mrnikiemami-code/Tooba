PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-SHIPPING-TYPE-ANALYSIS-195
Parent-Task: TB-SHIPPING-ADMIN-METHOD-MODE-DRAFTS-194
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 4065fa89dba21097b9a47435c41d7f85b63a3a01
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_AMSC_ANALYZE_SHIPPING_TYPE_OWNERSHIP_AND_REUSE_ONLY
Hard-Timebox: 20 minutes HARD including receipt, evidence, commit, push and result. Stop new investigation at minute 15; reserve 5 minutes for reporting. PARTIAL on expiry; do not enter a search/test loop.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Resolve the source/design uncertainty around D2 operational shipping TYPE (examples: normal, heavy, very heavy, grocery) before an implementation task. Produce ONE recommended canonical owner, reuse/binding model and minimal next implementation slice, supported by current code and exact lock provenance. Analysis/docs ONLY: no production, API, schema or UI implementation. Do not reopen the owner's approved simple Admin flow or silently turn this into a whole Shipping audit.

MANDATORY FIRST CHECK
Fetch and verify HEAD == origin/main == Expected-Base-SHA and inspect dirty/untracked work before edits; mismatch/unsafe divergence => STOP_FOR_ARCHITECT without edits. No reset/rebase/stash/force-push. Read AGENTS.md; current SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md; TB-194 ARCHITECT-REVIEW.md; SHIPPING-ADMIN-RULES-DECISION.md (especially type/method/owner/languages and sections 18-25); SHIPPING-STOREFRONT-UX-REFERENCE.md and its linked approved customer reference if accessible.
Apply repository .cursor/skills/tooba-architecture-analyze/SKILL.md. Read .cursor/skills/tooba-architecture-structure/SKILL.md BEFORE proposing physical target paths. The feature skill and certified baseline are constraints, not authority to implement or recertify. Scope the skill's fields to this capability, reuse unchanged verified evidence with exact source identity, and mark unexamined facts UNKNOWN/NOT_APPLICABLE rather than auditing every module. Do not consult the deferred architecture findings register.

BOUNDED QUESTIONS TO ANSWER FROM SOURCE

Actual current owner and seams
Trace existing Fulfillment method identity/catalog/translations/canonical binding, Shipping owner-local authoring drafts, and ONLY directly relevant product/offer shipping classification/fulfillment responsibility seams. Start with targeted rg queries and follow concrete callers/references; do not scan every file in Catalog/Order/Offer. Find whether an authoritative operational category/classification already exists, what its semantics are, who writes it and which consumers actually use it. A similarly named field, physical weight band or carrier service is not sufficient evidence of equivalence.
Classify each examined capability as HTTP_OWNING / INTERNAL_ONLY / MIXED_OR_UNKNOWN from actual routes/callers before proposing shape. Document existing project edges and canonical Contracts-only reuse. No foreign implementation dependency, DbContext/schema join or new generic shared module is an acceptable proposal.

Lock reconciliation
Locate the exact older no-Types-of-Service prohibition referenced in TB-187-W0/D2 and trace its current authority/scope (catalog, options, provider products, or operational classification). Compare with the later explicit user-approved requirement in SHIPPING-ADMIN-RULES-DECISION section 3. State precisely which decisions can coexist and which need an Architect amendment. Do not blindly replay the old task, delete an old lock, declare all historical constraints superseded or weaken a guard. Where source is insufficient, record UNKNOWN and the concrete decision required.

One recommended minimal model
Distinguish operational type, canonical shipping method, carrier service/product, owner-local rule draft, seller responsibility/inbound warehouse flow, membership benefits and customer delivery group. Show these as a short exact mapping table.
Recommend whether to reuse or extend an existing canonical taxonomy, including lifecycle/IDs/configurability, DB LanguageId-backed display text, method/draft reference, and future lawful item/offer compatibility reference. Give one preferred option with reasons; at most one alternative ONLY for a genuine unresolved architecture decision.
Do not hardcode the example labels into a universal enum or automatically create/seed four global categories. Plus is not assumed to be a shipping category. No name/weight/locale guessing, second method catalog, owner-local copy of a shared identity, cross-owner policy inheritance or coupling to product internals.
Explain classification authority and visibility for SingleStore, Marketplace and Seller: distinguish control of common type definitions from the owner's permitted method selection; do not grant a seller taxonomy administration merely because seller draft authoring exists. Do not invent admin-picks-any-seller permission. Reuse the trusted edition/owner/auth seams already implemented.
State configurable versus fixed semantics and which product decisions remain necessary; do not treat a recommendation as an accepted product rule.

Scope and consumer effects
A type may later constrain compatible methods/items and contribute to customer grouping; type alone MUST NOT determine final delivery groups. Responsibility, real origin, item compatibility, available method and independent customer time choice remain relevant.
For authoring, type sits ABOVE method name in the approved UI. It is not a freight tariff tier, region selection or calculation/payment mode. Geography remains once per group; multiple conditional rows remain under that group. No geography, schedules or language copies per type.
Explain target module ownership, exact Contracts-only edges and minimal changed projects/files for ONE coherent future authoring slice, with external consumers read-only until independently authorized. If current canonical binding uniqueness needs a change, identify the exact existing index/invariant and decision instead of guessing a migration.
Preserve TB-190-194 entities, revisions, legacy bindings/quotes/rows and historical migrations. No destructive legacy mapping or inferred existing-row classification. Unclassified/incomplete draft behavior must be explicit and safe; no synthetic default type.
Read Structure skill for physical proposals; report Structure-Handoff-State truthfully, not READY_FOR_CERTIFY or professional structure certification on an unimplemented proposal.

DELIVERABLES
Under docs/architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-ANALYSIS-195/:

SOURCE-MAP.md: precise current files/members/routes/seams, ownership/applicability and legal edges; finite searched terms/paths with negative findings and limits.
TYPE-DECISION-PROPOSAL.md: compact concept mapping, exact lock reconciliation, preferred owner/model, trusted authority/language semantics, affected contracts/compatibility/uniqueness, remaining decisions, ONE bounded implementation handoff and focused acceptance proof plan. Recommendations clearly marked PROPOSED, not APPROVED.
RESULT.md: all applicable Analyze structured states and narrative sections (may combine related sections); actual checks only, source-versus-runtime limits, scope/time/cleanup, disposition.
Persist the received task verbatim in docs/ai/tasks/TB-SHIPPING-ADMIN-SHIPPING-TYPE-ANALYSIS-195.task.md and append only a status pointer to current recovery. Do NOT append proposed product decisions to SHIPPING-ADMIN-RULES-DECISION as if approved. No AGENTS/manifest/SoT/certification/skill changes. No new task envelope or automatic scheduling.

CHECKS AND SAFETY
Documentation/source analysis only: NO build/test/typecheck/HTTP probe/browser/server/container/DB/seed/migration/package operation. No remote carrier call, no shared port/cache/bin/obj/.next mutation. No changes under src/**, no schema, production contract, dependency, flag, guard, test or certification edit. Preserve unrelated dirty/untracked work and stashes. Only scoped fast-forward documentation commits; verify HEAD == origin/main after push. No dummy/probe payload to Bridge /api/results.
This slice does not fix TB-194 evidence limitations by claiming new execution: no-op UpdatedAt is source-supported, subsequent post-settings region replacement was executed, post-settings rate/presentation/schedule preservation was not separately executed in its new facts. The 616/616 suite is Worker-reported, not fresh evidence in this task.
No frontend, taxonomy implementation, cutoff, runtime measurement/basket basis, pricing/evaluation, carrier/funder readiness, customer quote/promise/activation/deployment. TB-185 paused; default-OFF gates and PendingAllocation unchanged.
PASS means complete honest bounded analysis with a concrete recommendation and exact unresolved decisions, NOT product readiness or authorization to implement. NEEDS_ARCHITECT_DECISION may coexist with analysis PASS. PARTIAL if required source/lock mapping cannot be completed within the timebox; BLOCKED for unsafe recovery or inaccessible required authority. Stop after one complete result.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-SHIPPING-TYPE-ANALYSIS-195
Parent-Task: TB-SHIPPING-ADMIN-METHOD-MODE-DRAFTS-194
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_AMSC_ANALYZE_SHIPPING_TYPE_OWNERSHIP_AND_REUSE_ONLY
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Expected-Base-SHA: 4065fa89dba21097b9a47435c41d7f85b63a3a01
Base-SHA: ...
Code-SHA: UNCHANGED
Evidence-SHA: ...
HEAD-Origin-Parity: ...
Analyzed-Surface-And-Applicability: ...
Canonical-Type-Owner-And-Reuse-Evidence: ...
Old-Lock-Authority-And-Reconciliation: ...
Preferred-Model-And-Contracts-Only-Edges: ...
SingleStore-Marketplace-Seller-Authority: ...
Database-Language-Identity-And-Translation: ...
Method-Draft-Item-Compatibility-And-Grouping-Limits: ...
Legacy-Binding-Uniqueness-And-NonAttribution: ...
Unresolved-Architect-Product-Decisions: ...
Structure-Handoff-State: ...
Final-Disposition: READY_TO_MIGRATE|NEEDS_ARCHITECT_DECISION|FOUNDATION_REQUIRES_SEPARATE_BOUNDED_TASK|RECOVERY_CONFLICT|BLOCKED_BY_UNKNOWN_BEHAVIOR
First-Implementation-Slice-Proposal: ...
Focused-Checks-Actually-Run: NONE_SOURCE_ANALYSIS_ONLY
Production-Schema-Test-Manifest-Changes: NONE
Changed-Files: ...
Isolation-And-Cleanup: ...
Claim-UTC: ...
Result-UTC: ...
Elapsed-Minutes: ...
Hard-Deadline-Compliance: ...
Out-Of-Scope-Commands: ...
Production-Functional-Acceptance: PENDING
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK