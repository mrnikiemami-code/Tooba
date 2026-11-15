PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R1
Parent-Task: TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 1b1f8b68f0a1fa0278293f2ca91998d477943e2b
Mode: SHIPPING_CAPTURED_ATTEMPT_SCHEDULING_INPUTS_READ_ONLY
Bridge-Task-Row: f2e354c7-1c7c-4ac7-86d9-07c6bf309c2d
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

USER INTENT / SINGLE TASK
270 established that a resend or healthy-stock replacement attempt cannot receive its own dispatch plan because the
accepted evidence stores the plan's OUTPUTS but not the WEEK SHAPES that produced them, and the only existing Contracts
read that returns those weeks is the rate-quote lookup, which demands a Destination plus measurements and answers the
weeks only when a rate row matched.

270-R1 delivers ONLY the first, independently verifiable increment that was recommended in 270's result:
ONE additive, read-only, Shipping-owned Contracts seam that exposes the CAPTURED scheduling inputs of one exact
captured revision plus one exact accepted group/rate row — without destination, measurements, currency, price
calculation, rate selection, coverage, Geography or any provider call.

This is NOT 270-R2. This task must NOT add a plan table or migration, must NOT add a command/endpoint, must NOT change
Order, Fulfillment, Inventory, the queue, the operational detail or any frontend surface, and must NOT create, attach or
promise a plan for any parcel. No caller is wired in this round; the seam is delivered and proven on its own.

ONE worker, ONE active task. No delegation/subagents, no historical-agent resume, no parallel task, no loop. Return ONE
complete Result and STOP. Do not self-issue 270-R2 or 271.

RECOVERY / REQUIRED READS
Fetch origin normally; verify Expected-Base-SHA exists and is an ancestor of actual origin/main and of the work base. If
the remote advanced with compatible documentation only, inspect that delta and declare the actual base. Missing/unsafe
object or unrelated code divergence -> RECOVERY_CONFLICT, never silent substitution. Do not overwrite unrelated untracked
scripts.
Read current AGENTS.md top, docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md and
docs/architecture/evidence/TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270/{ownership-and-plan.md,RESULT.md}.
Before backend code read .cursor/skills/tooba-backend-feature/SKILL.md and follow repository ownership/AMSC rules without
starting an architecture audit or recertification.

SOURCE-CONFIRMED ENTRY POINTS (read at the expected base, do not improvise a second path)
src/backend/Modules/Shipping/Tooba.Shipping.Application/Authoring/Ports/IShippingAuthoringDraftDirectory.cs — the
existing exact capture read GetCapturedRevisionAsync(owner, draftId, sourceRevision, ct); no newest/current/active read
may be used or added.
src/backend/Modules/Shipping/Tooba.Shipping.Domain/Authoring/ShippingAuthoringRevisionSnapshot.cs — ReadPayload() and
the stored payload shape: MethodSettings (PreparationMinutes, HandoffMinutes, PreparationMode), WeeklySchedule,
CarrierAcceptance, ProcessingHours and Groups[].RateRows[] (MinDeliveryDays/MaxDeliveryDays live HERE, per group and per
rate row, not at the payload root).
src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/Authoring/ShippingAuthoringDraftDirectory.cs and
ShippingCapturedOwnerScopeResolver — the existing owner-scope resolution used by the other captured reads.
src/backend/Modules/Shipping/Tooba.Shipping.Contracts/Scheduling/ShippingDispatchSchedulingAuthority.cs — the ONE
existing scheduling authority. This task does not duplicate it, does not plan, and does not validate feasibility.
src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/Rates/ShippingCapturedRevisionQuoteLookup.cs — the existing
rate-quote read that must NOT be reused here (it needs Destination + measurements and only yields weeks when quoted).

IMPLEMENTATION CONTRACT
A — Contracts surface (Shipping.Contracts only)
Add ONE interface plus its request/result/view types in a new Scheduling area of Shipping Contracts. Contracts may
reference Contracts, never Domain/Application/Infrastructure/persistence types.
The request must carry the exact identity the caller is asking about: owner kind + owner tenant/seller identity, draft id,
source revision, expected service/option, the exact GroupId and the exact RateRowId, and an optional caller-bound
expected snapshot id.
The result must be an explicit typed verdict plus a stable reason code; only one verdict carries a payload. Every refusal
path returns NO payload, NO provenance and NO exception to the caller.
The payload must carry: resolved provenance (snapshot/draft/revision/format version/owner), stored service/option, group
and rate-row ids, that row's stored MinDeliveryDays/MaxDeliveryDays, stored preparation/handoff minutes, stored
processing mode, the stored receiving week (with its slot duration), the stored carrier week and the stored processing
week — each with its OWN timezone, never collapsed, never defaulted.
The payload must also carry the stable codes of the inputs this capture did NOT store (method settings, preparation/
handoff pair, processing mode, receiving/carrier/processing week). "Complete" means only "all inputs this seam declares
are stored"; it is never a claim that a plan is buildable, that the method is active, that a destination is covered or
that a customer is eligible.

