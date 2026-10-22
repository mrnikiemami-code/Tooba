PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192
Parent-Task: TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: cb3b52908d7cc88aba26d8d37052289b6abfcd81
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_DB_LANGUAGE_KEYED_OWNER_DRAFT_PRESENTATION_METADATA
Hard-Timebox: 45 minutes HARD INCLUDING cleanup, commit, push and report; STOP and report PARTIAL if exceeded.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Persist and expose owner-local multilingual method name/description and optional region-group titles on the accepted Shipping draft model. Use the actual authoritative language registry through TB-188, with one/two/three-plus active languages. Editing one content language must preserve every other language and every shared shipping rule.
This is backend authoring metadata only. No frontend, taxonomy, calculation/payment mode, schedule, runtime quote or activation in this task.

MANDATORY FIRST CHECK
Fetch origin/main, inspect branch/HEAD/dirty/untracked/stashes and preserve unrelated work. Require HEAD == origin/main == Expected-Base-SHA before edits; a safe strictly-behind branch may fast-forward only to the exact expected base. Mismatch/divergence/overlapping dirty work => STOP_FOR_ARCHITECT / RECOVERY_CONFLICT. No reset/rebase/stash/force-push.
Read AGENTS.md, docs/ai/AGENT-SKILLS.md, full .cursor/skills/tooba-backend-feature/SKILL.md and relevant Migrate/Structure rules for the changed surface, not a new AMSC cycle. Read current Shipping decision/recovery, TB-191-R1 ARCHITECT-REVIEW, existing authoring aggregate/directory/DTO/owner authorizers, TB-188 context query/source, Localization.Contracts language metadata and existing translation/length conventions.
No findings-register or unrelated-module audit. TB-190 and TB-191 through R1 are accepted bounded draft foundations, not production/editor activation.

CLOCK / COMMAND SCOPE
Record UTC claim/deadline and monotonic elapsed time at start. Reserve final 5 minutes for cleanup/commit/push/result; start no new implementation/test after minute 40. Every command <=180 seconds under an actual timeout; no blocking wait >60 seconds, detached orphan test or broad loop.
Only affected Shipping test project build, focused Shipping tests, one final whole Shipping.Tests run if time remains, Shipping EF migration/script tooling, isolated local PostgreSQL tools and scoped git/evidence/Bridge commands. NO Tooba.Host.Tests, solution build/test, whole Order/other-module suite, package/image/browser install/download or unrelated repair. TB-191 Host failure attribution remains UNVERIFIED; do not rerun Host to justify it.

OWNER-LOCAL PRESENTATION — NOT A SECOND CATALOG
Fulfillment retains immutable canonical method identity/binding and shared catalog translations. These new records are the shipping owner's presentation for ONE existing Shipping draft and its groups; never mutate/clone Fulfillment catalog rows, seed another carrier catalog, parse display codes or transfer identity authority.
Store keyed translations using REAL LanguageId, scoped internally to DraftId (method name/description) or GroupId (optional group title). Own Shipping tables and own cascade FKs only; no foreign FK/navigation/join to Localization, no copied registry/name/direction rows and no tenant key exposed in HTTP.
Keep method/group identities and shared geography, rates, costs/currency, conditions and duration independent of language. No copying rules once per language or changing another language during normalization.

LANGUAGE AUTHORITY / READINESS
Reuse IShippingAuthoringContextSource / ILanguageLookup and the existing TB-188 projection/readiness semantics. No fa/en/ru array, interface-locale/country-derived language, hardcoded default/order/direction or new per-seller language settings. SingleStore uses its resolved database registry; Marketplace uses the installation registry as established at 188. Do not invent active/store-enabled semantics beyond that source.
Avoid copying the TB-188 readiness algorithm. If necessary, narrowly extract its existing pure projection into a shared internal Shipping Application helper used by both the unchanged context query and this capability; preserve 188 API/status/behavior and tests. No Application dependency on Localization implementation, no HTTP call to another module, no handler-directly-calling-a-handler or service locator.
Resolve authoritative language facts BEFORE tracked mutations/transactions; canonical IModuleCallTracer on every actual foreign read through existing seam, no double tracing, PII tags or new telemetry authority.
New/edited entries must reference current ACTIVE language IDs; unknown, inactive, empty and duplicate IDs reject explicitly before mutation. Invalid/unavailable language/default/direction configuration fails closed with canonical configuration outcome, never fallback fixtures.
Previously saved text for an inactive or no-longer-returned language is RETAINED. GET returns stored raw language-keyed values truthfully plus active registry/default information; marks/exposes which IDs are currently configured rather than silently dropping or claiming them as active. A later language reactivation exposes the old text unchanged. No background delete or auto-translation.

READ/WRITE CONTRACT AND DRAFT COMPLETENESS
One aggregate metadata document for a draft:

method translations: LanguageId + Name + optional Description;
per-group optional title translations: GroupId + LanguageId + optional Title.
Use two own typed translation collections/tables or equivalent cohesive relational mapping, not an untyped JSON blob. Composite uniqueness enforces one translation per owning item/language. Reuse current catalog/editor length conventions where applicable; document exact technical lengths and enforce them before DB writes without new commercial caps. Store plain Unicode text, no transliteration or translation fabrication.
An included method translation replaces Name/Description only for that LanguageId: Name must be nonblank, Description may be empty/null to clear that selected description. A missing language entry means NO CHANGE. Group titles are optional: an explicit null/empty title clears only that group's selected-language title; omitted entries/groups are preserved. Reject duplicate (group,language) entries before mutation. Do not turn PATCH into whole-dictionary replacement.
The draft may remain incomplete: do not require every language, auto-fill default Name from another language, forbid old empty drafts or claim metadata save publishes anything. GET must let the client see missing default-name/translation completeness by comparing ACTUAL stored content with the ACTUAL active/default IDs. Optional description/title absence is not a required-field failure. If exposing counts/statuses, define them exactly (e.g. active-language names present / active-language count); no fallback text reported as a completed translation.
Interface language (locale/Accept-Language) is independent of content LanguageId. GET/PATCH selects content by explicit valid LanguageId; switching interface locale never changes stored text, active language identity/default, currency or rules. Return raw translations for authoring; do not invent a parallel fallback/display-name resolver.

AUTHORIZED API / CQRS / SHARED REVISION
Add explicit GET and PATCH under:
/v1/admin/shipping-services/authoring-drafts/{draftId:guid}/presentation
/v1/seller/shipping-services/authoring-drafts/{draftId:guid}/presentation
ONE Application query and ONE patch command/handler serve both audiences. Payload has expectedRevision, an explicit method-translation change list and group-title change list (empty lists are allowed; missing/null required lists reject). Fields for owner/mode/tenant/currency are absent.
Reuse IShippingAuthoringOwnerAuthorizer, fulfillment.view on reads and fulfillment.manage on PATCH independently and before dispatch. SingleStore seller remains unavailable before seller resolution; server owner scope is the sole authority. Full owner predicate on every draft/group read/update; unknown/foreign draft/group gets the same safe not-found outcome and no data change.
All route IDs, expectedRevision, required collections, null elements, LanguageIds, text lengths and duplicate entries have explicit FluentValidation/stable-code coverage through real MediatR pipeline. Keep repaired required-rule placement and safe parent guards.
Preflight ALL entries, referenced group membership and active-language facts before any tracked mutation. Bad last group/language/text must leave same-instance text/child collections, rules, currency, timestamp and revision unchanged; later SaveChanges cannot leak earlier changes.
Use the SAME root concurrency token as regions/rates. A changed metadata patch atomically advances root revision once with all selected-language changes in ONE implicit SaveChanges; no new explicit BeginTransaction and no weakening of R2 transaction guard. Define no-op behavior truthfully and test it; never let a stale metadata request overwrite newer rate/region work.
Existing create/region/rate bodies omit metadata and MUST preserve retained method/group translations. Reorder/selection/rate edits keep translations; explicit group removal deletes only that group's titles through own cascade while method/other groups stay intact.

PERSISTENCE / COMPATIBILITY / ERRORS
Authorize ONE additive Shipping migration/designer/snapshot for the new own translation storage and required constraints only. No old migration/designer edits, legacy table/registry alteration, owner backfill, copied catalog translations or shared DB application. DB keys/checks must enforce intended non-null/uniqueness/shape where appropriate; avoid nullable CHECK UNKNOWN loopholes from 191. Any foreign language-reference validation stays Contracts-based, not a cross-schema FK.
Canonical Endpoints -> ISender/CQRS -> Application/Domain/own persistence; no Host business code, foreign Application/Infrastructure/Domain/Endpoints reference, new package/project, second language resolver or new owner authority. DI lifetimes and path/namespace/capability structure remain canonical.
New semantic codes have exactly one owning descriptor/resource registration and canonical locale/fallback; preserve all existing statuses/resources. Existing neutral/fa resource files are not a production two-language limit. No hardcoded user-facing prose in executable code. Update actual request/route/validator classification by exact universes; no baseline widening, arbitrary validator exemption or certification promotion.

FOCUSED PROOF
Use controlled Contracts language fixtures clearly labelled as such for:

ONE active non-fa language, TWO mixed RTL/LTR languages, THREE-PLUS with a default that is not first and an inactive stored language;
editing one selected Name/Description leaves all other-language bytes and all rules/currency unchanged; group title clear affects only that pair;
duplicate/unknown/inactive LanguageId, bad last group/foreign group, omitted/null lists/null elements, invalid lengths and null parent reject before writes;
changing active/default registry updates read metadata/completeness without copying/deleting text; reactivation recovers the exact old text;
fa/en interface-culture switch produces identical raw content/registry identities/rules; no hardcoded two-language success condition;
actual admin/seller delegates, real pipeline/directory: create/reload metadata, safe foreign-owner 404, view-only PATCH denial before dispatch, SingleStore seller unavailability.

