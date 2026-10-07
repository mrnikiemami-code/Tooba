# TB-TMAR-PRICING-AMSC-001-W3-R2 — endpoint applicability audit

- Module: `Pricing`
- Skill: `tooba-architecture-structure`
- Verdict: `NOT_HTTP_OWNING_INTERNAL_CAPABILITY_PROVIDER` → `CANONICAL_NO_ENDPOINTS_PROJECT`
- HTTP route count: **0**; endpoint-reachable requests: **0**; required validators: **0**
- CQRS state: `NOT_APPLICABLE_INTERNAL_ONLY`
- Validator coverage: `NOT_APPLICABLE_INTERNAL_ONLY`
- Precedent: `Inventory` (`INTERNAL_ONLY`, no Endpoints project, registration in
  `Infrastructure/DependencyInjection/InventoryModule.cs`)

## 1. What the retired ceremony actually contained

`Tooba.Pricing.Endpoints/PricingEndpointModule.cs` at `2e664bb3`:

```csharp
public static class PricingEndpointModule
{
    /// <summary>Registers the Pricing route group. No public Pricing route is mapped yet.</summary>
    public static IEndpointRouteBuilder MapPricingModule(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        _ = app.MapGroup("/v1/pricing");          // deliberately empty group, discarded
        return app;
    }

    /// <summary>Registers the Pricing error catalog and resource set.</summary>
    public static IServiceCollection AddPricingEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IErrorCatalogContributor, PricingErrorCatalogContributor>();
        services.AddSingleton<IErrorResourceSet, PricingErrorResourceSet>();
        return services;
    }
}
```

The only substantive content is the two DI registrations. `MapPricingModule()` mapped **no** request; the
`/v1/pricing` group was created and discarded. This is exactly the ceremonial Endpoints project the
current skills forbid for an internal-only module.

## 2. Applicability probe matrix

| Probe | Scope | Before (`2e664bb3`) | After (W3-R2) |
| --- | --- | --- | --- |
| `MapGet(` | Pricing production | 0 | **0** |
| `MapPost(` | Pricing production | 0 | **0** |
| `MapPut(` | Pricing production | 0 | **0** |
| `MapDelete(` | Pricing production | 0 | **0** |
| `MapPatch(` | Pricing production | 0 | **0** |
| `MapGroup(` | Pricing production | 1 (empty `/v1/pricing`) | **0** |
| `"/v1/pricing"` literal | Pricing production | 1 | **0** |
| `IEndpointRouteBuilder` | Pricing production | 1 (`MapPricingModule`) | **0** |
| `ISender` / `MediatR` | Pricing production | 0 | **0** |
| `Tooba.Pricing.Endpoints` | Pricing production | 1 (namespace) | **0** |
| `MapPricingModule` | Host `Program.cs` | 1 | **0** |
| `AddPricingEndpointPresentation` | Host `Program.cs` | 1 | **0** |
| `Tooba.Pricing.Endpoints` | Host `Program.cs` | 1 (`using`) | **0** |
| `Tooba.Pricing.Endpoints` | Host `.csproj` | 1 (`ProjectReference`) | **0** |
| `Tooba.Pricing.Infrastructure.csproj` | Host `.csproj` | 1 | **1** (preserved) |
| Endpoints project directory | disk | present | **absent** |
| Endpoints test folder | disk | present | **absent** |

Raw machine-checked report: `audit.cjs` in this directory (run
`node docs/architecture/evidence/TB-TMAR-PRICING-AMSC-001-W3-R2/audit.cjs`).

## 3. Presentation registration — exactly once, before and after

| | Before | After |
| --- | --- | --- |
| Site | `Tooba.Pricing.Endpoints/PricingEndpointModule.AddPricingEndpointPresentation()` | `Tooba.Pricing.Infrastructure/DependencyInjection/PricingModule.AddServices(...)` |
| `IErrorCatalogContributor` registrations | 1 | **1** |
| `IErrorResourceSet` registrations | 1 | **1** |
| Concrete type owner | `Tooba.Pricing.Contracts.Errors` | `Tooba.Pricing.Contracts.Errors` (unchanged) |
| Lifetime | singleton | singleton (unchanged) |
| Semantics | unchanged | unchanged |

`PricingErrorCatalogContributor` class declarations in the module: **1**. `PricingErrorResourceSet`
class declarations: **1**. No duplicate registration was introduced and none was removed.

## 4. The only Pricing-reachable write stays Offer-owned

Pricing owns no module HTTP route. The single Pricing-reachable write remains the **Offer-owned** seller
price route `POST|PUT /v1/seller/offers/{offerId}/price` → `ISender` → `SetOfferPriceCommand` →
`ISellerOfferPricingGateway` (`Tooba.Pricing.Contracts.Seller`). Transport validation stays Offer-owned;
Pricing re-validates its own boundary inputs inside `PriceDirectory` through `MarketCode.TryParse` /
`CurrencyCode.TryParse` → `Result.Failure`. None of that is affected by W3-R2, and no new route,
request, handler or validator was created.

## 5. Error / localization surface identity (unchanged)

| Surface | Before | After |
| --- | --- | --- |
| Declared stable codes (`public const string`) | 11 | **11** |
| Declared-code guard | `KnownCodes` + `IsKnown(string?)` | **present** |
| Descriptor factories (`D(PricingErrorCodes.…)`) | 11 | **11** |
| EN resource keys (`pricing.*`) | 11 | **11** |
| FA resource keys (`pricing.*`) | 11 | **11** |
| EN/FA key sets identical | yes | **yes** |
| Migration files | `20260823085546_InitialPricing.cs` + `.Designer.cs` + `PricingDbContextModelSnapshot.cs` | **identical** |

## 6. Conclusion

Pricing owns zero HTTP routes, zero endpoint-reachable requests and zero validators; the Endpoints project
was pure ceremony whose only useful content was two DI registrations. W3-R2 removes the project, the empty
route group and all Host ceremony, and re-homes the registrations into the module's own Infrastructure
composition root — the canonical `INTERNAL_ONLY` shape. `Http-Applicability-State =
INTERNAL_ONLY_NO_ENDPOINTS`; `Pricing-Endpoints-Project-State = ABSENT`; `Pricing-Http-Route-State =
ZERO`; `Presentation-Registration-State = INFRASTRUCTURE_MODULE_EXACTLY_ONCE`.
