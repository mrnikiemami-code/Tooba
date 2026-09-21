# Domain Result Strategy — Offer (TB-TMAR-FND-RESULT-001-R1)

## Chosen strategy: **A — Domain returns Result for expected invariant failures**

Applied to Offer Domain aggregate methods that previously threw `SemanticException` for expected seller input / lifecycle rules:

- `SellerOffer.SetOrderQuantityLimits` → `Result`
- `SellerOffer.Activate` → `Result`

Mutating methods that cannot fail for expected business reasons remain void (`Suspend`, `Archive`, `SetReturnPolicy` after Application validation, `UpdateSellerSku`).

## Application / policy

- Application handlers return `Result` / `Result<T>` and propagate Domain/policy failures without `try/catch SemanticException`.
- `IReturnPolicyResolver.ValidateOfferChoice` returns `Result` (no exception control flow).
- `ResolveForCheckout` uses `ValidateOfferChoice(...).IsFailure` fallback instead of catch.

## Why not B/C alone

- B (validate-then-mutate) would duplicate quantity/activate rules between Application and Domain.
- C (Application-only policy) would weaken Domain invariants for non-HTTP call sites (seeds, infrastructure).

Strategy A keeps a single rule site in Domain and avoids SemanticException as normal Offer Golden HTTP control flow.

## Residual SemanticException

Offer Application no longer throws `SemanticException` for expected seller paths. Unexpected infrastructure failures still throw. FluentValidation remains exception-based temporarily (documented in current-result-gap / RESULT).
