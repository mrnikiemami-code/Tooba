# RESULT — TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R20

Task-ID: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R20
Parent: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R19
Channel: tooba-main · Worker: tooba-worker-01 · Mode: CONSOLIDATED_IMPLEMENT_VERIFY_AND_OWNER_HANDOFF
Expected-Base: `9c43563891c77a64a89f99f4bec248bad1fdc9e2` (verified ancestor of the claim HEAD)
Claim/Implementation base HEAD: `d6126b6afbc1ce727823bd121b844658c8fc2be2` (docs-only R20 issuance)
Status: **PASS_COMPLETE_R20**
Stop: **STOP_FOR_ARCHITECT_AND_USER_REVIEW** · `automaticNextImplementationTask: NONE`

Decision:
`CONSOLIDATED_R20_A_TO_F_COMPLETE_OWNING_STOP_AND_SELLER_MEDIA_AND_RETURN_TO_DRAFT_BOUND_STATE_EFFECT_SUCCESS_VERIFICATION_AUTHORITATIVE_REFUSAL_ONLY_SERVER_CAPABILITY_ENFORCEMENT_DB_NATIVE_SHOP_AND_NULLABLE_DATE_GRID_FILTERS_ONE_LOCALE_PRESERVING_WORKSPACE_STAGED_MEDIA_REPLACEMENT_JOURNAL_NATIVE_TOAST_WITH_USER_INVOKED_RECOVERY_SLIDE_LEVEL_PUBLIC_PLAYER_WITH_LOADED_DWELL_VIDEO_ENDED_HIDDEN_PAUSE_AND_BATCHED_CTA_WINDOW_REVALIDATION_NEXT_PRODUCTION_BUILD_PASSES_ZERO_MIGRATION_ZERO_LIVE_WRITE`

This is a **worker PASS**, **not Architect ACCEPT** and **not owner acceptance**.

## What changed

- Route binding is role-exact: admin stop → `POST /v1/admin/stories/{id}/disable`, seller stop →
  `POST /v1/seller/stories/{id}/stop`; cross-role envelopes are refused before dispatch.
- Approved-but-not-published now reads «تأییدشده، منتشر نشده» inside the shared status authority;
  Approved+Draft offers and executes `POST /v1/seller/stories/{id}/return-to-draft`.
- Success is verified against the owning response (`effectSatisfied`: Disabled/Approved/…, version regression
  refused) instead of a matching `storyId`.
- Only an owning `story.*` 4xx retires an intent. HTML/proxy 404, 408/425/429, ambiguous 5xx and malformed
  2xx stay uncertain and exactly replayable from the frozen envelope (method/path/body bytes/occurrence key).
- Server authorization is the registered Story capability (`story.view|create|edit|submit|approve|reject|publish`
  from `PermissionCatalog`), DENY and Unavailable fail closed, cross-shop and seller-moderation refused,
  no demo-shop fallback.
- Admin grid is DB-native: `shopDisplayName`/`shopName` (Party projection over the shops actually present),
  `updatedAt` sort, `startAt`/`endAt` through `EF.Property<DateTimeOffset?>` nullable-date translation,
  `items` through a grouped join; server-side counts/pages; seller grid mirrors it.
- Slide media replacement is a staged Story-owned journal (`replace_media`) that freezes the SHA-256 file
  digest, the slide identity, the reviewed version and the exact UPDATE bytes before dispatch. A lost upload
  resumes that upload once; a lost UPDATE retries only the frozen UPDATE. Metadata-only edits upload nothing.
- Feedback is one native toast (3 000 ms, no hover/focus pause, semantic severity) and recovery is a
  user-invoked `<details>` with a count — no persistent warning card, no result Dialog, no per-title storm.
- The public player decides video-ness per SLIDE, dwells a photo only after the image painted, advances a
  video on the real `ended` event, pauses while the document is hidden, traps focus, exposes
  `role="dialog" aria-modal="true"` and locks background scroll; `PublicStoryCard.EndAt` drives a bounded
  expiry timer; the public reader constrains the window in SQL and resolves every page CTA through one
  batched Catalog composition.
- **The production Next build now passes** (the R19 `Html404` prerender blocker is gone for this handoff).

## Verification actually run

