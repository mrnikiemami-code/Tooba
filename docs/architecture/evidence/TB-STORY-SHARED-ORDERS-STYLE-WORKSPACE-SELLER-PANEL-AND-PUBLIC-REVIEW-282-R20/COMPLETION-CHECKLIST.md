# COMPLETION-CHECKLIST — TB-282-R20

Maps every R19 obligation (1–16) and every R20 repair (A–F) to the production method, the actual
test/provider that exercised it, the served observation and the outcome. `MISSING` means there is no
owning proof in this delivery; nothing below is claimed from a helper-only or regex-only substitute.

Legend: **PROVEN** = exercised on the owning method/real mount and passed. **PROVEN-LIMITED** = exercised with
the named provider limitation. **MISSING** = no owning proof.

---

## A — coherent commands, state, permissions

| # | Obligation | Production method | Actual proof | Result |
|---|---|---|---|---|
| A1 (R19 1) | Stop route binds the owning endpoint; seller keeps its own path | `bindStoryEnvelopeRoute` (`app/stories/management/story-response.ts`); admin route `POST /v1/admin/stories/{id}/disable`, seller `POST /v1/seller/stories/{id}/stop` (`StoryAdminEndpoints.cs:38`, `StorySellerEndpoints.cs:38`) | Mount: *R19 A: admin stop binds the owning /disable route, seller keeps /stop* — admin `/disable` ok, seller `/stop` ok, cross-role refused | **PROVEN** |
| A2 (R19 2) | Truthful approved-but-unpublished display; lawful seller return-to-draft; no fabricated success | `StoryStatusBadge` + `faStoryCopy.displayApprovedUnpublished` (`تأییدشده، منتشر نشده`); `StoryPublicationPolicy.ProjectActionCodes` offers `approve*` only when playable and `return_to_draft` for Approved+Draft | Mount: *R19 A: approved-but-unpublished row never claims the storefront*; backend `StoryWorkflow282R19Tests.Approved_draft_story_offers_and_executes_seller_return_to_draft` (3/3 with the grid test); copy string present in the served chunk `9758-2b1c47fcd351ec72.js` | **PROVEN** |
| A3 (R19 3) | Success verification against the real response contract, not identity alone | `verifyStorySuccessPayload` / `effectSatisfied` / `verifyStoryTarget` (`story-response.ts`); state-effect per action (`Disabled`, `Approved`, …); version regression refused | Mount: *R20 A3: version regression and state-free approve are refused; real Approved is accepted*; *R19 A: same-id unchanged Draft is not approve completion* | **PROVEN** |
| A4 (R19 4) | Only an authoritative owning refusal retires an intent; transport/proxy/ambiguous stay uncertain | `isAuthoritativeStoryRefusal(status, payload)` — requires an owning `story.*` error body on 4xx; `story-executor.ts` keeps the frozen envelope on 408/425/429/5xx/HTML/parse failures | Mount: *R20 A4: only an owning story.* 4xx is an authoritative no-effect refusal* (+ negative controls for HTML 404 and 429) | **PROVEN** |
| A5 (R19 5) | Server authorization honours the registered Story capability, not menu visibility; no demo-shop fallback | `HostStoryAdminAuthorizer` / `HostStorySellerAuthorizer` call `IAuthorizationService.CanAsync` with `StoryPermissionIds` = the seven ids already registered in `PermissionCatalog.cs`; DENY and Unavailable are fail-closed; `StoryManagementScreen` renders the choose-shop state instead of a demo shop | Backend `Story282R20PermissionTests` 5 facts (allow / deny+unavailable / seller-moderation refused / cross-shop refused / permission-set) — 12/12 in the R20 filter; live: seller list 200 for the lawful seller, 403 `seller.authorization.denied` for admin-as-seller and for a foreign party, 403 `admin.authorization.denied` for seller-as-admin, `/v1/seller/stories/{id}/approve` → 404 (seller surface owns no moderation route) | **PROVEN** |

## B — usable list, routes and input state

