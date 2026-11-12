# TB-264-R1 receipts (commands actually run, one pass each)

## Focused tests

    dotnet test src\backend\Modules\Shipping\Tooba.Shipping.Tests\Tooba.Shipping.Tests.csproj -c Release \
      --filter "FullyQualifiedName~ShippingDispatchSchedulingAuthority264|FullyQualifiedName~ShippingCapturedRevisionQuote"
    -> Passed! - Failed: 0, Passed: 32, Skipped: 0, Total: 32 (97 ms)

    dotnet test src\backend\Modules\Order\Tooba.Order.Tests\Tooba.Order.Tests.csproj -c Release \
      --filter "FullyQualifiedName~AdminDispatchPreparation264"
    -> Passed! - Failed: 0, Passed: 2, Skipped: 0, Total: 2 (1 s)   [real tooba_alpha]

## Build / publish

    dotnet publish src\backend\Host\Tooba.Host\Tooba.Host.csproj -c Release -o artifacts\publish-primary
    -> Tooba.Host -> D:\Users\User\source\repos\SarvNewVer\artifacts\publish-primary\   (0 errors)

## Host restart

    Stop-Process -Id 14696            (previous host)
    Start-Process .\Tooba.Host.exe    (artifacts\publish-primary)  -> PID 8964
    GET /health       -> 200
    GET /health/ready -> 200

## Canonical authoring save + publication (real HTTP, real DB)

    PUT /v1/admin/shipping-services/authoring-drafts/01a17d0d-e033-7000-af43-49a1daa28822/editor-document
      expectedRevision 3, pair 240/60, preparationMode WorkingWeek, processing week Asia/Tehran 7 days
      -> 200  revision 4  settings {240,60,WorkingWeek}  processingSchedule Asia/Tehran

    POST .../publication {"expectedRevision":4,"expectedSelectionVersion":2}
      -> 200  selectionVersion 3  snapshot 01a1cd1e-2251-7000-982d-567c6f3af9bf  selectedFormatVersion 3   <-- DEFECT

    (after fixing the capture graph)
    PUT .../editor-document expectedRevision 4, preparationMinutes 300 -> 200 revision 5
    POST .../publication {"expectedRevision":5,"expectedSelectionVersion":3}
      -> 200  selectionVersion 4  snapshot 01a1cd2d-7601-7000-a896-156f68e0a8d5  selectedFormatVersion 4   <-- FIXED

    PUT .../editor-document expectedRevision 5, preparationMinutes 240 -> 200 revision 6
    POST .../publication {"expectedRevision":6,"expectedSelectionVersion":4}
      -> 200  selectionVersion 5  snapshot 01a1cd2d-dc41-7000-abf0-322ca18255c4  selectedFormatVersion 4

## Read-only DB verification (tb264r1-connected-proof.txt)

    shipping.__ef_migrations_history head = 20261112120000_AddProcessingHoursAndTimingPolicy
    "order".__ef_migrations_history head = 20261112130000_RelaxPreparationDispatchZeroAllowances
    draft 01a17d0d-… revision 6, settings 240/60/1, processing week Asia/Tehran / 6 working days
    selected capture = snapshot 01a1cd2d-dc41-… / selection v5 / sourceRevision 6 / formatVersion 4
    payload FormatVersion 4, prep/handoff/mode 240/60/1, ProcessingHours Asia/Tehran 6 working,
      CarrierAcceptance Asia/Tehran 7 days
    pre-fix capture 01a1cd1e-… = format_version 3 / ProcessingHours null   (defect, kept as history)

## Live storefront projection (tb264r1-storefront-projection.txt)

    POST /v1/storefront/cart {}                                        -> 200 cart 01a1cd1f-…
    POST /v1/storefront/cart/01a1cd1f-…/lines?expectedVersion=1        -> 200 version 3, 1 line 01a1cd20-…
    POST /v1/storefront/shipping/projection (no destination)           -> 200 destination_required
    POST /v1/storefront/shipping/projection (Iran destination)         -> 200
        seller group quoted from seller-owned capture 01a186f7-… (rev 10 / selection 11)
        availableDates [] , unavailableReason storefront.captured_group.carrier_schedule_missing

## Frontend / FE tests

No frontend source changed in the R1 round; the FE timing tabs and `preparationMode` contract shipped with
the earlier 264 round and its suites (`authoring-timing-264`, `authoring-timing-tabs-256-r1`,
`authoring-lifecycle-202-r1`, `shipping-groups-204`) are part of the same working tree.
