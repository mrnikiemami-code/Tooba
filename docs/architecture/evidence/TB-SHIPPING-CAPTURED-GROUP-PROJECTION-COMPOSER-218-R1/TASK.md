PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-CAPTURED-GROUP-PROJECTION-COMPOSER-218-R1
Parent-Task: TB-SHIPPING-CAPTURED-GROUP-PROJECTION-COMPOSER-218
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 3a583ab2c8a173bd4021046036074465218d75d1
Mode: BOUNDED_REQUEST_WIDE_CAPTURED_REVISION_CONSISTENCY_REPAIR

Read TOP AGENTS, canonical recovery and 218/ARCHITECT-REVIEW.md first.
HARD 12 minutes / STOP new implementation at minute7 / final5 focused evidence+commit.
One bounded correction max. Source review already identifies exact defect; no discovery audit.

RETAIN218 COMPOSITION; FIX ONLY ONE IDENTITY CONSISTENCY DEFECT
StorefrontCapturedGroupProjectionComposer.ValidateCandidates currently owns a separate seenBindings
dictionary per group. Same owner+service/option can request revision1 in g1 and revision2 in g2.
String keys also use raw case/destination although returned binding comparison is case-insensitive.
This is source-confirmed; execute regression FIRST, then make the bounded repair.

One selected captured revision per owner+service/option ACROSS THE REQUEST:

Move revision consistency ownership to the request-wide ValidatePlan traversal.
Use a typed cohesive identity key / comparer (not delimiter-concatenated strings).
Key = coherent owner kind + tenant + seller + canonical service/option. Preserve existing owner
equality semantics; NEVER lowercase tenant ids or merge different owners. Service/option comparison
must agree with existing trim/OrdinalIgnoreCase binding semantics; absent optional code remains
the same absent identity. Do not introduce a second normalization policy.
Destination and MethodCode presentation label are NOT publication identity. Different group
destinations cannot authorize two captured revisions of the same method binding.
Value = exact DraftId + SourceRevision. Same key with a different draft OR revision => existing
plan_invalid BEFORE ANY goods reader / Cart / Pricing / Shipping call.
SAME owner+binding+EXACT draft/revision across two groups is LAWFUL and REQUIRED:
each group retains its own actual goods facts, destination and once-per-candidate quote/calendar.
Do NOT reject it as a duplicate and do not coalesce groups/quotes.
Different owners or genuine different service/option bindings remain independent.
Keep per-group duplicate method/candidate checks, complete membership validation and all accepted
financial/calendar behavior unchanged. Do not expand into provenance/financial-rule reimplementation.

EXECUTED REGRESSIONS (existing OrderTests harness, same ComposeAsync)
1 g1 ownerX/express/std/D/rev1 + g2 same binding/D/rev2 -> plan_invalid, zero goods/Shipping calls;
prove pre-fix red, post-fix green.
2 same conflict with service/option casing changed and different destinations -> still rejected.
3 same key but different draft at same revision -> rejected before work.
4 control same owner+binding+exact draft/revision in g1 and g2 -> ACCEPTED, actual99/198 facts separately
passed, TWO appropriate quote calls (not merged), per-group result preserved.
5 independent owner or distinct option binding may have independent revisions.
Use focused table-driven variations if appropriate; no new framework.
One OrderTests build and only new focused tests (or exact modified composer class if needed).
No 194/119/9/43/18/8 replay, no solution/Host/backend-wide guard sweep, no provider/browser/HTTP.

BOUNDS
Composer validation + focused tests only (minimal cohesive key type allowed inside existing area).
No public API/route, Contracts change, schema/migration, publication pointer/activation, frontend,
flags, measurement source, shared DB or runtime operation. Do not read/write/capture user draft.
Keep OwnerResult, one clock, actual217 reader,216 quote,213 calendar and scoped DI unchanged.
TB185 paused/defaultOFF/PendingAllocation; §29 decisions not reopened.
NO Bridge probing, historical post-result.js, git add-A, reset/rebase/stash/force push.

FINISH
Persist task verbatim and RESULT under evidence/218-R1, append-only correction to218 RESULT;
update TOP canonical recovery, AGENTS and new-chat guide accurately with exact source and test counts.
Stage exact paths, commit/push/fetch parity. Record elapsed honestly, don't stretch box to add features.
STOP_FOR_ARCHITECT, Automatic-Next-Task NONE. No219 envelope.
Next after this closes: server-selected captured revision + actual fulfillment owner/type/membership
authority, then existing live projection/selection/commit wiring; no claim that this primitive is LIVE.