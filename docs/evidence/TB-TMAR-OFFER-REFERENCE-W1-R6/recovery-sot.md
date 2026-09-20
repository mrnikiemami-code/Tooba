# recovery-sot — TB-TMAR-OFFER-REFERENCE-W1-R6

## Outcome

Module-Recovery-State: **COMPLETE_REFERENCE_PATTERN**

Next-Recommended-Task: **USER_REVIEW_OFFER**

## Truth

Host production composers, grids, storefront, merchandising, reservation admin, and development seeds no longer type or query `OfferDbContext` / `Tooba.Offer.Infrastructure.Persistence`.

Offer-owned reads go through `IOfferQueryGateway` (+ existing `IOfferLookupGateway` methods). EF access remains only in `Tooba.Offer.Infrastructure`.

MigrationRunner uses `OfferModuleMigration` factory without naming `OfferDbContext` in Host business sources. Tests may still construct `OfferDbContext` for fixtures.

## Frontend

Unchanged (BACKEND_ONLY).

## Git

Silent worker leaves working tree ready; parent agent commits/pushes/Bridge Result.
Protected `.rar` user archives untouched.
