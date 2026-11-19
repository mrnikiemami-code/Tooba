# TB-281-R4 RECEIPTS

## Claim

- Bridge GET `/api/tasks/next?channelId=tooba-main` claimed row `7d9c8f9b-9de2-44fd-927d-45433c0dfc17`
- Worker `tooba-worker-01`; Task-SHA256 `E33961BB113282A428556E8E9B6032A764C9D078C574D41FF6EA6592016DABAC`
- Expected-Base `14b2395eea3585b7e12af71d135c84c77ce7f9c2` ancestor of claim HEAD `dbb612d6de21f9d223aa392b002339624e843392`

## Focused tests

```
dotnet test Host/Tooba.Host.Tests --filter "FullyQualifiedName~StoryMarketplace281R4|FullyQualifiedName~MediaStorySellerUpload281R4|FullyQualifiedName~StoryModuleAmsc001"
Passed!  - Failed: 0, Passed: 46, Skipped: 0, Total: 46
```

Earlier same session: R3+R4 media/story filter **20/20**; AMSC alone **36/36**.

### Negative control

- Temporarily disabled `RequireOccurrenceKey` blank refusal
- `Remove_and_reorder_require_version_and_occurrence_on_owning_path` → FAIL (Assert.Throws type mismatch)
- Restored implementation → focused suite PASS

### FE

```
npx tsc --noEmit -p tsconfig.json
exit 0
npm run build → BUILD_ID 17KblHHXwQQZBXZOOQ8u5
Next 3000 PID 49844; /fa 200
```

## Schema

- ZERO migration/DDL this task
- R3 `20261119020000_Tb281R3TenantScopedCommandOccurrences` left immutable

## Host refresh

- Publish: `artifacts/publish-primary`
- Env: `TOOBA_SKIP_DEVELOPMENT_BOOTSTRAP=1`, `ASPNETCORE_URLS=http://127.0.0.1:5088`
- PID `49032`, dll SHA256 `191F4065BAE8CD959D183E95C3616694368366C1553D2DB70FAD35938ACF7A33`
- `/health` 200; `/health/ready` 200 SingleStore + messaging Healthy; `/__platform-commerce` → store-alpha
- DELETE item routes use `[FromBody] ReviewedStoryActionBody` so minimal APIs accept occurrence/version without startup body-inference failure
- ZERO live Story/Media business POST
