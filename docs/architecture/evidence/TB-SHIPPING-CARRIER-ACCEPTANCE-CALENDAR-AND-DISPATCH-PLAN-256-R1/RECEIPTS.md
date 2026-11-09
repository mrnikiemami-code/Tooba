# RECEIPTS — TB-SHIPPING-CARRIER-ACCEPTANCE-CALENDAR-AND-DISPATCH-PLAN-256-R1

Point-in-time observations of ONE local run on the canonical environment. No disposable database, no Docker
PostgreSQL, no alternate port, no user-draft write.

## 0. Source

- Base when claimed: `162a319ad3d0ded796e5a474d1b269c26fa81be2`
  (`docs(shipping): record user feedback and active 256-R1 handoff`); the issued expected base `639947fc5` is an
  ancestor and both `8a1d64189` (task issue) and `162a319ad` (handoff) are legitimate descendants.
- Branch: `main`. Code + evidence commit recorded in section 7 after commit; no force/amend/reset/stash/rebase.

## 1. Changes

Backend
- `Modules/Shipping/Tooba.Shipping.Contracts/Scheduling/ShippingDispatchSchedulingAuthority.cs` — latest handoff
  accepted when conservative arrival ≤ chosen receiving date; bounded cycle; instant-based same-day slots.
- `Modules/Order/Tooba.Order.Application/Storefront/Shipping/StorefrontPlannedDispatchResolver.cs` — explicit
  `CarrierScheduleMissing` for new commitments, `allowAbsentCarrierSchedule` only for stored legacy evidence.
- `…/CapturedGroupProjection/StorefrontCapturedGroupCompositionCore.cs`,
  `StorefrontCapturedGroupProjectionComposer.cs`, `StorefrontCapturedGroupProjectionContracts.cs` — new
  commitments require the carrier calendar; stable reasons `carrier_schedule_missing/invalid`.
- `…/Checkout/Persistence/CheckoutDirectory.CapturedAcceptance.cs` — legacy revalidation passes
  `allowAbsentCarrierSchedule: evidence.PlannedDispatch is null`.
- `…/Admin/Detail/AdminOrderDetailComposer.cs`, `…/Admin/Detail/Models/AdminOrderDetailModels.cs` — read-only
  `plannedDispatch` on the operational delivery group view.

Frontend
- `features/admin-shipping-authoring/ui/timing-tab-navigation.ts` (new pure helper),
  `ui/authoring-screen.tsx` (underline tabs, ARIA, roving focus, shared keyboard model for the page tabs),
  `messages/authoring-messages.ts` (concise copy).
- `features/order-operational-groups/ui/operational-delivery-timeline.tsx` (new), `index.ts`,
  `app/admin/admin-order-operational-delivery-section.tsx`, `app/admin/admin-api.ts`,
  `app/storefront/storefront-shipping-group-cards.tsx` (localized carrier-schedule messages).

Tests
- `Tooba.Shipping.Tests/Scheduling/ShippingDispatchSchedulingAuthority256Tests.cs` (11 facts).
- `Tooba.Order.Tests/Checkout/PlannedDispatchCarrierRequirement256R1Tests.cs` (new, 3 facts),
  `StorefrontCapturedGroupProjectionComposerTests.cs` (+1 fact), `StorefrontCapturedPreviewPlannerTests.cs`,
  `StorefrontCapturedSelectionWriterTests.cs` (fixtures supply the independent carrier week).
- `src/frontend/features/admin-shipping-authoring/authoring-timing-tabs-256-r1.test.ts`,
  `src/frontend/app/admin/admin-order-operational-delivery.mount.test.ts`.
- Read-only probes: `scripts/tb256r1-authoring-tabs-dom.mjs`, `scripts/tb256r1-order-timeline-dom.mjs`.

## 2. Focused test receipts (this tree)

    dotnet test …Tooba.Shipping.Tests --filter FullyQualifiedName~ShippingDispatchSchedulingAuthority256Tests
      Passed!  Failed: 0, Passed: 11, Total: 11

    dotnet test …Tooba.Shipping.Tests
      Failed!  Failed: 9, Passed: 822, Total: 831, Duration: 54 s

    dotnet test …Tooba.Order.Tests
      Failed!  Failed: 33, Passed: 664, Total: 697

    dotnet test …Tooba.Order.Tests --filter FullyQualifiedName~PlannedDispatchCarrierRequirement256R1Tests
      Passed!  Failed: 0, Passed: 3, Total: 3

    dotnet test …Tooba.Order.Tests --filter FullyQualifiedName~StorefrontCapturedGroupProjectionComposerTests
      Passed!  Failed: 0, Passed: 12, Total: 12

    node --experimental-strip-types --test features/admin-shipping-authoring/authoring-timing-tabs-256-r1.test.ts
      tests 3 / pass 3 / fail 0

    npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-order-operational-delivery.mount.test.ts
      tests 27 / pass 27 / fail 0

    npx tsc --noEmit        → 0 diagnostics
    npm run build           → compiled successfully (BUILD_ID pz_oynTTa4nZUOPrjSPOm)
    npm run test:architecture → frontend-source-size 18 findings (pre-existing family/count)

## 3. Same-set proof for the pre-existing reds (base worktree `162a319ad`)

    Order   at base: Failed: 33, Passed: 660, Total: 693
    Order   this tree: Failed: 33, Passed: 664, Total: 697   (+4 new facts, same failures)
    Shipping at base: 9 unique failing names
    Shipping this tree: the SAME 9 unique failing names

    Shipping failures (pre-existing):
      Admin.ShippingErrorCatalogAndResourcesTests.Composed_catalog_has_one_owner_and_suppresses_nothing
      Admin.ShippingErrorCatalogAndResourcesTests.Declared_codes_and_registered_descriptors_are_set_equal
      Admin.ShippingErrorCatalogAndResourcesTests.Every_code_has_neutral_and_persian_resources_and_canonical_fallback
      Admin.ShippingErrorCatalogAndResourcesTests.Production_composition_registers_the_catalog_and_resources_once
      Carriers.ShippingCarrierPackageExecutionRelationalTests.Case03_two_packages_same_group_each_own_payload_once
      Carriers.ShippingCarrierPackageExecutionRelationalTests.Case04_concurrent_create_and_competing_manual_do_not_overship
      Packaging.ShippingPackagingCustomerChargeBoundaryTests.Customer_rate_quote_engine_has_no_packaging_input
      Packaging.ShippingPackagingCustomerChargeBoundaryTests.Rates_contracts_never_expose_packaging
      Persistence.ShippingPersistenceModelTests.Model_and_the_initial_migration_are_in_sync

    Order failures (pre-existing): the discretionary-PostgreSQL relational classes
    (MarketplaceOperationalGroupWorkflowRelationalTests / operational-group occurrence / carrier execution
    relational / accepted-evidence storage) plus StorefrontCapturedSelectionWriterTests' known coupon mock.
    The only failure among the focused families this task touched is that coupon mock.

## 4. Runtime identity (observed)

    Host   PID 21536  artifacts/publish-primary/Tooba.Host.exe   /health 200
    FE     PID 38148  node node_modules/next/dist/bin/next start -p 3000 -H 127.0.0.1
    BUILD_ID          pz_oynTTa4nZUOPrjSPOm
    GET /fa                                        200
    GET /admin/shipping-services/authoring/01a17d0d…?mode=edit   200
    GET /v1/admin/shipping-services/authoring-drafts/01a17d0d…/editor-document  200  revision 2
    GET /v1/admin/shipping-services/authoring-drafts/01a1beed…/editor-document  200  revision 3
    GET /v1/admin/orders/01a1b92d-e97e-7000-999a-143350d0edaf   200
          operationalDeliveryGroups 3, all with null plannedDispatch

SingleStore / store-alpha / tooba_alpha. No migration added/applied in this task. DataProtection preserved.
Local CapturedPreview ON; checked-in OFF. User draft `01a17d0d…` revision 2 / `updatedAt 2026-10-27T11:10:28Z`
unchanged; task draft `01a1beed…` revision 3 unchanged (the probes never click Save/Cancel/Publish).

## 5. Authorized read-only DOM (task-owned headless Chrome, fresh profile, no user tab/profile use)

    scripts/tb256r1-authoring-tabs-dom.mjs  (route 01a17d0d…)
      ready true
      timing tablist aria-label «زمان‌بندی ارسال», 2 tabs
      tab height 44, active border-bottom 2px rgb(37,99,235), border-radius 0px, background transparent
      aria-selected/aria-controls/panel aria-labelledby resolved for both tabs
      switch: customer panel hidden / carrier panel visible, customer rows still mounted 7
      editStep {beforeClosedPressed:false, afterClosedPressed:false, afterWorkingPressed:true}
      backToCustomer {editedStatePreserved:true, restoredBaseline:true}
      keyboard {rtl:true, afterArrow:…carrier, afterHome:…customer}
      overflowX 1440 −15 / 1024 −15 / 390 0; tabs 136×44 and 108×44 at 390
      forbiddenCopyHits []
      failedRequests [{400, OPTIONS, preflight, http://localhost:3000/fa}]  (pre-existing, site-wide)

    scripts/tb256r1-authoring-tabs-dom.mjs  (route 01a1beed…)
      carrierDayRows 7, carrierRowsVisible 7, carrierUnconfigured false
      copyVisible {height:44, width:191, text:«کپی از ساعات تحویل مشتری», disabled:false}
      forbiddenCopyHits [], editedStatePreserved true, restoredBaseline true, overflow none

    scripts/tb256r1-order-timeline-dom.mjs  (operational order)
      deliverySection true, groupCount 3, customerDeliveryLabels 3,
      plannedDispatchLabels 0, plannedDispatchNodes 0, dir rtl, overflow none

Raw probe output: `dom/authoring-review-draft.json`, `dom/authoring-task-draft.json`,
`dom/operational-order.json`. No screenshots were produced.

## 6. No business writes

No order/checkout/payment/reservation/parcel/tracking created, dispatched, delivered, cancelled or paid. No
authoring save, publication, rate/weight/responsibility change, no user cart substitution, no seed/reset/drop,
no manual DDL. Every SQL/HTTP call in this task was read-only against `tooba_alpha`; the only writes are the
repository's own source/docs/tests under `SarvNewVer`.

## 7. Commit

    git commit  →  see `git log -1` for the code+evidence SHA of this task
    git push    →  origin/main fast-forward, local == origin/main verified

## 8. Disclosure

- Pre-existing RED sets above are recorded, not repaired and not re-baselined.
- The `OPTIONS http://localhost:3000/fa` 400 is pre-existing and unrelated (a plain GET returns 200).
- The user's live method has no configured carrier acceptance week, so the cutoff table in RESULT §4 is
  code/fixture-proven; it is not claimed as end-to-end user acceptance.