B — Adapter (Shipping.Infrastructure only)
One adapter implementing the interface over the existing exact capture read + payload decode. Owner-scope mismatch, an
absent revision, another owner's capture and a corrupt/inconsistent body are distinct typed refusals. Duplicate group ids
and duplicate rate-row ids inside the same group are refusals, never "first match". Service/option comparison uses the
same canonical trim + case-insensitive rule the existing captured quote read uses.
Loss-less mapping only: no week is synthesized, no closed day gets invented hours, no missing slot duration becomes a
number, no absent timing pair becomes 0/0, no absent processing mode becomes WorkingWeek, and no currency/price/coverage
is read. Read-only: no SaveChanges/capture/transaction. Caller cancellation and unexpected dependency exceptions
propagate unchanged; only the canonical payload-decode semantic error becomes a refusal.

C — Registration
Register the adapter once, scoped, in ShippingModule, beside the other read-only captured seams, with a comment that
states it has no caller yet. No Host business logic, no endpoint, no consumer.

D — Preserved laws
No Order/Fulfillment/Inventory/Storefront/queue/frontend change; no schema/migration; no business SQL; no provider,
Geography, price or rate-selection call; no change to existing captured-quote or authoring behaviour. Existing module
dependency direction is preserved; no refactor of oversized files.

FOCUSED PROOF ONCE
One focused new suite in the Shipping tests project covering, grouped (not repeated):
A. an exact capture maps every stored input without inventing anything (weeks, both closed/open days, slot duration,
   that row's own duration, timing pair, processing mode, empty MissingInputs) and the read is read-only;
B. two rows in ONE group with different stored durations answer only the exact requested row, a count/amount-only row
   is read the same way with no destination/measurement/currency supplied, and a group id is not a ticket to another
   group's row;
C. wrong tenant/edition, an un-captured revision, another owner's capture and a caller-bound snapshot mismatch are
   refusals with no payload;
D. wrong service, wrong/absent option, unknown group, duplicate group id, duplicate rate-row id are refusals; the
   canonical comparison still accepts a differently-cased/whitespace-padded service/option;
E. historical formats read honestly: legacy (no carrier week, no processing week, no slot duration) reports exactly the
   inputs it lacks and never invents them; a format-3 body lacks only the processing week; an absent timing pair and an
   unspecified processing mode are reported as missing, not as 0/0/WorkingWeek; a corrupt format version and a body whose
   headers disagree with its row are refusals;
F. an unexpected dependency failure propagates with its own identity and a cancelled caller token propagates as
   cancellation — neither is disguised as "capture unavailable"; plus the real module registers exactly one scoped
   instance.
Run the focused suite once after the final change and the solution build once. No full-suite sweep, no certification
pass, no baseline comparison loop, no screenshots.

DELIVERY / EVIDENCE
Persist this received file at docs/ai/tasks/TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R1/TASK.md.
Evidence folder docs/architecture/evidence/TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R1/:
TASK.md, RESULT.md, RECEIPTS.md, result-contract.txt, scheduling-inputs-proof.md.
Keep evidence compact: the seam and its exact refusals, mapping rules, focused command/counts, build result, base/ancestry
and parity facts, changed paths, actual limits. No screenshots, credentials or giant logs.
Update the top AGENTS.md and START-HERE banners with the 270-R1 result and the standing boundary (no caller, no plan, no
queue/detail change, 270-R2/271 NOT issued). Preserve previous receipts/history; no broad documentation rewrite.
Commit/push normally; fetch parity; no force/amend/reset/rebase/stash and no unrelated file commit. The pre-existing
untracked scripts/tb266r1-*.mjs are not part of this change set.

RESULT CONTRACT
Send ONE final complete Result through Bridge using the canonical envelope; status PASS/PARTIAL/BLOCKED/
RECOVERY_CONFLICT; Stop-State STOP_FOR_ARCHITECT; fixed tail fields:
Expected-Base-SHA: 1b1f8b68f0a1fa0278293f2ca91998d477943e2b
Base-SHA: actual verified work base
Code-SHA: actual code commit or NONE
Evidence-SHA: actual evidence commit or NONE
automaticNextImplementationTask: NONE
No unresolved placeholders. Worker PASS != Architect ACCEPT; no user acceptance and no shipping/provider/financial claim.
After final Result delivery and task completion return IDLE and STOP.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK
