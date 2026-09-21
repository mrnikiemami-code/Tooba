# Foundation adoption scan — TB-TMAR-REFBATCH-TP-001

Scope: Tax and Pricing production sources (tests and EF migrations excluded).

| Token | Tax | Pricing |
| --- | --- | --- |
| PlatformHttpException | Absent. CANONICAL (not used). | Absent. CANONICAL (not used). |
| SemanticException | Absent. Tax failures are `TaxOutcome` or non-HTTP `InvalidOperationException` codes. LEGACY_SAFE. | CANONICAL. Domain and `PriceDirectory` throw `SemanticException` with explicit codes. |
| AcceptLanguage / StartsWith("en") / StartsWith("fa") | Absent. CANONICAL. | Absent. CANONICAL. |
| ProblemDetails / Results.Json | Absent from Tax endpoints. CANONICAL. | Absent from Pricing endpoints. CANONICAL. Seller price HTTP stays on Offer endpoints. |
| ex.Message / exception.Message | Absent from endpoints. CANONICAL. | Absent from endpoints and the module-call trace (`SetError` records exception type only). CANONICAL. |
| DateTime.UtcNow / DateTimeOffset.UtcNow | Absent in production. Directories use `IClock`. CANONICAL. | Same. CANONICAL. Optional constructor fallback constructs `SystemUtcClock` only when a caller omits DI; Host DI registers `IClock`. |
| Guid.NewGuid | Absent in production. CANONICAL. | Absent in production. CANONICAL. |
| UuidV7.New | Removed from aggregates. Ids come from `IIdGenerator`. CANONICAL. | Same. CANONICAL. |
| ActivitySource / StartActivity | Absent. No cross-module call, so no tracer. CANONICAL. | No raw `StartActivity`. Cross-module Offer lookup uses `IModuleCallTracer`. CANONICAL. |
| Direct logger scope / local correlation id | Absent. CANONICAL. | Absent. CANONICAL. |

No VIOLATION left in Tax or Pricing production for this token list.

## Error descriptors

Pricing HTTP-reachable codes (`pricing.amount.invalid` through `pricing.currency.display_unit`) have explicit descriptors, English `PricingErrors.resx`, and Persian `PricingErrors.fa.resx`. Unknown locales stay on the central fallback. Seller mismatch reuses Offer `offer.not_found`, which already has a descriptor.

Tax has no HTTP route and no user-facing semantic exception. No Tax resx was added.
