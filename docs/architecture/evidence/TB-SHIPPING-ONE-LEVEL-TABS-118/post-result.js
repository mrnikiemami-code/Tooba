const fs = require('fs');
const path = require('path');

const B = 'http://127.0.0.1:17321';
const ROW = '5e543785-a522-418f-8151-ae4a14084310';
const TASK = 'TB-SHIPPING-ONE-LEVEL-TABS-118';
const OUT = path.join('docs', 'architecture', 'evidence', TASK);

const result = `BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-118
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-117
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Bridge-Task-Row: ${ROW}
Status: PARTIAL
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
Expected-Base-SHA: 6f0105e4c573dd7550439974b24ea2fdddd0c9db
Base-SHA: 6f0105e4c573dd7550439974b24ea2fdddd0c9db
Base-SHA-Drift: none for source (git diff --stat 6f0105e..HEAD empty at claim). Non-overlapping bookkeeping drift only: 3 pre-existing untracked-modified evidence txts (TB-P0-INVENTORY-CONSUMPTION-JOURNAL-003/summary.md, TB-SHIPPING-FULL-OPERATIONS-075/host-stdout-075.txt, TB-SHIPPING-FULL-OPERATIONS-079/host-stdout-OFF.txt) plus 2 pre-existing frontend dirt files dated 2026-10-13 (admin-fulfillment-work-queue-screen.tsx, admin-order-detail-screen.tsx). Neither is a storefront shipping file and neither was adopted.
Code-SHA: NONE (no src/, test, frontend, migration, appsettings, feature-gate or DB edit)
Evidence-SHA: 0f2690076d291a9d7e6871bb329e218f32de7e84
HEAD==origin/main after push: true
Measured-Minutes: ~14 of the 45 min budget (investigation stopped well before minute 32)
Rollback: docs/evidence only; nothing to roll back.

IDENTIFIED STOREFRONT SHIPPING ROUTE AND OWNER
- Route: /shipping (GET) -> src/frontend/app/shipping/page.tsx:17-20 -> StorefrontShell + StorefrontShopeivaShipping (src/frontend/app/storefront/storefront-shipping.tsx:105).
- The one-level surface the customer sees is the 3-step hero at storefront-shipping.tsx:947-951 rendered at :965 (data-testid="shipping-steps"): step-1 سبد خرید (link /cart), step-2 ارسال (active), step-3 پرداخت (inactive). Option rows render at :608-660 (:632 data-testid="shipping-method-{methodCode}") with the label text {m.label}.
- Data flow: loadShippingProjection (storefront-shipping-api.ts:275-312, POST /v1/storefront/shipping/projection) -> StorefrontOrderEndpoints.cs:37 + :144-165 (gate at :152) -> CheckoutIdentityGate.cs:41-56 -> ProjectStorefrontShippingQuery.cs:33-91 -> StorefrontShippingService.ProjectAsync, with optional label-only overlay ApplyOneLevelDisplayNamesAsync (:434-461, :445) via IShippingOneLevelStorefrontLabelReader (FulfillmentModule.cs:111 -> ShippingOneLevelStorefrontLabelRead.cs:41-98 -> ShippingOneLevelCatalogPolicy at ShippingOneLevelCatalogContracts.cs:73-81).
- Selection path: onSelectMethod (:317-325, client state + read-only re-projection) -> persistAndContinue (:327-358) -> saveShippingSelection (storefront-shipping-api.ts:315-352, PUT /v1/storefront/shipping/selection, Order-owned draft store) -> only then router.push('/payment?cartId=…') at :350 -> /payment renders StorefrontPaymentHandoff (payment/page.tsx:17-23).

THREE-QUESTION VERDICTS
(a) Expected label/option mapping displayed? NOT OBSERVABLE LIVE (identity-gated) AND NOT ENABLED under current shared config.
    PROVEN BY DIRECT READ-ONLY HTTP OBSERVATION (shared :3000/:5088):
    - GET http://127.0.0.1:3000/fa/shipping -> 200, 110620 bytes SSR HTML. PRESENT: data-testid="shipping-page", "shipping-steps", "shipping-step-1", "shipping-step-2", "shipping-step-3", hero «اطلاعات ارسال». ABSENT: "shipping-methods", "shipping-method-", "shipping-delivery", "shipping-summary", "shipping-continue"; the page renders the no-projection fallback «در حال آماده‌سازی ارسال…» + «بازگشت به سبد».
    - nuance recorded: «روش ارسال» appears ONLY inside the hero subtitle «آدرس و روش ارسال خود را انتخاب کنید», NOT as the methods heading — no over-claim.
    - GET http://127.0.0.1:3000/shipping -> 308 -> http://localhost:3000/fa/shipping.
    - Exact reason: POST /v1/storefront/shipping/projection with no identity -> 401 {"errorCode":"checkout.authentication_required"}.
    SOURCE-ONLY: labels come from projection.methods[].label; the one-level catalog label overlay is fail-closed OFF because Tooba:ShippingOneLevelCatalog is absent from every appsettings (0 matches in appsettings.json + appsettings.Development.json) => ShippingOneLevelStorefrontLabelRead returns Empty at :53-56 without any DB read, so the storefront currently shows LEGACY labels.
(b) Is a shipping option selected/retained only through its proper owner and not as a side effect of navigating toward Payment? YES — PROVEN.
    FOCUSED FRONTEND TEST RUN ONCE: node --experimental-strip-types --test app/storefront/storefront-shipping-api.test.ts => tests 8, pass 8, fail 0 (119.8 ms). The decisive case "shipping continue persists the draft, keeps the cart active and never commits" asserts saveShippingSelection occurs BEFORE router.push('/payment?cartId=…') and that the continue path has no shipping/commit, no checkoutId and no cart clearing.
    Source corroboration: the legacy early-commit helper commitShippingToPayment (storefront-shipping-api.ts:383-418, POST /v1/storefront/shipping/commit) has NO caller anywhere in the frontend (grep finds it only in its own definition and in negative-assertion tests). Method selection alone (onSelectMethod) only sets client state and re-reads the projection; nothing is persisted until the explicit «ادامه به پرداخت» press.
(c) Exact gate/endpoint/state preventing a real non-mutating acceptance proof? = checkout.authentication_required.
    - POST /v1/storefront/shipping/projection (no identity) -> 401 checkout.authentication_required; thrown by CheckoutIdentityGate.EnsureCheckoutActorAsync (CheckoutIdentityGate.cs:41-56) from the Catalog policy ICatalogCheckoutIdentityPolicyLookup (missing row => AuthenticatedOnly), invoked at StorefrontOrderEndpoints.cs:152.
    - Route-mapping proof WITHOUT invoking write paths: GET /v1/storefront/shipping/projection -> 405, GET .../selection -> 405, GET .../commit -> 405.
    - Secondary gates observed: GET /v1/admin/shipping-services/shipping-methods/one-level -> 401 admin.actor.missing (so the one-level label source is not anonymously readable either); GET .../one-level/post/activation -> 405.
    - The one-level label overlay is additionally gated by the absent Tooba:ShippingOneLevelCatalog flag (fail-closed OFF; no toggle performed).

PROPOSED NEXT MINIMAL PROOF (separately authorizable, NOT started)
ONE isolated Development Host launch reusing the TB-117 guarded pattern, with a GuestAllowed checkout identity policy seeded on the scratch DB, plus ONE read-only render of /fa/shipping against that isolated Host with a live scratch cart — yielding the real option/label rows and real selection retention without touching shared tooba_alpha, without creating orders and without enabling any shipping flag. If the Architect wants the one-level catalog labels specifically, the same isolated launch must additionally set Tooba:ShippingOneLevelCatalog:Enabled=true, which is a flag toggle explicitly out of scope here.

SHARED PARITY (READ-ONLY; no writes)
- Preflight vs postflight identical: :5088 PID 35624 /health 200 {"status":"ok"}; :3000 PID 31580 -> 308 /fa; /__platform-commerce unchanged (SingleStore / dev-singlestore / store-alpha / tenant-alpha / 127.0.0.1 / theme-alpha / IR).
- tooba_alpha counts byte-identical to the TB-117 baseline: methods 7, translations 14, carts 50, cart_lines 78, drafts 14, seller_orders 26, products 567, offers 3223.
- Zero mutations performed: no cart/order created, no shipping option selected, no PUT issued, no feature gate toggled, no shared DB write, no identity synthesized. The only POST was identity-less and was rejected by the gate before any handler ran (the gate is a pure read of the Catalog policy).

CHANGED PATHS (docs/evidence only)
- docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-118.md
- docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-118/** (11 files incl. task.md and task-receipt.json)
- Bridge receipt docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-118.task.md preserved UNSTAGED.
Zero src/, test, frontend, migration, appsettings, feature-gate or DB edits. Unrelated dirty/untracked files preserved.

DEVIATIONS: none. One bounded verification attempt per seam; the single focused test was run exactly once; no rerun; no broad test sweep; no src inspection beyond the single representative path (plus the gate constants needed to answer question (c)).

NON-CLAIMS: no storefront label certification, no cart reachability, no checkout cutover, no option parity claim, no commercial shipping readiness. A 200 /health and populated scratch templates are not Shipping acceptance. Row presence is not storefront eligibility.

FIRST REMAINING BLOCKER: the live option/label mapping of the storefront one-level shipping surface is unprovable on the shared environment because the projection endpoint requires a customer identity (401 checkout.authentication_required) and the one-level label overlay is fail-closed OFF (Tooba:ShippingOneLevelCatalog absent) — resolved by the single isolated proof proposed above.
STOP FOR ARCHITECT.
END_TOOBA_WORKER_RESULT
`;

fs.writeFileSync(path.join(OUT, 'RESULT.bridge.txt'), result, 'utf8');

(async () => {
    const res = await fetch(`${B}/api/results`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            channelId: 'tooba-main',
            workerId: 'tooba-worker-01',
            agentType: 'cursor',
            taskId: TASK,
            status: 'PARTIAL',
            content: result,
        }),
    });
    console.log('RESULT_POST=' + res.status + ' ' + (await res.text()).slice(0, 300));

    const done = await fetch(`${B}/api/tasks/${ROW}/complete`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ workerId: 'tooba-worker-01', channelId: 'tooba-main' }),
    });
    console.log('TASK_COMPLETE=' + done.status + ' ' + (await done.text()).slice(0, 300));

    const hb = await fetch(`${B}/api/workers/heartbeat`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ workerId: 'tooba-worker-01', channelId: 'tooba-main', agentType: 'cursor', status: 'Idle' }),
    });
    console.log('IDLE=' + hb.status);
})().catch((err) => { console.error(err); process.exit(1); });
