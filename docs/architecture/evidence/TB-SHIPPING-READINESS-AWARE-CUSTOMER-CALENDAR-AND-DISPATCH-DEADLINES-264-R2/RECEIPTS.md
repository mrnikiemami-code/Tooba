# TB-264-R2 receipts (commands actually run, one pass each)

Sanitized: no token, guest secret or cookie is kept here. One canonical install only.

    Host   127.0.0.1:5088   (PID 49024, /health 200, /health/ready 200 -> SingleStore, messaging Healthy)
    Next   127.0.0.1:3000   (PID 48780, BUILD_ID ILzh0yMqPr3RDrjGp3wYE, /fa 200)
    DB     127.0.0.1:5432   tooba_alpha (SingleStore, tenant store-alpha)
    Base   c9d87308b8c5e3ec7347a984a42ce5b0a44c40fa

## 1. Frontend build + serve

    cd src/frontend
    npx tsc --noEmit
    -> exit 0

    npm run build
    -> exit 0 (production build complete)
    cat .next/BUILD_ID
    -> ILzh0yMqPr3RDrjGp3wYE

    Stop-Process -Id 17440        # previous server, BUILD_ID R9whZM-4FOR7dVyjVOgUr
    node node_modules/next/dist/bin/next start -p 3000
    -> new process PID 48780
    GET http://127.0.0.1:3000/fa                                    -> 200

    GET .../_next/static/chunks/6899-61d97f4a6a9808f2.js            -> 200  len=52283
        contains «زمان آماده‌سازی» = true, «پیوسته» (Continuous label) = true
    GET .../_next/static/chunks/6138-ded12a4885f63d10.js            -> 200  len=228270
        contains processingSchedule = true
    GET .../_next/static/ILzh0yMqPr3RDrjGp3wYE/_buildManifest.js    -> 200

## 2. Real authoring form (browser, real buttons)

    page   http://127.0.0.1:3000/fa/admin/shipping-services/authoring/01a1cd43-b98d-7000-b2a9-a35ab39d8058
    edit   click [data-testid=shipping-authoring-edit]
    read   prep=300  handoff=75  mode=WorkingWeek  processing editor present
           tabs  customer | carrier | processing

    change handoff minutes  -> 80        (input event on [data-testid=shipping-authoring-handoff-minutes])
    open tab processing
    change Thursday close   -> 17:30     ([data-testid=shipping-authoring-processing-close-4])
    state  handoff=80  close4=17:30  unsaved="تغییرات ذخیره‌نشده دارید"  apply disabled=false

    click [data-testid=shipping-authoring-apply]
    intercepted fetch:
        PUT /v1/admin/shipping-services/authoring-drafts/01a1cd43-b98d-7000-b2a9-a35ab39d8058/editor-document -> 200
    page badge after save -> «منتشرشده (نسخهٔ قدیمی‌تر)»

    API confirm: GET .../editor-document -> revision=8
        settings {Prepaid, Manual, preparationMinutes 300, handoffMinutes 80, preparationMode WorkingWeek}

    reload (location.reload) -> operator view:
        status badge «منتشرشده»
        prep=300  handoff=80  mode=WorkingWeek checked=true  Thursday range «۰۹:۰۰ تا ۱۷:۳۰»

    click [data-testid=shipping-authoring-publication-publish]
    confirm text: «بازبینی 8 به‌عنوان نسخهٔ منتشرشده انتخاب شود؟ متن ذخیره‌شدهٔ این روش تغییر نمی‌کند.»
    click [data-testid=shipping-authoring-publication-confirm-accept]
    badge -> «منتشرشده»

    GET .../publication ->
        state Selected, selectionVersion 5,
        selectedSnapshotId 01a1d50f-5275-7000-b050-dda3aedd1838,
        selectedSourceRevision 8, selectedFormatVersion 4,
        selectedMatchesCurrentDraftRevision true

## 3. Positive storefront chain (public endpoints only)

    POST /v1/auth/register {Phone 09992642003}                       -> 201 userId 01a1d511-23a3-7000-8238-a5ecff4171c7
    POST /v1/auth/login                                              -> 200
    POST /v1/storefront/cart                                         -> 200 cart 01a1d511-4c3f-7000-8d46-78a053510edb v1
    POST /v1/storefront/cart/{id}/lines {offer 0442a1fa-…, qty 1}    -> 200 v3
    POST /v1/storefront/cart/merge                                   -> 200, current cart v4, lines=1

    POST /v1/storefront/shipping/projection {تهران/تهران}            -> 200
        group  cap|platform|store|01a030d1…|01a16f43…
        method tipax/express  snapshot 01a1d50f-5275-7000-b050-dda3aedd1838
               sourceRevision 8  selectionVersion 5
               areaGroup 01a1cd44-6c1f-7000-8d2a-3650a23cc6a0
               rateRow   01a1cd44-6c21-7000-870e-0efc6efc584a
               availableDates = 2026-11-16,17,18,19,20,21,22
               slots per day 09:00-10:00 … 17:00-18:00
        sibling post/standard on another seller capture -> carrier_schedule_missing (no invented date)

    PUT /v1/storefront/shipping/selection (expectedSelectionRevision 0,
        selectedDeliveryDate 2026-11-16, slot 09:00-10:00)            -> 200
        selectionRevision 1, group 01a1d511-7412-7000-a5a7-2d42aef7c393, charge 158000.00 IRR

    POST /v1/storefront/shipping/commit (expectedSelectionRevision 1)-> 200
        checkoutId 01a1d511-adbb-7000-8575-d3b957453d76
        sellerOrder 01a1d511-adce-7000-8819-0185f673c4bf  number TB-20261113132108-01-320e47
        paymentState PendingPayment, payable 9,643,771 IRR

