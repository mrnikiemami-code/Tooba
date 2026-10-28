PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-LIVE-COUPON-MARKETPLACE-COMPLETION-238
Parent-Task: TB-SHIPPING-STOREFRONT-LIVE-WIRE-COMPLETION-237-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — live coupon and Marketplace owner completion
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
Mode: REAL_COUPON_AND_MIXED_OWNER_TRIAL_ON_EXISTING_CONNECTED_UI
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## 0. Outcome / time
237-R1 SingleStore Manual/no-coupon path is accepted WITHIN SCOPE. Do not reopen accepted quote/owner/storage/
calendar/atomicity algorithms or rebuild another framework. Read237-R1/ARCHITECT-REVIEW.md and full required
owner/coupon delivery scope. Expected base resolves to FULL issuance commit containing THIS TASK; fetch safely.
Now finish REAL valid/invalid/cleared coupon + actual Marketplace/platform/seller-owner buyer flow using SAME UI,
and repair only practical blockers in section3. No policy-unit-only substitute, no final SingleStore-only exclusion.
HARD40: handoff/setup8 / actual trial+small fixes25 / focused proof+recovery7. One correction. At cutoff report exact
completed path/config blocker and usable URL. No silent extension/broad suite loop. Worker PASS != Architect ACCEPT.
Persist TASK verbatim under docs/ai/tasks/ and this evidence folder. No239 envelope.

## 1. Give user the EXISTING working trial, preserve it
Do FIRST: verify task237R1 Host5188/FE3100 path/process and health without touching original3000/5088.
Prepare ONE dedicated visible NORMAL buyer review browser at http://127.0.0.1:3100/fa/shipping with own buyer
session and active cart01a1807a-e7ef-7000-82d1-285f4bd5c982 (if still Active/owned; otherwise a named NEW DEV cart).
Use only verified task-owned browser profile/normal login. If session not available use existing BFF CSRF+login
with the task account's private TEMP credential; never expose secrets, enumerate user browsers, copy other user's
cookies or invent auth. No password printed in RESULT. Report exact profile/window handoff securely.
After handoff this is USER WORK: NEVER enumerate/read/automate/reload/close that window. Leave237R1 source/worktree
D:\temp\tb237 and runtime3100/5188 unchanged so later HMR cannot affect user. Original SarvNewVer checkout3000/
5088 and original USER reviewwindow likewise NEVER touched. Do not stop them or kill processes by name.

Do new implementation/trial in a separate worktree/task238 branch from issuance, free verified ports (e.g.
SingleStore3101/5189, Marketplace3102/5190). Reuse installed dependencies safely; no dependency/source writes via
a junction into a handed-off tree. Existing supported browser tooling/fresh task-owned profiles, no new driver project.
Normal non-force HEAD:main push/fetch isolatedHEAD==origin/main; original watched checkout disk stays intentionally
old. No amend/force/reset/stash/rebase/mass staging/line-ending churn. Exact branch/paths/processes in Result.

## 2. Architecture / runtime / data authorization
Read .cursor/skills/tooba-frontend-feature/SKILL.md, .cursor/skills/tooba-backend-feature/SKILL.md,
FRONTEND-ARCHITECTURE-STANDARD, Shipping §29/UX and CRITICAL-STOREFRONT-VISUAL-REVIEW.
Same existing shipping cards/API adapters/state + payment handoff/final-click client. No new screen, design,
global CSS/ Home/PDP changes, owner-picker, quote/discount/calendar algorithm, generic framework or permission model.
Backend only narrowly necessary actual live blockers in existing owning modules; preserve Contracts-only,
EndPoints->ISender->handler/Result, typed localized errors, scoped DI. No foreign schema/DbContext/Domain imports.
Business seller identity is actual Offer.SellerPartyId, NOT login id/inventedStoreId. SalesChannel Marketplace is
NOT Marketplace edition. Existing full-owner policy and captured235-R1 scope remain.

User already authorized NORMAL local nonproduction setup/data/migrations; no repeated approval.
231/232/236 are NOW APPLIED on tooba_alpha/store-alpha per237R1. Verify actual MigrationRunner plan instead of
reapplying history. No new schema/migration/baseline/regeneration/reset/drop existing DB.
Marketplace may use a NEW named task-owned DB bootstrapped through official runner/config/seed workflow; this is
authorized. Reuse real role/seller-business grant APIs and configured Localization/Geography/type/catalog/Offer/
stock/promotion/payment authorities. No direct business SQL seed, mocked responses, fake headers/auth or fabricated
measurements/provider success. Existing permitted Development bootstrap/admin identity may be reused ONLY as its
established scope allows; ordinary buyer session must use normal auth. No gate/permission weakening.
Use NEW named DEV offers/methods/carts/buyers/coupon so user's review cart/method remains stable; do not reassign
the offer or edit publication currently used by237R1 user trial. Original authoring draft/rates/week untouched.
Checked-in gates OFF; coherent preview/selection/checkout+date policy ON only named local task processes, record
all local flags including one-level catalog if used. No production/real-money payment. TB185paused/defaultOFF/
PendingAllocation unchanged. Real existing manual Pending boundary allowed, not PSP/paid success.

