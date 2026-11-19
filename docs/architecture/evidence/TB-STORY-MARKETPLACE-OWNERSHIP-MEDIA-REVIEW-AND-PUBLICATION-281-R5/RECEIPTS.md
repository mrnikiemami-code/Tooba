# TB-281-R5 RECEIPTS

## Claim

- Bridge GET `/api/tasks/next?channelId=tooba-main` claimed row `29d18780-b564-490c-a85a-f18337b22f2b`
- Worker `tooba-worker-01`; Expected-Base `aad32713b7486c429dbbfb5e948e95e30eb6bb67`
- Claim HEAD `d78ecf39e165c7d10ede61e5de6bacf6856122bb` (docs-only issuance ancestor of Expected-Base)

## Focused tests

```
dotnet test Host/Tooba.Host.Tests --filter "FullyQualifiedName~StoryMarketplace281R5|FullyQualifiedName~MediaStorySellerUpload281R5|FullyQualifiedName~StoryModuleAmsc001"
Passed!  - Failed: 0, Passed: 48, Skipped: 0, Total: 48
```

Breakdown: Replay 6 + Relational 3 + Media R5 3 + Story AMSC 36 = 48.

### Negative control

- Temporarily disabled blank `RequireOccurrenceKey` refusal in `StoryDirectory.Ownership.cs`
- `A1_seller_and_item_updates_require_occurrence_replay_and_conflict` → FAIL (`Assert.Throws` type mismatch)
- Restored implementation → focused suite PASS

## Schema

- ZERO migration/DDL this task
- R3 `20261119020000_Tb281R3TenantScopedCommandOccurrences` left immutable

## Host refresh

- Publish: `artifacts/publish-primary`
- Env: `TOOBA_SKIP_DEVELOPMENT_BOOTSTRAP=1`, `ASPNETCORE_URLS=http://127.0.0.1:5088`
- PID `18716`, dll SHA256 `917D16EDAB76B1D08B9DF06D27F2E96B6EFA5ECB96B9B1D2B6BC894D594E85B3`
- `/health` 200; `/health/ready` 200 SingleStore + messaging Healthy; `/__platform-commerce` → store-alpha
- ZERO live Story/Media business POST
