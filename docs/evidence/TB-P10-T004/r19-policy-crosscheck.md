# R19 policy cross-check

Resolver + `ReservationLifecycleIntegrationGateTests.Precedence_offer_category_store_platform_and_settings_future_only`:

| Layer | Example | Effective |
| --- | --- | --- |
| Platform | 120 / 90 / 6 | unused when Store set |
| Store | 10 / 5 / 3 | wins empty-line / inherit |
| Category | 8 / 4 / 2 | wins over Store when no Offer field |
| Offer Initial=3 inherit retry/max | 3 / 4 / 2 | Offer field wins; others inherit Category |

Multi-line: min Initial, min Retry, min Max.

Cycle snapshot stores resolved policy. Settings PUT after start leaves active `ExpiresAt` unchanged; next resolve uses new Store values.

Commit-time hold TTL now uses variant→category lookup so Category overrides apply to inventory hold as well as Cycle #1.

Frontend does not compute Offer > Category > Store > Platform.
