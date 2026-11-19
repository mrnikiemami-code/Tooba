# TB-281-R3 RECEIPTS

## Claim

- Bridge GET `/api/tasks/next?channelId=tooba-main` claimed row `7c9b7b7e-b280-46d7-88ec-b163e3138705`
- Subsequent next empty; TASK from repo issuance (disclosure in `claimed.json`)
- Worktree TASK.md SHA256 `7C9D2F301CBCD613C90F628001D55CC822CDED09125EFD65B6A06989FD544A51` (13666 B)

## Focused tests

```
dotnet test Host/Tooba.Host.Tests --filter "FullyQualifiedName~StoryMarketplace281|FullyQualifiedName~MediaStorySellerUpload281R3|FullyQualifiedName~StoryModuleAmsc001"
Passed!  - Failed: 0, Passed: 57, Skipped: 0, Total: 57
```

## Backup + migration

- Pre-apply: `D:\temp\tb281r3-backup\tooba_alpha-pre-tb281r3.dump`
  - bytes `3072574` SHA256 `4863DE4F08DC1F7ABC6DCBBDA763E17A8980BA0880878FCB530788A208EFE7C5`
  - `pg_restore --list` exit 0 / TOC lines 1487 (postgres:17-alpine client)
- Plan before apply: Story pendingCount=1 only (`Tb281R3TenantScopedCommandOccurrences`); other modules 0
- Apply: `Tooba.MigrationRunner apply --tenant store-alpha` once
- Post plan: Story current=`20261119020000_Tb281R3TenantScopedCommandOccurrences` pendingCount=0

## Host refresh

- Publish: `artifacts/publish-primary`
- Env: `TOOBA_SKIP_DEVELOPMENT_BOOTSTRAP=1`, `ASPNETCORE_URLS=http://127.0.0.1:5088`
- PID `9864`, dll SHA256 `172A0A9FB55EAE4B5B0C80DCB877FD18F9E238CF237BD0F22B81E07B2C2A13AC`
- `/health` 200; `/health/ready` 200 SingleStore + messaging Healthy; `/__platform-commerce` → store-alpha
- ZERO live Story/Media business POST
