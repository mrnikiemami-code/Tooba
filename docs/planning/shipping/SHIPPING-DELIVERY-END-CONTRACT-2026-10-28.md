# CURRENT — 240 EXECUTED PARTIAL: real Marketplace sellable fixture + real mixed-owner read; reference data missing —2026-10-28

Read docs/architecture/evidence/TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240/{TASK.md,RESULT.md,RECEIPTS.md}.
Status PARTIAL, NOT accepted, no userVisualACCEPT, no activation, no PSP/paid claim; base/issue dc987ce4e.
LANDED: opt-in Host seam Tooba:Development:MarketplaceSellableSeed (default false) running the EXISTING Catalog-owned
IWorkspaceDemoSeed inside the assigned Tenant=NULL Marketplace scope, guarded by exact-live-fixture existence
(never Any(products), never a reseed, no stock refill); 15/15 focused Host facts, build 0 errors, no business data in Host.
CHECKPOINT PASSED (minute 8, real data on tooba_marketplace_tb239): parties=2, offers=3 Active (ARM-LN-01, ARM-SC-01
same Seller A, DGS-LN-01 Seller B), prices=3, tax 1+1+3, stock=4, locations=3; seller A own offers 200 / foreign 403,
seller B own 200; identical counts after 2 real restarts. NEW real mixed-owner read: normal buyer, real cart with the 3
real lines (v7, IRR, 5,090,000, sellerCount 2), projection 200 with THREE per-line captured groups on the REAL seller ids.
Admin shipping authoring authorized after access-control bootstrap (holds fulfillment.view+manage; GET drafts 200; POST
drafts reaches 400 shipping.service_code.unknown, not 403).
BLOCKERS (measured, source-confirmed): geography countries/areas/cities = 0 and Geography exposes ONLY MapGet with no
Development seed/importer in the repo ⇒ every group is storefront.captured_preview.destination_required even with legacy
province/city text; localization.languages = 0 ⇒ no canonical method can be created (name/language required) so no owner
publication exists; shipping catalog 0/0/0; a fresh owner needs a SECOND POST /v1/admin/access-control/bootstrap before
fulfillment.manage tuples are honored (first 204→403, second 204→400).
NOT delivered: 3-owner publications on one binding, per-group method/date, selection save, payment preview,
StartPayment→Order/manual Pending, coupon partial/invalid/clear, duplicate UI+HTTP, immutable Order read, 1440/390/360
PAYMENT captures. Cumulative ≈123 min of the 240-min envelope (53 + ≈70); ≈117 remain and the residual is multi-hour, so no
fresh clock was started. Preservation: checked-in gates OFF, no schema/migration/baseline edit, 231/232/236 applied, stale EF
snapshot gap retained, USER review window / FE3000 / Host5088 / 238 handoff 3100+5188 untouched, 230 publication + user
draft/rates/week/pointer/cart untouched, no reset/reseed/direct business SQL/cross-DB copy. TB185 paused/defaultOFF/
PendingAllocation; 238 SQLSTATE22001 still UNCLASSIFIED. Next decision (no automatic task): compose the EXISTING Localization
language seed and add ONE Geography-owned Development seed (same guards) or provision that dataset out-of-band, then the
remaining flow becomes executable. STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE.

# SUPERSEDED — 240 ISSUED: FINISH CONNECTED MARKETPLACE DELIVERY; CUMULATIVE FOUR-HOUR END CONTRACT — 2026-10-28

239-R1 DEV admin commissioning ACCEPTED WITHIN SCOPE after source + committed receipt review; full delivery NOT accepted.
Reviewed source 3c726019449f1ccad0dadc93e783292ac8de276f. Root did not execute worker tests/runtime.
Active task TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240; read docs/ai/tasks/TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240/TASK.md and 239-R1/ARCHITECT-REVIEW.md.
Expected base = issue commit containing task. STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE.

BINDING USER DEADLINE: the user treats 3–4 focused working hours as END OF COMPLETE AGREED DELIVERY, not partial code.
Count 239-R1 reported ~53 minutes against the SAME four-hour upper planning estimate (remaining ~187 if no other focus).
Reconcile actual cumulative timestamps; never reset a fresh four-hour clock. Task240 plans150 minutes, checkpoint20
real seller organizations + buyable offers + stock + normal seller access. Report slip risk at checkpoint, not after another long detour.
Required final scope: SingleStore + Marketplace Platform/real Sellers, partial/invalid/clear coupon, real Order/manual Pending,
existing UI desktop/mobile PAYMENT, duplicate UI/HTTP and immutable original authorized Order read. No PSP/paid claim.

SOURCE-VERIFIED FIX ROUTE: Marketplace bootstrap lacks IWorkspaceDemoSeed. Existing Catalog-owned SeedNewProductAsync
creates live product, two sellers, offers/prices/tax/stock + Seller A companion through existing Contracts. Call in SAME
Marketplace Tenant=NULL scope, explicit Development opt-in. Do NOT call store-alpha DevelopmentSchemaMigrator wholesale,
repeat bulk566 Draft catalog reset, or call profile-enrichment alone expecting organizations. Seller Ensure AFTER real parties.
Host orchestration only; owning modules keep data logic, no foreign EF/business SQL/new quote/calendar/auth engine.

Task target remains Marketplace5190 / DB tooba_marketplace_tb239 / TenantNULL, knownPID22832 verify. Real admin
01a1818b-5257-7000-bada-84233a2b21fb bound via default-OFF opt-in, normal admin200/seller403; secrets private task disk.
Use isolated checkout/FE3102, local named gates ON allowed, checked-in OFF. 231/232/236 already APPLIED on task Marketplace
and store-alpha, no reset/rebaseline/history edit. Stale EF snapshot remains disclosed.
USER watched3000/5088 and handoff3100/5188 + D:\temp\tb238-handoff-chrome and unsaved tabs/source NEVER enumerate/read/
automate/reload/close/edit/HMR. Preserve user original draft + review230 publication/rates/pointer/cart. Preserve238 handed-off targets.
238 SQLSTATE22001 remains UNCLASSIFIED, no proven correlation overflow/server simultaneous-idempotency success; capture actual provider
fields and parameter types/lengths on reproduction, no blind widening/truncation. Real payment mobile nativecaptures still required.
227 mobile empty-note residual and unrecovered225-R1 LOCAL edits remain recorded. TB185 paused/default-OFF/PendingAllocation.

---
