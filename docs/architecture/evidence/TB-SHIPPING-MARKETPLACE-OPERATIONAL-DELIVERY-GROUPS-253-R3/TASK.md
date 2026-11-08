PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R3
Parent-Task: TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R2
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: Tooba Shipping — correct dispatch origin and operation occurrence, then deploy
Mode: RESTORE_PROVEN_ORIGIN_FIX_REPLAY_IDENTITY_PREFLIGHT_AND_DELIVER_CANONICAL
Expected-Base-SHA: 2ac19888686b668530b146c7f80702c8edbeb415
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

1. Architect decision — precise residuals, no restart

R2 group workflow, real actor checks, shared facts reader integration and React event tests are useful and retained.
R2 PASS is REJECTED as an unqualified completion claim:

It changed the approved proven-origin rule without authorization.
It identifies a parcel occurrence by payload, a known defect already corrected in 249-R6/R7/R8.
It validates members inside a write loop and silently filters foreign selections/reuse IDs.
It did NOT deploy, although R2 section 5 explicitly required canonical deployment.

Repair these exact owning seams and deploy. Do not redo accepted UI or start another audit.
Read applicable governance/recovery, feature skills and 249-R6/R7/R8 occurrence decisions first.
Persist this task verbatim. Safely recover main; preserve unrelated edits and any harmless documentation fast-forward.

2. Restore the approved origin rule

MarketplaceOperationalDeliveryGroupAuthority.TryResolveOriginIdentity currently returns identity="marketplace" when origin location/address is absent.
Remove this ownership-as-origin fallback. An organization responsible for sending can have multiple warehouses; responsibility is not physical origin.
Missing origin must stay unproven and separate. Restore regression tests for Marketplace missing-origin fail-closed; do not rewrite the expected product behavior to make a green fixture.

Use an existing authoritative dispatch-warehouse/location seam if available, scoped to the correct Marketplace and with explicit provenance. Shared authority alone, tenant key, same carrier city code, recipient address or the word "marketplace" are not proof of one dispatch origin.
OriginLocationCode may be a carrier city code: establish its granularity/authority before accepting it as warehouse identity; two warehouses in one city must not be silently equated.
If no origin seam exists, keep missing-origin groups independent with a short actionable reason; implement the minimal required authoritative origin identity through the owning configuration/contract/capture path only if necessary for future genuine grouping. Do not guess/backfill existing orders.
Controlled positive tests must populate genuine origin evidence through that real seam.
Seller/Store responsibilities remain separate; no Store→Marketplace promotion.
No requirement is waived because current Manual groups lack data.

3. Occurrence identity must be independent of payload

Current BuildMemberParcelDispatchKey hashes checkout+group+line quantities and BuildWorkflowOperationLocator hashes only checkout+member groups.
RecordedQuantitiesMatch then auto-reuses every open parcel of the group with matching quantities. This confuses a deliberate second identical partial package with retry of the first, and a displayed group locator with an operation occurrence.

Reuse the established occurrence concept:

Prebind a fresh workflow occurrence ID BEFORE the first mutation; scope to actor/owner+checkout+operational group.
Same occurrence + same canonical full request → replay/reconcile same allocations and package.
Same occurrence + changed selection/reuse membership → typed intent conflict, zero mutation.
Explicit next occurrence after confirmed completion may create an identical quantity parcel from remaining packed quantity; must not reuse the previous occurrence's parcel/package.
Canonical quantities must retain full supported precision; current "0.####" formatting must not collide for distinct supported quantities.
Persist unresolved occurrence and immutable reviewed request before writes. Reuse existing durable dispatch/operation infrastructure if suitable; do not introduce a second incompatible idempotency framework.
Allocate/link exact constituent parcels under that occurrence; recovery uses recorded IDs, never all currently open parcels or aggregate-quantity guessing.
Do not automatically include unrequested existing parcels when a member has no selection.
Network/5xx/cancellation/response loss is unresolved; no new occurrence or blind resend until reconcile. Readiness changes must not hide recovery.
Same occurrence concurrent requests must produce one set of allocations and a typed pending/replay result, not arbitrary 500s.
Sequential partial progress must be durably recoverable, or transactionally rolled back. An async loop and deterministic payload hash alone are not sufficient.

UI retains existing compact common-group flow; add only the required operation/recovery state. One inline error, preserved quantities, synchronous single-flight and try/finally. Catch transport throws as well as decoded failures.

4. Validate the WHOLE plan before any member write

MarketplaceOperationalGroupWorkflow.PrepareAsync currently filters:
memberSelections = request.Selections.Where(member.OrderLineIds.Contains);
memberReuse = requestedReuse.Where(snapshot.Shipments.Any).
Foreign IDs can disappear silently. A bad later member can be refused after an earlier member was allocated.