| # | Obligation | Production method | Actual proof | Result |
|---|---|---|---|---|
| B6 (R19 6) | Grid filters/sorts match the owning contract incl. shop name and dates | `AdminStoryGridQueryEngine` — DB-native `shopDisplayName`/`shopName` (Party projection over the shops actually present), `updatedAt` sort, `startAt`/`endAt` via `ApplyNullableDateFilter` = `EF.Property<DateTimeOffset?>` comparisons translated to SQL, `items` via a grouped join; `StoryAdminGridPolicies.Normalize` no longer strips them | `StoryWorkflow282R19Tests.Admin_grid_query_applies_nullable_start_date_filter_and_updated_sort`; live admin query 200 with the review filter → server count `نمایش ۱ تا ۳ از ۳` (3 of 6) | **PROVEN-LIMITED** — the nullable-date translation is exercised through EF; the live Host runs it against PostgreSQL/SingleStore, but no dedicated PostgreSQL rollback test exists for `startAt`/`endAt` |
| B7 (R19 7) | Refresh with the CURRENT query; keep search/sort/page/selection; no refetch on typing; scope change invalidates | `StoryManagementScreen` keeps a ref-held current query, refetches only after a real mutation, and clears selection on actor/shop change | Mount: *R17: mixed reject → compact counts, typing stable, one toast, no result dialog*; *R20 E: 1000-row plan…* (page size 1000 preserved across the plan) | **PROVEN-LIMITED** — the Persian per-character typing bar was measured in the R16/R17 mounts; the R20 mount asserts it indirectly through the same component |
| B8 (R19 8) | Locale prefix on every route; no hardcoded operator/public copy; registry locales; consistent dates | `useLocalizedPath`, `faStoryCopy`/`enStoryCopy` catalogs, `/v1/admin/stories/locale-options` from the registry, `toPersianDigits` + Tehran window copy | Live: `/fa/admin/stories`, `/fa/vendor-panel/stories`, `/fa`, `/en/admin/stories`, `/fa/admin/stories/new` all 200; the `en` keys exist in `story-management-copy.ts` | **PROVEN** — served route prefix; copy presence in the served chunk |
| B9 (R19 9) | Persistent route error/retry/back/denied; dirty-leave guard on sidebar and browser Back | `detailLoading` / `detailRetry` / `detailBack` copy + persistent detail error state; `popstate` + link-click interception with the DS leave dialog; no router monkey-patch | Mount: dirty-leave/leave-dialog suite in `story-management-282r1.mount.test.ts`; the R20 choose-shop and detail-error copy keys ship in the served chunk | **PROVEN-LIMITED** — dirty-leave is jsdom behaviour, not a real browser Back measurement |
| B10 (R19 10) | Product picker: debounce, epoch-cancel, no cross-shop reuse, identity survives remount | `queryAdminSellersGrid` via the admin-sellers public barrel; `SellerProductOptions` on the seller side; epoch keyed by search/page/shop/actor | `story-management-shop-pagination.mount.test.ts` — *shop picker loads 50 at a time and server-searches from page 1*; tsc 0 after the import was moved to the public barrel | **PROVEN** — mounted |

## C — authoring and ordinary-merchant feedback

| # | Obligation | Production method | Actual proof | Result |
|---|---|---|---|---|
| C11 (R19 11) | One shared create/edit experience; inline validation after interaction; preview matches public behaviour | Shared `StoryManagementScreenBody`/editor for admin + VendorShell; `story-management-copy.ts` field labels + inline errors; preview reuses the storefront player semantics (image dwell / video ended / caption / CTA) | Mount: create/edit flow suites; the player is the same `StoryModal` proven in the three R20 player tests | **PROVEN-LIMITED** — preview/customer agreement is structural (same component); no pixel comparison was made |
| C12 (R19 12 + R20 12) | Explicit slide media replacement via owning upload then update; staged journal freezing digest and UPDATE bytes; metadata-only edit uploads nothing; legacy repair path | `story-intent.ts` action `replace_media` with `uploadMeta.digestHex` (SHA-256 of the file) + `bodyText` frozen before dispatch; `story-executor.ts` `buildReplaceFromJournal` dispatches ONLY the frozen update after a successful upload; `BindUpdatedItemMediaAsync` preserves unchanged media | Mount: *R20 C12: lost upload keeps the exact journal and resumes that same upload once*; *R20 C12: lost replace-media UPDATE retries ONLY the frozen update, never a second upload*; *R20 C12: replacement bind refuses a missing slide identity and cross-role routes* | **PROVEN** — mounted with the real intent/executor modules |
| C13 (R19 13 + R20 13) | In-page feedback = one native toast; no persistent warning card, no result Dialog, no per-title storm; recovery behind a user-invoked count | `storyToast` on the existing react-toastify container with `autoClose 3000`, `pauseOnHover false`, `pauseOnFocusLoss false`; severity = error for definite refusal / cannot-run, warning for uncertainty, success otherwise; recovery is `<details data-testid="story-pending-recovery">` with a count summary | Mount: *R17* all-ineligible → one toast/no dialog/no POST; *R20 E* → exactly one completion toast, zero writes on cancel; source has no `pendingActs`-always-rendered card | **PROVEN** |

## D — public display

| # | Obligation | Production method | Actual proof | Result |
|---|---|---|---|---|
| D14 (R19 14) | Real caption, lawful CTA, shop attribution; cover is decided per SLIDE | `MapPublic`/`MapPublicItem` + `itemIsVideo` (`story-modal.tsx:85`) reads the SLIDE `mediaType`/URL, never `story.isVideo`; `EnrichPublicAsync` resolves shop public attribution and CTA hrefs | Mount: *R19 D: a photo cover of a story that also has video dwells as an image and shows its caption*; live: `GET /v1/storefront/stories` returns items with `ctaHref` resolved and `isVideo:false` for a JPEG cover; live browser: dialog `aria-label="تست"`, caption rendered, shop attribution «دیجی‌استایل نمونه», image loaded (`naturalWidth 1745`) from `/v1/storefront/media/…` | **PROVEN** |
| D15 (R19 15) | Photo dwell starts only after load; hidden/loading/user pause; video uses real `ended`; focus trap/return, dialog semantics, scroll lock | `story-modal.tsx`: `imageLoaded` gate, `visibilitychange` → `isHidden`, `video.addEventListener("ended")`, focusable-set Tab trap, `aria-modal`, background `overflow:hidden`, focus restore | Mount (new): *R20 D15: a photo dwell starts only after the image actually loads*; *…a video slide waits for the real ended event, not for the photo duration*; *…a hidden document pauses the photo dwell and resumes when visible again* (3/3). Live browser: `aria-modal=true`, focus inside the dialog, body scroll locked | **PROVEN** for the three timer rules + semantics. **MISSING**: a live dwell advance was not observable in the served tab because Chrome throttles timers in the unfocused automation window; the advance is proven in the mounted player only |
| D16 (R19 16) | Revalidate public eligibility at window boundaries; bounded; DB-side window/locale/market constraint; no serial CTA amplification | Bounded expiry timer in `home-stories.tsx` from the soonest `EndAt`; `PublicStoryCard.EndAt` exposed; `GetPublicStoriesAsync` filters `StartAt <= now` and `EndAt > now` in SQL before materialising slides; `IStoryProductPathBatchPort` resolves every page CTA in one Catalog composition | Backend `Story282R20PublicWindowTests` — *Public_card_carries_the_owning_window_end_and_stops_at_expiry* and *Public_reader_resolves_every_page_product_cta_in_one_batch_lookup*; live endpoint 200 with `ctaHref` populated by the batch port | **PROVEN-LIMITED** — the batch port is exercised on the InMemory/controlled provider; the live Host asserts one page's resolution, not a PostgreSQL query-plan measurement |

## E — connected verification

