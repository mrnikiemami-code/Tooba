PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-OPERATIONAL-GROUP-PACKING-CHOICE-259
Parent-Task: TB-SHIPPING-NONDELIVERY-RETURN-AND-RESEND-258-R3
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: Tooba Shipping — compatible delivery group, deliberate common or separate packing
Mode: OPTIONAL_COMMON_PACKAGE_OR_INDEPENDENT_PARCELS_WITH_ONE_DURABLE_REVIEWED_OCCURRENCE
Expected-Base-SHA: fbf24b6aec52a62960227d57a40f3bbe45879e04
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## 1. Position / mandatory preflight
Read CURRENT TOPs, SHIPPING-OPERATIONAL-CLOSURE-PLAN-256-260.md,258-R3/ARCHITECT-REVIEW.md,253 occurrence/workflow evidence and mandatory repository backend/frontend feature skills, Contracts and design-system conventions.
Save256-R2 and issues1/2 in257 accepted; issue3 accepted258-R3 within internal-operator scope. This task ONLY issue4.260 due workqueue/shared operational view NOT ISSUED. One worker, then STOP_FOR_ARCHITECT. User authorizes sequential continuation and final meaningful handoff after five defects.
Verify safe current main/origin parity; issue/review/recovery doc descendants of Expected-Base legitimate. Preserve unrelated work; no force/reset/amend/stash/rebase.

## 2. Product rule — grouping is not physical packaging
Keep ONE server-owned operational delivery group for compatible Marketplace-responsibility members: same proven physical origin, destination/customer commitment, accepted method/service/option, exact receiving day/window/timezone, currency and transport compatibility. Responsibility alone is NOT physical origin.
Store/Seller remain separate; no owner promotion, no manufactured origin, no edition switch or backfill to make a demo.
Inside an ALREADY compatible Marketplace cross-seller group operator chooses in one purposeful review dialog:
- «یک بستهٔ مشترک»: existing constituent parcel create/explicit reuse then ONE common consolidated package.
- «مرسوله‌های جدا»: create/explicit reuse exact member parcels, NO consolidated package, each has its own tracking/dispatch/delivery actions.
The group card stays one in BOTH choices. Financial seller orders, accepted fees/method/date and committed history never regroup or change.
Use one deliberate entry «آماده‌سازی ارسال» (or equally concise), then clear labelled packing choice in dialog. These ARE alternatives, unlike timing calendar tabs. No inline checkboxes/quantity inputs on normal page, no ambiguous duplicate action surfaces.
Bounded separate mode: one reviewed constituent parcel per selected member as current workflow supports. Do not invent arbitrary bin packing/dimension sums/multi-box optimizer.

## 3. Owning workflow / durable exact intent
MarketplaceOperationalGroupWorkflow currently ALWAYS creates common package; extend the existing owner, not a parallel service.
Packing choice belongs to the immutable server occurrence envelope/fingerprint BEFORE ANY allocation, alongside exact member set, reviewed line quantities and explicit reuse ids, checkout/group/method/actor scope.
- same occurrence + same complete request/mode returns exact recorded results;
- same occurrence + different mode/selection/reuse/source membership conflicts BEFORE new writes, including unallocated later members;
- distinct deliberate occurrence after authoritative completion is a new allocation from real remainder;
- separate mode completes durably with exact parcel links and an explicit completion result WITHOUT needing a package id. Never infer incomplete from packageId=null for a completed separate operation;
- common mode keeps current exact package membership and package reconciliation;
- whole plan validated before first write, explicit reuse-only never invokes default-selection binder; sibling accepted group of same seller cannot lend a parcel;
- crash after parcel commit/before member link, partial progress, lost response and concurrent twins recover only exact occurrence-keyed recorded ids, no open-parcel sweep or quantity guessing;
- public endpoint/orchestrator must permit exact recorded reconciliation before fresh readiness gates, with current actor/checkout/group authority EVERY time. Absent occurrence still requires lawful fresh readiness; no broad bypass.
Persist choice/completion additively in EXISTING owning occurrence store if needed. Historical occurrences with package semantics retain common-mode meaning explicitly; no reinterpretation of committed data.
Client persists full envelope + choice before POST, same-key retry/remount/close/reopen; changed choice blocked while unresolved; no bare-code reset or blind remint. Storage failure cannot authorize an unsafe NEW send. Reconcile remains reachable when fresh readiness disappeared; one synchronous single-flight, catch/finally, one inline localized error.

