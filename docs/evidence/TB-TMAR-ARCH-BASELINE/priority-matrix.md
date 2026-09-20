# Priority Matrix

## Critical Now

- Host direct writes / SaveChanges / transactions
- Host business decisions (price/stock/buy-box/campaign)
- Host freeze enforcement missing

## High

- Missing CQRS/MediatR for new work
- Contracts inside Application (App→App)
- Wrong BC ownership (Catalog storefront/settings/templates)
- Read-side multi-DbContext coupling
- Localized Domain errors

## Medium

- IClock / ID generator abstractions
- Locale policy centralization
- Direct IMemoryCache bypasses
- Domain impurity leftovers

## Later

- Flat module folder layout reorganization
- Redis provider
- Full Directory elimination
