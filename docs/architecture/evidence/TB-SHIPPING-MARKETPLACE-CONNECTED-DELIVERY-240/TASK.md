PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240
Parent-Task: TB-SHIPPING-MARKETPLACE-ADMIN-BOOTSTRAP-AND-LIVE-FLOW-239-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — finish the existing connected delivery
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
Mode: REUSE_MODULE_OWNED_SELLABLE_SEED_AND_FINISH_REAL_MARKETPLACE_DELIVERY
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## 0 — Binding user delivery contract, cumulative time and actual outcome

The user explicitly treats the previously quoted 3–4 focused working hours as END OF THIS DELIVERY, not a partial milestone. Scope remains SingleStore + Marketplace Platform and real Seller owners, coupon parity, real Order/manual payment Pending, usable existing desktop/mobile UI, duplicate submission and immutable original Order evidence. No PSP/real-money success is promised. A build, fixture, empty storefront, administrator login or source-only connection is NOT completion.

239-R1 reported ~53 minutes after that commitment. Charge that time against the SAME four-hour upper planning budget; approximately 187 minutes remain if no other focused work has intervened. Reconcile actual cumulative timestamps at receipt; DO NOT restart a new four-hour clock or claim unmeasured splits. This task plans at most 150 focused minutes within the remaining envelope. Checkpoint at minute 20: real seller organizations + buyable offers + stock and normal seller access. If that checkpoint fails, record the exact current exception, owning seam and remaining estimate immediately in recovery/progress rather than silently spending another hour on catalog seed. Continue authorized necessary fixes; do not produce another helper-only completion claim.

Execution ordering: reuse existing target and module-owned sellable seed FIRST; actual mixed-owner HTTP flow SECOND; existing UI + native mobile payment + duplicate/history LAST. Small focused fixes needed to achieve that outcome are authorized in their owning modules. No separate survey/refactor/audit task. Do not restart the 890-second catalog reset seed.

## 1 — Source, current state and preservation

Read AGENTS.md, the other two TOP recovery files, 239-R1 TASK/RESULT/RECEIPTS and ARCHITECT-REVIEW, and the binding complete owner/coupon delivery scope. Verify receipt HEAD and issue commit before changes. Review source base 3c726019449f1ccad0dadc93e783292ac8de276f.

Reuse task Marketplace Host 5190, Development, DeploymentId tb239-marketplace, Tenant NULL, DB tooba_marketplace_tb239. Final known PID 22832 from D:\temp\tb239r1-build\out, verify before stopping. All module schemas already migrated. Real configured DEV administrator 01a1818b-5257-7000-bada-84233a2b21fb can login and access protected admin routes; sellers remain denied there. Private credentials stay on task disk only. Keep approved opt-in binding, no re-provisioning investigation.

Original watched checkout / FE3000 / Host5088 and USER handoff FE3100 / Host5188 / D:\temp\tb238-handoff-chrome are USER UNSAVED WORK. NEVER enumerate/read/automate/reload/close these windows or edit their watched source. Preserve 230 DEV publication and user original draft, rates, week, pointer, assignments and handed-off cart. Use isolated task checkout and free FE3102 paired with task Host5190, or verify another unused task-only port. Keep task238 handed-off targets separate too. No process kill by name, no DB reset/reseed/drop, no direct business SQL, no mass migrations. Queries for identity/count/readback are read-only and sanitized.

Checked-in preview/selection/checkout defaults stay OFF; named nonproduction task runtime env may stay ON. TB185 paused/default-OFF/PendingAllocation unchanged. Original 225-R1 lost unsaved LOCAL work is not recovered.

## 2 — The exact existing sellable seed: execute it, do not invent another one

Root source review confirmed:
- Host/Development/MarketplaceDevelopmentBootstrap.cs never calls IWorkspaceDemoSeed; its seller Ensure therefore sees no Party organizations.
- Catalog.Application/Development/IWorkspaceDemoSeed.SeedNewProductAsync is the existing module-owned Development seam.
- Catalog.Infrastructure/Development/WorkspaceDemoSeed.SeedNewProductAsync creates the live workspace product and invokes WorkspaceDemoMarketplaceSeed.EnsureMarketplaceAsync; it also creates the same-seller companion product/offer.
- WorkspaceDemoMarketplaceSeed uses ONLY existing Party/Offer/Pricing/Inventory/Tax Development Contracts gateways to create REAL organizations, active offers, prices, tax classification/rule, locations and stock. This supplies two sellers plus Seller A companion: enough for Platform + Seller A + Seller B partitions without inventing a third seller.
- PartyOrganizationProfileDevelopmentSeed does NOT create an organization; it finds an existing one then fills its profile. Calling that alone cannot close this blocker.
- DevelopmentSchemaMigrator.ApplyAsync assigns store-alpha. DO NOT call it for Marketplace and accidentally cross into the SingleStore DB.

