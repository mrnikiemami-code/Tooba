# TB-TMAR-PRICING-AMSC-001-W3-R3 — internal-only applicability

## Classification

```text
httpApplicability = NOT_HTTP_OWNING_INTERNAL_CAPABILITY_PROVIDER
endpointProjectState = ABSENT
endpointOwnershipState = NOT_APPLICABLE_INTERNAL_ONLY
moduleOwnedRouteCount = 0
hostOwnedRouteCount = 0
endpointReachableRequests = 0
cqrsState = NOT_APPLICABLE_INTERNAL_ONLY
validatorCoverageState = NOT_APPLICABLE_INTERNAL_ONLY
```

## Why Pricing is INTERNAL_ONLY

Pricing owns authored-price truth as a **capability provider**: other modules reach it only through the
`Tooba.Pricing.Contracts` ports. The only user-facing price write in the product is Offer-owned HTTP
(`POST|PUT /v1/seller/offers/{offerId}/price` → `SetOfferPriceCommand` → `ISellerOfferPricingGateway`), and
the Offer handler calls `pricing.SetPriceAsync(...)` inside the handler — never endpoint→directory.

Because Pricing owns zero endpoint-reachable requests, an `Endpoints` project, a route group, a
`PricingEndpointModule`, a CQRS command/query tree and a FluentValidation matrix would all be pure
ceremony: no route would ever reach them. This is exactly the certified Inventory INTERNAL_ONLY
precedent, and it is why the ceremonial project was retired by W3-R2 rather than kept.

## Proof of absence (machine-checked)

| Probe | Scope | Hits |
| --- | --- | --- |
| `Tooba.Pricing.Endpoints` directory | `Modules/Pricing/` | **absent** |
| `Modules/Pricing/Tooba.Pricing.Tests/Endpoints` | test project | **absent** |
| `Tooba.Pricing.Endpoints` | all Pricing production `.cs` | **0** |
| `MapPricingModule` | all Pricing production `.cs` | **0** |
| `AddPricingEndpointPresentation` | all Pricing production `.cs` | **0** |
| `PricingEndpointModule` | all Pricing production `.cs` | **0** |
| `IEndpointRouteBuilder` | all Pricing production `.cs` | **0** |
| `MapGroup(` / `MapGet(` / `MapPost(` / `MapPut(` / `MapPatch(` / `MapDelete(` | all Pricing production `.cs` | **0 / 0 / 0 / 0 / 0 / 0** |
| `"/v1/pricing"` | all Pricing production `.cs` | **0** |
| `ISender` / `MediatR` | all Pricing production `.cs` | **0 / 0** |
| `MapPricingModule` / `AddPricingEndpointPresentation` / `Tooba.Pricing.Endpoints` / `"/v1/pricing"` | Host `Program.cs` | **0 / 0 / 0 / 0** |
| `Tooba.Pricing.Endpoints` | Host `Tooba.Host.csproj` | **0** |
| `Tooba.Pricing.Infrastructure.csproj` | Host `Tooba.Host.csproj` | **1** (composition root preserved) |
| `/Modules/Pricing/` project entries | `src/backend/Tooba.slnx` | **5** (`Application`, `Contracts`, `Domain`, `Infrastructure`, `Tests`) |

Source: `certify-audit.cjs` → `audit-after.json` (`structure`, `httpApplicability` sections).

## Why the CQRS/validator matrix is `NOT_APPLICABLE_INTERNAL_ONLY`

The exhaustive endpoint-reachable request inventory is empty:

```text
endpointReachableRequests = 0
VALIDATOR_REQUIRED = 0
NO_VALIDATOR_REQUIRED = 0
```

No request exists, so no request can be unclassified and no durable validator-coverage guard is
applicable. This is not an omission: `Application/` deliberately holds only `Composition/` (the
typed-fault seam) and `Ports/` (the module-internal use-case guard), and both the W2 structure guard and
the new W3-R3 cert guard assert that `Commands`/`Queries`/`Validators`/`Models`/`Handlers`/`Requests`
must **not** exist.

Boundary input validation that Pricing genuinely owns stays where it belongs: inside the directory
(`MarketCode.TryParse` / `CurrencyCode.TryParse` → `Result.Failure`), i.e. business/domain validation,
correctly not transport FluentValidation.

## Presentation registration without an Endpoints project

The one presentation concern Pricing owns — its error catalog contributor and error resource set — is
registered exactly once by the module composition root:

```csharp
// Tooba.Pricing.Infrastructure/DependencyInjection/PricingModule.cs (AddServices)
services.AddSingleton<IErrorCatalogContributor, PricingErrorCatalogContributor>();
services.AddSingleton<IErrorResourceSet, PricingErrorResourceSet>();
```

Both concrete types stay Contracts-owned (`Tooba.Pricing.Contracts.Errors`), so the module keeps its own
user-facing text when extracted as a microservice. Machine-checked registration counts: contributor **1**,
resource set **1**.

## Conclusion

`INTERNAL_ONLY` is the correct, honest applicability for Pricing. There is no ceremonial project, no
route group, no dead CQRS tree and no orphan presentation extension, and nothing endpoint-reachable is
left unclassified.
