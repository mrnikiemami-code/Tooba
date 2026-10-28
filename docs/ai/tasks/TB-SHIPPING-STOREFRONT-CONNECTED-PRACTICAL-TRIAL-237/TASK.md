PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-STOREFRONT-CONNECTED-PRACTICAL-TRIAL-237
Parent-Task: TB-SHIPPING-CAPTURED-COUPON-CONTEXT-PARITY-236
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — existing storefront connected practical trial
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
Mode: CONNECT_EXISTING_STOREFRONT_AND_RUN_REAL_LOCAL_BUYER_FLOW
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## 0 — DELIVER A USABLE CONNECTED SCREEN NOW
User: "باید سریع تر وصل بشه دیتا که مهم نیست نگرانی وجود نداره".
This authorizes necessary NORMAL LOCAL NONPRODUCTION migration/test-data/runtime setup without another approval.
Connect existing Storefront cards -> each group's real method/date -> real selection save -> payment preview ->
existing final StartPayment/order boundary. No new design or parallel page. Do not return another survey or
negative-only/unit-only result. Prioritize one actual working buyer path and provide its usable local URL.
BOTH SingleStore and Marketplace/platform+seller-owner paths remain final requirements; do not strip owner/coupon
support for this trial. Reuse real DB/API authorities. No production deployment or real-money payment authorized.

Expected base is actual issuance commit containing THIS TASK. Read AGENTS/TOPs,236/ARCHITECT-REVIEW.md,
frontend/backend feature skills and FRONTEND-ARCHITECTURE-STANDARD, Shipping §29/UX reference and existing visual
locks BEFORE edits. Read actual full-owner/coupon scope. Preserve received artifact under docs/ai/tasks/ + evidence.
HARD45: source25 / real trial15 / recovery5. One correction; stop/report precise blocker at cutoff. No silent
extension or broad suite as substitute for missing practical flow. No next238 envelope. Worker PASS != acceptance.

## 1 — Protect USER unsaved work while allowing implementation/runtime changes
USER VISIBLE Chrome review window and original FE3000 watched checkout MUST NOT be enumerated/read/automated/
reloaded/closed or hot-reloaded by source edits. No HMR-triggering edits in that original checkout.
AUTHORIZED exception to normal main-checkout workflow: create an isolated implementation worktree on a task237
branch from issuance; all src/frontend edits + frontend/Host processes run there. Target branch remains main.
Push reviewed task source/docs via a normal non-force fast-forward HEAD:main (merge intervening docs if needed);
fetch and verify isolated worktree HEAD==origin/main. Leave original watched checkout's disk/branch untouched,
even if behind origin afterwards; report this intentional runtime-preservation state, not a false clean-main claim.
Do not reset/stash/amend/rebase/force-push, mass-add, kill-by-name or alter unrelated CRLF drift.
Use verified FREE task-owned ports (e.g. FE3100/Host5188; Marketplace FE3101/Host5189 if free); identify processes/
command lines before reuse. Do not stop original Host5088/FE3000 or other projects/ports6080/3001 etc.
Task browser: fresh TASK-OWNED profile and targets only; no URL matching/enumeration of any user session.
Leave dedicated connected review runtime/window for user handoff; do not automate that window after handoff.

## 2 — Architecture and owning files (mandatory)
Apply .cursor/skills/tooba-frontend-feature/SKILL.md and tooba-backend-feature/SKILL.md. Same existing route/page,
API adapters/state/cards; same design-system inputs/dialog/MessageBox, tokens, Shopeiva CSS, localization/RTL/LTR.
Existing src/frontend/app/storefront/storefront-shipping.tsx composes; storefront-shipping-api.ts,
storefront-checkout-api.ts and actual payment API adapter own network/strict decoding; existing
storefront-shipping-group-cards.tsx renders. Reuse established local feature/hook boundary for state; no second
shipping screen/client/framework/store, global CSS redesign, Home/PDP rewrite or UI authority for money/owners.
Backend changes limited to the two section3 blockers, existing additive capability/confirmation transport needed
by UI and actual endpoint forwarding. Order Application services/planner/writer, Infrastructure Directory, existing
Endpoints/handler/validators and Contracts boundaries remain; no Host business logic, foreign schema/DbContext or
foreign Domain/Application imports, DI cycle/service locator, new discount/calendar engine or permission bypass.
Any needed stable business error must use owning typed catalogue/descriptors/resources for configured locales.
Transport remains current module Endpoints -> ISender -> real request/handler/Result mapping. No direct DB shortcut.
Do not reopen231-235 accepted financial/owner/atomicity/idempotency algorithms.

## 3 — Close TWO source-confirmed blockers while wiring, no separate audit loop
A AssertGoodsEcho currently allows blank/omitted. Require ExpectedGoodsAmount per selected group, including explicit
"0" as lawful; exact canonical nonnegative decimal TEXT (no Number/parseFloat, grouping/sign/exponent/empty
coercion). Missing/malformed is owning intent refusal; actual mismatch is conflict BEFORE mutation. FE echoes
ONLY actual group goods from the CURRENT successful response and exact coupon/destination/cart context.
B Invalid coupon is currently ignored by Directory quote path: promotion.RejectionReasons is never checked.
Use actual Promotion application/rejection facts across the WHOLE quoted cart. A coupon applicable to one seller/
line but not another stays lawful; an unmatched/rejected coupon must show stable localized refusal, NOT appear
accepted with zero discount or an unrelated automatic promotion. Do not infer invalid from DiscountAmount==0.
Promotion owns code matching/eligibility. Prefer existing Applied coupon/rejection data; if insufficient, expose
the smallest additive authoritative status through existing Promotion.Contracts adapter, reusing evaluator
decision (no Order-side code matching policy, duplicate evaluator call or new engine).
Prove invalid+automatic-promotion control, valid partial applicability, exact provider exception identity and
caller cancellation once in focused facts; no wide suite. No silent downgrade to coupon_unavailable.
Correct236/RESULT.md's assertion that existing rejection handling was already implemented.

## 4 — Wire ACTUAL frontend behavior, preserve existing appearance
- Extend strict existing captured decoder to retain exact snapshot/source/selection-version/area/row echo strings,
  server GroupKey, real per-group goods and root SelectionRevision. Never parse long tokens through JS Number,
  String-coerce non-string IDs/tokens, reconstruct owner keys, identify methods by label or fuzzy binding.
- Store selected METHOD + allowed DATE per SERVER GroupKey, key method by canonical service/option WITH group,
  not global method label. Two sellers/owners using same binding/translation remain independent.
  Existing date row displays THAT method/date's interval; no global time slot or per-group client calculator.
  Allow change using existing card styling, no new mockup. Single-line captured cart uses same card path.
- Address/destination, coupon, cart version or projected provenance change invalidates previous confirmations.
  Keep recipient/address input. Requote via existing projection coordinator; stale/out-of-order success/error
  cannot install a wrong context. Pending/failed hides stale monetary promise; explicit retry works for EMPTY
  destination key too. New destination during in-flight must start/currently schedule the NEW key, not remain
  stuck on old request. Close recorded230 empty-key/new-key wait in same coordinator, not a second fetch framework.
- Coupon source uses existing UI/storage; explicit clear MUST mean no coupon (not value||stored fallback).
  Send SAME coupon to projection/save/payment-preview/StartPayment. Show invalid/refused using existing error UI.
- Continue validates ALL server groups have a currently quoted method/date, then sends CapturedGroups with all
  exact echoes including ExpectedGoodsAmount, current ExpectedSelectionRevision TEXT and coupon. No legacy
  ShippingGroups/shorthand mixed in captured request. Synchronous in-flight gate BEFORE await prevents double save.
  Consume real saved root revision, navigate existing payment page; do NOT commit on "Continue".
- Payment page loads REAL saved-selection preview and retains exact SelectionRevision from that accepted receipt.
  Final existing StartPayment request carries expectedSelectionRevision as TEXT + same coupon/cart version +
  existing stable idempotency. Double click/unknown outcome preserves key and reconciles, never places twice.
  Revalidation conflict requires new confirmation, never automatic price/date rewrite.
- Use server-provided gate/capability hints if necessary (small additive fields from SAME coherent options);
  preview-only/OFF stays honestly read-only. Do not leave blanket "not active" on an enabled fully configured local
  runtime, or let JS flags authorize writes. Actual server guards remain authoritative.
- Monetary group details remain exact text. Summary/payable comes from actual server accepted preview; no client
  float recompute or hardcoded/free/unknown=0. IRR display no zero fraction; other currencies retain exact scale.
  Goods and fees per owner stay separate; carrier collect never becomes checkout payable.
- Original history stays Order-only, no mutable draft fallback; if existing checkout GET suffices for trial,
  demonstrate persisted groups/lines from it, don't invent a new endpoint for this task.

## 5 — Prepare REAL LOCAL trial data/runtime; user's approval already given
Before any migration: verify LIVE database/server/user/tenant/edition and MigrationRunner plan, record exact pending
set. Tooba_alpha/store-alpha is known NONPRODUCTION; normal plan/apply of expected231/232/236 pending Order
migrations is authorized if exact set matches. If other migrations pending, inspect their identity/scope before any
apply; never blindly apply an unknown chain, reset/drop existing DB or edit historical migration files.
Alternatively/new Marketplace runtime: create a clearly TASK-OWNED DB and bootstrap through existing official
MigrationRunner/configuration. No forked tenant/identity model or manual business SQL seed. Preserve existing
Order migration history and disclosed stale EF snapshot; no baseline regeneration.
Run newly built Host from isolated output with coherent preview+selection+checkout options ON LOCAL ONLY and
explicit valid date-count policy. Checked-in defaults remain OFF. FE from isolated worktree points ONLY at that
task Host using existing BFF/proxy config; record process/path/port/environment and health.
Create/reuse named DEV buyer/cart/address/offers/stock/types/manual methods/captures/coupon via NORMAL existing
APIs, existing supported seed workflow or authorized admin UI, not direct SQL business inserts/role injection.
Use normal registration/login/CSRF/permissions. No invented session/CustomerPartyId/auth header bypass.
Data may be prepared/changed as needed in task-owned DEV setup; do not overwrite the user's original draft
01a16eeb-70b3-7000-87c2-564c82835496 or real account/cart. Record created ids; no secrets/tokens in git/screenshots.
Known original weight-gated draft must NOT have its conditions erased. Use lawful new DEV unconditional manual
method if actual measured weight is unavailable; no fabricated weights, free funders or CarrierApi success.
Build one configured SingleStore path first, then Marketplace-owned Platform+at least two actual Seller business
scopes with independent allowed methods/dates in mixed cart on real Marketplace configuration. Reuse same
application/components. If that second edition needs setup that exceeds box, deliver the working SingleStore URL
and report precise remaining owner setup, never label Marketplace delivered from fake Contracts or waive final
all-owner requirement. Do not stop useful connection work merely because no trial buyer existed.

## 6 — Practical completion receipts (real browser/HTTP, no response interception)
Normal task buyer chooses DB-backed destination -> independent method/date in each existing card -> Continue
PUT selection -> payment preview with same coupon/revision -> actual final StartPayment reaches existing order/
hold/payment boundary. Record actual response/state:
- At least one positive real configured selection/payment preview, zero order/reservation until final click.
- Actual final order boundary and idempotent replay, one Order with persisted own-group evidence; Payment only
  through existing configured SANDBOX/provider if available. Do not fake a successful hold/provider, invent
  inventory, flipTB185 or send real money. If configuration prevents final payment/order, capture EXACT owning
  error/stage and finish working UI; no false Initiated/checkout-ready certificate.