Add the smallest opt-in Development Marketplace commissioning orchestration in the existing Marketplace bootstrap, running INSIDE its current assigned Tenant=NULL marketplace CommerceContext. Reuse IWorkspaceDemoSeed through its existing Application/Development seam just as Host already does for SingleStore. Prefer extending current commissioning options or one narrowly named option; default false, actual Development + Marketplace guards inside the seam. No generic new seed framework, endpoint, new business policy or new auth mechanism.

On this verified target call the module-owned seed when the live fixture is absent. The existence check is for the exact live fixture, not Any(products): 566 Draft catalog products do NOT mean sellable fixtures exist. Do not repeat the 566-product reset.
Respect WorkspaceDemoProductSeed's documented create-only method: do not call SeedProductAsync unconditionally on repeat. If a partial prior attempt exists, repair narrowly INSIDE the module-owned seed via existing gateway identities (slug/SKU), not by Host knowing foreign tables. Ensure existing fixtures can reconcile missing offers/stock without duplicating or replenishing consumed stock on every restart; use the existing seed idempotency authority.
After real organizations exist, call ISellerDevContextStore.EnsureAsync in the SAME assigned scope to reconcile the TWO real Identity users with TWO real Party memberships/tuples. Do not treat MarketplaceSellerDevBootstrap's synthetic legacy published pair as real Seller A/B authority. Preserve the actual normal seller login sessions and test own/foreign scope using their real tokens.

Host remains composition: no new foreign EF/DbContext/Domain/business SQL imports or data arithmetic. Product/Offer/Inventory/Party persistence belongs to the owning module; use Contracts for foreign calls. No Shipping/Order quote/calendar/owner engine duplication. Existing CQRS endpoint -> request/handler -> Result transport retained; faults localized through existing error catalog.

Rebuild affected task Host once after the narrow change and restart ONLY verified task5190. Existing admin rebind must survive; seller admin-route denial remains. Record real organization/offer/stock ids, both sellers' ordinary own-business access and same-platform admin access. Minute20 checkpoint is these usable facts, not unit tests.

## 3 — Complete the actual Marketplace mixed-owner path

Use the three real seeded sellable offers: two Seller A lines may have DIFFERENT explicit responsibilities (Platform vs Seller), and Seller B owns the third. Assign fulfillment responsibility/type through existing authorized Offer API. Platform assignment is an admin decision; no seller may declare Platform. Reuse normal owner authoring APIs for Marketplace-owned Platform + Seller A + Seller B publications on ONE real service/option binding and active real type. Use clearly named task-only DEV drafts; independently identifiable labels, rate rows, fees and lawful dates/hours. Do not guess country/Party/type IDs or modify review230/user data. If Geography/catalog prerequisites are absent on this new DB, use the ALREADY EXISTING module-owned Development seam in this SAME target, not a cross-DB connection or SQL clone.

Use normal real buyer register/login/BFF session and cart APIs, real stock, real address/geography. Prove actual HTTP:
1. priced read -> three visible server groups with their OWN owner/full binding/capture/row/fees/dates;
2. choose own method and allowed date per group on existing surfaces; save200 with ALL groups, exact expected* echoes, exact goods text and root selection revision TEXT;
3. paymentpreview200 reflects same saved context with all three groups;
4. existing StartPayment -> durable Order + one reservation/conversion + existing manual Pending boundary. No fake hold, fake payment success, dev-actor substitution, mocked provider or intercepted /v1 response.
SingleStore existing completed path remains intact; do not rebuild/reseed it merely to repeat known evidence.

Authorization: seller Manage can manage own draft, view-only cannot write, foreign seller cannot read/write the other's private draft, and seller cannot administer Platform. Use normal existing permission/membership APIs or sanctioned existing module-owned DEV prelude to create a view-only control if needed. No wildcard or invented production role. Record actual denials with zero mutation.

## 4 — Coupon, stale-context and failure cases on the same real cart/owners

Create/activate real task-only coupon through actual Promotion API, applying to ONE seller/group while other groups remain lawful undiscounted. Record each group's own post-discount+tax goods, shipping and final totals from server snapshots, not JS arithmetic.
Exercise valid coupon read -> selection -> preview -> final Order. Invalid coupon must not be represented as accepted priced success; selection/final refuse checkout.coupon.not_applicable. Explicit clear or changed coupon invalidates prior confirmations; exactly one current-context read, typed recipient/address preserved, fresh confirmation before save.

Exercise one failed destination refresh then explicit retry with real task-owned browser network failure; no stale money/dates accepted after failure. Current response identity includes destination AND coupon. Fix a reproduced empty-key/in-flight blocker in the EXISTING coordinator only, if needed; no second fetch framework.

Mandatory checks: unavailable group cannot be silently dropped; missing echo/date not allowed; changed goods/calendar/row invalidates prior choice; stale root revision refuses without partial write. Reuse focused facts for already-covered negative matrix rather than replaying the entire suite, and record which negatives are live versus unit-only.