Real PostgreSQL/Npgsql with current own migrations:

metadata PATCH -> NEW context readback with exact stable language keys/text, unchanged regions/rates/currency;
invalid last entry leaves SAME context + fresh readback unchanged, including root revision;
uniqueness and own FK/cascade are real, titles survive retained-group reorder/rate edit and only removed-group titles cascade;
deterministic metadata PATCH versus rate/region write race on the SAME original root revision using existing bounded pre-write barrier: one winner, canonical loser conflict, exact winner-dependent state/no hybrid and actual loser tracker safe.
Use one representative cross-capability race; do not exhaustively repeat unrelated R1/R2 tests. One malformed request must not commit a partial translated document.

ISOLATED RUNTIME AND HONEST PROOF
Reuse existing parsed exact target fixture and already available PostgreSQL 17 image in a FRESH task-owned container; old container was removed. Verify free loopback port, exact container ownership, parsed host/port/database/user BEFORE connection and live database/user before writes. Never shared :5432/tooba_alpha/store-alpha, no credentials in logs.
Apply module migrations only to the disposable test database through existing module/test authority. Setup <=8 minutes; if unavailable within bound, report PARTIAL rather than install infrastructure or fake relational proof.
External language fixtures prove handler behavior and lawful source wiring, NOT an end-to-end live merchant Localization database/browser session. Label that limitation and retain actual configured-registry runtime/UI acceptance as a later integration gate; no claim that every test language came from a real database row.
Run affected focused build/tests first, then one whole Shipping suite only if focused checks pass and time remains, with required DB still live. Cleanup exact task container/database/listener/isolated output and prove shared apps untouched. No shared bin/obj/.next/cache purge.

LOCKS / ACCEPTANCE / STOP
No frontend/preview changes, taxonomy, working schedule/cutoff/preparation, payment/calculation/carrier/free-mode behavior, shipping evaluator, merchandise-basis allocation, customer quote/promise, activation/publish, financial/payment/Checkout/deployment, certification or unrelated module repair. Runtime-Merchandise-Basis-And-Measurement-Gate stays OPEN_NOT_IMPLEMENTED; PendingAllocation/default-OFF gates/TB-185 pause preserved.
Preserve unrelated work/stashes, shared :3000/:3419/:5088/:5432 and Redis/RabbitMQ. No reset/rebase/stash/force-push; no dummy/probe POST to Bridge /api/results.
PASS only for authorized persisted language-keyed metadata, real dynamic-registry/one-two-three-plus behavior, partial-language isolation, root atomicity/concurrency and relational proof with cleanup. PARTIAL for missing required proof/deadline; BLOCKED for architecture/authority/isolation conflict. This is not full Admin/production acceptance.

EVIDENCE
Persist received task verbatim at docs/ai/tasks/TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192.task.md after claim. Evidence under docs/architecture/evidence/TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192/: RESULT.md, API/language/ownership matrix, raw language preservation/completeness, migration SQL/parity, relational and race assertions, executed commands/counts and timing/cleanup.
Append actual decisions/outcome to Shipping rules/recovery; no Architect ACCEPT or automatic next task.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192
Parent-Task: TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_DB_LANGUAGE_KEYED_OWNER_DRAFT_PRESENTATION_METADATA
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Expected-Base-SHA: cb3b52908d7cc88aba26d8d37052289b6abfcd81
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
Owner-Local-Presentation-And-Catalog-Preservation: ...
Authoritative-Language-Source-And-OneTwoThreePlus: ...
Selected-Language-Only-Patch-And-TitleClear: ...
Inactive-Text-Retention-Default-And-Truthful-Completeness: ...
InterfaceLocale-Versus-ContentLanguage: ...
Authorized-API-And-Foreign-Identity-Denials: ...
Same-Context-Preflight-And-Root-Revision-Race: ...
RegionRate-Preservation-And-GroupTitle-Cascade: ...
Additive-Migration-And-Postgres-Roundtrip: ...
Contracts-DI-Tracing-Validation-Error-And-Guard-Parity: ...
Configured-Registry-Source-Versus-Executed-Proof: ...
Focused-Commands-And-Test-Counts: ...
Changed-Files: ...
Claim-UTC: ...
Result-UTC: ...
Elapsed-Minutes: ...
Hard-Deadline-Compliance: ...
Out-Of-Scope-Commands: NONE
Shared-Database-Data-Flag-Changes: NONE
Isolation-And-Cleanup: ...
Runtime-Merchandise-Basis-And-Measurement-Gate: OPEN_NOT_IMPLEMENTED
Production-Functional-Acceptance: PENDING
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK