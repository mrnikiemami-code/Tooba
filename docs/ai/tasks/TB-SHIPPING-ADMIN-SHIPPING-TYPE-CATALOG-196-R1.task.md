PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R1
Parent-Task: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: feabb7aaf9de99b69a791cd2b239670700f377fa
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_TYPE_CATALOG_INPUT_PREFLIGHT_LANGUAGE_AND_TRACING_REPAIR
Hard-Timebox: 45 minutes HARD including cleanup, commits, push and complete result. No new implementation/test after minute 38; final 7 minutes reserved for reporting. On expiry report PARTIAL immediately; do not finish an extra feature or test first.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Repair the specific TB-196 source defects R196-01..05 and prove the actual API/auth/registry/no-op/preflight/tracing behavior in a bounded in-process harness. TB-196 remains NOT ACCEPTED until later relational/snapshot proof. This is not a second catalog, new business feature, full-module refactor or attempt to complete all missing TB-196 acceptance in one wave. R196-06 PostgreSQL/migration-chain/snapshot/race proof is explicitly DEFERRED; no DB or migration edit here.

MANDATORY FIRST CHECK
Fetch and verify HEAD == origin/main == Expected-Base-SHA and inspect dirty/untracked work BEFORE edits. Unsafe divergence/mismatch => STOP without edits; no reset/rebase/stash/force-push. Read AGENTS.md, current recovery, TB-196 ARCHITECT-REVIEW.md (all six findings), TB-196 received task/result, rules section26 and flat-method clarification. Latest base includes the later transport-only c9f711dd commit; do not reuse the Worker's old parity SHA.
Apply .cursor/skills/tooba-backend-feature/SKILL.md and applicable AMSC Migrate/Structure instructions to changed surfaces only. Do not consult deferred findings register or recertify modules. Preserve all approved Shipping/D2 requirements. Missing governing skill or wider required authority => precise blocker.

REPAIR 1 — LOSSLESS INPUT AND CANONICAL HTTP

ShippingTypeAdminEndpoints.ToInputs must NOT silently remove null elements or normalize absent collection to []. Preserve null/missing/raw values until validated; use correctly typed nullable/presence-aware transport/input models as needed. A list containing valid first + null last must reject as a whole, never save the first or claim success.
Replace automatic complex-body binding for the TWO new write routes with the existing canonical body-reading pattern available to Fulfillment/foundation. Authorize FIRST; then read/malformed/null handling; then ISender. Do not reference Shipping.Endpoints or invent a second global body framework. Reuse an own existing seam if available; otherwise one small cohesive module-owned seam, with canonical stable codes/resources and ApiResponseFactory. No raw Results.Json/ProblemDetails or exception-message interpretation.
Presence must survive to real FluentValidation: isActive/sortOrder/expectedRevision/translations on PUT, sortOrder/code/translations on POST; omitted bool/order must not become false/zero. Keep POST server-created inactive identity/revision; do not gain caller owner authority.
GetShippingTypeQuery(Guid.Empty) requires a real validator. List's languageId parameter is currently bound but ignored. Remove the unused optional languageId binding/query member from this NEW UNACCEPTED capability and make the list parameterless; do not invent a filter or content-language inference to justify it. Explicitly classify its parameterless provenance as NO_VALIDATOR_REQUIRED. No legacy route/query changes.
Prove exact set equality for the FOUR new routes -> requests -> handlers -> validators/one justified parameterless exemption in a durable capability-scoped guard or extend the existing authoritative mechanism. Do not count-only, skip new requests or re-audit unrelated routes. Error response status/body/Location uses actual ApiResponseFactory methods.

REPAIR 2 — COMPLETE PREFLIGHT, REAL NO-OP

ShippingTypeStore.UpdateAsync currently writes translations before computing changed, and Translations.Count>0 treats identical text as a change. Prepare and validate the ENTIRE proposed state before the FIRST mutation of a tracked entity. Canonically normalized values decide equality: a nonempty patch repeating stored text must be a no-op.
Domain ShippingTypeTranslation.Replace currently assigns Name before validating Description: a bad description can partially change Name. Normalize/preflight BOTH fields into locals before assigning either. Validate every proposed translation and header before applying any of them. Reuse canonical normalization rules; no independent duplicate rule engine.
Keep language-existence/active-default I/O outside Domain. Use one coherent Application preflight/prepared-facts flow and own persistence seam; avoid competing normalization/registry policies or unnecessary duplicate foreign reads. Direct calls to exposed domain/store mutation paths must not leak partial tracked edits when the LAST entry is invalid; defense is not merely HTTP validators.
No-op preserves root Revision AND UpdatedAt (and translation rows); expectedRevision must be checked BEFORE no-op so stale identical request is still canonical conflict. A real change advances once. Use controllable canonical IClock in tests, not sleep; verify timestamp explicitly.
Unknown/inactive/duplicate language, blank/overlength text, null entry/collection and invalid final entry cannot leave pending edits for a later SaveChanges. Check SAME aggregate snapshot and SAME context tracker; execute a later SaveChanges and re-read to prove rejected changes did not publish. Do not fix by replacing the aggregate with a fresh instance while hiding a mutated original.
Selected-language patch remains selected-only; absent entry/[] preserves others, explicit null/empty description clears only its selected pair. Preserve inactive stored translations and truthful default-name completeness; no automatic translation or deletion.

REPAIR 3 — AUTHORITATIVE REGISTRY WITHOUT GUESSES

ShippingTypeLanguageFacts.ActiveDefault must not choose ordered FirstOrDefault from multiple active defaults. Validate exactly ONE active default, nonempty unique LanguageIds, actual known active flag and required actual order metadata, and a usable nonempty active set. Reject zero/multiple defaults and malformed owner facts canonically; no order=int.MaxValue fallback or inferred default from array position.
Reuse ILanguageLookup as the sole registry, including inactive rows; no fa/en list, second table/resolver or HTTP-locale targeting. Keep inactive text readable and immutable unless its active LanguageId is explicitly edited.
Prove one non-fa language, two mixed RTL/LTR languages, three-plus with non-first default; deactivate/reactivate a stored language, switch current default to one without a stored name (read completeness false, activation fails until explicitly written), explicit selected-description clear and byte-identical other text.
Registry fixture injected through the REAL ShippingTypeLanguageFacts adapter over controlled ILanguageLookup is required, not only constructed Application facts. Controlled facts are not a live merchant Localization DB proof.

REPAIR 4 — CANONICAL MODULE CALL TRACING

Add mandatory IModuleCallTracer dependency to ShippingTypeLanguageFacts; wrap each actually executed ILanguageLookup.ListAsync in ONE canonical Fulfillment -> Localization child span using existing tracer conventions. No optional/no-op fallback or new tracer registration/framework.
Real ActivityListener over real canonical ModuleCallTracer: exact source/target/operation/name, ambient parent + shared trace + correlation, single disposal and parent restoration. Success/business-invalid facts remain distinct from dependency exceptions. On dependency exception record TYPE only and rethrow SAME instance; caller cancellation propagates unchanged.
No language/type/tenant identifiers, text, tokens or exception messages in tags/events. Existing neighboring untraced catalog code is NOT exemption authority and is outside scope. A mock tracer assertion alone is insufficient.

REPAIR 5 — CHANGED-SURFACE STRUCTURE AND STABLE IDENTITIES

Commands/ShippingTypeCommands.cs and Queries/ShippingTypeQueries.cs currently declare Application.ShippingTypes instead of their physical subfolder namespaces. Follow Structure for exact path/namespace alignment and cohesive shallow organization; update imports/tests only for this capability. No broad folder refactor or per-request single-file folders.
Reject malformed type code including leading/trailing whitespace; TryNormalizeCode must not silently trim/rename canonical identity. Immutable code remains immutable. Use existing owned FulfillmentErrorCodes for new domain errors instead of scattering literal shipping_type.* strings.
IsUniqueViolation currently matches ConstraintName.Contains(fragment). Match exact owned constraint identity with ordinal equality; unknown lookalike constraint errors must propagate. Add one focused negative with a name that merely contains the allowed name; no DB needed.
Only directly affected structure/classification facts may change. Do not widen roots, weaken guards, baseline oversized files, promote certification or touch unrelated legacy Admin/Shipping routes. Keep existing canonical error descriptor ownership/resources/fallback; do not register Foundation duplicates.

REQUIRED FOCUSED PROOF

In-process real production route delegates and registered IShippingTypeAdminAuthorizer, real body reader/MediatR ValidationBehavior/handlers/directory/DbContext (EF InMemory explicitly labelled), canonical ApiResponseFactory/error catalog. Controlled platform auth and Localization Contracts fixtures only; do not replace the module authorizer or handler with fake success.
Create inactive rev1 -> GET list/detail -> update/activate/deactivate/reactivate; correct Created Location/id and code immutability; all four route classifications. Prove GET Guid.Empty rejects and parameterless list does not use content language from query/culture.
Pre-dispatch authorizations: independent view vs manage; panel rejection, seller-only grants, unresolved/unknown edition, SingleStore missing/inactive tenant, Marketplace with leftover tenant; no handler/language read on denial. The fact that ISellerPanelAccess is never called does NOT itself prove seller-only denial: explicitly model denied Admin panel/authorization seams. A malformed body from an unauthorized actor must still be denied before application/body processing. Scope spoof inputs cannot replace trusted context.
Transport matrix: empty/null/malformed body, missing/null required values, null collection and null last element, Guid.Empty, duplicate language, invalid code/sort/revision and overlength text. Reject before mutation/handler as appropriate, with exact canonical machine codes. Count dispatch separately from handler execution; no silent filtered rows.
Execute dynamic-language, unchanged nonempty-patch no-op with BOTH timestamps/revision asserted, stale identical conflict, selected clear, invalid LAST entry SAME-object/tracker/later-save safety and interface-culture invariance. Domain/store direct failure tests are separate from validation-only tests.
Execute real-listener and actual DI constructor/registration tests, exact constraint-name negative, and focused path/namespace/request-classification guards. No cosmetic replacement tests that only assert a line of code exists.
Build only affected Fulfillment test project/dependencies with isolated task-owned output if shared files are locked; run targeted new tests plus existing ShippingTypeCatalogRulesTests. At most one deterministic fix and targeted rerun per discovered failure; if wider repair needed or time short, report precise PARTIAL. No full Fulfillment/Shipping/Host/solution suite and no broad guard sweep. Each command bounded <=180s, no detached tests or package install.

EXPLICITLY OUT OF SCOPE
Do not run container/PostgreSQL/migration/seed/server/browser/HTTP network probe. In-process HTTP delegates are permitted and do not start a listener. Do NOT modify any existing migration, designer, snapshot or EF mapping, nor generate a corrective migration. R196-06 must later prove historical chain schema versus regenerated snapshot and PostgreSQL constraints/races; pending-model-changes is not that proof. If R1 reveals a schema-dependent mismatch, report it; do not improvise a migration here.
No Shipping production/Contracts/tests, owner draft assignment, frontend/Storefront/global CSS, item/offer classification, taxonomy restriction/grouping, pricing/quote/promise, cutoff, carrier/funder/Payment/Settlement/Promotion, flag or deployment changes. No Host business/new file/folder; use existing composition for necessary registration only. TB-185 paused; default-OFF flags and PendingAllocation unchanged.
Do not undo or re-scaffold the broad TB-196 snapshot on speculation. Existing accepted Shipping source and all historic migration/designer files remain byte-identical. Shared :3000/:3419/:5088/:5432, shared bin/obj/.next, caches and stashes untouched. No reset/rebase/stash/force-push or dummy/probe Bridge payload.

EVIDENCE / GATES
Persist received artifact verbatim in docs/ai/tasks/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R1.task.md. Sanitized docs/architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R1/RESULT.md + concrete input/auth/language/no-op/preflight/trace/structure proofs and exact commands/counts. Append a correction to TB-196 RESULT.md marking refuted claims (identical nonempty-patch no-op/full direct preflight/multiple-default readiness) as superseded; do not rewrite original history or claim the missing database proof. Update recovery status pointer.
PASS means R196-01..05 repaired and bounded required proof executed within deadline; TB-196 remains PARTIAL_PENDING_RELATIONAL_SNAPSHOT_PROOF. No certification/user/production acceptance or automatic next task. Report remaining unexecuted checks precisely instead of extending the hard deadline again. Finish scoped commit/push, verify HEAD==origin/main and cleanup exact task-owned output.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R1
Parent-Task: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_TYPE_CATALOG_INPUT_PREFLIGHT_LANGUAGE_AND_TRACING_REPAIR
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Expected-Base-SHA: feabb7aaf9de99b69a791cd2b239670700f377fa
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
Lossless-Input-And-Canonical-Body-Proof: ...
Four-Route-Request-Validation-Set-Equality: ...
Real-Authorizer-And-PreDispatch-Denials: ...
Nonempty-Patch-NoOp-Revision-And-Timestamp: ...
Invalid-Last-Entry-SameObject-Tracker-And-LaterSave: ...
Dynamic-Registry-Default-Cardinality-And-RawText: ...
Canonical-RealListener-ModuleCall-Proof: ...
Exact-Constraint-Identity-And-PathNamespace-Guards: ...
Focused-Commands-And-Test-Counts: ...
Changed-Files: ...
Migration-Designer-Snapshot-EFMapping-Changes: NONE
Postgres-Snapshot-Chain-And-Relational-Race-Proof: PENDING_NOT_RUN
TB196-Acceptance: PARTIAL_PENDING_RELATIONAL_SNAPSHOT_PROOF
Shipping-Assignment-And-UI: NOT_IMPLEMENTED
Shared-Database-Data-Flag-Changes: NONE
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