Build and validate a complete immutable plan before the first CreateShipmentAsync:

Every requested line belongs to exactly the intended accepted member, with valid supported quantity; reject unknown lines/duplicates according to the owning contract rather than silently dropping.
Every requested reuse shipment belongs to this checkout, exact accepted member, proper lifecycle and permitted package state; all supplied IDs must be accounted for.
Bind all methods/recipient metadata and verify actor/payment/packing/remaining quantities for every member first.
Existing packaged parcel is usable ONLY as replay of this exact recorded occurrence, not arbitrary reuse.
Mutation rechecks stale/concurrent state at its owning boundary.
A member with neither explicit usable selection nor explicit reuse is refused before any write.
Keep CarrierApi on dedicated execution; no generic consolidation bypass.
Preserve 252-R2 trusted Post recipient, R3 origin/collection guard and committed fees/history.
5. Focused proof that closes actual defects

Use task-owned disposable PostgreSQL with real Order evidence/history/facts + production workflow/Fulfillment adapter. Preserve positive R2 tests but do NOT call sequential invocations "concurrent twins".
Execute these necessary cases:
A. Same Marketplace authority, DIFFERENT physical origin → separate/refused; missing origin → separate/refused; same proven origin → compatible.
B. Same occurrence retry and actual barrier-coordinated two-context concurrency → one recorded allocation set/package; exact typed results.
C. First occurrence selects 1 of each seller's quantity-2 line; confirmed next occurrence selects the identical 1+1 → TWO distinct parcel sets/packages; total allocated quantity exactly 2 per line. Original occurrence replay must still return its original package.
D. Same occurrence payload/reuse mismatch → conflict; foreign line/reuse ID and invalid later member → zero partial writes.
E. Inject failure after first constituent persistence/before common package + retry; suppress success response and remount; recover exact recorded IDs without extra parcels or sweeping unrelated parcels.
Assert immutable accepted money/snapshots and financial seller orders.
No catch(Exception)-as-success. Classify tests correctly; report which cases executed, not just totals.

Real jsdom interaction tests on shipped group flow:
prebound occurrence in request, same-tick one request, unknown/remount retains occurrence+request, explicit next operation creates new occurrence, inline failure retains reviewed quantities.
No screenshots, no full suite or unrelated refactor loops.

6. Deployment is mandatory in THIS task

R2 claim "no runtime asset was required by the issued scope" is false: its section 5 explicitly said deploy.
After final focused tests/typecheck/production build/Host publish, install the final corrected code on:
D:\Users\User\source\repos\SarvNewVer
FE http://127.0.0.1:3000 → Host http://127.0.0.1:5088
SingleStore / store-alpha / tooba_alpha.
No alternate final runtime/DB/edition. Preserve keys and local CapturedPreview Enabled/SelectionEnabled/CheckoutEnabled/AvailableDateCount=7; checked-in defaults OFF.
If additive schema is needed for durable occurrence/origin: private verified backup + scoped MigrationRunner + exact applied/readback ledger; no manual DDL/history rewrite/reset/reseed.

Authorized read-only review of:
http://127.0.0.1:3000/admin/orders/01a1b92d-e97e-7000-999a-143350d0edaf
Store groups stay independent. No pack/create/dispatch/cancel/payment/merchant edits on real user data.
Confirm deployed build identity and authorized screen DOM, layout/focus/no covering overlay at 1440/1024/390; do not equate route200 with rendered authorized UI.
No screenshots/live Tipax/account request. Preserve arbitrary user tabs/unsaved work. Clean only owned disposable resources.

7. Recovery and result

Append Architect correction to R2 RESULT/RECEIPTS (do not erase history):
origin fallback was unauthorized; payload≠occurrence; R2 was not deployed; its claimed twin proof was sequential.
Update all three recovery TOPs with the ACTUAL corrected/deployed state:
AGENTS.md
docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md
Record responsibility and origin as distinct authorities, not a newly invented product rule.
Task/evidence directories for TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R3 contain exact TASK.md, RESULT.md, RECEIPTS.md.
Preserve 249 carrier and product physical-spec decisions, TB185 paused/default-OFF and retained known issues.
Commit/push normally; verify local==origin/main, no amend/force/reset/stash/rebase.
Return complete worker envelope with exact base/code/evidence SHA, measured elapsed, executed checks, migration ledger, actual runtime/code identity and canonical review URL.
Do not label undeployed/incomplete work PASS. If any requirement cannot be completed, precise PARTIAL/BLOCKED with exact reason.
Worker PASS is not Architect/User Visual ACCEPT.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK
