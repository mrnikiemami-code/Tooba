PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R1
Parent-Task: TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — complete the same connected delivery
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
Mode: COMMISSION_MISSING_REFERENCE_DATA_THEN_FINISH_MIXED_OWNER_BUYER_FLOW
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## 0 — This is continuation of the SAME delivery, not a new deadline

The user's END contract is still the original 3–4 focused hours for SingleStore and Marketplace/real sellers, coupon, Order/manual Pending, desktop/mobile and duplicate/history. Reported elapsed is 239-R1 ~53 + 240 ~70 = ~123 minutes of the same 240-minute ceiling. Remaining ~117 minutes, less any intervening focused work. DO NOT reset the clock. This continuation plans105 minutes including final evidence, leaving at most12 contingency; reconcile actual timing at receipt. Delivery is at risk; do not claim completion from source, seed, tests or unavailable read.

Checkpoint15: canonical languages, real Geography identities and active shipping type/binding exist through real task APIs.
Checkpoint40: THREE assigned owner partitions are priced, not unassigned/destination_required.
Checkpoint75: real mixed-owner Order/manual Pending with partial-applicability coupon.
Remaining time: duplicate/history, existing UI native payment captures and handoff. At a missed checkpoint, write exact blocker, remaining estimate and cumulative time immediately in progress/recovery; continue the authorized narrow fixes, never silently overrun then call it PASS.

Read and retain the original240 TASK sections3–6 for required owner/coupon/duplicate/history/UI outcome. This task explicitly supplies the missing reference-data authority. STOP_FOR_ARCHITECT after execution, no automatic241 or separate helper-only finish.

## 1 — Source base and real target already commissioned

Review source 6ece5f78415b03f5458ddb33b45dc3656b87bcca and 240 ARCHITECT-REVIEW/RESULT/RECEIPTS. Start from this issue commit by safe fast-forward in isolated task checkout. No forced history changes.
Reuse Marketplace Host5190 / Development / DeploymentId tb239-marketplace / Tenant=NULL / DB tooba_marketplace_tb239. KnownPID23444, verify listener/command line before task-only restart. Existing real admin, two real sellers, three active offers, prices/tax/stock and buyer/cart are usable:
Seller A 01a181b0-5c87-7000-976e-ae5ce19864f6; Seller B 01a181b0-5c9a-7000-bf9b-8c8e1c47da72.
ARM-LN-01 01a181b0-5caf-7000-9c44-e82f2457a122; ARM-SC-01 01a181b0-5e93-7000-bcc8-85f8532bd345; DGS-LN-01 01a181b0-5cd7-7000-8ba1-a45b0df5425f.
Cart 01a181b8-2640-7000-a27e-a9c926ca1cf1 v7 as last observed; re-read actual version normally.
Admin account 01a1818b-5257-7000-bada-84233a2b21fb has normal login/platform access; private credentials stay on task disk, never git/results/screens. AccessControl existing bootstrap eventually granted fulfillment.view/manage. Do not interpret its first-call403/second-call400 as proven idempotence.

The previous 200 projection has ONLY unassigned, destination_required groups with no methods. It is NOT a configured mixed-owner proof. No fulfillment assignment/publication exists yet; create those normally below.
No more full catalog reset, no recreateDB or seller/offer seeds, no whole-module migration pass. Already-applied231/232/236 stay applied.

## 2 — AUTHORIZE missing reference data once in existing Marketplace Development scope

This is explicit authorization to ADD ONE Geography-owned Development seed and compose the EXISTING Localization seed. Do not stop again merely because Geography has no write endpoint. No public write endpoint or generic importer is needed.

A. Localization: call existing
Tooba.Localization.Infrastructure.Bootstrap.LanguageBootstrapDevelopmentSeed.PostMigration.EnsureAsync(provider, ct)
inside the already assigned Marketplace CommerceContext, after schema availability and before authoring reference data.
Source-confirmed: it delegates to LanguageBootstrapRunner/ILanguageDirectory.BootstrapAsync and creates fa-IR/en-US only when appropriate. Do not call the startup hosted service without a bound context, instantiate foreign DbContexts in Host, invent language GUIDs, or assign store-alpha.

B. Geography: add a narrow Development seed under
src/backend/Modules/Geography/Tooba.Geography.Infrastructure/Development/
with composition registration only if needed in Geography.Infrastructure/DependencyInjection/GeographyModule.cs. Keep persistence inside Geography.Infrastructure using its OWN GeographyDbContext + existing Domain factories:
Country.Create("IR", now), AdministrativeArea.Create(countryId, stable local code, null, now), City.Create(countryId, areaId, stable local code, now).
Create just Iran / Tehran province / Tehran city and FA/EN name translations using REAL registered Localization language ids through existing Localization Contracts, or a typed validated input of those ids from canonical language lookup. Do not reference Localization.Domain/Application/Infrastructure from Geography.
No claim that a local province/city code is an official ISO/subdivision code; use clearly documented stable seed codes. Names are ایران/Iran, تهران/Tehran. Service areas remain honestly empty and unsupported by final acceptance; do not invent a fourth-level area to mask that.
Use existing UuidV7 factories to generate new actual DB ids and reuse persisted natural keys. No hardcoded business/foreign GUID, SQL insert, JSON clone or store-alpha copy.
Idempotent create-missing behavior by unique country and country-scoped area/city codes, translation key(place, language); repeat preserves all ids/counts and current operator state. Never reactivate/rename/reparent or overwrite existing non-seed data silently. Ambiguous or contradictory existing parent shape refuses explicitly rather than creating a second apparent Tehran. Do not delete/reseed any dataset.

