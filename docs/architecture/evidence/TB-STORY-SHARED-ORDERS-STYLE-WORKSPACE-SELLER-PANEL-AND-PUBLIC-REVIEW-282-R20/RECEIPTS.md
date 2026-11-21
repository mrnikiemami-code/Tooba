# RECEIPTS — TB-282-R20

Every number below was produced in this delivery on this Windows host. Nothing is inherited from R19.

## 1. Repository / provenance

```
git rev-parse HEAD                  -> d6126b6afbc1ce727823bd121b844658c8fc2be2
git merge-base --is-ancestor 9c43563891c77a64a89f99f4bec248bad1fdc9e2 HEAD  -> exit 0
git rev-parse --abbrev-ref HEAD     -> main
TASK.md (tasks)    SHA256 0C496ABF09472EAD082A26AAFDF0A0CE22B61B611C307230AE9619C0517E8014  (15 753 B)
TASK.md (evidence) SHA256 0C496ABF09472EAD082A26AAFDF0A0CE22B61B611C307230AE9619C0517E8014  (15 753 B, byte-identical)
ISSUANCE-RECEIPT.json taskSHA256    2b8b822a7a8cbe754083caab50e67fe5ecdffc30d947a1f1d2c15b5bb438e180  (delivery-form hash, not the repo bytes)
```

The expected base is an ancestor of the claim HEAD, so no source drift arrived with the issuance.

## 2. Backend

```
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj -c Release
  -> Build succeeded. 0 Error(s), 2 Warning(s) (NU1900 offline nuget feed only)
  -> src/backend/Host/Tooba.Host/bin/Release/net8.0/Tooba.Host.dll
     SHA256 96F060C4EFBCA12277E35E16ACE0D2B849966E272363EB340A3AF23F006427CC

dotnet test ... --filter FullyQualifiedName~Story282R20
  -> Passed! Failed: 0, Passed: 12, Skipped: 0, Total: 12

dotnet test ... --filter FullyQualifiedName~StoryWorkflow282R19Tests
  -> Passed! Failed: 0, Passed: 3, Total: 3

dotnet test ... --filter "StoryFoundationTests|StoryModuleAmsc001W3R2SetEqualityGuardTests|StoryMarketplace281R1DirectoryTests|Story282R10PublicationAndDeleteTests|Story282R11DeleteDirectoryTests"
  -> Passed! Failed: 0, Passed: 25, Total: 25
```

## 3. Frontend tests

```
npx tsx --tsconfig tsconfig.mount-tests.json --test app/stories/management/story-management-282r20.mount.test.ts
  ✔ R20 C12: lost replace-media UPDATE retries ONLY the frozen update, never a second upload
  ✔ R20 C12: lost upload keeps the exact journal and resumes that same upload once
  ✔ R20 C12: replacement bind refuses a missing slide identity and cross-role routes
  ✔ R20 A4: only an owning story.* 4xx is an authoritative no-effect refusal
  ✔ R20 A3: version regression and state-free approve are refused; real Approved is accepted
  ✔ R20 E: a 1000-row controlled plan renders compact confirmation and one completion toast with zero extra writes on cancel
  -> tests 6, pass 6, fail 0

npx tsx --tsconfig tsconfig.mount-tests.json --test app/storefront/stories/story-modal-282r20.mount.test.ts
  ✔ R20 D15: a photo dwell starts only after the image actually loads
  ✔ R20 D15: a video slide waits for the real ended event, not for the photo duration
  ✔ R20 D15: a hidden document pauses the photo dwell and resumes when visible again
  -> tests 3, pass 3, fail 0

npx tsx ... --test story-management-282r19 / 282r17 / 282r1 (+ the two R20 files above)
  -> tests 17, pass 17, fail 0

node --experimental-strip-types --test app/stories/story-api.test.ts app/stories/management/story-capabilities.test.ts app/stories/management/story-intent.test.ts app/stories/management/story-workspace-282.guard.test.ts
  -> tests 13, pass 13, fail 0

npm run test:stories
  -> ✔ shop picker loads 50 at a time and server-searches from page 1
  -> tests 6, pass 6, fail 0 (exit 0 across the whole chained script)

npx tsc --noEmit
  -> exit 0
```

## 4. Production build

```
cd src/frontend && npm run build
  ✓ Compiled successfully in 10.7s
  ├ ƒ /admin/stories                     248 B   1.03 MB
  ├ ƒ /vendor-panel/stories            3.78 kB    879 kB
  ...
  exit 0
  .next/BUILD_ID -> qrCkTUiLY-AzikCO6XrYO
```

The R19 `Html404` prerender blocker did **not** reproduce: the production build completed with 0 errors and a
fresh BUILD_ID, so this handoff is not release-BLOCKED.

