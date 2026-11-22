# RESULT — TB-STORY-R22

Status: **PASS within the four requested closures**, not Architect ACCEPT and not owner acceptance.

Code SHA: `1f93e3baa` (`fix(story): reject stale locale responses and harden date proof`).

## Closed

1. **Player identity without a reopen loop**
   - `StoryModal` seeds `initialStoryId` only on open or a genuinely new external target, suppresses the callback for that seed, and reads the callback through a stable ref.
   - Going A → B updates the parent; closing B and opening A no longer bounces between the old and new id.
   - Mounted tests: open A → go B → remove A keeps B open; close B → reopen A remains A with a bounded callback count.

2. **Expiry independent of a slow response**
   - `useHomeStoriesFeed` removes locally expired stories before starting the network refresh and allows only one refresh per locale in flight.
   - Every response carries a request sequence; a stale locale response is ignored even if it resolves after the new-locale request.
   - Mounted tests: slow pending refresh removes the expired row and keeps `calls` at 2; a pending fa response cannot overwrite the en feed.

3. **Explicit Media-owned cover kind**
   - `PublicStoryCard` carries `CoverMediaAssetId` and nullable `CoverIsVideo`; `StoryDirectory` resolves the cover through `IStoryMediaAssetPort.AuthorizeForStoryAsync`.
   - Frontend uses the explicit value first and keeps its fallback only when the owner returns `null`.
   - PostgreSQL test proves the directory asks the Media owner and publishes `CoverIsVideo = true`; frontend mapping test covers explicit and missing values.

4. **PostgreSQL and browser evidence**
   - Seller grid test covers search, status/review filters, nonblank date columns, start/end `between` and `before` boundaries, `startAt blank`, explicit title/updatedAt sort order, page/count and foreign-shop exclusion; rollback leaves zero tagged rows.
   - Browser proof script is committed at `browser-proof.mjs`; its output `browser-proof.json` records the real save/reload/restore.
   - Host `5088` was republished and restarted; `/health/ready` 200 and `/v1/storefront/stories` 200.

## Verification

| Check | Result |
| --- | --- |
| `npm run test:stories` | 24 unit + 66 mount = **90 passed** |
| `npm run test:home` | **7/7** |
| `npx tsc --noEmit` | 0 |
| PostgreSQL Story tests | **2/2** (seller grid, cover kind) |
| `dotnet build Tooba.slnx -c Release` | 0 errors |
| `npm run build` | success; `BUILD_ID 44a5DfAjiw3oXj00iX5q-` |
| Host | PID `32068`; DLL SHA256 `FC7853809CD260BCCC7741408188E77C0949B927228979ED90EE1B251ACD80D8` |
| Browser | changed title read after reload; original restored and reread |

## Browser mutation disclosure

The real browser proof intentionally saved a title on existing test story
`01a1f9d8-31af-7000-8e47-5da09230ff0d`:

- original: `CTA-internal-164417`
- saved: `CTA-internal-164417-R22`
- independently reread after reload: `CTA-internal-164417-R22`
- restored: `CTA-internal-164417`
- independently reread after reload: `CTA-internal-164417`

This is a reported controlled Story title write and restore. It is not a zero-live-write claim.

## Honest limits

- No current canonical story has an asset-backed cover, so the live `/v1/storefront/stories` payload does not show `coverIsVideo`; the field is proven by the PostgreSQL Media-owner test and served code, and appears when an asset-backed cover exists.
- The Home player tests mount the real `StoryModal` with a host that owns selection/removal; they do not mount the Swiper CSS component.
- The browser proof script requires local Headless Chrome and the running admin stack; the JSON is the transcript from that run.
- `test:critical-storefront` still has the pre-existing `storefront-landing-blocks.tsx:286 bg-white` failure.
- `StoryManagementScreen` remains oversized.
