# Recovery Start — TB-TMAR-NEXT-MODULE-BATCH-004-R4

- Claim: `d912b692-09ce-4190-bdeb-f657a8b1988d` (held / Claimed)
- Baseline HEAD: `42fd5f432936fe44c314021514932d8c263f3bd4` == `origin/main`
- Channel: `tooba-main` / Worker: `tooba-worker-01`
- Mode: FAST-SAFE Silent Bridge repair
- Parent: TB-TMAR-NEXT-MODULE-BATCH-004-R3 (PASS REOPENED as R4)

## Scope (R4 only)

1. Move `IShippingServiceLanguageGate` implementation out of Host into
   Fulfillment.Infrastructure; delete `HostShippingServiceLanguageGate`;
   register in Fulfillment module DI (Localization.Contracts only).
2. Close Order adapter anti-pattern in `AdminOrderFulfillmentOperations`:
   no `PlatformHttpException`, no localized Persian prose, no prose
   message→localized switch; expected failures return stable outcome codes;
   map only known stable machine codes from downstream `InvalidOperationException`.

## Out of scope

- BATCH-005
- Tax / Pricing / Checkout / frontend
- Broad Order / Host / Returns suites

## Verified defects at start

- `src/backend/Host/Tooba.Host/Admin/ShippingServiceEndpoints.cs` —
  `HostShippingServiceLanguageGate : IShippingServiceLanguageGate`
- `src/backend/Host/Tooba.Host/Program.cs` — Host DI registration of language gate
- `src/backend/Modules/Order/Tooba.Order.Infrastructure/Fulfillment/AdminOrderFulfillmentOperations.cs` —
  `PlatformHttpException` + Persian prose + `MapFulfillmentException` prose switch
