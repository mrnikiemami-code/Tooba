# TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R3 — WORKER RESULT

Status: **PASS** within scope. Stop-State: STOP_FOR_ARCHITECT. automaticNextImplementationTask: NONE.  
Worker ≠ Architect / Visual ACCEPT.

## Defect

Choosing province after delivery day/hour caused a double projection and unconditional choice clear: `onProvinceChange` called `refreshProjection` while the destination coordinator issued another; routing treated destination-key change as hard clear-all; pending emptied cards so the UI looked like a full reload.

## Repair

- Province handler updates address state only; destination-refresh coordinator is the sole owner of province/city projection.
- Hard invalidation is coupon or cart-version only (`isHardInvalidatingRoutingChange`). Destination revalidation keeps provisional choices through pending/failed/empty intermediate previews, then fingerprint-reconciles on the authoritative response (per-group clear when snapshot/rate/goods/date/slot diverge).
- Pending/failed destination keeps last non-empty cards visible, locks interaction, and does not present stale money as confirmed; Continue/selection intent remain blocked until confirmed identity.
- Saved-address / new-address destination refreshes defer to the coordinator when captured preview is active.

## Proof

Focused: captured-selection + destination-refresh + continue-state + destination-choice.guard → **47/47**; `npm run typecheck` clean; production build exit 0.

Canonical FE3000→Host5088 SingleStore/store-alpha/tooba_alpha, buyer `01a0996c-b8d9-7000-9854-80b3f59e8d7c`, cart `01a17b2d-e219…` **Active v48 qty18**:

- document path stayed `/fa/shipping`; `beforeunload` 0; shipping-groups root remained mounted;
- province identity → **1** projection; city identity → **1** more (total 2, no duplicate);
- after province, group A kept `1405/08/11 12:00–13:00`; group B cleared by authoritative fingerprint mismatch (lawful per-group clear, not blanket wipe);
- Continue stayed disabled while unconfirmed.

No checkout/Order/payment. Navigation timeout banner / native-click path untouched.

## SHAs

Expected-Base / Base: `a3e64896afb51432954b4253abd6453ef0902c7e`  
Code-SHA: `753f7db884ee51c1b1b3e37866abdaea007895c7`
Evidence-SHA: `35d960d213aec2a9fe795de54f8a58fff43c2649`
