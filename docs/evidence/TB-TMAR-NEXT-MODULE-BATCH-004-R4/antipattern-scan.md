# AntiPattern Scan — TB-TMAR-NEXT-MODULE-BATCH-004-R4

Scan date: worker execution on baseline tip + R4 repairs.

| Check | Count / result |
|-------|----------------|
| Host `IShippingServiceLanguageGate` implementation | **0** |
| `HostShippingServiceLanguageGate` type | **0** (deleted) |
| Host `IAdminOrderFulfillmentOperations` implementation | **0** |
| `PlatformHttpException` in `AdminOrderFulfillmentOperations.cs` | **0** |
| Localized Persian exception prose in adapter | **0** |
| Localized message-switch mapping (`MapFulfillmentException`) | **0** |
| Order fulfillment adapter → Host | **0** |
| Fulfillment.Infrastructure `ShippingServiceLanguageGate` | **present** |
| Language gate depends only on Localization.Contracts | **yes** (`ILanguageLookup`) |
| Path ↔ namespace aligned | `Shipping/ShippingServiceLanguageGate.cs` → `Tooba.Fulfillment.Infrastructure.Shipping` |
| Hidden service locator in language gate | **0** |

Guards: `FulfillmentArchitectureGuardTests`, Host `ShippingServiceAdminTests`, Host `AdminOrderFulfillmentOperationsTests`, Fulfillment `ShippingServiceLanguageGateTests`.