## 4. Durable row (read-only SQL, canonical DB)

    checkout 01a1d511-adbb-7000-8575-d3b957453d76
      group 01a1d511-7412-7000-a5a7-2d42aef7c393
      policy=3  prep=2026-11-14T09:30Z  transfer=2026-11-14T15:10Z  handoff=2026-11-14T16:30Z
      prepMin=300  handMin=80  handoffDate=2026-11-14
      receiving=2026-11-16  zone=Asia/Tehran
      snapshot=01a1d50f-5275-7000-b050-dda3aedd1838
    (control, same query) checkout 01a1cdba-7d2d-7000-a800-4b6d216eb0e3
      policy=3  prep=2026-11-12T09:30Z  transfer=2026-11-12T15:15Z  handoff=2026-11-12T16:30Z  prepMin=300 handMin=75

## 5. Existing admin queue (live Host, read-only)

    POST /v1/admin/fulfillments/work-queue/query {"search":"TB264R2C2","page":1,"pageSize":20}
    -> 200, one row
       workUnitKey            group:01a1d511-7412-7000-a5a7-2d42aef7c393
       checkoutId             01a1d511-adbb-7000-8575-d3b957453d76
       dispatchBucket         Upcoming
       preparationStartUtc    2026-11-14T09:30:00+00:00
       transferDepartureUtc   2026-11-14T15:10:00+00:00
       handoffDeadlineUtc     2026-11-14T16:30:00+00:00
       dispatchPolicyVersion  3
       plannedHandoffDate     2026-11-14   08:00–20:00   Asia/Tehran
       customerReceivingDate  2026-11-16   09:00-10:00
       preparationState       Upcoming       nextStepCode mark_processing
       locator /fa/admin/orders/01a1d511-adbb-…/operations
               ?fulfillmentId=9fe40e71-23cb-4014-8a35-e4c3231c9dab
               &shippingGroupId=01a1d511-7412-7000-a5a7-2d42aef7c393&from=queue

## 6. Focused tests

    $env:TB264R2_CONNECTION='Host=127.0.0.1;Port=5432;Database=tooba_alpha;Username=admin;Password=***'
    dotnet test src\backend\Modules\Order\Tooba.Order.Tests\Tooba.Order.Tests.csproj -c Release \
      --filter "FullyQualifiedName~AdminPreparationConnected264R2Tests"
    -> Passed! Failed: 0, Passed: 2, Total: 2   [real tooba_alpha]
       Case_A: bucket=Overdue window=Closed policyV=3 prep=2026-11-12T09:30Z transfer=15:15Z handoff=16:30Z
       Case_B: bucket=Upcoming window=Future policyV=3 prep=2026-11-14T09:30Z transfer=15:10Z handoff=16:30Z
               customer=2026-11-16 09:00-10:00

    cd src/frontend
    node --experimental-strip-types --test \
      features/admin-shipping-authoring/authoring-timing-264.test.ts \
      features/admin-shipping-authoring/authoring-general-ui.test.ts \
      app/storefront/storefront-shipping-unavailable-methods.test.ts
    -> tests 22  pass 22  fail 0

    (reported, NOT fixed, pre-existing) app/storefront/storefront-shipping-group-schedule.test.ts
    -> asserts /\{isGroupedCart \? null : \(\s*<StorefrontShippingSingleGroupSection/ against
       app/storefront/storefront-shipping.tsx, which is unmodified in this round and does not contain that
       text, so the assertion fails at the current base too. Left untouched.

## 7. Deployment facts

    migrations   none added, none applied this round
                 Shipping head 20261112120000_AddProcessingHoursAndTimingPolicy
                 Order    head 20261112130000_RelaxPreparationDispatchZeroAllowances
    host         artifacts\publish-primary\Tooba.Host.dll  2026-11-12 06:23:51
                 (newer than the format-4 source edit 2026-11-12 06:21:09, so the running build carries it)
                 /health 200, /health/ready 200 (SingleStore)
    frontend     rebuilt + restarted this round (PID 17440 -> 48780)
    no disposable/test/parallel/Docker DB, no 15433, no edition/connection/port change
    no manual DDL / business SQL / reset / reseed / backfill / global reclaimer
    DataProtection unchanged; CapturedPreview local ON (Enabled/Selection/Checkout, 7 dates), checked-in OFF
