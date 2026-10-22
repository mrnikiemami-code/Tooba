PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191
Parent-Task: TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R2
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: ebe9d10d3db0cf406f604fef4f2d650a15e2a554
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_OWNER_GROUP_MANUAL_RATE_ROW_AUTHORING_PERSISTENCE
Hard-Timebox: 45 minutes HARD; STOP and report PARTIAL if exceeded.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Extend the accepted TB-190 owner/region DRAFT with multiple persisted manual condition/cost/delivery rows under each existing region group. A row references its stable group identity; geography is NEVER entered or copied onto that row. Provide a real authorized read/whole-collection replace for one group's rows with the same root draft revision. This is usable authoring persistence, not another analysis report, live quote engine, publish/activate or frontend task.

MANDATORY FIRST CHECK
Fetch main, inspect branch/HEAD/dirty/untracked/stashes; preserve unrelated work. Require HEAD == origin/main == Expected-Base-SHA before edits. A safely behind branch may fast-forward only to the exact base. Mismatch/divergence/overlapping dirty files => STOP_FOR_ARCHITECT / RECOVERY_CONFLICT. No reset/rebase/stash/force-push.
Read AGENTS.md, docs/ai/AGENT-SKILLS.md, full .cursor/skills/tooba-backend-feature/SKILL.md, relevant Migrate/Structure rules for changed capability only, current Shipping decisions/recovery and TB-190-R2 ARCHITECT-REVIEW.md. Read current authoring aggregate/directory/DTOs/authorizers/validators/classification, existing ShippingNumericTier, rate/packaging monetary conventions/mapping, TB-188 context source and canonical API/errors/localization/DI/tracing.
No findings-register scan or new full AMSC cycle. TB-190 including R1/R2 is Architect ACCEPTED only for incomplete owner/region drafts; do not reopen that slice or claim full product acceptance.

APPROVED SHAPE AND BUSINESS LIMIT
Each existing group can hold zero or more manual rate rows. Row fields:

stable server-created rate-row ID (an existing ID may be retained only inside this SAME group);
optional weight interval in INTEGER GRAMS;
optional integer group-unit-count interval;
optional decimal merchandise-amount interval in the SAME explicit currency as this group's costs;
separate nonnegative freight and packaging amounts;
integer minimum/maximum DELIVERY DURATION DAYS (min <= max; equal is allowed).
Keep duration separate from preparation time, carrier operating hours, available customer date/windows and legacy PreparationDays/LeadDays. No conversion to a customer promise.
No geography columns/DTO inputs on rates, copied country labels, second coverage table, translated monetary/rule copies, untyped JSON blob or second method catalog.

CONDITION SEMANTICS
Use explicit half-open intervals [minInclusive,maxExclusive). Both bounds absent => no condition on that axis. A present interval has a required valid minimum and optional unbounded maximum; max-only, inverted/equal, fractional integer-axis or negative invalid input rejects before persistence. Count minimum is positive; weights/amount minima may be zero. Keep technical representational limits explicit and consistent with existing canonical types/storage; do not invent commercial shipping caps.
Conditions inside ONE row are AND. Rows are alternatives, never summed or selected by first/cheapest order. Reject two rows iff their FULL 3-axis condition products share a valid point: overlap in weight AND count AND amount, treating an absent condition as unbounded over that axis's valid domain. Identical rows conflict even if costs match. Adjacent half-open intervals do not overlap. One unconditioned row conflicts with every other row in the SAME group; no implicit fallback exception.
Groups/methods/owners are independent. Empty row collection is incomplete configuration, never zero/free/unrestricted coverage. Zero-valued explicit amounts are permitted draft data, not activation of Free mode or a Promotion.
Reuse existing canonical interval helpers where semantics/types match; add only the missing authoring amount/tuple logic in the proper owning layer. Do not alter legacy interval/quote behavior or clone an operational policy engine.

MEASUREMENT / AMOUNT AUTHORITY — DO NOT GUESS
These rows persist threshold configuration only. Do not add a cart/parcel evaluator or claim runtime value derivation. Count is the intended units of goods assigned to this shipping group, not cart-line count or all sellers' quantity; legacy ShippingMeasuredParcel.ItemCount has its own physical parcel semantics and is NOT a proven mapping.
Merchandise bounds refer to the goods belonging to that group. Exact before/after-discount/tax/allocation basis and authoritative measurements remain an OPEN integration gate documented in the decision/evidence. Do not resolve that gap by relabelling CartSubtotalExclusive, assigning whole-cart totals, adding guessed allocations, or saying rates are operationally ready. This gate does not block safe threshold-storage/overlap validation; it MUST block future runtime activation until explicitly resolved.

