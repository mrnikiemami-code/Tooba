PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-SELECTED-CAPTURED-REVISION-AUTHORITY-219
Parent-Task: TB-SHIPPING-CAPTURED-GROUP-PROJECTION-COMPOSER-218-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 4972c6c8af39c000bceaebe4f0846540ec37f107
Mode: BOUNDED_SHIPPING_OWNED_SELECTED_CAPTURE_POINTER_AND_CONTRACTS_READ
HARD30 minutes / STOP new implementation20 / final10 focused proof+evidence+commit.
One correction round maximum. No broad audit, confidence race loop or old-suite replay.

READ TOP AGENTS + canonical recovery +218-R1/ARCHITECT-REVIEW.md + applicable backend/Contracts skills.
218/218-R1 composition CLOSED for bounded behavior; 213–217 accepted. Do not rewrite their rules.
One concrete missing authority now: WHICH existing immutable capture is explicitly selected for a method.
Current Order composer accepts a trusted plan but no persisted owner supplies its selected revision.

DELIVER ONE COHESIVE SHIPPING CAPABILITY
Shipping owns a versioned selected-capture pointer for exact owner + canonical service/option.
Add one cohesive Domain entity, Shipping EF mapping + ONE additive migration, one Application write/read
port implemented by a scoped Shipping adapter, and one Contracts-only SELECTED revision reader.
No foreign DbContext, no lossy copy into legacy rates/packaging/schedules. Capture payload stays214-owned.

SEMANTICS (architect decision for this slice)

Pointer references an ALREADY EXISTING214 immutable snapshot, never mutable draft/current/latest.
Internal write takes AUTHORIZED ShippingAuthoringOwnerScope, exact draftId + sourceRevision and
ExpectedSelectionVersion. No cross-module public write contract, HTTP route or browser owner input.
Resolve target via existing exact GetCapturedRevisionAsync + ReadPayload/header verification.
Do NOT silently CaptureRevisionAsync here. A capture missing/foreign/corrupt cannot be selected.
Derive binding from verified target payload, not caller-declared service/option.
One pointer per FULL owner+canonical service/option (not destination/method label/type).
Preserve exact tenant identity; canonical service/option follows existing trim/case-insensitive
binding semantics. Persist a DB-enforced canonical key so casing cannot create a second pointer.
Missing nullable owner/option columns must not bypass uniqueness (NULLS NOT DISTINCT or equivalent
already established repository pattern). Check owner coherence and valid positive versions.
ExpectedSelectionVersion=0 means no pointer row yet. Existing row requires exact version; stale
version -> stable selection_conflict before mutation, never last-write-wins or automatic retry.
First select => version1. Different selected snapshot => version+1. Same target + CURRENT expected
version is a no-op preserving version/time. Stale expected version still conflicts even same target.
Explicit clear with exact expected version retains pointer row/version history token, sets selected
snapshot null, advances once if changed. Clear already-cleared at current version is no-op.
Do NOT delete/reset token to0 (no ABA). Clear absent at expected0 may return absent without insertion.
Rollback is deliberate selection of an OLDER EXISTING exact capture using CURRENT pointer version,
NOT decreasing pointer version or mutating snapshot. A later draft edit alone changes NOTHING selected.
Selection = explicit server publication INTENT only. NOT readiness/eligibility/customer permission.
No Active/CanCheckout flag, no blanket Store-only restriction, no seller=fulfillment inference.
Incomplete/Free/Carrier captures may be selected as data; existing quote/refusal remains authoritative.
Do not fabricate publish-readiness policy or claim selected implies quotable.

WRITE SAFETY
One pointer SaveChanges is sufficient atomic boundary; NO manual new transaction needed.214 capture
code/transaction guards must remain unchanged. Use real EF concurrency token + exact unique identity.
Refuse unrelated pending tracker changes before writing rather than flushing/clearing caller edits.
Load pointer fresh (no stale identity-map selection), and on failure/cancellation detach ONLY attempted
pointer entry so later SaveChanges cannot leak a failed selection. Preserve caller tracked entities,
original exception and caller token. Map only exact concurrency/unique-KEY collisions to conflict;
other provider failures propagate. No catch-all success/winner adoption or name-fragment matcher.
A scope/revision/payload rejection writes nothing, snapshot/draft rows/revisions/timestamps never change.

CONTRACTS READ FOR THE REAL NEXT CONSUMER
One Shipping.Contracts interface request: FULL server-derived owner identity + exact service/option
binding, no draft/latest field supplied by customer. Result:
selection version, explicit Absent/Cleared/Selected state, and for Selected exact snapshotId/draftId/
sourceRevision/format/binding + raw captured shippingTypeId and stored method translations if needed
for customer labels (Contracts-owned typed shape, no Domain/JSON payload leak).
Data must come from selected immutable capture; no fallback to mutable draft, newest snapshot, config,
or another owner. Corrupt/foreign/mismatched target fails closed, not a selected method.
Reader is read-only/AsNoTracking and preserves provider exception/cancellation.
Owner identity is DATA, not actor permission: use existing216 edition/active-tenant coherence. Extract
that mapping to ONE small Shipping-internal helper reused by216 and219 only if needed; no duplicate
owner validator or endpoint dependency, no functional changes to216, no blanket denial of Seller.
Application internal writes require existing authorized scope; do not manufacture one from browser.
Register adapters ONCE scoped in ShippingModule, same ShippingDbContext foundation. No new framework.

FOCUSED EXECUTED PROOF (ONE new class; real PostgreSQL for persistence claims)
Reuse existing214 target isolation:127.0.0.1:15432 /shipping_tb214 /tb214 via TOOBA_TB214_POSTGRES,
existing local postgres17 image, one task-owned container, verify identity before DDL, remove afterwards.
Old55432 family unchanged, NEVER shared5432. Apply normal migration chain only disposable DB.
Use existing snapshot fixture/capture primitives; no214 regression battery rerun.
Prove:
1 migration/table UNIQUE owner+canonical binding and version constraints; initial selection/read returns
exact capture provenance; actual draft advance leaves selected read old; select newer then rollback older.
2 current-version same target no-op; stale select rejected; clear retains/increments version; stale
version from before clear cannot resurrect target; explicit new-version selection succeeds.
3 foreign/missing capture and binding/owner mismatch cannot select/read a different owner's content;
independent Store/Marketplace/Seller owners remain separate (authorized scopes, not fake auth claims).
4 two independent contexts same expected version: exactly one target/version wins, loser conflict,
no hybrid or second pointer; one bounded barrier run, not repeated confidence loops.
5 sentinel or caller cancellation at pointer save leaves no attempted entry that a later SaveChanges
can publish; caller tracked instance/dirty edit preserved; unrelated dirty tracker refused first.
6 Contracts reader returns selected immutable metadata only, cleared never latest-fallback,
scope mismatch fails before read; provider/token propagation and scopedDI if narrowly feasible.
Use existing tests infra/test refs; no new project/package. BuildShippingTests once +exactnewclass only.
No43/18/8/119/194/16/fullShipping/Host/solution/wildcard suites. If provider setup unavailable, report
blocked execution honestly; do not substitute InMemory as concurrency proof or touch shared5432.

BOUNDS / PRESERVE
NO public route/MediatR command/FE/Order consumer yet; this is the selected revision source for the
next concrete publication+fulfillment-plan hookup, not another calculator/audit.
No shared migration, Host/FE restart, user-draft capture/write, HTTP/browser, flags, legacy activation
or rate deactivation, cutoff/capacity/holiday policy, guessed measurements. TB185 paused/defaultOFF/
PendingAllocation, §29 and actual user draft/revision/unsaved tabs preserved.214 shared migration still
pending;219 migration must also be recorded pending shared. Local-dev authorization stays valid later.
No Bridge interaction, historical post-result.js, gitadd-A, reset/rebase/stash/force push.

FINISH
Task verbatim + RESULT under evidence/TB-SHIPPING-SELECTED-CAPTURED-REVISION-AUTHORITY-219/.
Update TOP of ALL THREE: canonical recovery docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md,
AGENTS.md, docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md (canonical was omitted218/R1; do not omit again).
Exact code/proof/elapsed, selected!=live, remaining real fulfillment owner/type/group membership and
physical authority + explicit publish UI + projection/selection/commit hookup. No certification claim.
Stage exact paths, commit/push/fetch parity. STOP_FOR_ARCHITECT, Automatic-Next-Task NONE; no220 envelope.