| Suite | Result |
|---|---|
| `dotnet test --filter Story282R20` | 12 / 12 |
| `dotnet test --filter StoryWorkflow282R19Tests` | 3 / 3 |
| `dotnet test --filter StoryFoundationTests|…W3R2SetEquality…|…281R1Directory…|Story282R10…|Story282R11…` | 25 / 25 |
| `story-management-282r20.mount.test.ts` | 6 / 6 |
| `story-modal-282r20.mount.test.ts` (new) | 3 / 3 |
| `story-management-282r19.mount.test.ts` | 5 / 5 |
| `story-management-282r17.mount.test.ts` | 3 / 3 |
| `story-management-282r1.mount.test.ts` | 3 / 3 |
| Story unit/guard (`story-api`, `capabilities`, `intent`, `workspace-282.guard`) | 13 / 13 |
| `npm run test:stories` | exit 0 (incl. shop pagination 6/6) |
| `npx tsc --noEmit` | 0 |
| `npm run build` | exit 0 → BUILD_ID `qrCkTUiLY-AzikCO6XrYO` |
| `dotnet build Tooba.Host.csproj -c Release` | 0 errors |
| Archive guard `FE-BOUNDARY` (feature deep import) | now 1/1 (failed at base) |

## Served artifacts (fresh)

| Artifact | Value |
|---|---|
| Host 5088 | PID 25312, DLL SHA256 `96F060C4EFBCA12277E35E16ACE0D2B849966E272363EB340A3AF23F006427CC`, `TOOBA_SKIP_DEVELOPMENT_BOOTSTRAP=1` |
| `/health` | 200 `{"status":"ok"}` |
| `/health/ready` | 200 — `SingleStore`, `postgresql configured`, `messaging Healthy` |
| `/__platform-commerce` | 200 — `store-alpha` / `dev-singlestore` |
| FE 3000 | PID 46136, `next start` (production), BUILD_ID `qrCkTUiLY-AzikCO6XrYO` |
| Served routes | `/fa/admin/stories` 200 · `/fa/vendor-panel/stories` 200 · `/fa` 200 · `/en/admin/stories` 200 · `/fa/admin/stories/new` 200 |
| Management chunk | `/_next/static/chunks/9758-2b1c47fcd351ec72.js` carries `replace_media`, `return-to-draft`, `/disable`, «تأییدشده، منتشر نشده» |
| Player chunk | `/_next/static/chunks/2285-f2e6a631560f7621.js` carries the dialog/dwell player |

## Served observations (read-only)

- Admin grid 1440: 6 rows, `scrollWidth 1425 = clientWidth 1425`, overflowX 0, 0 alerts.
- Admin grid 390: 6 rows, `390 = 390`, overflowX 0, 0 alerts.
- Review filter «تأییدشده» → server count `نمایش ۱ تا ۳ از ۳ ردیف` (3 of 6), filter reset restores 6.
- VendorShell 1440: 2 rows scoped to the seller's own shop, overflowX 0; 390: `390 = 390`, overflowX 0.
- Public player: `role=dialog`, `aria-modal=true`, `aria-label="تست"`, image `complete` with
  `naturalWidth 1745` from `/v1/storefront/media/…`, caption + shop attribution «دیجی‌استایل نمونه», progress
  «۲ / ۵», focus inside the dialog, `body.overflow: hidden`.
- Live authorization: seller list 200 (lawful actor+party); admin-as-seller 403 `seller.authorization.denied`;
  foreign party 403; seller-as-admin 403 `admin.authorization.denied`; seller `/approve` 404.

## Data protection

ZERO migration / DDL / seed / backfill / reset / restore / cleanup. ZERO durable grant, key, provider, money,
stock or notification operation. ZERO live Story/Media business write by this worker (every live call was a
read or a grid query). The earlier worker's deletion and the four CTA test rows are recorded as **REPORTED
historical mutation**; the current 6 stories were inspected read-only and nothing was deleted or restored.
R6 bootstrap effects remain **UNKNOWN**. Returns acceptances, payout backlog, user data, VendorShell and the
shared DS/grid/Media authorities are preserved.

## Honest limits

1. `startAt`/`endAt` nullable-date translation is proven through EF and is exercised live against
   PostgreSQL/SingleStore, but no dedicated PostgreSQL rollback test exists for those two filters.
2. All mounted suites are jsdom: behaviour, not geometry. Real geometry was measured only in the served browser.
3. The served tab's dwell advance could not be observed because Chrome throttles timers in an unfocused
   automation window; loaded-dwell / video-`ended` / hidden-pause are proven in the mounted player.
4. The batch CTA port is proven on the controlled/InMemory provider; no PostgreSQL query-plan measurement.
5. `StoryManagementScreen.tsx` (3 294 LOC) and `story-api.ts` (1 319 LOC) remain oversized; `FE-SIZE` fails
   identically at base (28 violations) and is a pre-existing baseline defect. `FE-FOLDER-002` (9) and
   `FE-BOUNDARY-001` (23) also fail identically at base.
6. No owner-created real record was altered; the "approved but not published" state had no live row at
   inspection time (all three Approved rows are Active), so that copy is proven on the mount and in the served
   chunk, not on a live row.
7. Architect review and owner acceptance are still pending. This file is not a product/release acceptance.