C. Guards/composition: explicit default-OFF Development reference-data opt-in, e.g. Tooba:Development:MarketplaceReferenceData:Enabled. Actual IsDevelopment + actual Marketplace + current Tenant=NULL assigned target; no background global seeding. Guard before resolving mutable seed dependencies. Host only orchestrates Localization then Geography, using existing scoped CommerceContext. Geography owns its Domain/EF writes and no foreign DB access. Host may call a narrow public module-owned Development entry, consistent with existing module seed convention; do not add business logic/EF to Host.
No new schema/migration/ModelSnapshot or production access-policy change. Rebuild only necessary task Host once, restart only verified5190. Local opt-in ON, checked-in configuration OFF. Existing admin binding and consumed stock survive.

Focused proof: disabled/Production/SingleStore no new reference writes; canonical language ids/foreign keys and country-area-city ancestry valid; repeat no duplicate and ids unchanged. Prefer live readback of real lookups plus narrow new guard facts over a new test framework or broad replay. Ensure real API returns country/province/city options and final strict Geography resolver matches those real identities.

## 3 — Fill Fulfillment catalog by NORMAL existing authorized APIs, then owner publications

fulfillment shipping_services/methods/types are empty: this is authorized task-only business setup, not a missing-price bug. Use existing canonical admin type/service/one-level method routes now that real FA/EN languages exist. Read DTO/route once, create ONE real active type and ONE service/option binding, correct localized names and metadata required by those handlers. Manual+Prepaid, IRR, no fabricated carrier provider/funder. No direct Fulfillment SQL, seed-engine clone or bypassed validation.
Reuse existing admin AccessControl bootstrap. If first-call incompleteness blocks freshly created scopes, make only a source-confirmed minimal fix in the owning existing bootstrap ordering/idempotence, prove first-call and repeat; do not add a second grant list or tell the buyer to call twice.

Assign ARM-LN-01 Platform, ARM-SC-01 Seller A, DGS-LN-01 Seller B through existing admin fulfillment assignment API, all on the real active type.
Create/select THREE task-owned shipping captures through normal owner-authorized authoring APIs on that SAME binding:
Marketplace Platform (owner has no tenant/seller key), Seller A and Seller B actual business ids. Distinct labels/fees/rows/dates/hours, consistent single destination coverage and lawful common currency. Use unconditional DEV rate rows so no fabricated weight/count is needed; do not alter original user conditions. This is new task DEV setup, not changing review230.
Use current real Geography ids from API for destination and authored coverage. Owner method/week/price/calendar policy stays in existing Shipping authorities. No no-latest fallback, no inherited Store owner, no label-based identity, no foreign EF joins.

Own seller Manage allowed; foreign seller denied; view-only seller write denied using existing sanctioned permission/prelude and no new production role. Platform assignment/authoring remains admin only. Keep receipts with actual principal/business scope and unchanged state on denial.

## 4 — Finish the existing real buyer flow; API first, then existing UI

Normal session, real cart, actual inventory/pricing/promotion/tax, no mocks/interception/fake hold/dev-actor substitute.
Read verified destination -> actual THREE priced owner groups -> per-group own method/date -> all-group selection200 (exact expected* echoes + exact goods text + root revision TEXT) -> paymentpreview200 -> existing StartPayment -> durable Order + exactly one reservation/conversion -> existing manual Pending. Shared binding must not merge owners, rows, dates/hours or fees. No payment-provider success/paid claim.

Create real task coupon eligible for ONE group/seller only; other groups remain lawful undiscounted. Run valid coupon across read/save/preview/final. Invalid coupon refuses checkout.coupon.not_applicable, no priced accepted claim; explicit clear/change invalidates confirmation and requires fresh current-context read. Existing Promotion is sole discount authority; never implement Order matching/discount arithmetic.
Exercise failed destination refresh + explicit retry on task-owned browser; no old quote current or selectable. Fix reproduced coordinator defects only in the existing path.
Unavailable group / missing echo / wrong method date / stale root token must refuse without partial write. Reuse narrow existing tests for the rest of the already-covered matrix; report actual live vs controlled evidence precisely.

Actual HTTP attempts need not wait on difficult React automation: use normal authorized API to close data/order stages, then real existing UI smoke. UI must independently show usable choices/save/payment on its task cart, not a mocked rendered payload or data-injected DOM. Fresh task-only browser target/profile; keep reusable task scripts, do not build another automation framework.

## 5 — Required last acceptance items within the SAME remaining time

Duplicate: same-tick UI double click sends at most one request; two concurrent normal HTTP StartPayment attempts with SAME valid native key must yield one authorized durable checkout and no duplicated reservation/payment side effects, plus original authorized replay. If 238 SQLSTATE22001 recurs, capture PostgresException Schema/Table/Column/Constraint/SQLSTATE and parameter TYPES/LENGTHS only; fix the proven owning seam, no blind correlation-field widening/truncation/swallow. That error remains UNCLASSIFIED until measured, and client in-flight ref alone cannot certify server idempotency.

History: change ONLY one task-owned DEV publication after Order. Existing authorized Order GET must retain ORIGINAL own group capture/owner/row/money/date/ticks from Order storage with no mutable Shipping reprice; foreign buyer denied. If current authorized Order response omits necessary existing evidence, extend its existing Order DTO/mapping narrowly, no new history engine, no authored JSON copy.

UI: existing template/cards/payment, fresh task FE3102 on task5190. Native shipping AND PAYMENT captures1440x900/390x844/360x800. Record requested width/innerWidth/clientWidth/scrollWidth/visualViewport and actual PNG size; inspect pixels including payment stepper. No zoom/gutter excuse, horizontal clipping mask or reuse of shipping PNG as payment proof. Preserve SingleStore accepted runtime unchanged. Hand off task's usable authenticated URL/window; once handed off do not automate it again.

If any required item remains unmet at the cumulative deadline, report exact unmet outcome and time honestly. Do not relabel PARTIAL as full delivery, invent Visual ACCEPT or claim a new four-hour allowance.

## 6 — Architecture, verification, preservation, Git and result

Allowed edits: existing Host composition/options; narrow Geography-owned Development seed/registration; existing Localization entry reused; existing module-owned exact blockers encountered on real flow. Contracts/CQRS boundaries remain. No Host business/data persistence, foreign DbContext/Domain/Application import, new owner/pricing/discount/calendar authority, public seed/auth bypass, service locator, endpoint mock, schema baseline regeneration, full snapshot JSON per Order or historical repricing.
Run only narrow affected tests + necessary builds/typecheck. No full Order/Host/Shipping/solution sweeps or baseline-guard repair loop.

NEVER enumerate/read/automate/reload/close USER original3000/5088, handoff3100/5188 and D:\temp\tb238-handoff-chrome or edit their watched checkout/HMR. Preserve original user draft/rates/week/pointer/assignments and review230 DEV capture/cart. Preserve238 handed-off runtimes. Verify task process command line before restart, no kill-by-name.
No DB drop/reset/reseed, crossDBcopy, business SQL or shared migration rewrite. Task target normal module-owned DEV reference writes and normal business APIs are expressly authorized above. Checked-in shipping/seed gates OFF, named local task env ON only. TB185 paused/default-OFF/PendingAllocation. Secrets only task-private disk.
Known retained residuals:227 empty-note14px;225-R1 unrecovered unsaved LOCAL edit loss. No fake recovery.

Save received task verbatim under docs/ai/tasks/TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R1/TASK.md and evidence TASK.md. RESULT/RECEIPTS/screens in docs/architecture/evidence/TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R1/. Update all THREE TOP recovery documents with precise state + cumulative original240-minute contract, preserve history. Normal exact-path staging, nonforce push HEAD:main, fetch parity; no amend/force/reset/stash/rebase, no broad CRLF churn.
Result leads with usable URL and actual stage; ledger for three-owner Order, partial/invalid/clear coupon, own/foreign/view access, UI+HTTP duplicate, original Order history, actual mobile PAYMENT. Each has actual status/ids/artifact or exact blocker, no test result inflated into live proof.
STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE.

## RESULT CONTRACT — canonical envelope, NO diagnostic probe result

Use exactly one BEGIN_TOOBA_WORKER_RESULT / END_TOOBA_WORKER_RESULT and actual FULL SHA values (or NONE where applicable), no commentary inside SHA fields. Append the task-end marker as the final line of the transmitted result after this template. No placeholder/probe post and no overwritten result identity.

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R1
Parent-Task: TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — complete the same connected delivery
Mode: COMMISSION_MISSING_REFERENCE_DATA_THEN_FINISH_MIXED_OWNER_BUYER_FLOW
Status: PASS or PARTIAL or BLOCKED or RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: actual full issue commit SHA
Base-SHA: actual full source start SHA
Code-SHA: actual full code SHA or NONE
Evidence-SHA: actual full evidence SHA or NONE
automaticNextImplementationTask: NONE

Actual outcome, usable URL and per-item acceptance ledger; sanitized receipts; cumulative consumed/remaining
minutes of original240-minute ceiling; honest blockers; source/runtime parity; preserved user work.
END_TOOBA_WORKER_RESULT
```

END_TOOBA_TASK
