# Capability map (discovered from disk, not from type names)

Module applicability: **`HTTP_OWNING`** — proven by 16 real module-owned routes under
`/v1/admin` (see `route-matrix.js` output below) plus 16 real `ISender` requests. The module's
internal quote/lookup seams (`IShippingRateQuoteLookup`, `IShippingPackagingFeeLookup`,
`IShippingShipmentCostLookup`) are consumed in-process and do **not** remove the genuine HTTP surface.

| Capability | Contracts | Domain | Application | Infrastructure | Endpoints | Routes |
| --- | --- | --- | --- | --- | --- | --- |
| `Rates` | `Rates/ShippingRatePolicyContracts.cs`, `Admin/ShippingMethodFinancialStatusContracts.cs` | `Aggregates/ShippingRatePolicy.cs`, `Aggregates/ShippingMethodCoverage.cs`, `Aggregates/ShippingCoverageGrant.cs`, `Rates/ShippingRatePolicyEngine.cs` | `Rates/Commands`, `Rates/Queries`, `Rates/Ports` (4), `Rates/ReadModels`, shared `Validation/ShippingAdminValidators.cs` | `Rates/ShippingRateCoverageDirectory.cs`, `Rates/ShippingRateQuoteLookup.cs`, `Rates/GeographyDestinationScopeValidator.cs`, `Rates/FulfillmentShippingMethodOptionValidator.cs` | `Admin/ShippingRateCoverageEndpoints.cs`, `Admin/ShippingMethodFinancialStatusEndpoints.cs` | 7 + 1 |
| `Packaging` | `Packaging/ShippingPackagingContracts.cs` | `Aggregates/ShippingPackagingPolicy.cs`, `Packaging/ShippingPackagingFeeResolver.cs` | `Packaging/Commands`, `Packaging/Ports`, `Packaging/Validation` | `Packaging/ShippingPackagingPolicyDirectory.cs`, `Packaging/ShippingPackagingFeeLookup.cs`, `Packaging/ShippingShipmentCostLookup.cs` | `Admin/ShippingPackagingPolicyEndpoints.cs` | 4 |
| `Scheduling` | `Scheduling/ShippingScheduleContracts.cs` | `Scheduling/ShippingOperatingSchedule.cs`, `ShippingOperatingWindow.cs`, `ShippingScheduleAvailabilityEvaluator.cs` | `Scheduling/Commands`, `Scheduling/Ports`, shared `Validation/ShippingScheduleValidators.cs` | `Scheduling/ShippingOperatingScheduleDirectory.cs` | `Admin/ShippingOperatingScheduleEndpoints.cs` | 3 |
| `Operations` (quote evaluation) | `Operations/ShippingOperationalQuoteContracts.cs` | `Operations/ShippingOperationalQuoteEvaluator.cs` | `Operations/Queries`, shared `Validation/ShippingOperationalValidators.cs` | `Operations/ShippingOperationalQuoteLookup.cs` | `Admin/ShippingOperationalQuoteEndpoints.cs` | 1 |
| shared | `Admin/ShippingAdminContracts.cs` (write bodies + validation codes), `Errors/ShippingErrorCodes.cs` | `Aggregates/*` cross-capability invariants | `Validation/ShippingAdminValidators.cs` | `Persistence/*`, `DependencyInjection/ShippingModule.cs` | `Admin/ShippingAdminAuthorizer.cs`, `Errors/`, `Resources/`, `ShippingEndpointModule.cs` | – |

Names are the ones already used by the module (`Rates`, `Packaging`, `Scheduling`, `Operations`,
`Admin`); no folder was renamed and no capability name was invented from a command filename.

## Preserved route → request → provenance → permission matrix (W0/W1 handoff, unchanged by W2)

| Route (`/v1/admin`) | Verb | `ISender` request | Input provenance | Permission |
| --- | --- | --- | --- | --- |
| `/shipping-services/{serviceCode}/rates` | GET | `GetShippingRatesAndCoverageQuery` | route `serviceCode` | `fulfillment.view` |
| `/shipping-services/{serviceCode}/rates` | POST | `CreateShippingRatePolicyCommand` | route `serviceCode` + JSON body `ShippingRatePolicyWriteRequest` | `fulfillment.manage` |
| `/shipping-services/{serviceCode}/rates/{ratePolicyId:guid}` | PUT | `UpdateShippingRatePolicyCommand` | route ids + JSON body | `fulfillment.manage` |
| `/shipping-services/{serviceCode}/rates/{ratePolicyId:guid}/active` | PATCH | `SetShippingRatePolicyActiveCommand` | route ids + body `IsActive` | `fulfillment.manage` |
| `/shipping-services/{serviceCode}/coverage-grants` | POST | `CreateShippingCoverageGrantCommand` | route `serviceCode` + JSON body | `fulfillment.manage` |
| `/shipping-services/{serviceCode}/coverage-grants/{coverageGrantId:guid}/active` | PATCH | `SetShippingCoverageGrantActiveCommand` | route ids + body `IsActive` | `fulfillment.manage` |
| `/shipping-services/{serviceCode}/method-coverage` | PUT | `SetShippingMethodCoverageCommand` | route `serviceCode` + JSON body | `fulfillment.manage` |
| `/shipping-services/{serviceCode}/financial-status` | GET | `GetShippingMethodFinancialStatusQuery` | route `serviceCode` | `fulfillment.view` |
| `/shipping-services/{serviceCode}/operating-schedules` | GET | `GetShippingOperatingSchedulesQuery` | route `serviceCode` | `fulfillment.view` |
| `/shipping-services/{serviceCode}/operating-schedules` | PUT | `UpsertShippingOperatingScheduleCommand` | route `serviceCode` + JSON body | `fulfillment.manage` |
| `/shipping-services/{serviceCode}/operating-schedules/{scheduleId:guid}/active` | PATCH | `SetShippingOperatingScheduleActiveCommand` | route ids + body `IsActive` | `fulfillment.manage` |
| `/shipping-services/{serviceCode}/packaging-policies` | GET | `GetShippingPackagingPoliciesQuery` | route `serviceCode` | `fulfillment.view` |
| `/shipping-services/{serviceCode}/packaging-policies` | POST | `CreateShippingPackagingPolicyCommand` | route `serviceCode` + JSON body | `fulfillment.manage` |
| `/shipping-services/{serviceCode}/packaging-policies/{packagingPolicyId:guid}` | PUT | `UpdateShippingPackagingPolicyCommand` | route ids + JSON body | `fulfillment.manage` |
| `/shipping-services/{serviceCode}/packaging-policies/{packagingPolicyId:guid}/active` | PATCH | `SetShippingPackagingPolicyActiveCommand` | route ids + body `IsActive` | `fulfillment.manage` |
| `/shipping-services/{serviceCode}/operational-quote` | POST | `EvaluateShippingOperationalQuoteQuery` | route `serviceCode` + JSON body `ShippingOperationalQuoteEvaluationRequest` | `fulfillment.view` |

16 routes / 16 distinct requests / 5 capability route mappers — identical before and after W2.
`ShippingAdminPermissions.View`/`Manage` remain the existing `fulfillment.view`/`fulfillment.manage`
catalog ids; W2 invented no permission.
