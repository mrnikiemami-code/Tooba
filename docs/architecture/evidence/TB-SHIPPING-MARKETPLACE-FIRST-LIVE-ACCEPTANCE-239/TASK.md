PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-MARKETPLACE-FIRST-LIVE-ACCEPTANCE-239
Parent-Task: TB-SHIPPING-LIVE-COUPON-MARKETPLACE-COMPLETION-238
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — real Marketplace owner acceptance
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
Mode: BOOT_MARKETPLACE_FIRST_REAL_OWNER_APIS_THEN_EXISTING_UI
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## 0. Deliver the MISSING Marketplace path FIRST
Read238/ARCHITECT-REVIEW.md and full-owner/coupon delivery scope. Reviewed source600a96f1acf94f05dda8a69f80e7f8981b0276e5.
Resolve full issuance SHA containing THIS TASK, safe fetch + received artifact persisted. One active task.
HARD35: Marketplace setup10 / live functional proof15 / narrow fix+payment captures5 / recovery5. One correction,
actual wall-clock stop; no hidden extension. First milestone MUST be an actual Marketplace Host/health/edition/
DB plus authorized normal API read, NOT another SingleStore form/viewport pass.
Reuse already built current binaries and worktree where safe; no unnecessary rebuild or dependency install.
No UI automation detour may prevent creating the Marketplace target. Use NORMAL public/admin/seller APIs for
setup and initial buyer flow, then the SAME existing UI for focused smoke. API evidence != browser evidence;
record each, but do not wait for a fragile React input driver before proving the owner path.

## 1. Runtime/worktree protection + standing local setup authority
User already authorized normal LOCAL NONPRODUCTION setup/data/migrations. No repeat permission for these.
Original3000/5088 checkout+window and handed-off3100/5188/profile D:\temp\tb238-handoff-chrome are USER WORK:
NEVER enumerate/read/automate/reload/close/change their source/HMR or stop them. Preserve their DEV offer/method/
cart/pointer. Task238 isolated D:\temp\tb238 at3101/5189 may be reused only if NOT handed off; verify ownership.
If source needed, separate task239 branch/worktree, keep reviewed UI source frozen. Task-owned new Marketplace
ports3102/5190 (or actual verified free alternatives), normal edition config, NEW named task-owned Marketplace DB
through official MigrationRunner/bootstrap. No existing DB drop/reset/history rewrite/schema adoption.
Record actual edition Marketplace, tenantNULL, deployment/connection target, process path/hash/health. SalesChannel
Marketplace in SingleStore is NOT this proof. Do not change checked-in edition/gates/defaults to switch modes.
231/232/236 are APPLIED on store-alpha; new target normal chain only, no blind replay on original DB.
Fresh task-owned buyer browser/login/CSRF, no fake auth or copied user cookies. Keep all credentials local/private.
No real money/PSP. Existing manual Pending boundary only; TB185paused/defaultOFF/PendingAllocation unchanged.

## 2. Architecture constraints (before any fix)
Read AGENTS/TOPs and applicable backend/frontend feature SKILL.md, FRONTEND-ARCHITECTURE-STANDARD and Shipping§29/
UX/critical visual reference. Reuse existing full-owner policy, planner/writer/core/Directory/Promotion/Tax/
Outbox and CURRENT storefront cards/adapters. No owner/price/calendar/discount/tenancy/permission framework.
Business owners: Platform=>Marketplace(), seller=>actual Offer.SellerPartyId; no user-id owner/inventedStoreId.
Merchant Manage vs View/foreign stays actual existing authorizer; buyer authority is owned cart, not merchant grant.
Existing module Endpoints->ISender->handler/Result, Contracts-only foreign boundaries, typed localized faults,
scopedDI. No foreign DbContext/schema joins, Host business logic, new backend engine or direct business SQL seed.
Normal actual admin/seller APIs or existing supported bootstrap may create named DEV sellers/grants/offers/stock/
types/Manual drafts/captures/coupons. No ad-hoc role/header injection, all-permission bypass or fabricated weights.
No new migration/full snapshot/baseline regeneration/cross-module FK. Only a source-confirmed functional blocker
may change existing owning code; preserve transaction/CAS/primary-error/cancellation/financial invariants.
Exact-path commit/push from isolated branch normal HEAD:main; fetch isolatedHEAD==origin/main. Original watched
checkout remains intentionally old. No amend/force/reset/stash/rebase/global tracker clear/unrelated CRLF churn.

## 3. Actual owner/coupon/order receipts — APIs then browser
Create actual Marketplace-owned Platform + TWO real Seller business scopes with authorized own publications on
same catalog binding but distinct capture/labels/rows/fees/dates. Real normal buyer cart mixes all three groups.
1 Record real server owner responses (Marketplace no tenant/seller key, SellerA/B actual business ids), own
  Manage success vs View-only and foreign draft denial using same actual permissions. No client-invented owner.
2 Real normal buyer projection -> each own method/date -> complete save200 with exact echoes/root revision ->
  paymentpreview200 -> final StartPayment order/manualPending. Own group membership/owner/capture/row/money/date/
  ticks persists; no sellerA capture borrowed byB and no Platform reinterpreted as seller. All groups required.
3 Create real DB coupon applicable to one group/seller and not others via authoritative Promotion API. Show own
  post-discount+tax goods and same coupon through read/save/preview/final; other groups unchanged. Clear/change
  requires new confirmation; invalid intent refuses owning typed code, no silent disappearance.
  INVALID READ receipt MUST include full verified destination and sanitized response; a200 early-unavailable with
  no quotation is not "tolerant priced acceptance". Record actual response status/body/code, not an assumption.
