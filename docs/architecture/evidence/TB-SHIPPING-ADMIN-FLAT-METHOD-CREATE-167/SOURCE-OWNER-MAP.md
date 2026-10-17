# TB-SHIPPING-ADMIN-FLAT-METHOD-CREATE-167 — SOURCE → OWNER map

Scope: canonical flat shipping-method creation (Admin «سرویس جدید»). Evidence discovered before edits.

| Concern | Actual source | Owner (module) | Why it is the owner |
| --- | --- | --- | --- |
| Canonical method rows (`shipping_methods`) | `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/Aggregates/ShippingMethod.cs` | Fulfillment | Method identity, binding and activation live in the Fulfillment schema (`fulfillment.shipping_methods`). |
| Method translations (`shipping_method_translations`) | `Tooba.Fulfillment.Domain/Aggregates/ShippingMethodTranslation.cs` | Fulfillment | Unique `(shipping_method_id, language_id)`; FK to the method row. |
| Admin read of the flat list (gate/ready/methods) | `Application/Shipping/Queries/ListOneLevelShippingMethodsQuery.cs` + `Infrastructure/Shipping/ShippingOneLevelCatalogAdminRead.cs` | Fulfillment | Existing authorized admin read seam; the flat screen is a client of it. |
| Name write (only narrow write before this task) | `Application/.../Commands/UpdateOneLevelShippingMethodTranslationCommand.cs` + `Infrastructure/Shipping/ShippingOneLevelCatalogAdminWrite.cs` | Fulfillment | Writes only `name`; explicitly never identity/binding. |
| Activation / safe delete | `SetOneLevelShippingMethodActivationCommand` + `Infrastructure/Shipping/ShippingOneLevelCatalogActivation.cs` | Fulfillment | Requires an existing legacy service **and** option; optionless rows are rejected. |
| Canonical code shape | `ShippingMethod.TryNormalizeCode` (`a-z0-9_`, ≤32, at most one `:`) | Fulfillment domain | Single authority for canonical identity. |
| Canonical code → legacy binding rule | `Application/Shipping/ShippingMethodBackfillPlanner.cs` (`service:option` ⇒ `(service, option)`; single-segment ⇒ `(code, null)`) | Fulfillment | Existing backfill rule that produced the recognized 7 methods. |
| Legacy customer-selectable tree | `Application/Shipping/Queries/ListEnabledShippingMethodsTreeQuery.cs` over `shipping_services` / `shipping_service_options` | Fulfillment | The storefront projection reads this tree, **not** `shipping_methods`. |
| Price / coverage / schedule / packaging policy | `src/backend/Modules/Shipping/**` (keyed by `serviceCode` + `optionCode`) | Shipping | Net-rate authority; must not be duplicated or invented in Fulfillment. |
| Admin financial readiness | `ShippingMethodFinancialStatusEndpoints` / owned-rate authority | Shipping | Do not fake a monetary mode from a new method row. |
| Admin UI gate + flat screen | `src/frontend/app/admin/shipping-services/flat-screen-gate.tsx`, `flat-screen.tsx`, `flat-method-catalog-api.ts` | Frontend feature surface | Existing one-level UI; only «سرویس جدید» was an honest blocked dialog. |
| Language registry | `ILanguageLookup` (Localization contracts) + `loadAdminLanguages` | Localization | Only source of real language ids/names; no hardcoded fa/en/ru. |
| Admin authorization | `IFulfillmentAdminAuthorizer.RequireAuthorizedAsync` | Fulfillment endpoints | Same gate as every other admin shipping route. |

## Decision

* The only missing authority was a **method-scoped create** in the canonical owner (Fulfillment). No schema
  change was required: `legacy_service_code` is `NOT NULL` but the canonical mapping fully determines it
  from the method code, and `legacy_option_code` is nullable.
* Binding is therefore **server-derived** from the canonical code with the same rule the backfill used.
  The client never sends `serviceCode`/`optionCode`/`optionId`/`isActive`.
* A new method is **always written inactive**. Activation still requires the existing 096 seam, which
  demands a real legacy service **and** option; a brand-new method without a legacy identity can
  therefore never become customer-selectable in this task's scope.
* No legacy `shipping_services` / `shipping_service_options` row is created, so the storefront tree
  (the actual customer-selectable projection) cannot contain the new method.