## 5. Served artifacts

```
Host  5088 PID 25312  DLL SHA256 96F060C4EFBCA12277E35E16ACE0D2B849966E272363EB340A3AF23F006427CC
/health             200  {"status":"ok"}
/health/ready       200  {"status":"ready","checks":{"edition":"SingleStore","postgresql":"configured","authorization":"inmemory","messaging-transport":"postgresql-sql","messaging":"Healthy"}}
/__platform-commerce 200 {"edition":"SingleStore","deploymentId":"dev-singlestore","tenantId":"store-alpha",...}

FE    3000 PID 46136  next start (production)  BUILD_ID qrCkTUiLY-AzikCO6XrYO

GET /fa/admin/stories              200
GET /fa/vendor-panel/stories       200
GET /fa                            200
GET /en/admin/stories              200
GET /fa/admin/stories/new          200

/_next/static/chunks/9758-2b1c47fcd351ec72.js
  contains: replace_media, return-to-draft, /disable, "تأییدشده، منتشر نشده"
/_next/static/chunks/2285-f2e6a631560f7621.js
  contains: aria-modal, dwell (public player)
```

## 6. Served browser observations (read-only)

```
@1440  /fa/admin/stories   rows 6, scrollWidth 1425, clientWidth 1425, overflowX 0, alerts 0
       review filter -> Approved: "نمایش ۱ تا ۳ از ۳ ردیف" (3 of 6); reset -> 6
@390   /fa/admin/stories   rows 6, scrollWidth 390, clientWidth 390, overflowX 0, docOverflow 0
@1440  /fa/vendor-panel/stories rows 2 (own shop), 1425 = 1425, overflowX 0, no 403 text
@390   /fa/vendor-panel/stories rows 2, 390 = 390, overflowX 0
@/fa   public player: role=dialog aria-modal=true aria-label="تست",
       img complete naturalWidth=1745 src=/v1/storefront/media/01a1fa64-… ,
       caption "تست", attribution "دیجی‌استایل نمونه", progress "۲ / ۵",
       focusInside=true, body overflow=hidden
```

Live authorization probes (`X-Tooba-Dev-Actor-User-Id`; seller also `X-Tooba-Seller-Party-Id`):

```
GET  /v1/admin/stories                 admin actor     -> 200
GET  /v1/seller/stories                seller+party    -> 200
GET  /v1/seller/stories                admin actor     -> 403 seller.authorization.denied
GET  /v1/seller/stories                foreign party   -> 403 seller.authorization.denied
GET  /v1/admin/stories                 seller actor    -> 403 admin.authorization.denied
POST /v1/seller/stories/{id}/approve   seller+party    -> 404 (no moderation route on the seller surface)
POST /v1/admin/stories/query           admin actor     -> 200, 6 rows
GET  /v1/storefront/stories            anonymous       -> 200 with resolved ctaHref
GET  /v1/admin/dev-context             admin actor     -> 200
```

## 7. Base-vs-working guard comparison (how "pre-existing" was established)

A detached worktree at the pristine base (`git worktree add --detach D:\temp\r20-head HEAD`, removed after use)
was used to run the architecture guards against unmodified base bytes:

```
FE-SIZE at base        28 violations, incl.
  NEW_OVERSIZED_FILE src/frontend/app/stories/management/StoryManagementScreen.tsx: 3158 > 800
  NEW_OVERSIZED_FILE src/frontend/app/stories/story-api.ts: 1317 > 800
FE-SIZE in working tree 28 violations, same two lines with 3294 / 1319
FE-BOUNDARY (feature deep imports) at base   1 violation:
  src/frontend/app/stories/management/StoryManagementScreen.tsx -> ../../../features/admin-sellers/api/sellers-api
FE-BOUNDARY in working tree                  1 test, pass 1  (import moved to the public barrel)
FE-FOLDER-002 at base  9 exports;  FE-BOUNDARY-001 at base 23 novel shared→feature imports  (unchanged by R20)
```

## 8. Data protection receipt

```
migrations added/applied ............ 0
DDL / seed / backfill / reset ....... 0
durable grants / keys / provider .... 0
money / stock / notification ops .... 0
live Story/Media business writes .... 0  (all live calls were GET or POST /query)
rows deleted / restored ............. 0  (the R19 deletion is recorded as REPORTED historical mutation)
canonical DB ........................ tooba_alpha@127.0.0.1:5432 / SingleStore store-alpha (unchanged)
R6 bootstrap effects ................ UNKNOWN (preserved)
```

Final implementation SHA ........... 063f86f1823627a82c53bab7431680dda331cb57  (40 files, source + evidence)
Final pointer commit ................ this docs-only commit (records the SHA above)
Origin parity ....................... origin/main == final pointer commit, working tree clean

