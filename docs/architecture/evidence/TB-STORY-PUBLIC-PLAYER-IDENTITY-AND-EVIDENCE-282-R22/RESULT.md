# RESULT — TB-STORY-R22

Status: **PASS within the four requested closures**, not Architect ACCEPT and not owner acceptance.

Code SHA: `d971d4895` (`fix(story): track player identity and prove revalidation`).

## Closed

1. **Player identity**
   - `StoryModal` reports `currentStoryId` through `onCurrentStoryChange`.
   - `HomeStoriesSection` passes `setSelectedStoryId` in both the Swiper and icon-shortcut layouts.
   - Going A → B now makes the parent own B; removing A no longer closes the healthy B player.

2. **Revalidation behavior**
   - `useStoryRevalidation` schedules the nearest end with a hard five-minute cap.
   - Every tick increments a cycle, so an unchanged payload still schedules the next check.
   - A failed refresh runs `filterLiveStories(previous, Date.now())`, removing expired rows without claiming the service succeeded.

3. **Explicit cover kind**
   - `PublicStoryCard` now carries `CoverMediaAssetId` and nullable `CoverIsVideo`.
   - `StoryDirectory` enriches an asset-backed cover through `IStoryMediaAssetPort.AuthorizeForStoryAsync` and uses its `IsVideo`.
   - Missing authorization or a legacy URL-only cover stays `null`; the frontend keeps its honest extension/item fallback instead of inventing a kind.

4. **PostgreSQL and browser evidence**
   - Seller grid test filters/search/sorts/counts on real PostgreSQL and excludes a foreign shop; rollback leaves zero tagged rows.
   - Browser CDP on the real admin form changed the title, saved, reloaded, read the changed value, restored the original, saved and reloaded again.

## Verification

| Check | Result |
| --- | --- |
| `npm run test:stories` | 24 unit + 63 mount = **87 passed** |
| `npm run test:home` | **7/7** |
| `npx tsc --noEmit` | 0 |
| `StoryMarketplace281R4RelationalTests.R22_seller_grid...` | **1/1** on PostgreSQL |
| `dotnet build src/backend/Tooba.slnx -c Release` | 0 errors |
| `npm run build` | success; `BUILD_ID g2-RnFpbxKuOTAPCDN4ZZ` |
| Next `3000` | 200 on `/fa` |
| Browser save/reload | changed title read after reload; original restored after reload |

## Browser mutation disclosure

The real browser proof intentionally saved a title on existing task/test story
`01a1f9d8-31af-7000-8e47-5da09230ff0d`:

- original: `CTA-internal-164417`
- saved: `CTA-internal-164417-R22`
- independently reread after reload: `CTA-internal-164417-R22`
- restored: `CTA-internal-164417`
- independently reread after reload: `CTA-internal-164417`

This is a reported controlled Story title write and restore. It is not a claim of zero live writes for this round.

## Honest limits

- The Host process was not republished/restarted in this round; the backend cover-kind field is built but served only after the next Host publish/refresh.
- The Home player behavior test mounts the real `StoryModal` with a host that owns selection/removal; it does not mount the Swiper CSS component.
- `StoryManagementScreen` remains oversized; this round does not restructure it.
- Existing `test:critical-storefront` failure at `storefront-landing-blocks.tsx:286 bg-white` is unchanged and unrelated.