## 5 — Finish duplicate submit + original Order history + actual mobile payment

Use one NEW task cart and the existing native idempotency-key format. Real same-tick double-click must send at most one request from the UI. Also exercise two concurrent HTTP attempts with SAME key: one durable authorized checkout and no extra reservation/payment side effect; authorized replay returns ORIGINAL checkout. Do not certify server idempotency from only the client ref.
238 two calls failed SQLSTATE22001: cause is STILL UNCLASSIFIED, not proven inventory correlation overflow. If reproduced, retain actual PostgresException SqlState/SchemaName/TableName/ColumnName/ConstraintName, parameter TYPES/LENGTHS only (no secrets) and exact built source/binary identity. Fix only a source-confirmed owning defect; do not widen/truncate/swallow or blind schema edit. If the actual server failure remains, delivery stays blocked and must be reported explicitly.

After successful Order, change ONLY a task-owned DEV publication via normal API. Authorized existing Order GET must still return the original per-group accepted owner/capture/row/money/date/ticks, without mutable Shipping lookup/repricing; foreign buyer denied. Prefer existing historical Order surface; if an existing response omits the required compact facts, extend that existing Order read/DTO from Order storage only, narrow authorized mapping, no separate history subsystem or authored JSON copy.

Use existing storefront cards/payment page; no mockup/redesign. Fresh task-owned browser profile and task-created targets only. Capture ACTUAL shipping AND PAYMENT at requested 1440x900, 390x844 and 360x800; PNG width == measured innerWidth, clientWidth/scrollWidth/visualViewport recorded, no zoom workaround, clipping or overflow-hidden mask. Inspect native pixels including payment stepper. No reuse of shipping images as payment proof. Keep usable authenticated task URL/session for user review; do not automate it again after handoff.

## 6 — Focused verification and completion evidence

Only focused new seam/affected guards + necessary typecheck/build and actual live flow. Do not run full Order/Host/Shipping/solution sweeps or use broad test replay to consume trial time. Existing baseline bg-white / DateTimeOffset.UtcNow / packaging allow-list reds remain labeled, not silently repaired or counted green.
No new quote/discount/calendar authority; no full JSON per group/line, no historic repricing. 231/232/236 already APPLIED to task Marketplace and store-alpha; stale EF snapshot gap retained, do not regenerate baseline. New migrations only if a genuinely confirmed required storage defect cannot be fixed otherwise; STOP and identify it before altering schema outside this task's no-schema plan.

Result must lead with usable URL + exact completed stage and a compact acceptance ledger:
SingleStore preserved; Marketplace 3-owner Order; partial coupon/invalid/clear; own/foreign/view access; duplicate UI+HTTP; immutable Order read; desktop/mobile PAYMENT.
For EACH item give actual request/status/order ids + sanitized artifact path or explicit unmet blocker. No unit/model evidence mislabeled live; manual Pending is not PSP success. Report cumulative time against original four-hour delivery budget including prior53m and any intervening focus, no new clock. If PARTIAL, do not label it the end of delivery and do not automatically create another task number.

Preserve received TASK verbatim in docs/ai/tasks/TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240/TASK.md and evidence folder TASK.md. RESULT.md/RECEIPTS.md/screens under docs/architecture/evidence/TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240/. Update ALL THREE TOP recovery files with current true state and the cumulative deadline, preserve history. Normal exact-path staging/commit/push HEAD:main/fetch parity; no amend/force/reset/stash/rebase/history rewrite. Keep source isolated from user watched checkout.

## RESULT CONTRACT — fixed canonical fields, no diagnostic probe posting

The real result must have exactly one canonical BEGIN_TOOBA_WORKER_RESULT and one END_TOOBA_WORKER_RESULT. Fill actual FULL SHAs (or NONE where applicable), no editorial notes in SHA fields. Follow this template, then append the task-end marker as the final line of the transmitted result. Do not send placeholder/probe result bodies or overwrite an earlier result identity.

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240
Parent-Task: TB-SHIPPING-MARKETPLACE-ADMIN-BOOTSTRAP-AND-LIVE-FLOW-239-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — finish the existing connected delivery
Mode: REUSE_MODULE_OWNED_SELLABLE_SEED_AND_FINISH_REAL_MARKETPLACE_DELIVERY
Status: PASS or PARTIAL or BLOCKED or RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: actual full issue commit SHA
Base-SHA: actual full source start SHA
Code-SHA: actual full code SHA or NONE
Evidence-SHA: actual full evidence SHA or NONE
automaticNextImplementationTask: NONE

Actual completed outcome, usable URL and acceptance ledger; sanitized receipts; honest open defects;
cumulative deadline accounting; source/runtime parity; preservation and no inferred Visual ACCEPT.
END_TOOBA_WORKER_RESULT
```

END_TOOBA_TASK
