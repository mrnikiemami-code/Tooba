# RECEIPTS — TB-282-R19

All commands were executed by the single worker on this machine, in
`D:\Users\User\source\repos\SarvNewVer`. No command was delegated and no historical subagent was resumed.

## 1. Claim / base

```
Bridge GET /api/tasks  → row 80faea2c-6881-40fa-a0d8-4bd3a4439179
  taskId      TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R19
  status      Claimed
  claimedAt   2026-11-21T03:24:18.2021204Z
git log -1 --format="%H %s"
  d3de64c2c6692b551c8193e842bdd04fe99ceded feat(story): paginate shop picker, enable admin shop submit and fix modal date picker
git rev-parse origin/main
  d3de64c2c6692b551c8193e842bdd04fe99ceded
```

Claim HEAD equals the Expected-Base, so no source drift arrived with the claim.

## 2. Changed surface (11 files: 10 modified + 2 added)

```
 M src/backend/Modules/Story/Tooba.Story.Domain/Policies/StoryPublicationPolicy.cs
 M src/backend/Modules/Story/Tooba.Story.Infrastructure/Adapters/StoryProductOptionsAdapter.cs
 M src/backend/Modules/Story/Tooba.Story.Infrastructure/DependencyInjection/StoryModule.cs
 M src/backend/Modules/Story/Tooba.Story.Infrastructure/Grid/AdminStoryGridQueryEngine.cs
 M src/backend/Modules/Story/Tooba.Story.Infrastructure/Grid/StoryAdminGridPolicies.cs
 M src/frontend/app/storefront/stories/story-modal.tsx
 M src/frontend/app/stories/management/StoryManagementScreen.tsx
 M src/frontend/app/stories/management/story-executor.ts
 M src/frontend/app/stories/management/story-management-copy.ts
 M src/frontend/app/stories/management/story-response.ts
?? src/backend/Host/Tooba.Host.Tests/StoryWorkflow282R19Tests.cs
?? src/frontend/app/stories/management/story-management-282r19.mount.test.ts
```

No migration, csproj, config, seed or CI file was added or edited.

## 3. Focused backend proof

```
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj \
  --filter "FullyQualifiedName~StoryWorkflow282R19Tests" -v q --nologo

Test run for …\bin\Debug\net8.0\Tooba.Host.Tests.dll (.NETCoreApp,Version=v8.0)
Passed!  - Failed: 0, Passed: 3, Skipped: 0, Total: 3, Duration: 1 s
```

First build attempt failed to compile the new test (`CS1503` at line 112 — a positional argument landed on
`CreateStoryCommand.CoverMediaUrl` instead of `StartAt`). Fixed by passing `StartAt:` by name; the interface contract
itself was not changed.

## 4. Story regression set + baseline

```
dotnet test … --filter "FullyQualifiedName~Story"
Failed!  - Failed: 6, Passed: 168, Skipped: 0, Total: 174, Duration: 17 s

Failing:
  Architecture.StoryModuleAmcW6CertGuardTests.Story_has_zero_foreign_Application_Infrastructure_Domain_coupling
  StoryMarketplace281R1DirectoryTests.Fresh_materialized_active_and_scheduled_stories_are_public_with_items
  StoryFoundationTests.Public_visibility_status_cta_reorder_locale_and_admin_auth_behave
  StoryFoundationTests.Seller_review_workflow_public_eligibility_and_authorization
  ProductHistoryTests.Product_workspace_history_endpoint_is_authorized_and_read_only
  ContentArticlePublicationWorkflowTests.Readiness_gates_publish_and_history_records_lifecycle
```

Baseline control — the same six, at HEAD, with the R19 work stashed:

```
git stash push -u -m "r19-wip-baseline-check"   → Saved working directory and index state
dotnet test … --filter "…AmcW6CertGuardTests|…StoryMarketplace281R1DirectoryTests|…StoryFoundationTests|…ProductHistoryTests|…ContentArticlePublicationWorkflowTests"
Failed!  - Failed: 6, Passed: 10, Skipped: 0, Total: 16, Duration: 15 s
git stash pop                                   → Dropped refs/stash@{0}
```

Same six failures before and after: they are pre-existing, not R19 regressions. Ownership evidence:
`git show HEAD:…/Tooba.Story.Infrastructure.csproj | Select-String Catalog` already contained
`..\..\Catalog\Tooba.Catalog.Contracts\Tooba.Catalog.Contracts.csproj`, and
`git diff --stat -- …/Tooba.Story.Infrastructure.csproj` is empty.

## 5. Backend build

```
cd src/backend && dotnet build Tooba.slnx -c Release --nologo -v q
Build succeeded.
    234 Warning(s)
    0 Error(s)
```

Warnings are pre-existing (`NU1900` offline feed, `xUnit2013`, `xUnit2031`, `EF1002` in unrelated test files).

## 6. Frontend proof

```
cd src/frontend
npm run typecheck
> tsc --noEmit          (0 errors)

npm run test:stories
ℹ tests 22 · pass 22 · fail 0
ℹ tests 6  · pass 6  · fail 0

npx tsx --tsconfig tsconfig.mount-tests.json --test app/stories/management/story-management-282r19.mount.test.ts
✔ R19 A: admin stop binds the owning /disable route, seller keeps /stop (0.8804ms)
✔ R19 A: seller media upload binds the real /v1/seller/media endpoint (0.1562ms)
✔ R19 A: same-id unchanged Draft is not approve completion; a real Approved snapshot is (0.5743ms)
✔ R19 A: approved-but-unpublished row never claims the storefront (287.6685ms)
✔ R19 D: a photo cover of a story that also has video dwells as an image and shows its caption (7.2802ms)
ℹ tests 5 · pass 5 · fail 0
```

## 7. Served evidence (read-only)

```
Get-NetTCPConnection -LocalPort 3000 → LISTEN pid 33972   (next dev)
Get-NetTCPConnection -LocalPort 5088 → LISTEN pid 21636   (Host, untouched this pass)

GET http://127.0.0.1:3000/fa/admin/stories        → 200  len 46017
GET http://127.0.0.1:3000/fa/vendor-panel/stories → 200  len 47072
GET http://127.0.0.1:3000/fa                      → 200  len 409429
```

Emitted dev chunk `.next/static/chunks/app/admin/stories/page.js` (compiled from this worktree):

```
تأییدشده، منتشر نشده → 1
/v1/seller/media      → 3
/disable              → 11
return-to-draft       → 3
تعویض فایل این اسلاید  → 1
```

Storefront chunk `.next/static/chunks/app/page.js` contains `story-caption`.

```
rg -n "seller/media/upload-story" src
src\frontend\app\stories\management\story-management-282r19.mount.test.ts:71:  path: "/v1/seller/media/upload-story",
```

Only the negative-control assertion still mentions the removed route.

## 8. Route existence cross-check

```
tooba.Story.Endpoints/Admin/StoryAdminEndpoints.cs:37    admin.MapPost("/{id:guid}/disable", AdminDisableAsync);
tooba.Story.Endpoints/Seller/StorySellerEndpoints.cs:36  seller.MapPost("/{id:guid}/return-to-draft", SellerReturnToDraftAsync);
tooba.Story.Endpoints/Seller/StorySellerEndpoints.cs:38  seller.MapPost("/{id:guid}/stop", SellerStopAsync);
tooba.Media.Endpoints/Seller/MediaSellerEndpoints.cs:23  var group = app.MapGroup("/v1/seller/media"); group.MapPost("", UploadAsync);
```

Every path the UI now binds is a route the owning module actually maps.
