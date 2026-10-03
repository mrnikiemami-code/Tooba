# TB-TMAR-WISHLIST-AMC-001-W0 — Analyze

## Mode

`ARCHITECT_DIRECT_AMSC` — Analyze only.

## Ownership

| Surface | Owner |
|---|---|
| Customer wishlist HTTP (list/add/remove/membership) | Wishlist.Endpoints.Customer |
| CQRS commands/queries + presentation composer | Wishlist.Application |
| WishlistItem aggregate | Wishlist.Domain |
| Directory / seed / DbContext | Wishlist.Infrastructure |
| Stable error codes + IWishlistCountPort | Wishlist.Contracts |
| Host Wishlist HTTP | CLOSED_HOST_ZERO |

## Foreign coupling

- Foreign Application/Infrastructure/Domain: **ZERO**
- Cross-module Contracts only: `Catalog.Contracts` (product lookup/cards) + `Order.Contracts.Fulfillment` (actor session seam)
- Self-contained persistence schema `wishlist`

## Blockers for COMPLETE_REFERENCE_PATTERN

1. **Solution Explorer:** all 5 projects under flat `/Modules/`; need `/Modules/Wishlist/`.
2. **Structure:**
   - Domain root `WishlistItem.cs`
   - Application technical-axis `Commands/`/`Queries/` with single-file use-case leaves + root `Validators/` + `Presentation/`
   - Contracts root `IWishlistCountPort.cs`
   - Infrastructure root `WishlistDirectory.cs` + root `Migrations/` + Outbox in Module
3. **Error ownership:** Catalog + resx in Endpoints; validation codes ad-hoc in Application; move catalog/resx/codes to Contracts; register in Infrastructure.
4. **API result:** Endpoints use `Results.Json` / `Results.NoContent` without `Result` pipeline; handlers return raw DTOs — need `IRequest<Result>` + `WishlistOperation` + `ApiResponseFactory.From`/`Created`.
5. **Fault semantics:** Domain/Directory throw `InvalidOperationException` with Persian text — must become typed `SemanticException`.
6. **Validator inventory:** ListWishlistPageQuery missing classification; validation codes not in Contracts catalog.
7. **Structure-Handoff-State:** `REQUIRED`

## Wave plan

| Wave | Focus |
|---|---|
| W1 | `/Modules/Wishlist/` slnx grouping |
| W2 | Split Domain/App/Infra/Contracts physical tree (capability-first Customer) |
| W3 | Contracts catalog/resx + Result/Operation + SemanticException + thin endpoints |
| W4 | Structure + Certify + SoT/manifest |

## Microservice extractability

Blocked until Solution Explorer, structure, typed faults, Result/API mapping, and Contracts-owned catalog close. Foreign coupling already Contracts-only — strong extractability once quality gates pass.
