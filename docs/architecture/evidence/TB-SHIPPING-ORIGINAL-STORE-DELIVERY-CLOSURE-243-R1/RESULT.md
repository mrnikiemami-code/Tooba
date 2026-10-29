PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ORIGINAL-STORE-DELIVERY-CLOSURE-243-R1
Parent-Task: TB-SHIPPING-ORIGINAL-STORE-COMPLETE-HANDOFF-243
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — close actual 243 regressions
Mode: CLOSE_WEIGHT_AUTHORITY_BINDING_WORKAROUND_AND_REPORTED_LIVE_FAILURES
Bridge-Task-Row: 9d9b7460-74b1-461a-b182-395c4162250f
Status: PASS
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 5080b833e2aa6990e4fe75fd82340bd1a1cc1a5d
Base-SHA: 5080b833e2aa6990e4fe75fd82340bd1a1cc1a5d
Code-SHA: 0e2ec5282f88d33a517cfd733a53971f96f71c24
Evidence-SHA: 0e2ec5282f88d33a517cfd733a53971f96f71c24
automaticNextImplementationTask: NONE

PRIMARY http://127.0.0.1:3000/fa/shipping → Host 127.0.0.1:5088
FE PID46476 (D:\temp\tb243-fe, source 7ea79556f unchanged this slice); Host PID35260 (D:\temp\tb243-primary from tb243r1-build\out, DeploymentId=store-alpha-tb243-primary, DLL SHA256 D09E6E4D…).
Edition=SingleStore Tenant=store-alpha DB=tooba_alpha. Marketplace primary NOT used. CDP9350/9351 frozen.

A — WEIGHT AUTHORITY
CatalogShippingWeightGrams (Domain) + CatalogDirectory rewrite: only active Number defs with Code `weight`/`*_weight`; unit g/kg conversion only; per-definition variant override; PRESENT invalid override never heals from product; conflicting effective defs → unknown; canonical parse without NumberStyles.Number thousands; checked kg→g and Order group sum overflow → unknown.
Semantic for laptop: demo_attr_laptop_weight id 01a05387-d58c… unit kg IsActive; stored canonical 1.4 → 1400g. food_volume (g) excluded.
Focused CatalogShippingWeightGramsTests 9/9 + planner coexistence/null-probe facts; StorefrontCapturedPreviewPlannerTests green (25 with weight).
Live: projection freight 120000 on 1000–2000g rate with goods 2624545 proves priced tier after reader fix.

B — BINDING / post:standard
Restored post:standard via PUT …/one-level/post:standard/activation {isActive:true} → method+option ACTIVE (express stays inactive).
ResolveCatalogBindings: always probe (service,null) in addition to active options so option rows cannot shadow null-option Selected captures; 219 remains authority (no synthetic priced method).
Live: post/NULL remains the priced laptop method (optionCode null) while post:standard is ACTIVE in one-level+options; no deactivation workaround.

C — StartPayment 500 CAUSE + FIX
Exact first escaping cause: ambient TransactionScope submit → CheckoutAbuseGate.AcquireCustomerLockAsync INSERT raced on pk_checkout_abuse_customer_locks (23505) → PG txn aborted → follow-up query 25P02 → unhandled 500.
Fix: ExecuteSqlInterpolated INSERT … ON CONFLICT (customer_id) DO UPDATE SET touched_at (no 23505 / no doomed txn).
Live concurrent same-key StartPayment: one 200 checkout 01a185a2-2e5a… payment 01a185a2-2ffc…; loser typed 409 cart.version.conflict (not 500); same-key replay 200 identical ids checkoutReplayed=true. Zero duplicate payment/attempt from this pair.

D — Duration 120
Same draft 01a16eeb weekly PUT+publish with current destination/cart: 60→8 slots 09–10…16–17; 120→4 slots 09–11/11–13/13–15/15–17; restore 60→8. Fresh projections each time (no stale empty). Original Order 01a1858c-1dfe… GET 200 still 2026-11-02 post. Final leave at 60 published.

E — NEW proof Order + review
Task buyer selected non-first 14:00–15:00 ticks 504e9–540e9 on post/NULL → selection200 → payment-draft payable 2749545 → StartPayment as above.
Review cart 01a1858d-06ee… still Active v4; /v1/auth/me 200. Profiles 9350/9351 untouched.

Focused commands: `dotnet test --filter CatalogShippingWeightGramsTests|StorefrontCapturedPreviewPlannerTests` → 25/25. Host Release publish → primary swap. No FE source change. No screenshots. No new ports/DB. TB185 paused/defaultOFF. Checked-in gates OFF; local StorefrontCapturedPreview ON.
Original deadline MISSED. 243 worker PASS was NOT Architect-accepted (PARTIAL) — corrected by this R1.

END_TOOBA_WORKER_RESULT
