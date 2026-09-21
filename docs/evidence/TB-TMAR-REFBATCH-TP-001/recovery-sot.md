# Recovery SoT — TB-TMAR-REFBATCH-TP-001

- Claim: 2041d551-04cf-4314-93fe-9d326473b4f0
- Baseline HEAD: bcbb717fd167e7787aa673feb09b236b5ca8ab04
- Parent ships git and Bridge. This worker did not commit, push, or POST a Result.
- Stashes untouched.
- User `.rar` files untouched.
- Frontend production: unchanged.
- Checkout: PAUSED_AT_SAFE_W5_CHECKPOINT
- Foundation: unchanged (FOUNDATION_COMPLETE). BuildingBlocks tests not required.
- Offer contracts: unchanged. Offer.Tests not required.
- Tax-State: COMPLETE_REFERENCE_PATTERN
- Pricing-State: COMPLETE_REFERENCE_PATTERN
- Batch-State: COMPLETE
- Module-Recovery-State: REFERENCE_BATCH_COMPLETE
- Next-Recommended-Task: TB-TMAR-NEXT-MODULE-BATCH-001

Validation:
- Tooba.Tax.Tests: 12 passed, 0 failed
- Tooba.Pricing.Tests: 13 passed, 0 failed
- Host ContractsW4 + ContractsW6: 7 passed, 0 failed
- `dotnet build src/backend/Tooba.slnx`: succeeded, 0 errors

Residuals:
- Tax and Pricing still have Persian XML documentation on older types. Not an HTTP error surface.
- Tax has no module-owned HTTP use case, so no MediatR handlers and no error resx.
- Pricing seller HTTP remains on the Offer endpoint and calls `ISellerOfferPricingGateway`.
- Host tests still construct module DbContexts for fixtures.
- Other Host modules still use their own DbContexts. Out of scope.