## 4. Projection / operations / existing lifecycle
Backend defines which modes are allowed and why; UI never regroups or guesses authority.
Separate parcels expose their own lawful tracking/dispatch/delivery/non-delivery/return/resend capabilities through existing projections. Common package exposes ONE parent action owner; locked child lifecycle actions never duplicate or bypass its parent.
Keep existing package cancel/dispatch/deliver behavior coherent. For unsupported parent non-delivery/return, keep precise localized refusal and disclose limitation; do NOT silently unlock children or pretend258 parent support exists. Parent return redesign is not required for the packing-choice defect.
If a previously separate Created parcel is explicitly reused in common mode, validate compatibility, ownership and not-already-packaged status before mutation; after locking, actions have one owner. Never silently change already dispatched packaging.
Method and trusted provider metadata derive from committed evidence. CarrierApi remains dedicated path; no fabricated Manual fallback or live carrier call.
Preserve257 packed-minus-shipped-minus-open, source-safe258 resend capacity, inventory reservation/customer-delivery conservation, original tracking/history/money and accepted scheduling evidence.
No fee recalculation/extra shipping charge caused by packing choice. Show existing «جمع هزینه ارسال» as accepted sum; never present it as newly charged per physical box.
Use existing public feature boundaries, DB language registry and RTL/design-system conventions; no flat-folder/baseline debt or raw tenant/source/enum diagnostics.

## 5. Focused proof on ONE DB — realistic compatible group without runtime promotion
ONE canonical DB tooba_alpha5432, FE3000 -> Host5088 SingleStore/store-alpha only. NO disposable/test/parallel DB, Docker PostgreSQL,15433 or edition/connection switch.
Canonical runtime cannot create Marketplace-owned groups via ordinary SingleStore checkout; do NOT repeat255/block on this or relabel an existing Store order. Positive proof may use minimal TASK-LABELLED committed-domain fixture via production domain/application paths IN SAME DB, with distinct proven physical origin facts and owning adapters. No raw business SQL or fabricated user history; record exactly which layer/fixture supplied Marketplace evidence. Never claim this proves a real Marketplace storefront checkout/live provider.
Real user order/cart/parcel/payment/services READ-ONLY. Task-labelled production-path fixtures authorized, exact IDs recorded, no sweeping delete.
Required cases:
A compatible two-seller group COMMON mode -> one group, exact constituent parcels+one package, one action owner; replay same ids.
B equivalent compatible fixture SEPARATE mode -> one group, exact member parcels, ZERO package rows, independent actions; replay completed separate mode works after remainder consumed.
C same occurrence changed mode/quantity/reuse conflicts zero added rows; duplicate submit and two independent contexts no duplicate allocation; crash/partial member progress recovers exact ids for BOTH completion shapes.
D incompatible origin/date/hour/method, Seller/Store ownership, foreign reuse/sibling group and unauthorized actor refuse before writes. No owner promotion.
E mounted REAL section+panel+dialog: clear two choices, exact request/mode persisted before POST, normal page no selections; separate completion shows individual actions, common completion one package owner; lost response remount keeps choice/request and reconcile reachable; storage failure zero unsafe POST.
F focused regressions for257/258 and253 durable envelope; no broad whole-module/base comparison or repeated green loops. Run affected tests + typecheck/build/publish.
No screenshots required; authorized task-owned read-only DOM can verify RTL/full titles/44px actions/no horizontal overflow at1440/1024/390. Existing Store review order stays correctly separate and unchanged.

## 6. Canonical deploy / schema boundaries
D:\Users\User\source\repos\SarvNewVer.
Deploy changed components on existing FE3000/Host5088 and verify served build/API/runtime. Additive owning migration only if needed, private backup first, repository MigrationRunner, old rows readable; no manual DDL/reset/reseed/backfill.
Preserve DataProtection, local CapturedPreview ON/7dates and checked-in OFF.255 parked; Tipax deferred, no credentials/provider calls/fake waybills.
No other database/process/port/edition, no user business mutation. Preserve approved timing tabs/save and256 MAX/cutoff/customer vs carrier calendars.
Review URL existing order http://127.0.0.1:3000/admin/orders/01a1b92d-e97e-7000-999a-143350d0edaf remains READ-ONLY; not a positive cross-seller fixture.

## 7. Evidence / recovery / result
Persist TASK verbatim docs/ai/tasks/TB-SHIPPING-OPERATIONAL-GROUP-PACKING-CHOICE-259/TASK.md plus TASK/RESULT/RECEIPTS/result-contract and focused proof under docs/architecture/evidence/TB-SHIPPING-OPERATIONAL-GROUP-PACKING-CHOICE-259/.
Update TOPs AGENTS.md, START-HERE-IF-CHATGPT-IS-LOST.md, SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md and closure plan: issues1–3 accepted, issue4 implemented pending Architect review, issue5 task260 NOT ISSUED. Do not claim five-defect complete or user ACCEPT.
Return BEGIN_TOOBA_WORKER_RESULT with full Base/Code/Merge/Evidence SHAs, HEAD==origin/main, exact schema/fixture IDs/layers/mode results, replay/concurrency/public-command proof, runtime/build, honest limits. Worker PASS != Architect/user acceptance.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK
