# TB-P10-T022-R15 — Focused Validation

## Test class

`src/backend/Host/Tooba.Host.Tests/MerchandisingCampaignFoundationTests.cs`

Uses Testcontainers PostgreSQL (same pattern as `PromotionFoundationTests`).

## Required cases covered

| # | Case | Covered |
| --- | --- | --- |
| 1 | AMAZING seed idempotent | YES |
| 2 | PromotionType Code unique | YES |
| 3 | System type rename/delete protection | YES |
| 4 | Campaign requires valid PromotionType | YES |
| 5 | StartAt &lt; EndAt | YES |
| 6 | Draft inactive | YES |
| 7 | Published future inactive | YES |
| 8 | Published current window active | YES |
| 9 | Expired inactive | YES |
| 10 | Archived inactive | YES |
| 11 | Translation unique per language | YES |
| 12 | CampaignOffer unique membership | YES |
| 13 | Cross-store membership rejected | YES |
| 14 | Ordering stable | YES |
| 15 | Archive/remove never deletes SellerOffer | YES (membership only; Offer type unchanged) |
| 16 | Checkout PromotionDefinition regression | YES (evaluate still works) |
| 17 | AuthoredPrice regression | YES (Base-only; no PromoAmount) |
| 18 | StockPosition regression | YES |
| 19 | Migration verification | YES (tables + unique index) |
| 20 | Recovery-staleness PASS | YES (R14 evidence + R15 task present) |
| 21 | git diff --check style | YES (no trailing whitespace on new sources) |

## How to run

```powershell
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter "FullyQualifiedName~MerchandisingCampaignFoundationTests" -o .tmp-r15-test-out
```

Requires Docker for Testcontainers.

## Result (this worker)

```
Passed!  - Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: 3 s
```

Log: `docs/evidence/TB-P10-T022-R15/test-run.log`

`git diff --check` on R15 sources: clean.