SERVER CURRENCY AND MONETARY PRECISION
Derive currency from the trusted StoreContext/TB-188 source through the existing lawful seam; no caller-supplied currency authority, locale/country inference, IRR literal, seller-owned currency registry or FX. Resolve foreign facts BEFORE tracked writes/transactions and preserve canonical tracing on actual foreign reads.
Store explicit currency with the manual row set and all monetary fields share it. Bind a nonempty group's currency when first saving its rows; subsequent writes must match the trusted current currency. A configuration change MUST NOT silently reinterpret old amounts, even if the collection is cleared. Fail closed with a stable canonical configuration/mismatch outcome; return the old stored currency truthfully on read. No automatic conversion or currency-change workflow in this slice.
Reuse current canonical monetary precision/normalization foundations and document exact decimal/storage precision. Reject unrepresentable/out-of-range precision before saving; never silently let PostgreSQL round a threshold or fee into a different rule. Money stays decimal, not float, display-toman multiplication or arbitrary new minor-unit conventions. If existing currency semantics cannot be preserved, report the precise blocker instead of inventing a second money model.

HTTP / CQRS / OWNER / REVISION
Add explicit non-colliding authoring routes for BOTH admin and seller:
GET /shipping-services/authoring-drafts/{draftId:guid}/region-groups/{groupId:guid}/rate-rows
PUT same path: replace the entire group's row collection with required expectedRevision.
Reuse ONE Application query and ONE command/handler for the two audiences, existing IShippingAuthoringOwnerAuthorizer, fulfillment.view on reads and fulfillment.manage on writes, independently and before dispatch. SingleStore seller remains 404 before seller resolution. No owner/mode/tenant/seller bound from body/query.
GET returns group/draft IDs, current root draft revision, stored currency state and exact rows. PUT returns persisted IDs/revision/currency/rows; null/missing rows rejects while [] is allowed. New row IDs are server-generated; unknown/foreign/duplicate retained IDs reject before mutation. All routes, untrusted IDs/collections/ranges/amounts/duration/expectedRevision have explicit stable-code FluentValidation; reuse the repaired null-parent/required-collection pattern.
Every query/update scopes by full server-authorized owner AND draft/group relationship, never groupId alone. Missing/foreign draft or group gets the same safe canonical 404; no ownership leakage. A stale root revision gives canonical 409; group-rate update and region-tree replacement use the SAME root concurrency token so they cannot overwrite each other's work. No caller-returned owner as credential.
A whole row collection is fully validated BEFORE tracked changes; one failed last row leaves prior rows, root revision and other groups unchanged in the SAME context as well as new-context readback.

PERSISTENCE AND COMPATIBILITY
Extend the existing Shipping-owned aggregate/capability with typed row persistence and own FK to group. Stable group IDs remain the only geography reference. One logical SaveChanges transaction includes row changes and the root revision update; use EF's implicit atomic SaveChanges for this operation. Do NOT add a second explicit BeginTransaction or loosen R2's guard. Avoid an immediate unique ordering design that recreates the fixed swap problem; row order has no pricing priority.
Use stable row identities and keep unrelated groups' rows unchanged. Existing region replacement must preserve rows/currency on retained groups across reorder/selection edits. Explicit removal of a group removes only that group's dependent draft rates atomically; no owner/aggregate crossing. Test this interaction against the R1/R2 staged transaction. Existing create/region PUT bodies must never clear retained rate data just because old DTOs omit it.
Authorize ONE additive migration/designer/snapshot for the new draft-rate table and necessary new nullable authoring metadata only. New own FK/check/index scope only; no external FK, altered legacy table, historical migration/designer edit, owner backfill or copied legacy rates. Prepare SQL; apply it ONLY to the disposable task-owned database for proof. No shared migration/Host startup migrator.
Canonical module Endpoints -> ISender/CQRS -> Application ports -> Domain/own persistence. No Host business logic, foreign Application/Infrastructure/Domain/Endpoints/DbContext/schema join/transaction, new project/package or caller-driven mode/language authority.
Register new typed semantic errors once in the owning catalog with canonical resources/fallback; preserve the 44 legacy 400 statuses and 188/189/190 special statuses. Configured languages are not restricted to the existing resource filenames; no hardcoded display prose or fa/en inventory. Update request classification by real route/request/validator universe with exact set equality, not count-only or weakened guards. Keep root/path/DI/structure guards current for authorized additions without a certification promotion.

MEANINGFUL VERIFICATION
Use focused tests and real existing endpoint/pipeline/directory composition:

multiple rows under ONE group/one unchanged geography; save -> NEW context/read shows exact IDs/costs/currency/bounds/duration; another group unaffected;
same weight [1000,2000) with disjoint counts [1,4) / [4,11) succeeds; amount split [5000,10000) / [10000,20000) succeeds; full-tuple overlap/duplicate/unconditioned overlap rejects;
max-only/equal/inverted/null-element/missing-rows/fractional integer inputs/invalid money precision/negative fee/bad duration reject before writes; equal duration allowed;
Store A/B, Marketplace/Seller A/B ownership, view-only PUT denial, SingleStore seller unavailable, foreign draft/group/row IDs safe and unchanged;
invalid last row leaves SAME aggregate/tracker/revision/currency unchanged and later SaveChanges cannot leak it;
root revision conflict between rate update and region replacement, with no partial rates; retained group rates survive reorder; explicit group removal deletes only its own dependent rows;
authoritative non-IRR currency, currency change refuses silent reinterpretation, interface-locale switch does not change amounts/currency, unavailable trusted currency fails closed.

REAL POSTGRES / FOCUSED CHECKS
Reuse R2's parsed exact test target and existing image/harness on a FRESH task-owned container (prior one was removed). Verify exact ownership, free unique loopback port, task DB/user and live server target before any write; no raw substring guard. Preserve existing exact fixture expectations or explicitly scoped test-only configuration, never shared :5432/tooba_alpha/store-alpha. No image/package install/download or shared service start/restart.
Execute at least the rate-row relational roundtrip, invalid-replacement atomicity, root concurrency race with a deterministic pre-write barrier, retained-group/reorder/delete interaction and schema constraints on Npgsql/PostgreSQL. No InMemory substitute for relational proof; use controlled Contracts fixtures only for external facts. SQL/model proof alone is not transaction proof.
Setup <=8 minutes; commands <=180 seconds; no blocking waits >60 seconds. Run focused affected build/tests first. Whole Shipping suite ONCE after targeted checks pass and only if time remains, while the required isolated DB is still live; no full solution build, broad loop, skip/exclusion or baseline refresh. Stop at hard deadline with precise PARTIAL rather than test/fixture expansion.
If the requested slice cannot fit safely, commit only coherent in-scope progress and report exact remaining cases honestly; do not claim final rate/editor completion. Cleanup exact task container/database/listener/output, no shared bin/obj/.next/cache purge.

LOCKS / NON-GOALS
No frontend/preview redesign, taxonomy, owner-specific translations, schedules/cutoff, preparation policy, prepay/collect or carrier/free-mode implementation, carrier call, promo/free-above field, order/customer quote/evaluator/activation/publish, money movement, payment/Checkout or deployment. Do not alter existing ShippingRatePolicyEngine or operational read consumers. Default-OFF gates, PendingAllocation and TB-185 pause stay unchanged. No architecture recertification or manifest promotion.
Preserve unrelated dirty/untracked/stashes, shared apps/ports :3000/:3419/:5088/:5432, Redis/RabbitMQ and secrets. No reset/rebase/stash/force-push. No dummy/probe POST to Bridge /api/results; post only the complete finished result.

ACCEPTANCE / EVIDENCE / STOP
PASS only for real authorized persisted group rows, correct full-tuple/range/currency semantics, owner/root-revision/atomic behavior, additive migration parity, relational evidence and cleanup with no legacy runtime/architecture regression. PARTIAL for missing required API/relational/check proof. BLOCKED for authority/architecture/isolation conflict.
Record the exact received artifact in docs/ai/tasks/TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191.task.md after claim. Evidence in docs/architecture/evidence/TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191/: RESULT.md, model/API/range-and-currency matrix, relational assertions/SQL, commands/counts, isolation/cleanup. Append the actual draft-storage semantics/open amount-basis gate to Shipping decisions/recovery; no Architect ACCEPT or production claim. No automatic next task.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191
Parent-Task: TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R2
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_OWNER_GROUP_MANUAL_RATE_ROW_AUTHORING_PERSISTENCE
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Expected-Base-SHA: ebe9d10d3db0cf406f604fef4f2d650a15e2a554
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
Group-Row-Persistence-And-No-Duplicated-Geography: ...
Full-Tuple-Overlap-And-HalfOpen-Boundaries: ...
Trusted-Currency-Precision-And-Change-Safety: ...
Duration-Versus-Preparation-And-Promise: ...
Authorized-API-And-Foreign-Identity-Denials: ...
Same-Context-Atomicity-And-Unified-Revision-Race: ...
Retained-Group-Rate-Preservation-And-Deletion: ...
Additive-Migration-And-Historical-Byte-Parity: ...
Real-Postgres-Acceptance: ...
Contracts-DI-Tracing-Validation-Errors-And-Guard-Parity: ...
Runtime-Merchandise-Basis-And-Measurement-Gate: OPEN_NOT_IMPLEMENTED
Customer-Quote-And-Activation: NOT_IMPLEMENTED
Focused-Commands-And-Test-Counts: ...
Changed-Files: ...
Shared-Database-Data-Flag-Changes: NONE
Isolation-And-Cleanup: ...
Elapsed-Minutes: ...
Production-Functional-Acceptance: PENDING
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK