# Recovery SoT — TB-TMAR-FND-OBSERR-001-R4

Claim: `d71cad90-9bd7-4b15-8190-614dddcfccf7`

Recovery-Start: `main` at `1623b33886f22aa42c1622766e67cae6ce194321`, equal to `origin/main`. Protected ancestor `18ca10c9` is an ancestor. Stashes were not touched. No `.rar` was staged or deleted.

Architect-Verified-R3-State: FOUNDATION_ERROR_LOCALIZATION_COMPLETE. R4 re-verified that state and repaired two defects before accepting it:

1. `OfferDevelopmentSeedGateway` called `DateTimeOffset.UtcNow`. It now uses `IClock`. A guard forbids direct clock/id bypass in Offer production.
2. Parallel Host `WebApplicationFactory` starts collided in MassTransit `PostgresDatabaseMigrator.GrantAccess` (`tuple concurrently updated`). `ErrorContractTests` and `CorrelationRuntimeTests` now use collection `PostgresSerial`.

Module-Recovery-State: FOUNDATION_COMPLETE

Offer-State: COMPLETE_REFERENCE_PATTERN

The central Observability / Error / Localization foundation is the canonical pipeline. Offer is the first Golden consumer of that pipeline (catalog contributor, `.resx`, thin seller endpoints, module-call tracing).

Next-Recommended-Task: USER_REVIEW_OFFER

Pricing stays gated until that review. Checkout stays PAUSED_AT_SAFE_W5_CHECKPOINT.

Frontend-Production-Changes: NONE

Parent ships git. This worker did not commit, push, or post to Bridge.

Git: TIP_SHA_PLACEHOLDER
