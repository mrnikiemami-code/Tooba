# TB-P10-T022-R16 — Focused Validation

```
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj \
  --filter FullyQualifiedName~MerchandisingCampaign \
  -o .tmp-r16-test-out
```

## Result

```
Passed MerchandisingCampaignRuntimeTests.Merchandising_campaign_runtime_selection_availability_locale_price_and_seed
Passed MerchandisingCampaignFoundationTests.Merchandising_campaign_foundation_covers_required_cases
Total: 2  Passed: 2  Failed: 0
```

Also: recovery-staleness markers present; `18ca10c9` ancestor; locks through LOCK-SF-404.