- Valid coupon, explicit clear/change and invalid coupon; group own goods/fees, changed context must reconfirm.
- Genuine failed destination refresh and retry; duplicate click at save/final; foreign scope denied under actual
  auth (no broad auth changes). For Marketplace use actual installed edition/business scopes, not SingleStore
  screenshot relabelled Marketplace.
- Desktop1440 and mobile390/360: existing group cards/address/payment preview visible, no overflow, method/date
  controls usable; screenshot native viewport + inspect. USER Visual ACCEPT separate.
- After Order persisted, change a DEV publication and read original Order via existing authorized GET; amounts/
  receiving facts/groups stay original with no repricing. No authored JSON per Order.
If blocked at a stage, state before/after actual receipts, source vs environment cause, already usable URL and
what exact step remains. No endless auth/driver rewrite: use available supported browser skill/control and existing
task-owned tools. Never inspect user tabs. Stop at hard limit honestly.

## 7 — Focused validation, commit/handoff
Affected focused FE state/decoder/API tests + one typecheck, narrow backend regression facts only for section3.
Because existing shared storefront cards change, run npm run test:critical-storefront once and follow
CRITICAL-STOREFRONT-VISUAL-REVIEW.md. No173/412/fullOrder/Shipping/solution/guard confidence loops or unrelated
baseline repairs. Real trial receipts outrank repeated unit passes. Record pre-existing failures without hiding.
Commit exact paths source/tests/evidence+ALL3 TOP recovery, normal push/fetch isolatedHEAD==origin/main,
original watched checkout intentionally untouched. Artifact TASK verbatim + RESULT + screens/receipt paths.
Leave connected task-owned local runtime available, supply exact review URL and ordinary account handoff securely
(no credentials in result/git); user review window still untouched. Do not automate handed-off window afterwards.
Preserve all known nonblocking gaps/TB185paused/defaultOFF/PendingAllocation, originalruntime previewONcount7.
No production activation/sale-ready/full owner/PSP or Visual ACCEPT unless actually proven and authorized.
STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE.

## RESULT CONTRACT — exact fixed envelope
One final complete real Result; no placeholder/probe/dry-run POST. Fill ACTUAL_* with verified values or NONE,
no unresolved template tokens in transmitted content. Status exactly PASS|PARTIAL|BLOCKED|RECOVERY_CONFLICT.
Expected-Base-SHA = full resolved issuance SHA of THIS artifact; actual worktree/branch/source start recorded.
Wire content without Markdown fences:
```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-STOREFRONT-CONNECTED-PRACTICAL-TRIAL-237
Parent-Task: TB-SHIPPING-CAPTURED-COUPON-CONTEXT-PARITY-236
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — existing storefront connected practical trial
Mode: CONNECT_EXISTING_STOREFRONT_AND_RUN_REAL_LOCAL_BUYER_FLOW
Bridge-Task-Row: ACTUAL_ROW
Status: ACTUAL_ALLOWED_STATUS
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: ACTUAL_FULL_ISSUE_SHA
Base-SHA: ACTUAL_FULL_START_SHA
Code-SHA: ACTUAL_FULL_SOURCE_SHA_OR_NONE
Evidence-SHA: ACTUAL_FULL_EVIDENCE_SHA_OR_NONE
automaticNextImplementationTask: NONE
ACTUAL_RESULT_BODY
END_TOOBA_WORKER_RESULT
```
Append the exact task closing marker on the last line of this artifact; nothing follows it.
Body MUST state: exact completed usable stage/URL; changed architecture/path/namespace/API flow; section3 actual
fix/focused facts; real server/DB/tenant/edition/process identity and pending/apply records; test-data authority/id
ledger; real read/save/preview/final request statuses/counts and payment stage; screenshot viewports/inspection;
all remaining edition/provider blockers; actual tests/time/corrections; original worktree/runtime/userwindow
preservation; isolated source/worktree git state/commits/push; checked-in OFF vs local ON distinction.
Worker PASS != Architect ACCEPT. No live/multi-owner/payment/Visual ACCEPT from unit/controlled evidence.

END_TOOBA_TASK