4 After successful DEV Order, change ONLY that task-owned DEV publication, then authorized existing Order GET
  returns original groups/charges/receiving facts unchanged. No live mutable Shipping lookup to reprice history.
5 Browser smoke using SAME Marketplace UI at least two groups independently chosen, actual save/paymentpreview;
  then exact payment-page390/360 captures. If form automation fails, retain API proof and state precise UI blocker,
  not "Marketplace wasn't attempted". No payload mocks/synthetic rendered text.
Record created ids, exact request/status ledger (no secret), original Order facts, profile/process/URLs.

## 4. Investigate the REPORTED22001 narrowly; no blind schema fix
238 two same-tick HTTP submits BOTH failed SQLSTATE22001, not concurrency success. Before more such retries,
read the saved exception/trace and record actual PostgresException SchemaName/TableName/ColumnName (if present),
failing parameter lengths/types (not values/secrets), request idempotency/correlation length and binary provenance.
Current source normalizes Outbox correlation to32 chars (MessagingCorrelation/CorrelationIdContext), table max128:
attribution to correlation_id is NOT source-confirmed. Do NOT enlarge varchar, truncate keys, swallow DB error,
bypass Outbox/Inventory or return fake success to make trial green.
Reproduce using the NATIVE normal final-click payload/idempotency key and normal correlation header/session,
two independent requests same actual intent. Invalid overlength driver-generated test input must be identified
as such, not blamed on concurrent checkout. Use existing validation/owning normalization and transaction rules.
If a small producer/transport validation/runtime mismatch is confirmed, repair ONLY it with one focused real
regression and repeat the real duplicate request: original authorized order replay, no extra inventory commit.
If not reproducible, keep concrete unresolved receipt and don't certify simultaneous idempotency; still finish
Marketplace single-order flow. No generic Outbox/Inventory audit, new retry scheme or distributed-lock redesign.
Client pay ref remains helpful but never closes a server-side failure by itself.

## 5. Focused proof / finish usable
Real HTTP owner flow is first; don't replay173/337/412/fullOrder/Shipping/solution/guards with missing target env.
Typecheck + exact affected frontend/backend class only if source changed. For backend relational fact use its
actual task-owned disposable target, identity beforeDDL/cleanup, not a blanket target-guard failure.
Run critical-storefront check ONCE only if shared visual source changed; preserve unrelated baselinebg-white red.
Actual PAYMENT native PNG390x844/360x800: requested==innerWidth==visualViewport atscale1, docscrollWidth<=innerWidth.
238 captured shipping only; do NOT recertify payment with shipping images. Inspect actual PNG and visible controls.
Keep existing CSS/cards, no overflowhidden mask/new design. User Visual ACCEPT separate.
If late source state blocks requested trial (e.g. response identity mismatch), freeze destination+coupon input at
request start and reuse existing coordinator only; no alternate fetch framework or automatic repricing.
At cutoff supply working Marketplace API/UI URL + exact completed stage OR exact configuration/API blocker already
observed on an actual target. No final sellable/all-owner/paid claim from SingleStore/manualPending or tests.
Leave stable task runtime; preserve all handed-off windows. No surprise update of3100 user runtime.
Task+RESULT+sanitized receipts/payment screenshots under evidence folder (not new root/screens). ALL3 recoveryTOPs,
append238 corrections (invalid-read and22001 attribution/proof limits), normal commits/push/fetch exactsource parity.
Actual time/corrections, permissions/DB/edition/gates/localON vs checkedOFF. No probe/test Result POST or oldposter.
STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE. No240 envelope.

## RESULT CONTRACT — canonical fixed tail
One complete final real Result. Fill ACTUAL_* with verified values/NONE; status exactly
PASS|PARTIAL|BLOCKED|RECOVERY_CONFLICT. No placeholders/editorial SHA text in transmitted Result.
Wire (omit Markdown fence):
```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-MARKETPLACE-FIRST-LIVE-ACCEPTANCE-239
Parent-Task: TB-SHIPPING-LIVE-COUPON-MARKETPLACE-COMPLETION-238
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — real Marketplace owner acceptance
Mode: BOOT_MARKETPLACE_FIRST_REAL_OWNER_APIS_THEN_EXISTING_UI
Bridge-Task-Row: ACTUAL_ROW_ID
Status: ACTUAL_ALLOWED_STATUS
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: ACTUAL_FULL_ISSUE_SHA
Base-SHA: ACTUAL_FULL_SOURCE_START_SHA
Code-SHA: ACTUAL_FULL_CODE_SHA_OR_NONE
Evidence-SHA: ACTUAL_FULL_EVIDENCE_SHA_OR_NONE
automaticNextImplementationTask: NONE
ACTUAL_RESULT_BODY
END_TOOBA_WORKER_RESULT
```
Append exact task closing marker shown at end of this artifact. Nothing follows.
Body leads with actual Marketplace target/owner/API/UI completion+URL (or exact target blocker), then normal
auth/role/data/DB/edition/process ledger; per-group coupon/quote/save/Order/history receipts;22001 exact input/
provider attribution+native duplicate proof or unresolved scope; payment native viewports; changed boundaries/
tests/time/corrections/git/runtime preservation. No controlled-to-live, Pending-to-paid or Visual ACCEPT inflation.
END_TOOBA_TASK
