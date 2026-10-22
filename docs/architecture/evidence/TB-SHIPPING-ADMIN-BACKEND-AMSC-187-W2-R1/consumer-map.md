# Consumer map and historic authorization provenance

Re-derived from disk at base `0d9936f07045c9f7568a6e1f27336ac2d15ea7c2`, outside the Shipping module,
over non-generated, non-test backend sources.

## Enumerated outside-Shipping consumers of the operational Contracts (exactly 3)

| # | Path | Role | What it actually references |
| --- | --- | --- | --- |
| 1 | `src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/OperationalShadow/StorefrontShippingOperationalShadowComparer.cs` | pure comparison (observation) | `using Tooba.Shipping.Contracts.Operations;`, `ShippingOperationalQuote`, `ShippingOperationalQuoteReasonCodes` |
| 2 | `src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/OperationalShadow/StorefrontShippingOperationalShadowObserver.cs` | observation adapter | `using Tooba.Shipping.Contracts.Operations;`, `IShippingOperationalQuoteLookup`, `ShippingOperationalQuoteRequest`, `Tooba.Shipping.Contracts.Rates.ShippingMeasuredParcel` |
| 3 | `src/backend/Modules/Order/Tooba.Order.Infrastructure/OrderModule.cs` | DI registration only | `sp.GetService<Tooba.Shipping.Contracts.Operations.IShippingOperationalQuoteLookup>()` passed to the observer factory; `StorefrontShippingOperationalShadowOptions.SectionName` |

No fourth production consumer exists. Enumeration was done with the full operational marker set
(`Tooba.Shipping.Contracts.Operations`, `IShippingOperationalQuoteLookup`,
`ShippingOperationalQuoteRequest`, `ShippingOperationalQuoteOutcome`,
`ShippingOperationalQuoteReasonCodes`, `ShippingOperationalQuote`, `operational-quote`) over executable
lines only (XML docs / line comments excluded so documentation prose cannot create a false consumer or
hide a real one).

## Immediate call site of the observation

`src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/Queries/ProjectStorefrontShipping/ProjectStorefrontShippingQuery.cs`

* L34–35 receive `StorefrontShippingShadowObserver shadow, StorefrontShippingOperationalShadowObserver operationalShadow`.
* L52–59 await the **simple-rate** shadow; L73–90 await the **operational** shadow.
* L92 `return result;` — the awaited observation value is not bound to a variable at all, so nothing
  feeds `SelectedShippingAmount`, `MinimumDeliveryDate`, the order draft, the paid snapshot or any
  capacity decision.
* The parcel is deliberately *not* taken from the public read request (L69–72 comment), so a
  client-supplied number can never become commercial weight.

## Project / type boundaries

* Order projects reference `Tooba.Shipping.Contracts` only (no `Tooba.Shipping.Domain/Application/
  Infrastructure/Endpoints` project reference anywhere in `Modules/**` outside Shipping).
* The only Shipping-implementation consumers in the repository are the Host composition roots
  (`Program.cs`, `Composition/ToobaModuleComposition.cs`, `Tooba.MigrationRunner/ModuleMigrationRegistry.cs`),
  which are the canonical `HOST_COMPOSITION_ROOT` seam — not module logic.
* No cross-module `DbContext`/`DbSet`/join: the observer receives `IGeographyLookup` and
  `IShippingOperationalQuoteLookup` contracts only.

## Historic authorization provenance (history only; none of these tasks was replayed)

| Source | Exact authorization |
| --- | --- |
| `docs/ai/tasks/TB-SHIPPING-FULL-OPERATIONS-079.task.md` L42–L53 (Mission A) | "add only a read-only, fail-isolated operational shadow observation that can compare the real legacy customer quote to the operational quote"; "Reuse existing `ShadowEnabled`/`PrerequisitesConfirmed` … if the existing gate is scoped to the earlier simple-rate shadow, introduce an explicitly separately disabled new gate"; "Default OFF"; "Preserve 100% identical legacy storefront response shape and bytes when gate OFF and when ON"; "existing Contracts-only seam and MediatR `ISender`, no direct foreign `DbContext`". |
| `docs/ai/tasks/TB-SHIPPING-FULL-OPERATIONS-080.task.md` L18 | "079 has been ACCEPTED for its bounded read-only storefront operational-shadow scope." |
| `docs/ai/tasks/TB-SHIPPING-FULL-OPERATIONS-080.task.md` L26 | verify `Tooba:ShippingOperationalShadow:Enabled` and `PrerequisitesConfirmed` remain OFF by default and "both are observation-only". |
| `docs/ai/tasks/TB-SHIPPING-FULL-OPERATIONS-076.task.md` | the origin of the now-stale guard: at that time no external consumer was authorized, so "no module outside Shipping mentions these types" was used as a temporary proxy. |

Conclusion: the three discovered consumers are the ones 079 authorized and 080 accepted. Their
existence is lawful; the 076 blanket prohibition is the stale artifact. No authority conflict was found,
and the discovered set matches the Architect's approved three roles exactly, so no BLOCKED outcome is
required.
