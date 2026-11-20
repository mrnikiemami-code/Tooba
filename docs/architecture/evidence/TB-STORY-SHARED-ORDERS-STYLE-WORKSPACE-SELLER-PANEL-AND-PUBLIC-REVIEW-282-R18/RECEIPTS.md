# RECEIPTS — TB-282-R18

## Claim / base

```
GET  /api/tasks/next                     → row d3fc091e-0cf8-4f43-b960-f2c022426655 (Claimed)
bridgeRowId                              = d3fc091e-0cf8-4f43-b960-f2c022426655
expectedBaseSha                          = 1d02e87d49049e96f4d175284b96165b4c3b4777
claimHeadSha (docs-only issuance)        = 583ce6e6cc34924320cf1f79c90e12f838d14e45
git merge-base --is-ancestor <base> HEAD → 0 (ancestor)
```

## Approval diagnosis (read-only)

```
GET /v1/admin/stories/01a04246-b309-7000-b1de-681552e70532
  X-Tooba-Dev-Actor-User-Id: 01a036c2-970e-7000-8eb7-94bf5cc2d8db
  → reviewStatus=Submitted contentReady=False origin=Seller status=Draft version=3
  → actionCodes=update,add_item,delete,reject            (after ProjectActionCodes fix)
  → items=1, mediaAssetId empty, mediaUrl=/images/stories/2.jpg

docker exec postgres-db psql -U admin -d tooba_alpha
  story.stories  → 01a04246-… | review_status 1 | status 0 | version_token 3 | updated_at 2026-08-27 06:00:00+00 | reviewed_by NULL
  story.story_items → 1 row, media_asset_id IS NULL = 1
  story.command_occurrences for that story → 0
```

Actor assignments used (read-only, no grant change):

```
01a036c2-970e-7000-8eb7-94bf5cc2d8db  platform-admin  scope 1
01a03628-3f68-7000-844d-99f1cadb54b0  seller-owner    scope 2 / 01a030d1-40cb-7000-8abe-6d31739956c5
01a04407-47ae-7000-a6b1-43f69a17cd1a  mobile-order-op scope 2 / 01a030d1-…
```

## Backend

```
dotnet test src/backend/Host/Tooba.Host.Tests -c Release --filter "FullyQualifiedName~StoryApprovalReadiness282R18Tests"
  → Passed 2/2, Failed 0 (482 ms)

dotnet test … --filter "FullyQualifiedName~Story"  (context only)
  → Failed 12, Passed 156, Total 168
  → all 12 failures are in files untouched by R18:
     StoryMarketplace281R1DirectoryTests.Fresh_materialized_active_and_scheduled_stories_are_public_with_items
       (story.occurrence.required — stale test missing the occurrence key)
     StoryFoundationTests.Public_visibility_status_cta_reorder_locale_and_admin_auth_behave
       (StoryDevelopmentSeed SubmitForReview → story.content.notReady on a URL-only seed)
     StoryFoundationTests.Seller_review_workflow_public_eligibility_and_authorization
       (BindSellerMediaAsync → story.media.unavailable in the legacy fixture)
     StoryModuleAmsc001W3R2SetEqualityGuardTests ×3, StoryModuleAmcW5ValidatorGuardTests,
     StoryModuleAmcW6CertGuardTests (pre-existing `Tooba.Catalog.` coupling / route-map drift)
     ProductHistoryTests.Product_workspace_history_endpoint_is_authorized_and_read_only,
     ContentArticlePublicationWorkflowTests… (matched by the "history" substring, different module)
```

## Frontend

```
npx tsc --noEmit                                            → 0 errors
npx tsx --tsconfig tsconfig.mount-tests.json --test \
  story-management-282r18.mount.test.ts story-management-282r17.mount.test.ts
  → tests 6, pass 6, fail 0
     ✔ R17 all-ineligible approve → one toast, no dialog, no POST, selection kept
     ✔ R17 mixed reject → compact counts, typing stable, one toast, no result dialog
     ✔ R17 helpers bounded 1000-row plan
     ✔ R18 toast expires at 3000ms + no-pause overrides, error severity
     ✔ R18 definite refusal names the Persian reason, error, clears stored command
     ✔ R18 helper severity success/warning/error never blue info

node --experimental-strip-types --test app/stories/story-api.test.ts \
  app/stories/management/story-capabilities.test.ts story-intent.test.ts \
  story-workspace-282.guard.test.ts app/storefront/stories/story-modal.mount.test.ts
  → tests 15, pass 15, fail 0
```

## Served controlled browser (read-only; no Story/Media command)

```
Host  5088 pid 23016  dll SHA256 CF23A240C168702BFD46DF9E8372F314D45C21EF9CD50E559E41A9B8ACB79925
      /health 200 · /health/ready 200 · /__platform-commerce store-alpha
      log: AccessControl InMemory projection recovery eligible=True users=3 tenantMembers=1 partyMembers=2
Next  3000 next dev (started 22:49)  /fa/admin/stories 200

/fa/admin/stories  (admin dev actor 01a036c2-…, 6 rows)
  select non-ready Submitted row → bulk «تأیید»
  toast: count=1 class="Toastify__toast--error … close-on-click"
         text="رسانهٔ آماده ندارد؛ اسلاید باید تصویر یا ویدئوی بارگذاری‌شده داشته باشد."
  error=1 info=0 warning=0 success=0 dialog=0 result=0
  lifecycle samples: n=1 at 465…4149 ms, n=0 at 4450 ms (no manual close, no hover/focus)
  overflowX: 1440 → 0 ; 390 → 0 ; permanent recovery banner: 0

/fa/vendor-panel/stories (seller 01a03628-…, VendorShell)
  5 rows, no denial, overflowX 0 at 390 and 1440, banner 0
```

## Source touched

```
src/backend/Modules/Story/Tooba.Story.Domain/Policies/StoryPublicationPolicy.cs   (+5/−1)
src/frontend/app/stories/management/story-toast.ts                                (new)
src/frontend/app/stories/management/story-admin-ops.ts                            (+112/−10)
src/frontend/app/stories/management/story-executor.ts                             (+17/−2)
src/frontend/app/stories/management/story-management-copy.ts                      (+31/−6)
src/frontend/app/stories/management/StoryManagementScreen.tsx                     (toast helper + <details> recovery + localized literals)
src/frontend/app/stories/management/story-workspace-282.guard.test.ts             (guards localized copy + storyToast)
src/frontend/app/stories/management/story-management-282r18.mount.test.ts         (new)
src/backend/Host/Tooba.Host.Tests/StoryApprovalReadiness282R18Tests.cs            (new)
```

No migration, no schema, no grant, no config/appsettings, no package, no VendorShell/menu/login change.
