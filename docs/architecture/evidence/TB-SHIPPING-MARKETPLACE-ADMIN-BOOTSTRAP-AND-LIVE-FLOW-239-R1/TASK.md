PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-MARKETPLACE-ADMIN-BOOTSTRAP-AND-LIVE-FLOW-239-R1
Parent-Task: TB-SHIPPING-MARKETPLACE-FIRST-LIVE-ACCEPTANCE-239
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — legitimate Marketplace development commissioning
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
Mode: BIND_REAL_DEV_ADMIN_THEN_CONTINUE_EXISTING_MARKETPLACE_FLOW
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## 0. Concrete decision / outcome / time
Read239/ARCHITECT-REVIEW.md. Real Marketplace target5190 already exists and all modules are migrated. Do not
repeat boot/schema survey. The fixed admin tuple has no corresponding normal login principal: finish that
commissioning through existing Contracts, then seed real data and continue Marketplace shipping.
THIS TASK EXPLICITLY AUTHORIZES a dedicated normally registered Development admin to receive the EXISTING
Marketplace admin relation through guarded opt-in bootstrap. It is legitimate provisioning, not a forbidden
permission invention. Do not stop again merely because this approved first-admin binding creates a grant.
Never promote a seller/request actor/arbitrary first user, bypass403 or change Production authorization.
Resolve full issuance SHA containing THIS TASK, safe fetch; preserve received artifact verbatim.
HARD35: source/provision12 / real normal-authorized API flow18 / recovery5. One correction, hardstop; no broad
suite or extra framework. At least normal admin login -> protected existing seed/authoring200 AND seller still403
must be executed, not just a helper test. Continue to actual mixed-owner save/paymentpreview/Order within box.
No240 envelope, no automatic next task. Full owner/coupon/practical delivery still required, not claimed by bootstrap.

## 1. Mandatory architecture / exact allowed seam
Read AGENTS/TOPs and .cursor/skills/tooba-backend-feature/SKILL.md; existing Host Development and Identity/
AccessControl contracts/security patterns. This is Host STARTUP COMPOSITION only:
Host/Development/MarketplaceAdminDevBootstrap.cs (plus minimal options/startup wiring/test if needed).
Use IIdentityAuthenticationService from Identity.Contracts.Auth and existing IAuthorizationTupleWriter from the
existing neutral boundary. No Identity/AccessControl EF, Domain/Infrastructure import, rawSQL, new permission/
role catalogue, new endpoint, service locator workaround or mutable global authority. No CQRS bypass for HTTP:
normal module routes keep Endpoints->ISender->handler/Result and their actual authorizers FIRST.
Shipping/Order/Promotion/Tax/core/receiving calendar unchanged unless a concrete later live blocker is reported.
No Host-owned registration/password hash/price/stock/business policy. Existing Identity owns normal registration,
password/session; existing authorization relationship/guard owns permission. No production commissioning design
or AMSC/authorization recertification is in scope. No unrelated cleanup of old Development code.

## 2. Actual registered administrator + narrowly guarded configuration
A On EXISTING task Marketplace5190, register a NEW dedicated DEV admin with normal POST /v1/auth/register and
random private password (outsidegit), obtaining REAL generated Identity user id. This account initially has no
privilege. Do not impersonate fixed cccc actor, use X-dev-actor, manufacture token or set arbitrary UserId.
B Add/use explicit opt-in local configuration, e.g. Tooba:Development:MarketplaceAdmin:{Enabled,Identifier},
Enabled defaultfalse, Identifier email kind under existing Identity normalizer. NO password in checked-in
configuration/code/results. Resolve that exact existing account using
IIdentityAuthenticationService.FindUserIdByIdentifierAsync(LoginIdentifierKind.Email,...).
No new Identity API or forced fixed user id required. Missing/unknown/blank config refuses provisioning, not fallback.
C New binding activates ONLY when BOTH actual host environment Development AND actual registry/current commerce
edition Marketplace (tenantNULL + task connection target) AND explicit opt-in. Ensure method itself is safe
even if called outside the normal Program Development branch. Production/SingleStore/disabled/missing config:
no new registered-admin tuple/identity writes; no session/auth policy relaxation.
D Bind ONLY resolved real principal to SAME existing tenant marketplace-platform relation Member used by
MarketplaceAdminDevBootstrap, through existing writer. No wildcard/all users or promotion of existing sellers.
Retain original no-opt-in legacy behavior where needed; guard tests count ADDITIONAL configured provisioning,
not assume old bootstrap never wrote its fixed legacy tuple. Never reset an existing password/account status.
Re-run is idempotent; no duplicate principal/relationship or extra privilege. If extra EXISTING module admin
capabilities are required, reuse canonical current development prelude/role binder, not invented grant lists.
E The user-authorized taskDB is tooba_marketplace_tb239; no effect on SingleStore/handed-off targets. Rebuild
isolated Host ONCE if source changed, restart ONLY verified task5190 process (wasPID40544) with private opt-in
env; normal login admin must then pass protected existing route. Do NOT just fake a successful guard in a test.

## 3. Commission real data + prove access immediately
First receipts: real normal admin login200; protected official catalog/demo/authoring route succeeds; same real
seller session continues403 on admin seed, unknown/foreign scope denied. Record actual user IDs and tuple subject/
resource/relation (no credentials/tokens), not fixed orphan GUID. Read-only/Manage remains existing permissions.
Official /v1/admin/catalog/demo/reset-and-seed is authorized ONLY against this named TASK-OWNED Marketplace DB
whose initial business rows are empty; don't target/reset any existing user DB/runtime. Prefer smaller existing
seed API when available. No direct business SQL seed.
Then existing SellerDevContextBootstrap.EnsureAsync (same assigned Marketplace commerce scope) may reconcile real
users+real Party organizations through its EXISTING Contracts after official Party seed. That source currently
returns early with 0 parties, explaining no membership. Do not assume legacy MarketplaceSellerDevBootstrap's
fixed Published pair equals the real logged-in seller identities. Use actual Identity lookup + Party membership,
and verify two DIFFERENT real seller business scopes/actors with own authorized access.
Do not modify normal seller authorization to make a mismatched legacy demo pair work. Keep403 meaningful.

## 4. Continue the missing actual owner shipping flow, APIs FIRST
Use current full-owner shipping path, real normal APIs/admin/seller:
- Marketplace-owned Platform + SellerA + SellerB publications on same binding, distinct captures/labels/rows/
  fees/dates, real Offer responsibility/type and real stock. No invented weights/price/free funder/provider.
- Real normal buyer/cart: projection -> all groups own method/date -> complete selection200 with exact echoes/
  rootrevisionTEXT -> paymentpreview200 -> actual StartPayment/manualPending if configured. No server mock/fakehold.
- A real DB coupon applicable to one group only, own after-discount+tax goods; other groups unchanged. Invalid/
  clear/change refuses/reconfirms as actual policy; record full destination + status/body, not200 early-refusal
  mislabelled priced acceptance. Final Order uses its OWN per-group fullowner/money/date evidence.
- Exact raw sanitized HTTP receipts first. Use normal API setup so controlled React input automation does not
  prevent useful proof. Same existing Marketplace UI smoke afterwards; do not present APIs as browser evidence.
- Post-Order task-owned publication change + existing authorized Order GET original evidence unchanged when an
  Order exists. No new history subsystem/endpoint or mutable repricing.
- Payment390/360 native captures only after actual payment page reached; shipping screenshots are not payment
  proof. RequestedCSSwidth==innerWidth atscale1, no document overflow, inspect PNG. UI edits only if a concrete
  blocker: apply frontend feature skill and preserve current cards/Shopeiva/design-system/no globalCSS.
If cut off after legitimate admin/data commissioning, give exact completed200/403/data/owner stages and remaining
step. No vague "grant forbidden" after this explicit decision and no final Marketplace claim without owner flow.

## 5. Narrow checks / preserved runtime / handoff
Focused provisioning facts: validrealidentifier->correctexistingtuple; repeat idempotent; unknown identifier no
grant; disabled/Production/SingleStore no ADDITIONAL provisioning. Use real normal HTTP admin+seller control as
main gate. Small affected build/host-seam class only, no fullHost/Order/Shipping/337/173/412/guard/audit sweep.
Existing marketplace modules already migrated viaofficialrunner; inspect plan only if needed, no new schema/
migration/baseline/history edit or blind replay. New local opt-in config not checked-in Enabled defaults.
Original3000/5088 checkout/window and handed-off3100/5188/profile D:\temp\tb238-handoff-chrome are USER WORK:
never enumerate/read/reload/automate/close/source-edit/HMR. Task238SingleStore3101/5189 source also preserved
if handed off; use safe task239isolated worktree/branch/FE3102 only. Verify process/path before restart5190.
Normal nonprod setup already approved; no repeated routine permission. No realmoney/PSP/productionactivation.
Checked-in shipping gatesOFF/localtaskcoherentON; TB185paused/defaultOFF/PendingAllocation unchanged.
Keep238unknown22001 unclassified (normaloutboxcorrelation32); don't invent schema fix or claim concurrent
idempotency. Record remaining defect honestly, no generic Outbox work in this task. Known227/225R1lostLOCAL retained.
TASK/RESULT + sanitized receipts/screens, append239 commissioning decision, ALL3 recoveryTOPs; exact source/
process/DB/identity/access/data ledger and usefulURL. Normal exact-path commit/push HEAD:main/fetch isolatedparity;
original watchedcheckout intentionallyold. No amend/force/reset/stash/rebase/massstage/lineending churn.
No secret/password/token/cookie in git/result/screen. Leave taskruntime; don't automate a handed-off browser.
STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE. No probe/test/dryrun Results POST.

## RESULT CONTRACT — canonical fixed tail
One complete final real Result, ACTUAL_* replaced by verified values/NONE; status exactly
PASS|PARTIAL|BLOCKED|RECOVERY_CONFLICT. Exact fullSHA fields, no placeholders/editorial notes transmitted.
Wire content without Markdown fences:
```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-MARKETPLACE-ADMIN-BOOTSTRAP-AND-LIVE-FLOW-239-R1
Parent-Task: TB-SHIPPING-MARKETPLACE-FIRST-LIVE-ACCEPTANCE-239
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — legitimate Marketplace development commissioning
Mode: BIND_REAL_DEV_ADMIN_THEN_CONTINUE_EXISTING_MARKETPLACE_FLOW
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
Append exact taskclosing marker on lastline; nothing follows.
Body leads actual adminlogin/protectedroute200/seller403 and ownerflow/URL stage, then guardedenv+realIdentity/
tuple/data/DB/process/provisioningtests; pergroup coupon/save/Order/manual/history receipts or concrete missing
stage; paymentviewport actualstatus; sourcearchitecture/changedpaths/time/corrections; gitparity and userwindow/
localvscheckedflags. WorkerPASS!=ArchitectACCEPT; no productionsecurity/paid/PSP/fullrelease/VisualACCEPT inflation.
END_TOOBA_TASK
