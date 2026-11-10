PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-DUE-WORKQUEUE-AND-OPERATIONAL-ORDER-VIEW-260-R1
Parent-Task: TB-SHIPPING-DUE-WORKQUEUE-AND-OPERATIONAL-ORDER-VIEW-260
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Mode: REPAIR_PHYSICAL_WORK_ROW_PLAN_SCOPE_EXACT_LOCATOR_AND_TARGET_READINESS
Expected-Base-SHA: a5de61258f71ce5cceef30729a35ffac0751a972
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## Scope / no-loop boundary
Read CURRENT TOPs,260/ARCHITECT-REVIEW.md, original260 task and mandatory repository feature/design-system rules.
Issue5 is PARTIAL for the concrete row-ownership defect below. Preserve accepted timing authority, finance-free shared panel, filters and approved UI. Issues1–4 stay accepted. Fix ONLY correct work-unit identity, scoped plan/status/locator and associated counts/paging; no new feature/page/framework.
User explicitly says no loops. Focused failing cases below, fix once, run affected checks/build once, deploy and STOP. No whole-module/solution suites/base parity/repeated green loops/screenshots. Unrelated reds disclosed, not swept. No261.
Verify current HEAD/origin safe; issue/docs descendants legitimate. No force/reset/amend/stash/rebase or unrelated overwrite.

## 1. Confirmed defect
AdminFulfillmentWorkQueueQueryEngine.MapPageAsync returns rows.Select(unit=>...) and classifies ALL seller-order plans using ONE whole-unit pending flag. Earliest(plans) is also the target ShippingGroupId. This is a seller summary, NOT one physical work unit.
Case: group A due today already dispatched; group B tomorrow still pending => whole-unit pending=true, earliest plan A => false overdue/today and target A. Same seller two Created parcels collapse; cross-seller common package duplicates across sellers.
Do not solve by merely choosing another aggregate min date or suppressing rows.

## 2. Owning row identity and scope
Build work rows from existing owning accepted-group/parcel/package facts through Contracts:
- BEFORE parcel creation: a work row for exact still-unallocated accepted group/compatible operational group scope, using its own quantities and own accepted plan only;
- existing independent parcels: one row per relevant physical parcel, own accepted group/plan and own lifecycle/readiness;
- active common package: ONE parent physical row across members, exact package locator; no duplicated locked-child action rows/counts;
- mixed first-allocation remainder + existing parcels: represent distinct pending work without double-counting their quantities or re-offering shipped quantity;
- separate packing259 keeps independent physical parcels, no artificial package;
- dispatched work must not lend its old plan to a later uncreated/Created sibling. Historical null plan stays explicitly unspecified for THAT work scope.
Reuse existing production query/Contracts and quantity/capability authorities. Preserve ShippingGroupId, ResendOfShipmentId and relevant257/258 fields in snapshots; don't reconstruct incomplete snapshots that turn resend allocations into fresh allocations.
Stable discriminated row key (group/parcel/package + id), used for selection, sorting tie-breaks, pagination and locator. No FulfillmentId-only key collision across multiple rows.
Filter/search/count and page all operate on this SAME correctly scoped full set. Keep existing accepted timezone/[open,close) rule, no customer-date fallback/recomputation/backfill.
No domain policy change to force future dispatch prohibition; urgency != authorization.

## 3. Exact target and truthful operations summary
Queue operation link carries exact group/parcel/package locator, not just seller's earliest group. Shared route resolves/focuses that target using SAME shared operational panel; invalid locator explicitly unresolved, not silently another group.
readinessOf currently uses remainingPackedQuantity alone and labels Created/tracked parcels 'کار فیزیکی باقی نمانده'. Use actual target parcel/package state + projected capabilities:
Created awaiting tracking/dispatch is real work; Dispatched/InTransit awaiting delivery is real work; Returning/Returned has truthful return/receipt/resend state. No fresh creation quantity does not mean no physical work.
Keep server authority/one action owner/finance-free UI, current actor checks and retained request recovery. No duplicate lifecycle forms, no financial surfaces.
Do not reopen secondary visual cosmetics/unknown token fallback/pagination navigation polish unless directly needed for this correction.

## 4. Focused proof on same DB
Only canonical tooba_alpha5432, FE3000/Host5088 SingleStore/store-alpha. No extra/disposable/test/Docker DB/15433/edition switch.
Real user rows read-only; minimal task-labelled production-path fixtures in SAME DB permitted, exact IDs recorded. No raw business SQL/DDL/reset/reseed/backfill/deletion or clock change.
Required targeted integration cases:
A ONE seller, two accepted groups: A today dispatched, B tomorrow pending -> B Upcoming, correct locatorB; A only transit, neither incorrectly pending-due. Also A null plan and B real plan cannot lend evidence.
B same seller TWO independent Created parcels -> two distinct stable physical row keys/locators, own plans/status, paging size1 yields both once, totals consistent.
C common package TWO sellers -> ONE parent row/action target, zero locked child duplicates/count inflation; separate259 -> two physical rows.
D mixed partial packed remainder + existing parcel and258 Created resend -> conservation/scoped urgency, no phantom first-allocation quantity or shipped-plan contamination.
E mounted real route/section: exact Created parcel not labelled no-work, exact package/parcel target correct; finance absent and shared component unchanged. No manual stub proving just a wrapper.
Use existing relevant fixtures/owning paths. Include at least real query + Contracts bridge canonical persistence proof of A–C; lower-level pure classification does not prove row projection.
Run affected tests + typecheck/build/publish. No broad parity suites/green repeats. Read-only DOM targeted sanity optional; no screenshot matrix required.

## Deploy / evidence / handoff
Source D:\Users\User\source\repos\SarvNewVer. Same FE3000 -> Host5088 only; no migration expected. Preserve keys, CapturedPreview locally ON/7dates and checked-in OFF,256 scheduling/save,257 quantities/tracking,258 exceptions and259 immutable packing mode.
255 parked, Tipax deferred; no provider calls/credentials/fake labels or real user business mutations.
Persist TASK verbatim docs/ai/tasks/TB-SHIPPING-DUE-WORKQUEUE-AND-OPERATIONAL-ORDER-VIEW-260-R1/TASK.md; TASK/RESULT/RECEIPTS/result-contract and exact focused proof docs/architecture/evidence/TB-SHIPPING-DUE-WORKQUEUE-AND-OPERATIONAL-ORDER-VIEW-260-R1/.
Update AGENTS.md, START-HERE-IF-CHATGPT-IS-LOST.md, SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md and closure plan: issues1–4 accepted, issue5 pending260-R1 review, no five-defect completion until Architect review. Return full Base/Code/Merge/Evidence SHA, HEAD==origin/main, exact new row identity/plan binding, test cases/fixture IDs/runtime/build and limits. Clear final user trial links, no new task proposed.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK
