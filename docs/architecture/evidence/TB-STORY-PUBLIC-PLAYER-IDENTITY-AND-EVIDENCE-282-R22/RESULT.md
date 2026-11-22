# RESULT — TB-STORY-R22

Status: **PASS within the four requested closures**, not Architect ACCEPT and not owner acceptance.

Code SHA: `8ec894cb8` (`fix(story): bind locale, slide deletion and seller multi-sort`).

## Closed

1. **Player identity without a reopen loop**
   - `StoryModal` seeds `initialStoryId` only on open or a genuinely new external target, suppresses the callback for that seed, and reads the callback through a stable ref.
   - Going A → B updates the parent; closing B and opening A no longer bounces between the old and new id.
   - Mounted tests: open A → go B → remove A keeps B open; close B → reopen A remains A with a bounded callback count.

2. **Expiry independent of a slow response**
   - `useHomeStoriesFeed` removes locally expired stories before starting the network refresh and allows only one refresh per locale in flight.
   - Every response carries a request sequence and the current locale; the new-locale request starts even while the old one is pending, and the old response is ignored even if it resolves first.
   - Mounted tests: slow pending refresh removes the expired row and keeps `calls` at 2; fa resolves while en is pending, is ignored, then en supplies the feed.

3. **Omitted items cannot prove slide deletion**
   - `AdminStorySnapshot.itemsPresent` records whether the server actually sent `items`.
   - `remove_item`, `reorder_items`, `add_item` and `update_item` refuse `{storyId, versionToken}` with no `items` as `unverified_shape` instead of treating `[]` as evidence.

4. **Explicit Media-owned cover kind**
   - `PublicStoryCard` carries `CoverMediaAssetId` and nullable `CoverIsVideo`; `StoryDirectory` resolves the cover through `IStoryMediaAssetPort.AuthorizeForStoryAsync`.
   - Frontend uses the explicit value first and keeps its fallback only when the owner returns `null`.
   - PostgreSQL test proves Story consumes the Media port response and publishes `CoverIsVideo = true`; the Media implementation in that test is a fake, so this is not a full Media-owner integration certificate. Frontend mapping test covers explicit and missing values.

5. **Seller multi-column sort**
   - `SellerQueryAsync` now applies every requested sort column in order, with stable title/display fallbacks, instead of only `Sort.FirstOrDefault()`.
   - PostgreSQL test uses title asc + displayOrder desc and asserts `2,1,0`.

6. **PostgreSQL and browser evidence**
   - Seller grid test covers search, status/review filters, nonblank date columns, start/end `between` and `before` boundaries, `startAt blank`, explicit title/updatedAt sort order, page/count and foreign-shop exclusion; rollback leaves zero tagged rows.
   - Browser proof script is committed at `browser-proof.mjs`; its output `browser-proof.json` records the real save/reload/restore.
   - Host `5088` was republished and restarted; `/health/ready` 200 and `/v1/storefront/stories` 200.

## Verification

| Check | Result |
| --- | --- |
| `npm run test:stories` | 24 unit + 66 mount = **90 passed** |
| `npm run test:home` | **7/7** |
| `npx tsc --noEmit` | 0 |
| PostgreSQL Story tests | **3/3** (seller grid, multi-sort, cover kind) |
| `dotnet build Tooba.slnx -c Release` | 0 errors |
| `npm run build` | success; `BUILD_ID xNgWpswKQKxXAseQhRyW1` |
| Host | PID `35436`; DLL SHA256 `53B49649BA896F63DDB0B668197660A55DF6E655858FD40C25CF10701030C03A` |
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