## 3. Minimal fixes driven by practical trial
A Payment pay() still relies on React paying: add synchronous ref gate BEFORE await, release in finally, preserve
same final-click idempotency through error/unknown outcome. Do not create a second checkout on retry.
B Shipping isValid/persist still uses raw projection while pending/failed cards hide it: require confirmed CURRENT
destination+coupon/cart/provenance context to build/send. Coupon change/explicit clear triggers SAME existing
coordinator re-read and clears confirmations without discarding typed recipient/address. Fix empty-key retry and
new-key during old in-flight in that coordinator (superseded answers/errors cannot install current state).
No second fetch framework; no client repricing, "missing=>0", silent reconfirmation or old price after failure.
C Payment mobile screenshot360 has expanded CSS width378; screenshot390 reports innerWidth393. This is NOT a
valid360/390 CSS proof. Fix only payment stepper/local layout shrinking/wrapping/overflow at exact widths; no
masking with body overflow hidden or clipping labels/buttons. Every requested capture must measure:
window.innerWidth == requestedWidth; documentElement.clientWidth <= innerWidth;
documentElement.scrollWidth <= innerWidth; visualViewport.width consistent with scale1.
Offcanvas inert drawer excluded from visible geometry, but it must not expand document/viewport. Inspect actual
PNG bytes; no cropping/downscaling masquerading as CSS viewport. Existing cards/styles preserved.
Only make other source changes when a real requested flow exposes a concrete blocker. No audit/helper-only work.

## 4. REAL live proof — coupons first, then Marketplace
Normal buyer and real HTTP/browser, no /v1 interception/fake payload:
1 SINGLESTORE valid real DB coupon from current Promotion rules reaches shipping read -> selection200 ->
  paymentpreview200 -> final-click Order/manual Pending. Record actual post-discount+tax per-group goods/fees/
  own date/selection revision. Create DEV coupon through actual admin API, not fake promotion port.
2 Invalid coupon clearly refuses; explicit clear/another code requires new read+confirmation, no resurrected stored
  code or old selection. Include partial applicability (one seller/offer eligible, another not) on actual mixed
  cart if available. No invented discount allocation, no amount==0 rejection. Old invoice/evidence stays original.
3 Actual MARKETPLACE edition/trusted context on separate target, Marketplace-owned Platform group AND at least two
  real Seller business scopes with own authorized drafts/publications on same binding, independent rows/fees/
  dates. Normal actual merchant Manage vs View/foreign denial through existing authorizer; no client scope grants.
  Real buyer read -> own method/date per ALL groups -> save -> paymentpreview -> final Order/manual boundary.
  Persist full own owner/evidence per group; swapping owner/foreign draft denied with zero order/reserve.
  Do not relabel SingleStore channel as Marketplace. No frontend mock or controlledContracts integration test
  presented as this live edition trial.
4 Real double-click save/final count and failed refresh+explicit retry. Preserve one checkout/reservation under
  replay; record actual requests/order ids/counts and exact codes. Stop at manual Pending; no real funds/PSP.
5 Change only a TASK-OWNED DEV publication AFTER a completed DEV order, read original through existing authorized
  Order GET, prove original group fee/date/facts unchanged. No current Shipping fallback/new history subsystem.
Capture sanitized raw response/status receipts (no auth tokens) and native1440/390/360 screenshots of actual
multi-group shipping and payment. User Visual ACCEPT separate; exact unfinished setup/stage stated if blocked.
Preserve first working user URL even if second-edition setup not finished. All-owner/coupon final gate remains
OPEN until actual evidence; no "not supported" waiver or indefinite setup detour.

## 5. Focused checks / recovery / handoff
No missing-env broad Storefront337 replay. Existing38 target-guard failures are NOT executed relational passes.
Only new affected frontend state/final-click/decoder facts + one typecheck; required npm run test:critical-storefront
ONCE if shared storefront visuals change. Record known unrelated baseline red, don't repair it. No wholeOrder/
Shipping/solution/guards/historical suites. If backend truly changed, run ONLY exact owning affected class with
its actual required disposable target, remove/verify closed. No fake-zero fixture fixes or lowered validators.
Main completion evidence is actual running app and positive requests, not another hundreds-test count.
Persist TASK/RESULT/receipts/screens, append237R1 mobile-proof correction. Update ALL3 recovery TOPs (AGENTS.md,
START-HERE-IF-CHATGPT-IS-LOST.md, SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md); last one was stale in237R1.
Record actual applied migrations/local gates/runtime/URLs/account handoff and precise edition/coupon/PSP limits.
Stable source/normal exact-path commits/push/fetch isolated parity. No secrets in git/screens/result.
Leave working dedicated user trial runtime/session available; never automate handed-off windows.
Known Offer API/227mobile-note/225-R1lostLOCALwork retained; resolved cross-owner label-map is not reopened.
STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE. No diagnostic probe Result POST.

## RESULT CONTRACT — fixed canonical wire envelope
One complete final real Result. Fill ACTUAL_* with verified real values/NONE; status exactly
PASS|PARTIAL|BLOCKED|RECOVERY_CONFLICT. No editorial notes inside SHA fields, no unresolved template transmitted.
Keep automaticNextImplementationTask immediately after Evidence-SHA (237R1 omitted it). No text after task end.
Wire content without Markdown fences:
```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-LIVE-COUPON-MARKETPLACE-COMPLETION-238
Parent-Task: TB-SHIPPING-STOREFRONT-LIVE-WIRE-COMPLETION-237-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — live coupon and Marketplace owner completion
Mode: REAL_COUPON_AND_MIXED_OWNER_TRIAL_ON_EXISTING_CONNECTED_UI
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
Append the exact task-closing marker on the last line; nothing follows.
Body leads with working URL + dedicated normal session handoff and exact completed stage; then actual process/
DB/edition/tenant/permissions/normal data ledger; coupon and mixed-owner real request/status/money/date/Order/
manual boundaries; viewport metrics+native screenshots; source architecture/changes; exact checks/time/corrections;
applied vs pending migrations/gates, checked-inOFF/localON; preservation and remaining concrete blockers.
Worker PASS != Architect ACCEPT. No full-release/paid/PSP/Visual ACCEPT from manual Pending or unit facts.
END_TOOBA_TASK