| Gate | What was actually run | Result |
|---|---|---|
| Owning backend facts | `Story282R20PermissionTests` + `Story282R20PublicWindowTests` (12/12); `StoryWorkflow282R19Tests` (3/3); `StoryFoundationTests` + W3R2 set-equality + `StoryMarketplace281R1Directory` + `Story282R10` + `Story282R11` (25/25) | PASS |
| Mounted workspace / grid | `story-management-282r20.mount.test.ts` 6/6; `story-management-282r19.mount.test.ts` 5/5; `story-management-282r17.mount.test.ts` 3/3; `story-management-282r1.mount.test.ts` 3/3 | PASS (17/17 combined) |
| Mounted public player | `story-modal-282r20.mount.test.ts` 3/3 (loaded dwell, video `ended`, hidden pause) | PASS |
| Unit/guard | `story-api`, `story-capabilities`, `story-intent`, `story-workspace-282.guard` 13/13; `npm run test:stories` exit 0 (incl. shop pagination 6/6) | PASS |
| Counterexamples | `/disable` vs old binder, same-id unchanged Draft, lost command + HTML 404, 408/429/ambiguous 5xx, engine epoch, Approved+Draft return, failed detail GET — all in the R20/R19 mounts above | PASS |
| 1000-row plan | `R20 E` mounts the real screen, switches page size to 1000, uses header select-all, asserts a compact confirmation, zero writes on cancel and exactly one completion toast; `.ag-row` stays bounded | PASS |
| Typecheck / build | `tsc --noEmit` 0; `npm run build` exit 0 → BUILD_ID `qrCkTUiLY-AzikCO6XrYO`; `dotnet build Tooba.Host.csproj -c Release` 0 errors / 0 warnings that are ours (only `NU1900` offline-feed warnings) | PASS |

## F — data protection, provenance, stop

| Item | State |
|---|---|
| Migration / DDL / seed / backfill / reset / restore | **ZERO** — no migration was added or applied in R20 |
| Durable grants / keys / provider / money / stock / notification | **ZERO** — no such operation was performed |
| Live Story/Media business write by this worker | **ZERO** — every live call in this delivery was a GET/POST *query* read; no create/upload/delete/approve/stop |
| Reported historical mutation | The earlier worker's deletion of old stories and the four CTA test rows are recorded as **REPORTED historical mutation**, not claimed as ZERO. Current read-only state (`POST /v1/admin/stories/query`, 2026-11-21): **6 stories** — 3 with `reviewStatus Approved` and status `Active` (2 shop-owned, 1 platform Admin), 2 platform Admin rows `Active` with `reviewStatus None`, and 1 `Draft` with `reviewStatus None` that was never published. Nothing was deleted or restored. |
| R6 bootstrap effects | **UNKNOWN** — preserved as unknown |
| Contract/port/config/edition changes | **ZERO** — `SingleStore` / `store-alpha` unchanged; `TOOBA_SKIP_DEVELOPMENT_BOOTSTRAP=1` unchanged |
| Preserved | Returns acceptances, payout backlog, user data, VendorShell/login/menu, shared DS/grid/Media authorities |

---

## Architecture notes (reported, not hidden)

1. **`StoryManagementScreen.tsx` is 3 294 lines and `story-api.ts` 1 319 lines.** The frontend size guard
   (`FE-SIZE`) flags them as `NEW_OVERSIZED_FILE`, and it did so at the pristine base too (3 158 / 1 317,
   28 violations at base and 28 now). This is a pre-existing baseline defect of the guard, not an R20
   regression; R20 grew the file by ~137 lines. Splitting it is a real refactor, not a cosmetic move, and was
   not performed inside this consolidated task.
2. **Deep feature import removed.** `StoryManagementScreen` imported
   `features/admin-sellers/api/sellers-api` directly, which `FE-BOUNDARY` refused. It now imports the
   feature's public barrel `features/admin-sellers`; the guard passes (1/1) where it failed at base.
3. **`play()` promise guard.** `video.play()` is now optional-chained because embedded/webview players may
   return no promise; without it a render crash was reachable. This is a robustness fix, not a behaviour change
   in browsers that return a promise.
4. **Pre-existing failing guards unchanged:** `FE-SIZE` (28) and `FE-FOLDER-002` (9) fail identically at the
   base commit; `FE-BOUNDARY-001` (shared→feature, 23) also fails at base and is unchanged by R20.
