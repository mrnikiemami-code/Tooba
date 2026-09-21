# Host Language Gate Audit — TB-TMAR-NEXT-MODULE-BATCH-004-R4

## Defect (R3 leftover)

`HostShippingServiceLanguageGate : IShippingServiceLanguageGate` lived in
`src/backend/Host/Tooba.Host/Admin/ShippingServiceEndpoints.cs` and was
registered in `Program.cs`. Host implemented a Fulfillment Application port.

## Repair

| Item | Result |
|------|--------|
| Implementation | `Tooba.Fulfillment.Infrastructure.Shipping.ShippingServiceLanguageGate` |
| Port | `IShippingServiceLanguageGate` remains in Fulfillment.Application |
| Dependency | `ILanguageLookup` (Localization.Contracts only) |
| DI | `FulfillmentModule`: `AddScoped<IShippingServiceLanguageGate, ShippingServiceLanguageGate>()` |
| Host class | **deleted** (`HostShippingServiceLanguageGate` absent) |
| Host Program registration | **removed** |

## Host shipping endpoints after repair

Wire DTOs + auth + `ISender` + `ApiResponseFactory` + minimal wire→command mapping only.
No module port implementation classes in Host Admin shipping file.
