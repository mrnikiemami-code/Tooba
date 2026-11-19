# TB-281-R2 RECEIPTS

## Claim

- Bridge row: `ef0e6cfa-a465-440d-b720-e7415bc61d2f`
- Task: `TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R2`
- Worker: `tooba-worker-01` / channel `tooba-main`
- Claim HEAD: `73202686f10ee4bceeb56fc4a2e43d0e74d8f949`
- Expected base ancestor: `ae947c863867437c13008029971a77eeb90edd41`

## TASK envelope identity

- Issuance receipt: taskBytes=14986 taskSha256=`9915ff8d37dcd8381fb4571981a5e87e511ca7246f74621224900caf144cc29b`
- Restored git blob `290ce3e8d3d0efb6f46f70ab83fd2520d0318204` working measure: 15116 B SHA256=`14F77A0522A3B5274FBD2AEDD03910E645CA377B3E689D8576006FC6C9D48D68`
- Clarification: receipt hash and working-tree hash differ; envelope content taken from issuance commit paths without reconstruction of alternate requirements.

## Focused tests

```
dotnet test Tooba.Host.Tests --filter StoryMarketplace281R2|StoryMarketplace281R1|StoryModuleAmsc001W3R2
Passed: 17 / Failed: 0
```

## Build / publish

- `dotnet build Tooba.slnx -c Release` → 0 errors
- `dotnet publish Tooba.Host -c Release -o artifacts/publish-primary` → success

## Served Host (skip bootstrap)

- Env: `TOOBA_SKIP_DEVELOPMENT_BOOTSTRAP=1`, `ASPNETCORE_URLS=http://127.0.0.1:5088`
- PID: 16856
- DLL SHA256: `9023ADA92B191A2E7E4EE0C78237E29A4C99EDB73BD66D1882E01550AE76215C`
- `/health` 200 ok
- `/health/ready` 200 SingleStore + messaging Healthy
- `/__platform-commerce` → store-alpha
- Live Story/Media business write: ZERO

## Schema

- New migrations applied this task: ZERO
